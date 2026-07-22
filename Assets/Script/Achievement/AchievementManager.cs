using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 업적 진행/수령 관리(퀘스트와 유사). 모든 업적은 티어 래더로 동시에 진행된다.
// 진행도는 QuestEventManager의 누적 카운터(또는 Absolute 현재값)로 판정하고,
// 티어에 도달하면 젬을 수령(수동)해 다음 티어로 올라간다.
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    // id -> 이미 수령한 티어 수(진행 티어 인덱스)
    readonly Dictionary<string, int> claimedTier = new();

    public UnityEvent onChanged = new();                    // 진행/수령 변화 → UI 갱신
    public UnityEvent<AchievementData, int> onClaimed = new(); // (업적, 받은 젬)

    bool subscribed;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (QuestEventManager.Instance != null) QuestEventManager.Instance.OnEventChanged -= OnEvent;
    }

    void Update()
    {
        // QuestEventManager/시트가 준비되면 구독(싱글톤 타이밍 안전망)
        if (!subscribed) TrySubscribe();
    }

    void TrySubscribe()
    {
        if (subscribed) return;
        var loader = AchievementLoader.Instance;
        if (QuestEventManager.Instance == null || loader == null || !loader.IsLoaded) return;

        foreach (var a in loader.AllOrdered())
            if (!claimedTier.ContainsKey(a.id)) claimedTier[a.id] = 0;

        QuestEventManager.Instance.OnEventChanged += OnEvent;
        subscribed = true;
        onChanged.Invoke();
    }

    // 어떤 이벤트든 진행 변화 가능 → UI가 다시 조회하도록 통지(간단)
    void OnEvent(string key, long value) => onChanged.Invoke();

    #region 조회

    // 현재 누적/현재값(Counter는 누적, Absolute는 현재값 — 둘 다 GetValue)
    public long GetProgress(AchievementData a)
        => (a != null && QuestEventManager.Instance != null) ? QuestEventManager.Instance.GetValue(a.eventKey) : 0;

    public int GetClaimedTier(string id) => claimedTier.TryGetValue(id, out var t) ? t : 0;

    public bool IsMaxed(AchievementData a) => a != null && GetClaimedTier(a.id) >= a.TierCount;

    // 다음 수령할 티어의 목표값(모두 수령했으면 -1)
    public long NextThreshold(AchievementData a)
    {
        if (a == null) return -1;
        int t = GetClaimedTier(a.id);
        return t < a.tiers.Count ? a.tiers[t] : -1;
    }

    // 다음 티어 보상 젬(없으면 0)
    public int NextGems(AchievementData a)
    {
        if (a == null) return 0;
        int t = GetClaimedTier(a.id);
        return t < a.gems.Count ? a.gems[t] : 0;
    }

    // 지금 수령 가능한가(다음 티어 도달)
    public bool IsClaimable(AchievementData a)
    {
        if (a == null || IsMaxed(a)) return false;
        return GetProgress(a) >= a.tiers[GetClaimedTier(a.id)];
    }

    #endregion

    // 다음 티어 수령. 성공 시 젬 지급 + 티어 상승.
    public bool Claim(string id)
    {
        var a = AchievementLoader.Instance != null ? AchievementLoader.Instance.Get(id) : null;
        if (a == null || !IsClaimable(a)) return false;

        int t = GetClaimedTier(id);
        int gems = t < a.gems.Count ? a.gems[t] : 0;

        claimedTier[id] = t + 1;
        if (DataManager.Instance != null) DataManager.Instance.IncreaseGem(gems);

        onClaimed.Invoke(a, gems);
        onChanged.Invoke();
        return true;
    }

    #region 저장 연동

    [System.Serializable]
    public class SaveData
    {
        public List<string> ids = new();
        public List<int> tiers = new();
    }

    public SaveData GetSaveData()
    {
        var data = new SaveData();
        foreach (var kv in claimedTier)
        {
            if (kv.Value <= 0) continue;
            data.ids.Add(kv.Key);
            data.tiers.Add(kv.Value);
        }
        return data;
    }

    public void ApplySaveData(SaveData data)
    {
        if (data == null) return;
        int n = Mathf.Min(data.ids.Count, data.tiers.Count);
        for (int i = 0; i < n; i++)
            claimedTier[data.ids[i]] = data.tiers[i];
        onChanged.Invoke();
    }

    #endregion
}
