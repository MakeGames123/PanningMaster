using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 무기(리볼버) 뽑기 관리 (프로토 v1.0.40 P5 REVOLVER GACHA 포팅).
// 뽑기 1회 = 🧰 무기 상자 1개 = XP 1 → 레벨업하면 확률 창이 위로(WeaponGradeWindow).
// 뽑은 무기는 현재 장착과 비교(pending) → 택1, 탈락 무기는 골드로 즉시 판매.
// 최상위 등급은 미등장 천장(WeaponCeiling)으로 보증 — 창에서 열린 등급만 카운트.
public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    // 밸런스 상수 (WeaponConfig 시트와 동일 값 — 프로토 v1.0.40)
    const float LevelStatMul = 1.15f; // 주스탯 레벨 배율(레벨은 저축)
    const int AtkBase = 12;           // 주스탯 기준값
    const float JitterMin = 0.95f;    // 지터 하한(같은 등급 +1레벨 = 항상 업그레이드 보장)
    const float JitterMax = 1.05f;    // 지터 상한
    const int XpBase = 22;            // 레벨업 필요 뽑기 수 = round(XpBase × XpGrow^(lv-1))
    const float XpGrow = 1.4f;        // 22/31/43/60/85… 커브
    const int SubSlotBonusLevel = 5;  // 이 뽑기 레벨부터 부옵 슬롯 +1(캡 4)
    const int MaxSubSlots = 4;

    // 뽑기 상태
    public int DrawLevel { get; private set; } = 1;
    public int DrawXp { get; private set; }
    public int DrawCount { get; private set; }
    public int MaxSeenGrade { get; private set; }

    // 슬롯별 장착 무기(파티 슬롯 0~2와 1:1). 뽑기 레벨/XP/천장은 계정 공용, 무기만 슬롯별.
    readonly WeaponData[] equipped = new WeaponData[CharacterManager.PartySize];

    public int SelectedSlot { get; private set; } // 패널이 보여주는 슬롯(WeaponSlotSwitcher가 전환)

    public WeaponData Equipped => equipped[SelectedSlot]; // UI 호환: 현재 선택 슬롯의 무기
    public WeaponData GetEquipped(int slot) => slot >= 0 && slot < equipped.Length ? equipped[slot] : null;

    public WeaponData Pending { get; private set; } // 비교 대기 중인 신규 무기(없으면 null)

    // 패널 표시 슬롯 전환 — 비교 미해결 중에는 잠금(Pending이 다른 슬롯에 장착되는 사고 방지)
    public void SelectSlot(int slot)
    {
        if (slot < 0 || slot >= equipped.Length || slot == SelectedSlot) return;
        if (Pending != null) return;

        SelectedSlot = slot;
        onChanged.Invoke(); //패널이 선택 슬롯 무기로 다시 그림
    }

    readonly Dictionary<int, int> ceilCounters = new(); // 등급 → 미등장 누적 카운트

    public UnityEvent onChanged = new();                    // 상태 변화 → UI 갱신
    public UnityEvent<WeaponData, WeaponData> onPending = new(); // (현재, 신규) — 비교 팝업 열기
    public UnityEvent<WeaponData> onEquipped = new();       // 장착 확정(첫 무기 포함)
    public UnityEvent<int> onLevelUp = new();               // 뽑기 레벨업(새 레벨) — 배너 연출용

    bool initialized;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        // 시트/DataManager가 준비되면 스타터 무기 지급(싱글톤 타이밍 안전망)
        if (!initialized) TryInit();
    }

    void TryInit()
    {
        if (initialized) return;
        if (WeaponGradeWindowLoader.Instance == null || !WeaponGradeWindowLoader.Instance.IsLoaded) return;
        if (WeaponSubStatLoader.Instance == null || !WeaponSubStatLoader.Instance.IsLoaded) return;
        if (WeaponCeilingLoader.Instance == null || !WeaponCeilingLoader.Instance.IsLoaded) return;
        if (PlayerData.Instance == null) return; // 스탯 반영 대상이 생길 때까지 대기

        // 시작 = 슬롯마다 Lv1 E등급 고정(결정론·최약 베이스 — 첫 뽑기부터 상승 체감, 프로토 v34f)
        for (int i = 0; i < equipped.Length; i++)
            equipped[i] ??= new WeaponData { level = 1, grade = 0, atk = AtkBase };

        initialized = true;
        ApplyEquippedStats();
        onChanged.Invoke();
    }

    // 장착 무기(3슬롯 전부) 스탯을 전역 스탯 집계기에 "weapon" 소스로 반영.
    // ※ 현재 스탯은 전역 합산 — 무기 스탯을 그 캐릭터에게만 적용하려면 DamageCalculator의 사수별 스탯 분리가 필요(미구현).
    // wboss/wdgn(보스/던전 상황부 스탯)은 미적용 — 추후 상황부 배율 구현 시 연결.
    void ApplyEquippedStats()
    {
        StatSet total = default;
        foreach (var w in equipped) AccumulateWeapon(ref total, w);

        PlayerStatAggregator.SetContribution("weapon", total); // 집계기가 전투력 재계산까지 트리거
    }

    // 무기 1자루의 스탯 기여분(StatSet) 구성
    static StatSet BuildStatSet(WeaponData w)
    {
        StatSet set = default;
        AccumulateWeapon(ref set, w);
        return set;
    }

    static void AccumulateWeapon(ref StatSet set, WeaponData w)
    {
        if (w == null) return;

        set.Damage += w.atk; // 주스탯 = 공격력%
        foreach (var s in w.subs) AddSubStat(ref set, s.sid, s.value);
    }

    // 부옵 sid → 전역 스탯 필드 매핑 (WeaponSubStat 시트의 Id 기준)
    static void AddSubStat(ref StatSet set, string sid, float value)
    {
        switch (sid)
        {
            case "atk": set.Damage += value; break;
            case "elem": set.TypeDamage += value; break;
            case "critR": set.CriticalChance += value; break;
            case "critD": set.CriticalDamage += value; break;
            case "gdmg": set.FinalDamage += value; break;
            case "reload": set.ReloadSpeed += value; break;
            case "wgold": set.GoldAcq += value; break;
            // wboss/wdgn: 상황부(보스/던전) 스탯 — 미적용
            default: break;
        }
    }

    public bool IsReady => initialized;

    // 레벨업 필요 뽑기 수 = round(22 × 1.4^(lv-1))
    public int XpNeed(int level) => Mathf.RoundToInt(XpBase * Mathf.Pow(XpGrow, level - 1));
    public int XpNeed() => XpNeed(DrawLevel);

    // 🧰 1개 = 1회. 성공 시 true — 첫/스타터 이후엔 Pending이 생기고 onPending 발화.
    public bool TryDraw()
    {
        if (!initialized) return false;
        if (Pending != null) return false; // 비교 미해결 상태에선 다음 뽑기 불가
        if (DataManager.Instance == null || !DataManager.Instance.UseCrate(1)) return false;

        DrawCount++;
        DrawXp++;

        bool lvUp = false;
        while (DrawXp >= XpNeed(DrawLevel))
        {
            DrawXp -= XpNeed(DrawLevel);
            DrawLevel++;
            lvUp = true;
        }

        int forceG = CeilCheck();            // 천장: 미등장 보증(창에서 열린 등급만)
        var w = RollWeapon(DrawLevel, forceG);
        CeilBump(w.grade);

        if (w.grade > MaxSeenGrade) MaxSeenGrade = w.grade;

        if (QuestEventManager.Instance != null) QuestEventManager.Instance.AddEvent("revDraw");

        if (lvUp) onLevelUp.Invoke(DrawLevel);

        if (equipped[SelectedSlot] == null)
        {
            // 첫 무기 = 즉시 장착(빈 슬롯 — 비교 없음)
            equipped[SelectedSlot] = w;
            ApplyEquippedStats();
            if (QuestEventManager.Instance != null) QuestEventManager.Instance.AddEvent("revEquip");
            onEquipped.Invoke(w);
        }
        else
        {
            Pending = w;
            onPending.Invoke(equipped[SelectedSlot], w);
        }

        onChanged.Invoke();
        return true;
    }

    // 비교 택1: 신규를 선택 슬롯에 장착(기존 판매). 판매 골드 반환.
    public long EquipPending()
    {
        if (Pending == null) return 0;

        long gold = SellGold(equipped[SelectedSlot]);
        if (DataManager.Instance != null) DataManager.Instance.IncreaseGold((int)gold);

        equipped[SelectedSlot] = Pending;
        Pending = null;
        ApplyEquippedStats();

        if (QuestEventManager.Instance != null) QuestEventManager.Instance.AddEvent("revEquip");
        onEquipped.Invoke(equipped[SelectedSlot]);
        onChanged.Invoke();

        return gold;
    }

    // 비교 택1: 현재 유지(신규 판매). 판매 골드 반환.
    public long KeepCurrent()
    {
        if (Pending == null) return 0;

        long gold = SellGold(Pending);
        if (DataManager.Instance != null) DataManager.Instance.IncreaseGold((int)gold);

        Pending = null;
        onChanged.Invoke();
        return gold;
    }

    // 현재 장착 기준 전투력(비교 기준값)
    public float PowerCurrent()
    {
        var revolver = FindAnyObjectByType<RevolverSlots>();
        return revolver != null ? revolver.ComputePower() : 0f;
    }

    // 후보 무기를 장착했다고 가정한 전투력 — 실제 상태는 변경하지 않음(비교 표시용, 프로토 revPowerWith).
    // CalculateDamage가 참조하는 전투 스탯 5종만 후보 기준으로 잠시 치환해 계산 후 원복한다.
    public float PowerWith(WeaponData candidate)
    {
        var revolver = FindAnyObjectByType<RevolverSlots>();
        var p = PlayerData.Instance;
        if (revolver == null || p == null) return 0f;

        StatSet cur = BuildStatSet(equipped[SelectedSlot]);
        StatSet cand = BuildStatSet(candidate);

        var saved = (p.Damage, p.TypeDamage, p.CriticalChance, p.CriticalDamage, p.FinalDamage);
        p.Damage += cand.Damage - cur.Damage;
        p.TypeDamage += cand.TypeDamage - cur.TypeDamage;
        p.CriticalChance += cand.CriticalChance - cur.CriticalChance;
        p.CriticalDamage += cand.CriticalDamage - cur.CriticalDamage;
        p.FinalDamage += cand.FinalDamage - cur.FinalDamage;

        float power = revolver.ComputePower();

        (p.Damage, p.TypeDamage, p.CriticalChance, p.CriticalDamage, p.FinalDamage) = saved;
        return power;
    }

    // 리롤: 신규(Pending)를 즉시 판매하고 곧바로 다음 뽑기(🧰 1개 소모).
    // 상자가 없으면 아무것도 하지 않음 — 선택지는 그대로 유지된다.
    public bool RerollPending()
    {
        if (Pending == null) return false;
        if (DataManager.Instance == null || DataManager.Instance.crate < 1) return false;

        KeepCurrent();    // 신규 판매(골드 지급) + Pending 해제
        return TryDraw(); // 새 무기 뽑기 → onPending으로 팝업 갱신
    }

    // 판매 골드 = max(1, 클리어골드(현재 스테이지) × 0.25 × (1 + 등급 × 0.5))
    // 클리어골드 공식은 OnlineConfig 상수 기반(NormalField와 동일)
    public long SellGold(WeaponData w)
    {
        var cfg = GameConfigLoader.Instance;
        int stage = DataManager.Instance != null ? DataManager.Instance.stage : 1;

        float clearGold = stage
            * cfg.GetFloat("ClearGoldPerStage")
            * (1 + Mathf.Max(0, stage - cfg.GetFloat("ClearGoldRampStage")) * cfg.GetFloat("ClearGoldRampPerStage"))
            * Mathf.Pow(cfg.GetFloat("ClearGoldExpoRate"), Mathf.Max(0, stage - cfg.GetFloat("ClearGoldExpoStage")));

        int grade = w != null ? w.grade : 0;
        return (long)System.Math.Max(1, System.Math.Floor(clearGold * 0.25 * (1 + grade * 0.5)));
    }

    #region 롤

    WeaponData RollWeapon(int level, int forceGrade)
    {
        int g = forceGrade >= 0 ? forceGrade : RollGrade(level);

        float jitter = Random.Range(JitterMin, JitterMax);
        long atk = (long)System.Math.Max(1,
            System.Math.Round(AtkBase * System.Math.Pow(LevelStatMul, level - 1) * WeaponGrades.Mul(g) * jitter));

        var w = new WeaponData { level = level, grade = g, atk = atk };

        // 부옵: min(4, 1 + 등급/2 (+Lv5부터 1))개 — 풀에서 중복 스탯 회피(선형 탐사)
        var pool = WeaponSubStatLoader.Instance.SubPool();
        int cnt = Mathf.Min(MaxSubSlots, 1 + g / 2 + (level >= SubSlotBonusLevel ? 1 : 0));
        List<int> used = new();

        for (int k = 0; k < cnt && pool.Count > 0; k++)
        {
            int si = Mathf.Min(pool.Count - 1, Mathf.FloorToInt(Random.value * pool.Count));
            int tries = 0;
            while (used.Contains(si) && tries++ < pool.Count) si = (si + 1) % pool.Count;
            used.Add(si);

            var st = pool[si];
            w.subs.Add(new WeaponSub
            {
                sid = st.id,
                value = Mathf.Max(1, Mathf.RoundToInt(st.step * (1 + g) * (1 + Random.value))),
            });
        }

        return w;
    }

    int RollGrade(int level)
    {
        var probs = WeaponGradeWindowLoader.Instance.GetWindow(level);
        float r = Random.value * 100f;
        for (int i = 0; i < probs.Length; i++)
        {
            if (r < probs[i]) return i;
            r -= probs[i];
        }
        return 0;
    }

    #endregion

    #region 천장(피티)

    // 창에서 열린 등급 중 천장 도달 시 그 등급 강제(상위 우선). 없으면 -1
    int CeilCheck()
    {
        var window = WeaponGradeWindowLoader.Instance.GetWindow(DrawLevel);
        foreach (var c in WeaponCeilingLoader.Instance.AllOrdered())
        {
            if (window[c.gradeId] > 0 && ceilCounters.TryGetValue(c.gradeId, out var n) && n >= c.pityCount)
                return c.gradeId;
        }
        return -1;
    }

    // 창에서 열린 천장 등급 카운트 +1, 해당 등급 이상이 나오면 리셋
    void CeilBump(int rolledGrade)
    {
        var window = WeaponGradeWindowLoader.Instance.GetWindow(DrawLevel);
        foreach (var c in WeaponCeilingLoader.Instance.AllOrdered())
        {
            int k = c.gradeId;
            if (window[k] > 0) ceilCounters[k] = (ceilCounters.TryGetValue(k, out var n) ? n : 0) + 1;
            if (rolledGrade >= k) ceilCounters[k] = 0;
        }
    }

    #endregion

    #region 저장 연동

    [System.Serializable]
    public class SaveData
    {
        public int drawLevel = 1;
        public int drawXp;
        public int drawCount;
        public int maxSeenGrade;
        public List<int> ceilGrades = new();
        public List<int> ceilCounts = new();
        public List<WeaponData> equippedSlots = new(); // 파티 슬롯 0~2 순서
    }

    public SaveData GetSaveData()
    {
        var data = new SaveData
        {
            drawLevel = DrawLevel,
            drawXp = DrawXp,
            drawCount = DrawCount,
            maxSeenGrade = MaxSeenGrade,
        };
        data.equippedSlots.AddRange(equipped);
        foreach (var kv in ceilCounters)
        {
            data.ceilGrades.Add(kv.Key);
            data.ceilCounts.Add(kv.Value);
        }
        return data;
    }

    public void ApplySaveData(SaveData data)
    {
        if (data == null) return;

        DrawLevel = Mathf.Max(1, data.drawLevel);
        DrawXp = data.drawXp;
        DrawCount = data.drawCount;
        MaxSeenGrade = data.maxSeenGrade;
        for (int i = 0; i < equipped.Length && i < data.equippedSlots.Count; i++)
            if (data.equippedSlots[i] != null) equipped[i] = data.equippedSlots[i];

        ceilCounters.Clear();
        int n = Mathf.Min(data.ceilGrades.Count, data.ceilCounts.Count);
        for (int i = 0; i < n; i++)
            ceilCounters[data.ceilGrades[i]] = data.ceilCounts[i];

        if (initialized) ApplyEquippedStats(); // 초기화 전이면 TryInit이 반영
        onChanged.Invoke();
    }

    #endregion
}
