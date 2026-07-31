using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.Events;

// v45 월드보스 매니저(프로토 evt 시스템 포팅). 30초 동안 불사신 보스에게 넣은 총 데미지 = 점수.
// 주간 로테이션(WorldBossOutlaw 시트, 주차%6) · 일 3회 무료 · 주간 베스트 1판만 랭킹 반영.
// 랭킹 = PlayFab 리더보드(프로토 고스트 폴백 없음) — 베스트 갱신 시 통계 보고, 보드는 RankBoard가 그림.
// 주간 보상 정산(우편)은 서버 몫이라 미구현. 씬에 1개 배치 필요(매니저류 싱글톤 규약).
public class WorldBossManager : MonoBehaviour
{
    public static WorldBossManager Instance { get; private set; }

    [SerializeField] string statisticName = "WorldBossDamage"; // PlayFab 리더보드 통계명(RankBoard와 일치시킬 것)

    // 프로토 v1.0.45 기본값 — WorldBossConfig 시트 로드 시 시트 우선
    const int DefFreeTries = 3;
    const float DefElemMul = 2f;
    const int DefUnlockChars = 3;
    const float DefDurationSec = 30f;
    const long WeekMs = 604800000L; // 7일 에포크(주차 = UTC ms / WeekMs — 경계 목요일 00:00 UTC)

    public int FreeTriesPerDay => WorldBossConfigLoader.Instance != null && WorldBossConfigLoader.Instance.IsLoaded
        ? WorldBossConfigLoader.Instance.GetInt("FreeTriesPerDay", DefFreeTries) : DefFreeTries;
    public float ElemDmgMul => WorldBossConfigLoader.Instance != null && WorldBossConfigLoader.Instance.IsLoaded
        ? WorldBossConfigLoader.Instance.GetFloat("ElemDmgMul", DefElemMul) : DefElemMul;
    public int UnlockCharCount => WorldBossConfigLoader.Instance != null && WorldBossConfigLoader.Instance.IsLoaded
        ? WorldBossConfigLoader.Instance.GetInt("UnlockCharCount", DefUnlockChars) : DefUnlockChars;
    public float DurationSec => WorldBossConfigLoader.Instance != null && WorldBossConfigLoader.Instance.IsLoaded
        ? WorldBossConfigLoader.Instance.GetFloat("DurationSec", DefDurationSec) : DefDurationSec;

    // 주간 상태
    public long BestDamage { get; private set; }
    public long WeekStamp { get; private set; } = -1; // 상태가 속한 주차(롤오버 판정)
    public bool BattleActive { get; private set; }    // 전투 중에만 속성 ×2 집계

    int triesUsedToday;
    string dayStamp = "";

    public UnityEvent onChanged = new(); // 상태 변화(베스트/횟수/주 롤오버) → UI 갱신

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── 주차/로테이션 ──
    static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public long WeekIndex() => NowMs() / WeekMs;

    // 주 경계까지 남은 시간 "5d 23h 14m" (프로토 evtCdStr 고정 포맷)
    public string CountdownText()
    {
        long ms = Math.Max(0, (WeekIndex() + 1) * WeekMs - NowMs());
        long d = ms / 86400000L; ms -= d * 86400000L;
        long h = ms / 3600000L; ms -= h * 3600000L;
        long m = ms / 60000L;
        return $"{d}d {h:00}h {m:00}m";
    }

    // 이번 주 보스(시트 미로드 시 null — 패널이 로드 대기)
    public WorldBossOutlawData CurrentOutlaw()
        => WorldBossOutlawLoader.Instance != null && WorldBossOutlawLoader.Instance.IsLoaded
            ? WorldBossOutlawLoader.Instance.GetByWeek(WeekIndex()) : null;

    // ── 해금/도전 횟수 ──
    public bool IsUnlocked =>
        CharacterManager.Instance != null && CharacterManager.Instance.IsReady
        && CharacterManager.Instance.OwnedCount >= UnlockCharCount;

    public int TriesLeft()
    {
        RolloverCheck();
        return Mathf.Max(0, FreeTriesPerDay - triesUsedToday);
    }

    // 주/일 경계 롤오버 — 주가 바뀌면 베스트·횟수 리셋(보상 정산은 서버 몫), 날이 바뀌면 횟수만 리셋
    void RolloverCheck()
    {
        bool changed = false;

        long wk = WeekIndex();
        if (WeekStamp != wk)
        {
            WeekStamp = wk;
            BestDamage = 0;
            triesUsedToday = 0;
            changed = true;
        }

        string today = DateTime.Now.ToString("yyyy-M-d");
        if (dayStamp != today)
        {
            dayStamp = today;
            triesUsedToday = 0;
            changed = true;
        }

        if (changed) onChanged.Invoke();
    }

