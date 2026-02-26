using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    public int ticket = 10;
    public double gold = 0;
    public int stage = 1;
    public int upgradeLevel = 1;
    public float possPower;
    private float power;
    public float Power
    {
        get { return power * (possPower + 1); }
        set { power = value; }
    }

    public UnityEvent<int> onTicketChanged = new();
    public UnityEvent<double> onGoldChanged = new();
    public UnityEvent<int> onStageChanged = new();
    public UnityEvent<double> onPowerChanged = new();
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    void Start()
    {
        IncreaseTicket();
    }
    private void IncreaseTicket()
    {
        ticket += 1;
        onTicketChanged.Invoke(ticket);

        Invoke(nameof(IncreaseTicket), 1);
    }
    public bool UseTicket(int amount)
    {
        if (ticket - amount >= 0)
        {
            ticket -= amount;
            onTicketChanged.Invoke(ticket);
            return true;
        }
        else
        {
            return false;
        }
    }
    public void IncreaseStage()
    {
        stage++;
        onStageChanged.Invoke(stage);
    }
    public void IncreaseGold(int amount)
    {
        gold += amount;
        onGoldChanged.Invoke(gold);
    }
    public void IncreaseTicket(int amount)
    {
        ticket += amount;
        onTicketChanged.Invoke(ticket);
    }
    public bool TryUseGold(double amount)
    {
        if (gold - amount >= 0)
        {
            gold -= amount;
            onGoldChanged.Invoke(gold);
            return true;
        }
        else
        {
            return false;
        }
    }
    public void UpdatePower(float power)
    {
        this.Power = power;
        onPowerChanged.Invoke(power);
    }
}
