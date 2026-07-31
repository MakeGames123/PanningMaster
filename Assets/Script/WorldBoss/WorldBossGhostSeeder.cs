using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

// 월드보스 유령 유저 시딩 도구(개발용 — 구 PvpGhostSeeder 문법 승계).
// 플레이 모드에서 컴포넌트 우클릭 -> "Upload WorldBoss Ghost Scores" (에디터: F3).
// 유령 계정(GHOST_0001~)에 순차 로그인해 이번 주 월드보스 점수(WorldBossDamage)를 등록하고,
// 끝나면 기기 계정으로 재로그인한다.
//
// 점수 = 프로토 v45g evtGhostScore 공식:
//   내 전투력(앵커) × GhostCal(13) × exp(U(GhostLogUMin, GhostLogUMax)) (+8% 고래 꼬리 +0.6~2.0)
//   GhostSkipRate(25%)는 불참 — 그 유령은 이번 주 등록을 건너뛴다.
//   상수는 WorldBossConfig 시트 우선(Ghost* 키), 시드 = 주차×유령번호 결정론 —
//   같은 주에 재실행하면 같은 점수, 주가 바뀌면 새 분포(보드가 매주 리롤).
// 주의:
// - 테이블 로드("All Tables Loaded") + 기기 로그인 이후 실행.
// - PlayFab 대시보드에서 "클라이언트의 플레이어 통계 게시 허용" 필요.
// - WorldBossDamage 통계를 주간 리셋(버전)으로 설정했다면 매주 1회 재실행해야 유령이 다시 채워진다.
public class WorldBossGhostSeeder : MonoBehaviour
{
    [SerializeField] PlayFabLoginManager login; //완료 후 원래(기기) 계정 재로그인용

    [Header("등록 설정")]
    [SerializeField] string statisticName = "WorldBossDamage";
    [SerializeField] string customIdPrefix = "GHOST_"; //PVP 시더와 같은 계정 풀 재사용(닉네임 유지)
    [SerializeField] int ghostCount = 100;
    [SerializeField] float delayBetweenUsers = 5f; //API 스로틀 방지 간격(초)
    [SerializeField] bool setDisplayName = true;   //새 계정 대비 닉네임도 등록(기존 계정은 실패해도 계속)

    // 프로토 RIVAL_ADJ × RIVAL_NM — 닉네임 결정론 조합(인덱스 기반)
    static readonly string[] Adj = { "무법자", "빠른손", "황야의", "저격수", "도박꾼", "검은모자", "쌍권총", "은탄환", "현상금사냥꾼", "붉은노을", "방랑자", "턱수염", "외눈", "강철", "새벽의", "유령", "모래바람", "6연발", "녹슨", "금이빨", "침묵의", "천둥", "코요테", "독사", "매의눈" };
    static readonly string[] Nm = { "잭", "케이트", "콜트", "로사", "에이스", "카일", "맥스", "실버", "밥", "진", "하울", "더건", "웨스", "메이", "로건", "새디", "럭키", "스미스", "엘라", "레드", "버드", "도크", "이든", "벨", "칼립소" };

    class Ghost
    {
        public string customId;
        public string displayName;
        public int score;
    }

    List<Ghost> ghosts;
    int index;
    int failCount;
    bool uploading;

#if UNITY_EDITOR
    void Update()
    {
        // 디버그: F3 -> 월드보스 유령 점수 업로드
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.f3Key.wasPressedThisFrame)
            Upload();
    }
