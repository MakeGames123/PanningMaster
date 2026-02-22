using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    public int ticket = 10;
    public double gold = 100000;
    public double power;
    public int stage = 1;
    public int upgradeLevel = 1;

    public SpecificStat damage = new();
    public SpecificStat typeDamage = new();//전체 탄환 속뎀 증가
    public SpecificStat finalDamage = new();
    public List<SpecificStat> damageByType = new();
    public List<SpecificStat> typeDamageByType = new(); //특정 속성 탄환 속성 증가
    public List<SpecificStat> finalDamageByType = new();
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
        IncreaseGold(999999);
    }
    void Start()
    {
        IncreaseTicket();

        for (int i = 0; i < 4; i++)
        {
            damageByType.Add(new SpecificStat());
            finalDamageByType.Add(new SpecificStat());
            typeDamageByType.Add(new SpecificStat());
        }
    }
    private void IncreaseTicket()
    {
        if (ticket < 10)
        {
            ticket++;
            onTicketChanged.Invoke(ticket);
        }

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
        this.power = power;
        onPowerChanged.Invoke(power);
    }
}
