using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using System.Threading.Tasks;
public enum TicketUseType
{
    Draw = 0
}
public class TicketData
{
    private long value = 0;
    private long plusPending = 0;
    private Dictionary<TicketUseType, long> minusPending = new();//어디까지나 로컬 계산, ui용 사용은 사용처 코드에서
    public Action<long> onValueChanged;
    public int GetValue()
    {
        return (int)(value + plusPending + CalculateMinusPending());
    }
    public void Add(long amount)
    {
        plusPending += amount;
        onValueChanged?.Invoke(value + plusPending + CalculateMinusPending());
    }
    public bool Use(TicketUseType type, long amount)
    {
        if (value + plusPending + CalculateMinusPending() >= amount)
        {
            if (!minusPending.ContainsKey(type))
                minusPending[type] = 0;

            minusPending[type] -= amount;
        }
        else return false;

        onValueChanged?.Invoke(value + plusPending + CalculateMinusPending());
        return true;
    }
    public void Set(long amount)
    {
        value = amount;
        plusPending = 0;
        onValueChanged?.Invoke(value);
    }
    public long GetPendingAmount()
    {
        return plusPending;
    }
    public long GetUseAmount(TicketUseType type)
    {
        return minusPending.TryGetValue(type, out var val) ? val : 0;
    }
    public void ResetMinusPending(TicketUseType type)
    {
        minusPending[type] = 0;
        onValueChanged?.Invoke(value + plusPending + CalculateMinusPending());
    }
    private long CalculateMinusPending()
    {
        long sum = 0;
        foreach (long val in minusPending.Values.ToList())
        {
            sum += val;
        }
        return sum;
    }
    public void SyncData() //일반 대기 골드만 정산 60초마다 호출하기
    {
        //Debug.Log("Start Sync");
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "SyncPendingTicket",
            FunctionParameter = new { pendingTicket = plusPending },
            GeneratePlayStreamEvent = true
        };
        PlayFabClientAPI.ExecuteCloudScript(request,
            result =>
            {
                if (result.Error != null)
                {
                    Debug.LogError("CloudScript Error: " + result.Error.Message);
                    return;
                }

                var dict = result.FunctionResult as IDictionary<string, object>;
                if (dict != null && dict.TryGetValue("success", out var successObj))
                {
                    bool success = successObj is bool b && b;
                    if (success && dict.TryGetValue("newTicket", out var newTicketObj))
                    {
                        int newTicket = Convert.ToInt32(newTicketObj);
                        value = newTicket;
                        plusPending = 0;
                        onValueChanged?.Invoke(value + CalculateMinusPending());
                        //Debug.Log("Sync Sucess");
                    }
                    else
                    {
                        //Debug.Log(dict["message"]);
                    }
                }
            },
            error =>
            {
                Debug.LogError("PlayFab API Error: " + error.GenerateErrorReport());
            }
        );
    }
}