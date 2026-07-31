using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 순위표 UI 공용 보드. statisticName/scoreSuffix만 바꿔 PVP(PvPRank,"점") /
// 전투력(Power,"") / 스테이지(Stage,"층") 보드로 인스턴스를 여러 개 만들어 쓴다.
// 상단: TOP 3 + 내 순위 요약. 하단: "내 주변(±aroundCount)" / "TOP 50" 두 리스트 탭.
// 두 리스트는 각자 루트에 씬에서 미리 만들어둔 행(RankEntryUI)들을 갖고 있고,
// 탭 전환 시 행을 지우지 않고 루트 좌표만 옮긴다(활성 (0,0) / 비활성 (9999,0) - NavButtons 방식).
// 행은 생성/파괴하지 않는다: 순서대로 채우고, 남는 행은 비활성화, 행 수를 넘는 데이터는 생략.
// PlayFab 리더보드 2회 조회로 채운다:
//   GetLeaderboard(0~topCount)     -> TOP3 슬롯 + TOP 50 리스트(1~3위는 리스트에서 제외)
//   GetLeaderboardAroundPlayer(41) -> 내 주변 리스트 + 내 순위 요약
// 얼굴은 서버에 없으므로 PlayFabId 해시로 faces 배열에서 고정 배정한다(같은 유저 = 항상 같은 얼굴).
public class RankBoard : MonoBehaviour
{
    [SerializeField] PlayFabLoginManager login; //내 PlayFabId 판별 + 로그인 후 자동 갱신
    [SerializeField] string statisticName = "PvPRank";
    [SerializeField] string scoreSuffix = "점"; //점수 단위(PVP="점", 스테이지="층", 전투력은 비움)
    [SerializeField] int aroundCount = 20;      //내 위/아래로 가져올 인원
    [SerializeField] int topCount = 50;         //TOP 리스트 인원

    [Header("내 순위 요약")]
    [SerializeField] TextMeshProUGUI myRankText;  //"206위"
    [SerializeField] TextMeshProUGUI myScoreText; //"1000점"

    [Header("TOP 3 슬롯 (0=1위, 1=2위, 2=3위)")]
    [SerializeField] RankEntryUI[] top3Slots;

    [Header("내 주변 리스트")]
    [SerializeField] RectTransform aroundListRoot; //뷰 루트(스크롤 포함). (0,0)=활성, (9999,0)=비활성
    [Tooltip("비우면 aroundListRoot 자식에서 자동 수집")]
    [SerializeField] List<RankEntryUI> aroundRows = new(); //씬에 미리 만들어둔 행들

    [Header("TOP 50 리스트")]
    [SerializeField] RectTransform topListRoot;
    [Tooltip("비우면 topListRoot 자식에서 자동 수집")]
    [SerializeField] List<RankEntryUI> topRows = new();

    [Header("탭 버튼(선택)")]
    [SerializeField] Button aroundTabButton; //"내 주변"
    [SerializeField] Button topTabButton;    //"TOP 50"

    [Header("얼굴(선택): PlayFabId 해시로 고정 배정")]
    [SerializeField] Sprite[] faces;

    static readonly Vector2 hiddenPos = new(9999f, 0f);

    float lastRefreshTime = -999f;

    void Awake()
    {
        if (login != null) login.onLogined.AddListener(OnLogined);
        if (aroundTabButton != null) aroundTabButton.onClick.AddListener(ShowAround);
        if (topTabButton != null) topTabButton.onClick.AddListener(ShowTop);

        //행 리스트가 비어 있으면 각 루트 자식에서 자동 수집(씬에 미리 배치된 행)
        if (aroundRows.Count == 0 && aroundListRoot != null)
            aroundRows.AddRange(aroundListRoot.GetComponentsInChildren<RankEntryUI>(true));
        if (topRows.Count == 0 && topListRoot != null)
            topRows.AddRange(topListRoot.GetComponentsInChildren<RankEntryUI>(true));
        aroundRows.RemoveAll(r => r == null);
        topRows.RemoveAll(r => r == null);

        ShowAround(); //기본 탭: 내 주변
    }

    void OnDestroy()
    {
        if (login != null) login.onLogined.RemoveListener(OnLogined);
    }

    void OnEnable() => Refresh();

    void Start()
    {
        // NavButtons는 패널을 비활성화하지 않고 화면 밖으로 옮기므로 시작 시에도 시도
        Refresh();
    }

    void OnLogined()
    {
        lastRefreshTime = -999f; //로그인 직후에는 무조건 갱신
        Refresh();
    }

