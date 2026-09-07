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
    public Dictionary<string, int> climbBestStage = new(); //등반 던전 최고 기록 (Dungeon 시트 Id → 단계, 0이면 미클리어)
    public int bestTrainCarts = 0; //열차 강탈 최고 기록(한 판에 부순 화물칸 수)
    public long bestGoldMined = 0; //골드 던전 최고 기록(한 판 채굴 골드)
    public int gem = 0;       //젬(업적 등 보상)
    public int crate = 100;     //무기 상자(🧰) — 무기 뽑기 재화
    public int scroll = 100;  //모집서(🪪) — 캐릭터 모집 재화 (획득처 미구현 — crate와 동일하게 테스트용 기본값)
    public int hourglass = 100; //모래시계(⏳) — 연구 시간 단축(1개=30분). 획득처(연구 던전/퀘스트/상점) 미구현 — 테스트용 기본값
    //던전 열쇠: 던전마다 개별 종류(프로토 keyA~D 문법). 충전 = 매일 아침 9시 전량(RefillAllKeys)
    public Dictionary<DungeonKind, int> dungeonKeys = new();
    static readonly DungeonKind[] KeyKinds =
        { DungeonKind.Normal, DungeonKind.Gold, DungeonKind.Train, DungeonKind.Laboratory, DungeonKind.Armory };
    [SerializeField] int startKeys = 2;      //시작 지급(프로토 전환 지급 2)
    [SerializeField] int defaultMaxKey = 4;  //보유 캡 폴백 — 시트 로드 시 Dungeon.KeyCap 우선
    public DrawLevelData drawData;

    public UnityEvent onDrawDataChanged = new();
    public UnityEvent<int> onStageChanged = new();
    public UnityEvent<double> onPowerChanged = new();
    public UnityEvent<int> onDungeonFloorChanged = new();
    public UnityEvent onKeyChanged = new();
    public UnityEvent<int> onGemChanged = new();
    public UnityEvent<int> onCrateChanged = new();
    public UnityEvent<int> onScrollChanged = new();
    public UnityEvent<int> onHourglassChanged = new();
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

        foreach (DungeonKind kind in KeyKinds) //열쇠 초기화(종류별)
            dungeonKeys[kind] = startKeys;

        login.onLogined.AddListener(StartSync);
        table.OnAllTablesLoaded.AddListener(IncreaseTicket);
    }
    void Start()
    {
        Ticket.Add(999);
        IncreaseGold(9999999);
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
    //등반 던전(연구소·무기고) 최고 기록. 도전 단계 = 최고 기록 + 1
    public int GetClimbBest(string dungeonId)
        => climbBestStage.TryGetValue(dungeonId, out var v) ? v : 0;

    public void SetClimbBest(string dungeonId, int stageCleared)
    {
        if (stageCleared > GetClimbBest(dungeonId))
            climbBestStage[dungeonId] = stageCleared;
    }

    //열차 강탈 최고 기록(최고치 갱신 시에만)
    public void SetBestTrainCarts(int carts)
    {
        if (carts > bestTrainCarts) bestTrainCarts = carts;
    }

    //골드 던전 최고 기록(최고치 갱신 시에만)
    public void SetBestGoldMined(long gold)
    {
        if (gold > bestGoldMined) bestGoldMined = gold;
    }

    //열쇠 관리 — 던전마다 개별 종류(DungeonKind가 곧 열쇠 종류)
    public int GetKey(DungeonKind kind) => dungeonKeys.TryGetValue(kind, out var v) ? v : 0;

    //보유 캡 = Dungeon 시트 KeyCap(로드 시), 폴백 defaultMaxKey
    public int GetMaxKey(DungeonKind kind)
    {
        string id = kind.SheetId();
        DungeonData d = (id != null && DungeonLoader.Instance != null) ? DungeonLoader.Instance.Get(id) : null;
        return (d != null && d.keyCap > 0) ? d.keyCap : defaultMaxKey;
    }

    public bool IsKeyFull(DungeonKind kind) => GetKey(kind) >= GetMaxKey(kind);

    //지급은 캡까지만, 캡 초과 보유분(보상 등)은 깎지 않음(프로토 kUp 문법)
    public void AddKey(DungeonKind kind, int amount)
    {
        int cur = GetKey(kind), cap = GetMaxKey(kind);
        dungeonKeys[kind] = cur >= cap ? cur : Mathf.Min(cap, cur + amount);
        onKeyChanged.Invoke();
    }

    //일일 열쇠 충전: 모든 던전 열쇠를 캡까지 한 번에 채운다(캡 초과 보유분은 유지).
    //매일 아침 9시 발동 예정 — 시간 판정/트리거는 아직 미구현, 생기면 이 메서드를 호출하면 된다.
    public void RefillAllKeys()
    {
        foreach (DungeonKind kind in KeyKinds)
        {
            int cap = GetMaxKey(kind);
            if (GetKey(kind) < cap) dungeonKeys[kind] = cap;
        }
        onKeyChanged.Invoke();
    }

    public bool UseKey(DungeonKind kind, int amount)
    {
        if (GetKey(kind) < amount) return false;

        dungeonKeys[kind] = GetKey(kind) - amount;
        onKeyChanged.Invoke();
        return true;
    }
    public void IncreaseGold(int amount)
    {
        Gold.Add(amount);
    }
    public void IncreaseGem(int amount)
    {
        gem += amount;
        onGemChanged.Invoke(gem);
    }
    public void IncreaseHourglass(int amount)
    {
        hourglass += amount;
        onHourglassChanged.Invoke(hourglass);
    }

    public bool UseHourglass(int amount)
    {
        if (hourglass < amount) return false;
        hourglass -= amount;
        onHourglassChanged.Invoke(hourglass);
        return true;
    }

    public void IncreaseCrate(int amount)
    {
        crate += amount;
        onCrateChanged.Invoke(crate);
    }
    //무기 상자 소모. 부족하면 false
    public bool UseCrate(int amount)
    {
        if (crate < amount) return false;
        crate -= amount;
        onCrateChanged.Invoke(crate);
        return true;
    }
    public void IncreaseScroll(int amount)
    {
        scroll += amount;
        onScrollChanged.Invoke(scroll);
    }
    //모집서 소모. 부족하면 false
    public bool UseScroll(int amount)
    {
        if (scroll < amount) return false;
        scroll -= amount;
        onScrollChanged.Invoke(scroll);
        return true;
    }
    public void IncreaseTicket(int amount)
    {
        Ticket.Add(amount);
    }
    public void UpdatePower(float power)
    {
        PlayerData.Instance.Power = power;
        onPowerChanged.Invoke(power);

        //업적: 전투력 달성(Absolute — SetValue가 최고값 유지)
        if (QuestEventManager.Instance != null)
            QuestEventManager.Instance.SetValue("power", (long)PlayerData.Instance.Power);
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
