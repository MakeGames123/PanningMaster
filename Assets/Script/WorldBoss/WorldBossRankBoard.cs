using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

// v45 월드보스 전용 랭킹 보드(프로토 renderEvent 랭킹부). 일반 RankBoard와 표시가 달라 별도 구현:
// 탭/TOP3 슬롯/내 주변 없음 — 내 행(rk-mine)을 맨 위에 고정하고 그 아래 TOP 1~50을 그대로 나열.
// 행 = RankEntryUI 재사용(rewardText 연결 시 순위 보상 🔴/🎫 표기, 점수는 축약 데미지 "2597M").
// 내 기록이 없으면 내 행은 순위/보상/데미지 전부 "—"(프로토 동일). 행은 씬에 미리 배치(자동 수집).
public class WorldBossRankBoard : MonoBehaviour
{
    [SerializeField] PlayFabLoginManager login;
    [SerializeField] string statisticName = "WorldBossDamage"; // WorldBossManager.statisticName과 일치시킬 것
    [SerializeField] int topCount = 50;

    [Header("내 행(맨 위 고정)")]
    [SerializeField] RankEntryUI myRow;
    [SerializeField] string myFallbackName = "명사수"; // DisplayName 없을 때 표기
    [SerializeField] Sprite myFace;                    // 내 행 얼굴(선택 — 🤠)

    [Header("TOP 1~50 리스트")]
    [SerializeField] RectTransform listRoot;
    [Tooltip("비우면 listRoot 자식에서 자동 수집")]
    [SerializeField] List<RankEntryUI> rows = new();

    [Header("얼굴(선택): PlayFabId 해시로 고정 배정")]
    [SerializeField] Sprite[] faces;

    float lastRefreshTime = -999f;
    bool hooked;

    void Awake()
    {
        if (login != null) login.onLogined.AddListener(OnLogined);

        if (rows.Count == 0 && listRoot != null)
            rows.AddRange(listRoot.GetComponentsInChildren<RankEntryUI>(true));
        rows.RemoveAll(r => r == null);
    }

    void OnDestroy()
    {
        if (login != null) login.onLogined.RemoveListener(OnLogined);
        if (hooked && WorldBossManager.Instance != null)
            WorldBossManager.Instance.onChanged.RemoveListener(OnManagerChanged);
    }

    void OnEnable() => Refresh();
    void Start() => Refresh(); // NavButtons는 패널을 끄지 않고 화면 밖으로 옮기므로 시작 시에도 시도

    void Update()
    {
        // 매니저 준비 후 1회 구독 — 베스트 갱신 시 보고 반영 텀을 두고 재조회
        if (!hooked && WorldBossManager.Instance != null)
        {
            WorldBossManager.Instance.onChanged.AddListener(OnManagerChanged);
            hooked = true;
        }
    }

    void OnLogined()
    {
        lastRefreshTime = -999f; //로그인 직후에는 무조건 갱신
        Refresh();
    }

    // 베스트 갱신 직후에는 PlayFab 통계 반영이 비동기라 잠깐 기다렸다 재조회
    void OnManagerChanged()
    {
        if (!isActiveAndEnabled) return;
        StopAllCoroutines();
        StartCoroutine(DelayedRefresh());
    }

    IEnumerator DelayedRefresh()
    {
        yield return new WaitForSeconds(1.5f);
        lastRefreshTime = -999f;
        Refresh();
    }

    public void Refresh()
    {
        RefreshMyRow(); // 내 행은 로컬 베스트 기준이라 로그인 전에도 "—" 표기 가능

        if (!PlayFabClientAPI.IsClientLoggedIn()) return;
        if (Time.unscaledTime - lastRefreshTime < 1f) return; //중복 호출 방지(OnEnable+Start 등)
        lastRefreshTime = Time.unscaledTime;

        FetchTop();
        FetchMyRank();
    }