    // 탭 전환: 행은 그대로 두고 리스트 루트 좌표만 이동
    public void ShowAround() => SwitchList(true);
    public void ShowTop() => SwitchList(false);

    void SwitchList(bool around)
    {
        if (aroundListRoot != null) aroundListRoot.anchoredPosition = around ? Vector2.zero : hiddenPos;
        if (topListRoot != null) topListRoot.anchoredPosition = around ? hiddenPos : Vector2.zero;
    }

    // 순위표 다시 불러오기(새로고침 버튼에 연결해도 됨)
    public void Refresh()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;
        if (Time.unscaledTime - lastRefreshTime < 1f) return; //중복 호출 방지(OnEnable+Start 등)
        lastRefreshTime = Time.unscaledTime;

        FetchTop();
        FetchAround();
    }

    // TOP 조회 한 번으로 TOP3 슬롯과 TOP 50 리스트를 같이 채운다
    void FetchTop()
    {
        PlayFabClientAPI.GetLeaderboard(
            new GetLeaderboardRequest
            {
                StatisticName = statisticName,
                StartPosition = 0,
                MaxResultsCount = Mathf.Max(3, topCount)
            },
            result =>
            {
                ApplyTop3(result.Leaderboard);
                FillRows(topRows, result.Leaderboard);
            },
            e => Debug.LogError($"[RankBoard] TOP 조회 실패: {e.GenerateErrorReport()}"));
    }

    void FetchAround()
    {
        PlayFabClientAPI.GetLeaderboardAroundPlayer(
            new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = statisticName,
                MaxResultsCount = aroundCount * 2 + 1 //위 20 + 나 + 아래 20
            },
            result =>
            {
                FillRows(aroundRows, result.Leaderboard);
                UpdateMySummary(result.Leaderboard);
            },
            e => Debug.LogError($"[RankBoard] 내 주변 순위 조회 실패: {e.GenerateErrorReport()}"));
    }

    void ApplyTop3(List<PlayerLeaderboardEntry> entries)
    {
        if (top3Slots == null) return;

        for (int i = 0; i < top3Slots.Length; i++)
        {
            if (top3Slots[i] == null) continue;

            bool has = i < entries.Count;
            top3Slots[i].gameObject.SetActive(has);
            if (has) SetEntry(top3Slots[i], entries[i]);
        }
    }

    // 씬에 미리 만들어둔 행을 순서대로 채운다. 남는 행은 비활성화,
    // 준비된 행 수를 넘는 데이터는 생략. (TOP3는 상단 별도 표시라 제외)
    void FillRows(List<RankEntryUI> rows, List<PlayerLeaderboardEntry> entries)
    {
        int used = 0;
        foreach (PlayerLeaderboardEntry entry in entries)
        {
            if (entry.Position < 3) continue; //1~3위 제외
            if (used >= rows.Count) break;    //준비된 행 초과분은 생략

            RankEntryUI row = rows[used];
            row.gameObject.SetActive(true);
            SetEntry(row, entry);
            used++;
        }

        for (int i = used; i < rows.Count; i++)
            rows[i].gameObject.SetActive(false);
    }

    void UpdateMySummary(List<PlayerLeaderboardEntry> entries)
    {
        string myId = login != null ? login.PlayFabId : null;
        if (myId == null) return;

        foreach (PlayerLeaderboardEntry entry in entries)
        {
            if (entry.PlayFabId != myId) continue;

            if (myRankText != null) myRankText.text = $"{entry.Position + 1}위";
            if (myScoreText != null) myScoreText.text = $"{entry.StatValue}{scoreSuffix}";
            return;
        }
    }

    void SetEntry(RankEntryUI ui, PlayerLeaderboardEntry entry)
    {
        bool isMe = login != null && entry.PlayFabId == login.PlayFabId;
        string name = string.IsNullOrEmpty(entry.DisplayName) ? "이름없는 총잡이" : entry.DisplayName;

        ui.Set(entry.Position + 1, name, entry.StatValue, isMe, PickFace(entry.PlayFabId), scoreSuffix);
    }

    // PlayFabId로 항상 같은 얼굴이 나오게 결정적 해시로 선택
    Sprite PickFace(string playFabId)
    {
        if (faces == null || faces.Length == 0 || string.IsNullOrEmpty(playFabId)) return null;

        int hash = 0;
        foreach (char c in playFabId)
            hash = (hash * 31 + c) & 0x7FFFFFFF;

        return faces[hash % faces.Length];
    }
}
