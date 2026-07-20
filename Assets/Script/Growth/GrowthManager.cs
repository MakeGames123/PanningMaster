using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 성장 탭(스탯 레벨업)의 중심. 시트(GrowthStat)로부터 데이터를 받아
// 레벨/비용/효과반영(PlayerData)/저장을 담당하는 씬 싱글톤.
// 골드로 즉시 레벨업(연구소와 달리 시간 없음).
public class GrowthManager : MonoBehaviour
{
    public static GrowthManager Instance { get; private set; }

    [SerializeField] SheetService table;

    readonly Dictionary<string, int> levels = new();

    public UnityEvent<string> onStatChanged = new(); // (statId) 특정 스탯 레벨 변경
    public UnityEvent onGrowthChanged = new();        // 전체 갱신(저장 훅 등)

    bool initialized;
    GrowthSaveData pendingLoad;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (table != null) table.OnAllTablesLoaded.AddListener(Init);
    }

    void Update()
    {
        // 로더가 준비되는 즉시 초기화 — table 등록/발화 순서에 의존하지 않는 안전망.
        // Init 내부에서 initialized/IsLoaded 가드를 하므로, 준비 전엔 아무 일도 안 하고 준비 후엔 1회만 실행된다.
        if (!initialized) Init();
    }

    void Init()
    {
        if (initialized) return;
        if (GrowthStatLoader.Instance == null || !GrowthStatLoader.Instance.IsLoaded) return;

        levels.Clear();
        foreach (var d in GrowthStatLoader.Instance.AllOrdered())
            levels[d.id] = 0;

        initialized = true;

        if (pendingLoad != null)
        {
            ApplyLoaded(pendingLoad);
            pendingLoad = null;
        }

        RecomputeEffects();
        onGrowthChanged.Invoke();
    }

    public bool IsReady => initialized;

    #region 조회

    public int GetLevel(string id) => levels.TryGetValue(id, out int lv) ? lv : 0;

    // 다음 레벨 비용
    public long GetCost(string id)
    {
        var d = GrowthStatLoader.Instance?.Get(id);
        return d != null ? d.CostAt(GetLevel(id)) : 0;
    }

    // 현재 레벨까지의 누적 효과(%)
    public float GetCurrentEffect(string id)
    {
        var d = GrowthStatLoader.Instance?.Get(id);
        return d != null ? GetLevel(id) * d.effectPerLevel : 0f;
    }

    // 다음 레벨 시 효과(%)
    public float GetNextEffect(string id)
    {
        var d = GrowthStatLoader.Instance?.Get(id);
        return d != null ? (GetLevel(id) + 1) * d.effectPerLevel : 0f;
    }

    public bool CanUpgrade(string id)
    {
        if (!initialized || GrowthStatLoader.Instance?.Get(id) == null) return false;
        if (DataManager.Instance == null) return false;
        return DataManager.Instance.Gold.GetValue() >= GetCost(id);
    }

    #endregion

    #region 레벨업

    // 골드를 소모하고 즉시 레벨 +1.
    public bool TryUpgrade(string id)
    {
        if (!CanUpgrade(id)) return false;

        long cost = GetCost(id);
        if (!DataManager.Instance.Gold.Use(GoldUseType.Growth, cost)) return false;
        DataManager.Instance.Gold.GoldUseReq(GoldUseType.Growth, cost);

        levels[id] = GetLevel(id) + 1;

        RecomputeEffects();
        onStatChanged.Invoke(id);
        onGrowthChanged.Invoke();
        return true;
    }

    #endregion

    #region 효과 반영

    // 모든 스탯 레벨을 합산해 성장 몫으로 집계기에 등록(연구소 등 다른 소스와 합산됨).
    // 스탯 Id가 StatType 키와 일치하므로 enum으로 파싱해 분류한다.
    void RecomputeEffects()
    {
        StatSet set = default;

        foreach (var kv in levels)
        {
            var d = GrowthStatLoader.Instance?.Get(kv.Key);
            if (d == null) continue;

            float v = kv.Value * d.effectPerLevel;
            if (Enum.TryParse(kv.Key, true, out StatType type))
                set.AddEffect(type, v);
            else
                Debug.LogWarning($"[Growth] StatType 미인식: {kv.Key}");
        }

        PlayerStatAggregator.SetContribution("growth", set);
    }

    #endregion

    #region 저장

    public GrowthSaveData ToSaveData()
    {
        GrowthSaveData data = new();
        foreach (var kv in levels)
        {
            if (kv.Value <= 0) continue; // 0레벨은 저장 생략
            data.stats.Add(new GrowthStatSave { statId = kv.Key, level = kv.Value });
        }
        return data;
    }

    // SaveManager가 서버에서 불러온 레벨을 반영
    public void ApplyLoaded(GrowthSaveData data)
    {
        if (data == null || data.stats == null) return;

        if (!initialized) // 시트가 아직 안 왔으면 보류했다가 Init에서 반영
        {
            pendingLoad = data;
            return;
        }

        foreach (GrowthStatSave save in data.stats)
        {
            if (save == null || !levels.ContainsKey(save.statId)) continue;
            levels[save.statId] = Mathf.Max(0, save.level);
        }

        RecomputeEffects();
        onGrowthChanged.Invoke();
    }

    #endregion
}

[Serializable]
public class GrowthStatSave
{
    public string statId;
    public int level;
}

[Serializable]
public class GrowthSaveData
{
    public List<GrowthStatSave> stats = new();
}
