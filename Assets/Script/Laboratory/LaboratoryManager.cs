using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum LabNodeState
{
    Locked,      // 선행 노드를 아직 다 완료하지 못함
    Available,   // 연구 가능 (마스터 전, 연구중 아님)
    Researching, // 연구 진행 중
    Mastered     // 최대 레벨 도달
}

// 연구소 노드 트리의 중심. 시트(LabNode/LabEdge)로부터 데이터를 받아
// 해금/연구(시간+골드)/효과반영(PlayerData)/저장을 담당하는 씬 싱글톤.
public class LaboratoryManager : MonoBehaviour
{
    public static LaboratoryManager Instance { get; private set; }

    [SerializeField] TableLoaderManager table;

    readonly Dictionary<int, LabNodeData> nodeById = new();
    readonly Dictionary<int, int> levels = new();
    readonly Dictionary<int, float> researching = new(); // id -> 남은 시간(초)
    readonly List<int> tickBuffer = new();

    public UnityEvent<int> onNodeChanged = new();
    public UnityEvent onTreeChanged = new();
    public UnityEvent onResearchCompleted = new(); //연구 1건 완료 시(페이지 버튼 갱신용)

    SaveManager saveManager;
    LabSaveData pendingLoad;
    bool initialized;

    public IReadOnlyDictionary<int, LabNodeData> Nodes => nodeById;
    public bool AnyResearching => researching.Count > 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        saveManager = FindAnyObjectByType<SaveManager>();
        if (table != null) table.OnAllTablesLoaded.AddListener(Init);
    }

    void Init()
    {
        if (initialized) return;
        if (LabNodeLoader.Instance == null || !LabNodeLoader.Instance.IsLoaded) return;

        nodeById.Clear();
        levels.Clear();

        foreach (var kv in LabNodeLoader.Instance.All)
        {
            nodeById[kv.Key] = kv.Value;
            levels[kv.Key] = 0;
        }
        initialized = true;

        if (pendingLoad != null) //서버 저장이 먼저 도착했으면 이제 반영
        {
            ApplyLoaded(pendingLoad);
            pendingLoad = null;
        }

        RecomputeEffects();
        onTreeChanged.Invoke();
    }

    void Update()
    {
#if UNITY_EDITOR
        // 디버그: F1 -> 진행 중인 연구 즉시 완료
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.f1Key.wasPressedThisFrame)
            CompleteAllResearchInstant();
#endif

        if (researching.Count == 0) return;

        tickBuffer.Clear();
        foreach (int id in researching.Keys) tickBuffer.Add(id);

        foreach (int id in tickBuffer)
        {
            float rem = researching[id] - Time.deltaTime;
            if (rem <= 0f)
            {
                researching.Remove(id);
                CompleteResearch(id);
            }
            else
            {
                researching[id] = rem;
            }
        }
    }

#if UNITY_EDITOR
    // 디버그: 진행 중인 모든 연구를 즉시 완료
    void CompleteAllResearchInstant()
    {
        if (researching.Count == 0) return;

        tickBuffer.Clear();
        foreach (int id in researching.Keys) tickBuffer.Add(id);
        researching.Clear();

        foreach (int id in tickBuffer) CompleteResearch(id);
    }