    // ── 도전 ──
    // 도전 시작: 횟수 소모 + FieldManager 월드보스 필드 진입. 실패 사유는 false 반환(UI가 비활성으로 선차단).
    public bool TryBeginChallenge()
    {
        if (!IsUnlocked || BattleActive)
        {
            Debug.Log($"[WorldBoss] 도전 불가: {(BattleActive ? "이미 전투 중" : $"해금 조건 미달(동료 {(CharacterManager.Instance != null ? CharacterManager.Instance.OwnedCount : 0)}/{UnlockCharCount})")}");
            return false;
        }
        if (TriesLeft() <= 0)
        {
            Debug.Log($"[WorldBoss] 도전 불가: 오늘 도전 횟수 소진({FreeTriesPerDay}회)");
            return false;
        }
        if (FieldManager.Instance == null || CurrentOutlaw() == null)
        {
            Debug.Log($"[WorldBoss] 도전 불가: {(FieldManager.Instance == null ? "FieldManager 싱글톤 없음" : "이번 주 보스 없음(WorldBossOutlaw 시트 미로드)")}");
            return false;
        }
        if (!FieldManager.Instance.EnterWorldBoss())
        {
            Debug.Log("[WorldBoss] 도전 불가: EnterWorldBoss 실패 — 이미 던전 진행 중이거나 FieldManager.worldBossField 인스펙터 미연결");
            return false;
        }

        triesUsedToday++;
        if (QuestEventManager.Instance != null) QuestEventManager.Instance.AddEvent("dgEnter_evt"); // 퀘·일일 미션 키(프로토 동일)

        onChanged.Invoke();
        return true;
    }

    // WorldBossField가 전투 시작/종료에 호출 — 전투 중에만 속성 ×2 집계
    public void SetBattleActive(bool active) => BattleActive = active;

    // 이번 주 보스의 약점 배율 — 보스별 ElemMul(Outlaw 시트) 우선, 없으면(0) 공용 ElemDmgMul
    public float CurrentElemMul()
    {
        var o = CurrentOutlaw();
        return o != null && o.elemMul > 0f ? o.elemMul : ElemDmgMul;
    }

    // 발사 시 속성 집계 배율(프로토 evtElMul) — 월드보스 전투 중 + 이번 주 보스 속성과 매칭 시 보스별 배율
    public float ElemMulFor(string bulletElementId)
    {
        if (!BattleActive || string.IsNullOrEmpty(bulletElementId)) return 1f;
        var o = CurrentOutlaw();
        return o != null && o.element == bulletElementId ? CurrentElemMul() : 1f;
    }

    // 전투 종료 정산 — 주간 베스트 갱신 시 PlayFab 통계 보고
    public void SubmitResult(long totalDamage)
    {
        RolloverCheck();
        if (totalDamage <= BestDamage) { onChanged.Invoke(); return; }

        BestDamage = totalDamage;
        ReportBest();
        onChanged.Invoke();
    }

    void ReportBest()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        // PlayFab 통계는 int32 — 초과분은 클램프(리더보드 상한 표기)
        int value = (int)Math.Min(BestDamage, int.MaxValue);
        PlayFabClientAPI.UpdatePlayerStatistics(
            new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate { StatisticName = statisticName, Value = value }
                }
            },
            _ => Debug.Log($"[WorldBoss] 베스트 보고: {value}"),
            e => Debug.LogError($"[WorldBoss] 베스트 보고 실패: {e.GenerateErrorReport()}"));
    }

    #region 저장 연동 (SaveManager 연결 대기 — Weapon/Achievement 문법)

    [System.Serializable]
    public class SaveData
    {
        public long bestDamage;
        public long weekStamp = -1;
        public int triesUsedToday;
        public string dayStamp = "";
    }

    public SaveData GetSaveData() => new()
    {
        bestDamage = BestDamage,
        weekStamp = WeekStamp,
        triesUsedToday = triesUsedToday,
        dayStamp = dayStamp,
    };

    public void ApplySaveData(SaveData data)
    {
        if (data == null) return;

        BestDamage = data.bestDamage;
        WeekStamp = data.weekStamp;
        triesUsedToday = data.triesUsedToday;
        dayStamp = data.dayStamp ?? "";

        RolloverCheck(); // 구 세이브가 지난 주/어제 것이면 즉시 리셋
        onChanged.Invoke();
    }

    #endregion
}
