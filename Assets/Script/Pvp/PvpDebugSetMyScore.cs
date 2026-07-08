using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

// (임시/일회용) 내 계정의 PVP 점수를 순위표 targetRank(50)등 언저리에 꽂아 넣는 디버그 도구.
// 현재 targetRank등의 점수를 조회해 그 값 그대로 내 PvPRank 통계에 등록한다(동점 -> 50~51등 언저리).
// 사용법: 플레이 모드(로그인 후) 컴포넌트 우클릭 -> "Set My Score Around Rank" (에디터에선 F4)
// 순위표 UI 확인용이므로 테스트 끝나면 이 스크립트는 지워도 된다.
public class PvpDebugSetMyScore : MonoBehaviour
{
    [SerializeField] string statisticName = "PvPRank";
    [SerializeField] int targetRank = 50;   //이 등수 언저리로 들어간다
    [SerializeField] RankBoard board;    //등록 후 자동 새로고침(선택)

#if UNITY_EDITOR
    void Update()
    {
        // 디버그: F4 -> 내 점수 50등 언저리로 등록
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.f4Key.wasPressedThisFrame)
            SetMyScore();
    }
#endif

    [ContextMenu("Set My Score Around Rank")]
    public void SetMyScore()
    {
        if (!Application.isPlaying || !PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.LogWarning("[PvpDebug] 플레이 모드에서 로그인 후 실행하세요");
            return;
        }

        // 목표 등수의 현재 점수 조회 (Position은 0부터라 targetRank-1)
        PlayFabClientAPI.GetLeaderboard(
            new GetLeaderboardRequest
            {
                StatisticName = statisticName,
                StartPosition = Mathf.Max(0, targetRank - 1),
                MaxResultsCount = 1
            },
            result =>
            {
                if (result.Leaderboard.Count == 0)
                {
                    Debug.LogError($"[PvpDebug] 순위표에 {targetRank}등이 없습니다. 유령 유저를 먼저 넣거나 targetRank를 낮추세요");
                    return;
                }
                Register(result.Leaderboard[0].StatValue);
            },
            e => Debug.LogError($"[PvpDebug] 순위표 조회 실패: {e.GenerateErrorReport()}"));
    }

    void Register(int score)
    {
        PlayFabClientAPI.UpdatePlayerStatistics(
            new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate { StatisticName = statisticName, Value = score }
                }
            },
            _ =>
            {
                Debug.Log($"[PvpDebug] 내 {statisticName} 점수를 {score}점으로 등록 완료(현재 {targetRank}등과 동점)");
                if (board != null) board.Refresh(); //순위표 UI 새로고침
            },
            e => Debug.LogError($"[PvpDebug] 점수 등록 실패: {e.GenerateErrorReport()}"));
    }
}