#endif

    void CompleteResearch(int id)
    {
        levels[id] = GetLevel(id) + 1;

        RecomputeEffects();
        SaveToServer();

        onNodeChanged.Invoke(id);
        onTreeChanged.Invoke(); // 이 노드 마스터 시 후행 노드가 열릴 수 있음
        onResearchCompleted.Invoke();
    }

    #region 조회

    public LabNodeData GetNode(int id) => nodeById.TryGetValue(id, out var d) ? d : null;
    public int GetLevel(int id) => levels.TryGetValue(id, out int lv) ? lv : 0;
    public int GetMaxLevel(int id) { var n = GetNode(id); return n != null ? n.maxLevel : 0; }
    public bool IsMastered(int id) { var n = GetNode(id); return n != null && GetLevel(id) >= n.maxLevel; }
    public bool IsResearching(int id) => researching.ContainsKey(id);
    public float GetRemaining(int id) => researching.TryGetValue(id, out float r) ? r : 0f;

    public float GetProgress(int id)
    {
        var n = GetNode(id);
        if (n == null || n.timeSeconds <= 0f || !researching.TryGetValue(id, out float r)) return 0f;
        return Mathf.Clamp01(1f - r / n.timeSeconds);
    }

    public LabNodeState GetState(int id)
    {
        var n = GetNode(id);
        if (n == null) return LabNodeState.Locked;

        // 선행 노드가 전부 마스터되어야 열린다
        if (LabEdgeLoader.Instance != null)
        {
            foreach (int pre in LabEdgeLoader.Instance.GetPrerequisites(id))
                if (!IsMastered(pre)) return LabNodeState.Locked;
        }

        if (IsResearching(id)) return LabNodeState.Researching;
        return IsMastered(id) ? LabNodeState.Mastered : LabNodeState.Available;
    }

    // 다음 연구 비용 (마스터면 0)
    public long GetCost(int id)
    {
        var n = GetNode(id);
        return (n == null || IsMastered(id)) ? 0 : n.cost;
    }

    public bool CanResearch(int id)
    {
        if (GetState(id) != LabNodeState.Available) return false;
        return DataManager.Instance.Gold.GetValue() >= GetCost(id);
    }

    // 특정 효과 타입의 총합 (레벨 * 레벨당효과)
    public float GetTotalValue(LabEffectType type)
    {
        float sum = 0f;
        foreach (var kv in nodeById)
            if (kv.Value.effect == type) sum += GetLevel(kv.Key) * kv.Value.amount;
        return sum;
    }

    #endregion

    #region 연구

    // 연구 시작. 골드를 소모하고 시간이 지나면 완료(레벨 +1).
    // 서로 다른 열린 노드는 동시에(병렬로) 연구 가능하다.
    public bool TryResearch(int id)
    {
        if (!CanResearch(id)) return false;

        long cost = GetCost(id);
        if (!DataManager.Instance.Gold.Use(GoldUseType.Laboratory, cost)) return false;
        DataManager.Instance.Gold.GoldUseReq(GoldUseType.Laboratory, cost);

        var n = GetNode(id);
        if (n.timeSeconds <= 0f)
        {
            CompleteResearch(id); // 시간이 0이면 즉시 완료
        }
        else
        {
            researching[id] = n.timeSeconds;
            onNodeChanged.Invoke(id);
            onTreeChanged.Invoke();
        }
        return true;
    }

    #endregion

    #region 효과 반영

    // 모든 노드 레벨을 합산해 PlayerData 스탯에 반영(idempotent set).
    void RecomputeEffects()
    {
        if (PlayerData.Instance == null) return;

        float dmg = 0, shoot = 0, reload = 0, gold = 0, crit = 0, critDmg = 0, finalD = 0, typeD = 0;

        foreach (var kv in nodeById)
        {
            float v = GetLevel(kv.Key) * kv.Value.amount;
            switch (kv.Value.effect)
            {
                case LabEffectType.Damage: dmg += v; break;
                case LabEffectType.ShootSpeed: shoot += v; break;
                case LabEffectType.ReloadSpeed: reload += v; break;
                case LabEffectType.GoldAcq: gold += v; break;
                case LabEffectType.CriticalChance: crit += v; break;
                case LabEffectType.CriticalDamage: critDmg += v; break;
                case LabEffectType.FinalDamage: finalD += v; break;
                case LabEffectType.TypeDamage: typeD += v; break;
            }
        }

        var p = PlayerData.Instance;
        p.Damage = dmg;
        p.ShootSpeed = shoot;
        p.ReloadSpeed = reload;
        p.GoldAcq = gold;
        p.CriticalChance = crit;
        p.CriticalDamage = critDmg;
        p.FinalDamage = finalD;
        p.TypeDamage = typeD;
    }

    #endregion

    #region 저장

    public void SaveToServer()
    {
        if (saveManager == null) saveManager = FindAnyObjectByType<SaveManager>();
        if (saveManager == null) return;

        saveManager.SaveLabToServer(ToSaveData());
    }

    public LabSaveData ToSaveData()
    {
        LabSaveData data = new();
        foreach (var kv in levels)
        {
            if (kv.Value <= 0) continue; // 0레벨은 저장 생략
            data.nodes.Add(new LabNodeSave { nodeId = kv.Key, level = kv.Value });
        }
        return data;
    }

    // SaveManager가 서버에서 불러온 완료 레벨을 반영 (진행중 연구는 저장/복원하지 않음)
    public void ApplyLoaded(LabSaveData data)
    {
        if (data == null || data.nodes == null) return;

        if (!initialized) //시트가 아직 안 왔으면 보류했다가 Init에서 반영
        {
            pendingLoad = data;
            return;
        }

        foreach (LabNodeSave save in data.nodes)
        {
            if (save == null || !nodeById.TryGetValue(save.nodeId, out LabNodeData node)) continue;
            levels[save.nodeId] = Mathf.Clamp(save.level, 0, node.maxLevel);
        }

        RecomputeEffects();
        onTreeChanged.Invoke();
    }

    #endregion
}

[Serializable]
public class LabNodeSave
{
    public int nodeId;
    public int level;
}

[Serializable]
public class LabSaveData
{
    public List<LabNodeSave> nodes = new();
}
