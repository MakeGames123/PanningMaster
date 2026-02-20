using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
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

    public int ticket = 10;
    public SpecificStat damage = new();
    public List<SpecificStat> typeDamage = new();//전체 x속성 탄환 속뎀 증가
    public SpecificStat finalDamage = new();
    public List<SpecificStat> damageByType = new();
    public List<SpecificStat> typeDamageByType = new(); //특정 속성 탄환 속성 증가
    public List<SpecificStat> finalDamageByType = new();
    [SerializeField] TextMeshProUGUI ticketText;
    void Start()
    {
        IncreaseTicket();

        for (int i = 0; i < 4; i++)
        {
            typeDamage.Add(new SpecificStat());
            damageByType.Add(new SpecificStat());
            finalDamageByType.Add(new SpecificStat());
            typeDamageByType.Add(new SpecificStat());
        }
    }
    private void IncreaseTicket()
    {
        ticket++;
        if (ticket > 10) ticket = 10;
        //ticketText.text = ticket.ToString();

        Invoke(nameof(IncreaseTicket), 1);
    }
    public bool UseTicket()
    {
        if (ticket > 0)
        {
            ticket--;
            //ticketText.text = ticket.ToString();
            return true;
        }
        else
        {
            return false;
        }
    }
}
