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
    public int clearedDungeonFloor = 0; //클리어한 던전 최고 층 (0이면 1층도 아직 미클리어)
    public int normalKey = 3; //일반 열쇠
    public int eliteKey = 3;  //정예 열쇠
    public int maxNormalKey = 5; //일반 열쇠 최대 보유 개수
    public int maxEliteKey = 5;  //정예 열쇠 최대 보유 개수
    [SerializeField] float keyChargeSeconds = 1800f; //열쇠 자동 충전 주기(초) = 30분
    float normalChargeTimer;
    float eliteChargeTimer;
    public DrawLevelData drawData;

    public UnityEvent onDrawDataChanged = new();
    public UnityEvent<int> onStageChanged = new();
    public UnityEvent<double> onPowerChanged = new();
    public UnityEvent<int> onDungeonFloorChanged = new();
    public UnityEvent onKeyChanged = new();
    public PlayFabLoginManager login;
    public SheetService table;
    Coroutine syncCoroutine;
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Application.runInBackground = true;
        Instance = this;

        login.onLogined.AddListener(StartSync);
        table.OnAllTablesLoaded.AddListener(IncreaseTicket);
    }
    void Start()
    {
        Ticket.Add(999);
    }
    public void StopSync()
    {
        StopCoroutine(syncCoroutine);
    }
    public void StartSync()
    {
        //syncCoroutine = StartCoroutine(SyncCycle());
    }
    private void IncreaseTicket()
    {
        Ticket.Add(1);

        Invoke(nameof(IncreaseTicket), GameConfigLoader.Instance.GetInt("TicketTimerSec"));
    }
    public void IncreaseStage()
    {
        stage++;
        onStageChanged.Invoke(stage);

        if (QuestEventManager.Instance != null)
            QuestEventManager.Instance.SetValue("stage", stage);
    }
    //던전 층 클리어 기록(최고 층 갱신 시에만)
    public void ClearDungeonFloor(int floor)
    {
        if (floor > clearedDungeonFloor)
        {
            clearedDungeonFloor = floor;
            onDungeonFloorChanged.Invoke(clearedDungeonFloor);
        }
    }
    //열쇠 관리
    public int GetKey(KeyType type) => type == KeyType.Normal ? normalKey : eliteKey;
    public int GetMaxKey(KeyType type) => type == KeyType.Normal ? maxNormalKey : maxEliteKey;
    public bool IsKeyFull(KeyType type) => GetKey(type) >= GetMaxKey(type);
    public void AddKey(KeyType type, int amount)
    {
        if (type == KeyType.Normal) normalKey = Mathf.Min(normalKey + amount, maxNormalKey); //최대치 제한
        else eliteKey = Mathf.Min(eliteKey + amount, maxEliteKey);
        onKeyChanged.Invoke();
    }

    //열쇠 자동 충전: 종류별로 최대치 미만이면 keyChargeSeconds마다 1개 지급
    private void Update()
    {
        ChargeKey(KeyType.Normal, ref normalChargeTimer);
        ChargeKey(KeyType.Elite, ref eliteChargeTimer);
    }

    void ChargeKey(KeyType type, ref float timer)
    {
        if (IsKeyFull(type)) { timer = 0f; return; } //가득 차면 타이머 정지

        timer += Time.unscaledDeltaTime;
        if (timer >= keyChargeSeconds)
        {
            timer -= keyChargeSeconds;
            AddKey(type, 1);
        }
    }

    //다음 충전까지 남은 시간(초). 가득 찼으면 0.
    public float GetKeyChargeRemaining(KeyType type)
    {
        if (IsKeyFull(type)) return 0f;
        float timer = type == KeyType.Normal ? normalChargeTimer : eliteChargeTimer;
        return Mathf.Max(0f, keyChargeSeconds - timer);
    }
    public bool UseKey(KeyType type, int amount)
    {
        if (GetKey(type) < amount) return false;

        if (type == KeyType.Normal) normalKey -= amount;
        else eliteKey -= amount;
        onKeyChanged.Invoke();
        return true;
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
        PlayerData.Instance.Power = power;
        onPowerChanged.Invoke(power);
    }
    public void UpdatePossPower(float power)
    {
        float tmp = PlayerData.Instance.PossPower;
        PlayerData.Instance.PossPower = power;
        if(tmp != PlayerData.Instance.PossPower) onPowerChanged.Invoke(PlayerData.Instance.Power);
    }
    private IEnumerator SyncCycle()
    {
        while (true)
        {
            Gold.SyncData();

            yield return new WaitForSecondsRealtime(0.1f);
            Ticket.SyncData();
            yield return new WaitForSecondsRealtime(10);
            
            yield break;
        }
    }
}