#endif

    [ContextMenu("Upload WorldBoss Ghost Scores")]
    public void Upload()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[WB GhostSeeder] 플레이 모드에서 실행하세요");
            return;
        }
        if (uploading) return;
        if (WorldBossManager.Instance == null)
        {
            Debug.LogError("[WB GhostSeeder] WorldBossManager가 없습니다(씬 배치 확인)");
            return;
        }
        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.LogError("[WB GhostSeeder] 로그인 후 실행하세요");
            return;
        }

        uploading = true;
        BuildGhosts();
        index = 0;
        failCount = 0;
        Debug.Log($"[WB GhostSeeder] 이번 주(주차 {WorldBossManager.Instance.WeekIndex()}) 유령 {ghosts.Count}명 점수 등록 시작 (불참 제외)");
        ProcessNext();
    }

    // 프로토 evtGhostScore — 앵커(내 전투력) × Cal × 로그 분포, 주차×번호 시드 결정론
    void BuildGhosts()
    {
        var cfg = WorldBossConfigLoader.Instance;
        bool cfgOk = cfg != null && cfg.IsLoaded;
        float cal = cfgOk ? cfg.GetFloat("GhostCal", 13f) : 13f;
        float skipRate = cfgOk ? cfg.GetFloat("GhostSkipRate", 0.25f) : 0.25f;
        float uMin = cfgOk ? cfg.GetFloat("GhostLogUMin", -3.0f) : -3.0f;
        float uMax = cfgOk ? cfg.GetFloat("GhostLogUMax", 0.72f) : 0.72f;
        float whaleRate = cfgOk ? cfg.GetFloat("GhostWhaleRate", 0.08f) : 0.08f;
        float whaleMin = cfgOk ? cfg.GetFloat("GhostWhaleMin", 0.6f) : 0.6f;
        float whaleMax = cfgOk ? cfg.GetFloat("GhostWhaleMax", 2.0f) : 2.0f;

        double anchor = System.Math.Max(50, PlayerData.Instance != null ? (double)PlayerData.Instance.Power : 50);
        long week = WorldBossManager.Instance.WeekIndex();

        ghosts = new List<Ghost>();
        for (int i = 1; i <= ghostCount; i++)
        {
            //주차×번호 시드 — 같은 주 재실행 = 같은 점수(결정론), 다음 주 = 새 분포
            var rng = new System.Random(unchecked((int)(week * 100003 + i)));

            if (rng.NextDouble() < skipRate) continue; //이번 주 불참

            double u = uMin + rng.NextDouble() * (uMax - uMin);
            double whale = rng.NextDouble() < whaleRate
                ? whaleMin + rng.NextDouble() * (whaleMax - whaleMin) : 0;
            double score = anchor * cal * System.Math.Exp(u + whale);

            ghosts.Add(new Ghost
            {
                customId = $"{customIdPrefix}{i:0000}",
                displayName = $"{Adj[(i - 1) % Adj.Length]} {Nm[(i - 1) / Adj.Length % Nm.Length]}#{i:000}",
                score = (int)System.Math.Clamp(System.Math.Round(score), 1, int.MaxValue),
            });
        }
    }

    void ProcessNext()
    {
        if (index >= ghosts.Count)
        {
            Finish();
            return;
        }

        Ghost g = ghosts[index];
        PlayFabClientAPI.LoginWithCustomID(
            new LoginWithCustomIDRequest { CustomId = g.customId, CreateAccount = true },
            _ => SetName(g),
            e => Skip(g, "로그인 실패", e));
    }

    void SetName(Ghost g)
    {
        if (!setDisplayName) { SetScore(g); return; }

        PlayFabClientAPI.UpdateUserTitleDisplayName(
            new UpdateUserTitleDisplayNameRequest { DisplayName = g.displayName },
            _ => SetScore(g),
            e =>
            {
                // 닉네임 실패(중복/이미 설정)해도 점수는 등록하고 계속 진행
                SetScore(g);
            });
    }

    void SetScore(Ghost g)
    {
        PlayFabClientAPI.UpdatePlayerStatistics(
            new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate { StatisticName = statisticName, Value = g.score }
                }
            },
            _ =>
            {
                Debug.Log($"[WB GhostSeeder] {index + 1}/{ghosts.Count} 완료: {g.displayName} 데미지 {NumberFormatLoader.Abbrev(g.score)}");
                Advance();
            },
            e => Skip(g, "점수 등록 실패", e));
    }

    void Skip(Ghost g, string reason, PlayFabError e)
    {
        failCount++;
        Debug.LogError($"[WB GhostSeeder] {g.customId} {reason}: {e.GenerateErrorReport()}");
        Advance();
    }

    void Advance()
    {
        index++;
        StartCoroutine(DelayNext());
    }

    IEnumerator DelayNext()
    {
        yield return new WaitForSeconds(delayBetweenUsers);
        ProcessNext();
    }

    void Finish()
    {
        uploading = false;
        Debug.Log($"[WB GhostSeeder] 등록 종료: 성공 {ghosts.Count - failCount} / 실패 {failCount}");

        // 세션이 마지막 유령 계정으로 남아있으므로 기기 계정으로 복귀
        if (login != null)
        {
            Debug.Log("[WB GhostSeeder] 기기 계정으로 재로그인");
            login.Login();
        }
        else
        {
            Debug.LogWarning("[WB GhostSeeder] login 참조가 비어 있음. 게임을 재시작해 기기 계정으로 돌아가세요");
        }
    }
}
