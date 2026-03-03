using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System;
using PlayFab;
using PlayFab.ClientModels;
using Unity.Mathematics;
using PlayFab.Json;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    public TicketData Ticket = new();
    public GoldData Gold = new();
    public int stage = 1;
    public int drawLevel = 1;
    public float possPower;
    private float power;
    public float Power
    {
        get { return power * (possPower + 1); }
        set { power = value; }
    }

    public UnityEvent<int> onStageChanged = new();
    public UnityEvent<double> onPowerChanged = new();
    public PlayFabLoginManager login;
    public TableLoaderManager table;
    Coroutine syncCoroutine;
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        login.onLogined.AddListener(StartSync);
        table.OnAllTablesLoaded.AddListener(IncreaseTicket);
    }
    public void StopSync()
    {
        StopCoroutine(syncCoroutine);
    }
    public void StartSync()
    {
        syncCoroutine = StartCoroutine(SyncCycle());
    }
    private void IncreaseTicket()
    {
        Ticket.Add(1);

        Invoke(nameof(IncreaseTicket), GameConfigLoader.Instance.GetInt("ticketTimerSec"));
    }
    public void IncreaseStage()
    {
        stage++;
        onStageChanged.Invoke(stage);
    }
    public void IncreaseGold(int amount)
    {
        Gold.Add(amount);
    }
    public void IncreaseTicket(int amount)
    {
        Ticket.Add(amount);
    }
    public void UpdatePower(float power)
    {
        this.Power = power;
        onPowerChanged.Invoke(power);
    }
    private IEnumerator SyncCycle()
    {
        while (true)
        {
            Gold.SyncData();

            yield return new WaitForSecondsRealtime(0.1f);
            Ticket.SyncData();
            yield return new WaitForSecondsRealtime(10);
        }
    }
}