    // TOP 1~50 — 일반 보드와 달리 1~3위 제외 없이 전부 나열
    void FetchTop()
    {
        PlayFabClientAPI.GetLeaderboard(
            new GetLeaderboardRequest
            {
                StatisticName = statisticName,
                StartPosition = 0,
                MaxResultsCount = topCount
            },
            result =>
            {
                int used = 0;
                foreach (var entry in result.Leaderboard)
                {
                    if (used >= rows.Count) break;

                    var row = rows[used];
                    row.gameObject.SetActive(true);
                    SetEntry(row, entry);
                    used++;
                }
                for (int i = used; i < rows.Count; i++)
                    rows[i].gameObject.SetActive(false);
            },
            e => Debug.LogError($"[WorldBossRank] TOP 조회 실패: {e.GenerateErrorReport()}"));
    }

    // 내 순위 조회 → 내 행 갱신(기록이 있을 때만 의미 있음)
    void FetchMyRank()
    {
        if (WorldBossManager.Instance == null || WorldBossManager.Instance.BestDamage <= 0) return;

        PlayFabClientAPI.GetLeaderboardAroundPlayer(
            new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = statisticName,
                MaxResultsCount = 1
            },
            result =>
            {
                string myId = login != null ? login.PlayFabId : null;
                foreach (var entry in result.Leaderboard)
                {
                    if (myId != null && entry.PlayFabId != myId) continue;
                    ApplyMyRow(entry.Position + 1, entry.StatValue, entry.DisplayName);
                    return;
                }
            },
            e => Debug.LogError($"[WorldBossRank] 내 순위 조회 실패: {e.GenerateErrorReport()}"));
    }

    // 내 행 기본 표기 — 기록 없으면 전부 "—"(프로토), 있으면 로컬 베스트로 우선 표기(서버 조회가 순위를 채움)
    void RefreshMyRow()
    {
        if (myRow == null) return;

        var mgr = WorldBossManager.Instance;
        if (mgr == null || mgr.BestDamage <= 0)
        {
            myRow.Set("—", myFallbackName, "—", true, myFace);
            myRow.SetReward("—");
        }
        else
        {
            myRow.Set("—", myFallbackName, NumberFormatLoader.Abbrev(mgr.BestDamage), true, myFace);
            myRow.SetReward("—"); // 순위 확정 전 — FetchMyRank가 채움
        }
    }

    void ApplyMyRow(int rank, long score, string displayName)
    {
        if (myRow == null) return;

        string name = string.IsNullOrEmpty(displayName) ? myFallbackName : displayName;
        myRow.Set(rank.ToString(), name, NumberFormatLoader.Abbrev(score), true, myFace);
        myRow.SetReward(RewardText(rank));
    }

    void SetEntry(RankEntryUI ui, PlayerLeaderboardEntry entry)
    {
        bool isMe = login != null && entry.PlayFabId == login.PlayFabId;
        string name = string.IsNullOrEmpty(entry.DisplayName) ? "이름없는 총잡이" : entry.DisplayName;

        ui.Set((entry.Position + 1).ToString(), name, NumberFormatLoader.Abbrev(entry.StatValue), isMe, PickFace(entry.PlayFabId));
        ui.SetReward(RewardText(entry.Position + 1));
    }

    // 순위 → "🔴400 🎫40" (시트 미로드 시 숨김)
    static string RewardText(int rank)
    {
        var loader = WorldBossRankRewardLoader.Instance;
        if (loader == null || !loader.IsLoaded) return null;

        var c = loader.GetForRank(rank);
        return c != null ? $"🔴{c.red} 🎫{c.ticket}" : null;
    }

    // PlayFabId로 항상 같은 얼굴이 나오게 결정적 해시로 선택(RankBoard 문법)
    Sprite PickFace(string playFabId)
    {
        if (faces == null || faces.Length == 0 || string.IsNullOrEmpty(playFabId)) return null;

        int hash = 0;
        foreach (char c in playFabId)
            hash = (hash * 31 + c) & 0x7FFFFFFF;

        return faces[hash % faces.Length];
    }
}
