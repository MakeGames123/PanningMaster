using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 캐릭터 모집(뽑기) 메커니즘 (프로토 v1.0.40 캐릭터 뽑기 포팅 — 뽑기만, 천장은 추후).
// 모집 1회 = 🪪 모집서 1개. 미보유 = 획득 / 중복 = 🃏 카드 +1.
// 보유 로스터·모집 레벨 상태는 CharacterManager가 소유 — 여기는 확률 창과 롤만.
public class CharacterRecruiter : MonoBehaviour
{
    public static CharacterRecruiter Instance { get; private set; }

    public UnityEvent<CharacterRosterData, bool> onRecruited = new(); // (캐릭터, 신규 여부)
    public UnityEvent<int> onRecruitLevelUp = new();                  // 모집 레벨업(새 레벨) — 축하 연출용

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // 모집 확률 창: 데뷔 전 등급 0 + 잔여 정규화(합 100 유지)
    public float[] GetWindow()
    {
        var grades = CharacterGradeLoader.Instance.AllOrdered();
        int lv = CharacterManager.Instance != null ? CharacterManager.Instance.RecruitLevel : 1;

        float[] open = new float[grades.Count];
        float sum = 0;
        for (int g = 0; g < grades.Count; g++)
        {
            open[g] = lv >= grades[g].debutRecruitLv ? grades[g].prob : 0f;
            sum += open[g];
        }
        if (sum <= 0) sum = 1;
        for (int g = 0; g < open.Length; g++) open[g] = open[g] * 100f / sum;
        return open;
    }

    [System.Serializable]
    public struct RecruitResult
    {
        public CharacterRosterData character;
        public bool isNew;
    }

    // ── 천장(프로토 CH_CEIL=300 · chPityG): 보증 대상 = 현재 창에서 열린 최고 등급. 대상 등급 획득 시 리셋 ──

    public const int PityCeil = 300;

    public int CeilCount { get; private set; } // 보증 대상 미등장 누적
    public int PityRemaining => Mathf.Max(0, PityCeil - CeilCount);

    // 보증 대상 등급 = 현재 창에서 확률이 열려 있는 최고 등급
    public int PityTargetGrade()
    {
        var window = GetWindow();
        for (int g = window.Length - 1; g >= 0; g--)
            if (window[g] > 0) return g;
        return 0;
    }

    // 🪪 1개 = 1뽑. 결과는 onRecruited(캐릭터, 신규 여부)로 통지.
    public bool TryRecruit() => TryRecruitMany(1) != null;

    // 🪪 count개 = count뽑(x10 버튼). 부족하면 null — 하나도 뽑지 않는다.
    public List<RecruitResult> TryRecruitMany(int count)
    {
        var mgr = CharacterManager.Instance;
        if (mgr == null || !mgr.IsReady) return null;
        if (DataManager.Instance == null || !DataManager.Instance.UseScroll(count)) return null;

        var results = new List<RecruitResult>();
        for (int n = 0; n < count; n++)
        {
            int levelBefore = mgr.RecruitLevel;
            mgr.AddRecruitCount();

            int pityGrade = PityTargetGrade();
            CharacterRosterData picked;
            if (CeilCount + 1 >= PityCeil)
                picked = PityPick(pityGrade); //천장 = 보증 등급·미보유 우선(시트순 결정론)
            else
                picked = RollCharacter();

            if (picked.grade >= pityGrade) CeilCount = 0; //보증 등급 이상 획득 = 리셋
            else CeilCount++;

            bool isNew = mgr.Acquire(picked.id);

            if (QuestEventManager.Instance != null) QuestEventManager.Instance.AddEvent("chDraw");

            if (mgr.RecruitLevel > levelBefore) onRecruitLevelUp.Invoke(mgr.RecruitLevel);
            onRecruited.Invoke(picked, isNew);

            results.Add(new RecruitResult { character = picked, isNew = isNew });
        }

        return results;
    }

    // 천장 발동 픽: 보증 등급 풀에서 미보유 우선, 전부 보유면 시트 첫 번째(프로토 v37 결정론)
    CharacterRosterData PityPick(int grade)
    {
        var mgr = CharacterManager.Instance;
        var pool = CharacterRosterLoader.Instance.ByGrade(grade);
        if (pool.Count == 0) return RollCharacter();

        foreach (var c in pool)
            if (!mgr.IsOwned(c.id)) return c;
        return pool[0];
    }

    // 테스트용 모집 — 재화 소모 없이 롤만 진행. 누적 카운트/모집 레벨/보유·카드는 실제와 동일하게 굴러간다.
    public CharacterRosterData DebugRecruit()
    {
        var mgr = CharacterManager.Instance;
        if (mgr == null || !mgr.IsReady) return null;

        int levelBefore = mgr.RecruitLevel;
        mgr.AddRecruitCount();

        var picked = RollCharacter();
        bool isNew = mgr.Acquire(picked.id);

        if (mgr.RecruitLevel > levelBefore)
            Debug.Log($"[모집] 모집 레벨 {mgr.RecruitLevel} 도달 — 상위 등급 데뷔 확인");

        var grade = CharacterGradeLoader.Instance.Get(picked.grade);
        var st = mgr.GetState(picked.id);
        Debug.Log(
            $"[모집 #{mgr.RecruitCount} | Lv.{mgr.RecruitLevel}] " +
            $"<color={(grade != null ? grade.colorHex : "#ffffff")}>[{(grade != null ? grade.nameKo : picked.grade.ToString())}]</color> " +
            $"{picked.emoji} {picked.nameKo}" +
            (isNew ? " — 신규 획득!" : $" — 중복(🃏 {st.cards})"));

        return picked;
    }

    CharacterRosterData RollCharacter()
    {
        var window = GetWindow();

        // 등급 롤
        float r = Random.value * 100f;
        int grade = -1;
        float acc = 0;
        for (int g = 0; g < window.Length; g++)
        {
            if (window[g] <= 0) continue;
            acc += window[g];
            if (r < acc) { grade = g; break; }
        }
        if (grade < 0) // 부동소수 합산 폴백 = 창 바닥
            for (int g = 0; g < window.Length; g++)
                if (window[g] > 0) { grade = g; break; }

        // 캐릭터 롤(해당 등급 풀에서 균등)
        var pool = CharacterRosterLoader.Instance.ByGrade(grade);
        if (pool.Count == 0) return CharacterRosterLoader.Instance.AllOrdered()[0];
        return pool[Random.Range(0, pool.Count)];
    }
}
