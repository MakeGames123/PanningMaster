using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using UnityEngine;

// PVP 유령 유저 일괄 삭제 도구(개발용). 시더(PvpGhostSeeder)가 만든 GHOST_ 계정들을 지운다.
// 유령 계정으로 직접 로그인해 자기 자신을 지우는 방식은 실행 세션과 충돌해 실패하므로,
// 기기(본인) 계정 세션에서 순위표로 대상 PlayFabId를 모아 CloudScript로 제3자 삭제한다:
//   1) Power 순위표를 페이지 단위로 전부 조회해 PlayFabId 수집(본인 포함 전체)
//   2) batchSize개씩 CloudScript DeleteGhostBatch 호출
//      -> 서버가 각 대상의 CustomId가 GHOST_ 로 시작하는지 검증 후 server.DeletePlayer
//      -> 실계정(본인 포함)은 "not ghost"로 스킵되므로 안전하다
//   3) 개별 실패는 try/catch로 잡아 사유를 그대로 반환 -> Unity 콘솔에서 원인 확인 가능
// 사용법: 플레이 모드(로그인 후) 컴포넌트 우클릭 -> "Delete All Ghost Users" (에디터에선 F3)
// 사전 작업: CloudScript에 아래 DeleteGhostBatch 핸들러를 추가하고 배포(파일 하단 주석 참고).
//            기존 DeleteGhostSelf 핸들러는 지워도 된다.
// 참고: 통계 등록까지 못 가고 실패했던 유령은 순위표에 없어 남을 수 있다(게임에는 안 보임).
public class PvpGhostCleaner : MonoBehaviour
{
    [Header("수집")]
    [SerializeField] string sourceStatistic = "Power"; //유령이 등록된 순위표
    [SerializeField] int pageSize = 100;               //순위표 조회 페이지 크기(최대 100)

    [Header("삭제")]
    [SerializeField] string cloudFunction = "DeleteGhostBatch";
    [SerializeField] int batchSize = 5;              //호출당 삭제 수(계정 삭제는 무거워서 작게)
    [SerializeField] float delayBetweenCalls = 2f;   //API 스로틀 방지 간격(초)

    readonly List<string> targetIds = new();
    int deletedCount;
    int skippedCount;
    int failCount;
    bool running;

#if UNITY_EDITOR
    void Update()
    {
        // 디버그: F3 -> 유령 유저 일괄 삭제
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.f3Key.wasPressedThisFrame)
            DeleteAll();
    }
#endif

    [ContextMenu("Delete All Ghost Users")]
    public void DeleteAll()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[GhostCleaner] 플레이 모드에서 실행하세요");
            return;
        }
        if (running) return;
        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.LogError("[GhostCleaner] 로그인 후 실행하세요");
            return;
        }

        running = true;
        targetIds.Clear();
        deletedCount = 0;
        skippedCount = 0;
        failCount = 0;
        Debug.Log($"[GhostCleaner] {sourceStatistic} 순위표에서 대상 수집 시작");
        FetchPage(0);
    }

    // 1단계: 순위표를 페이지 단위로 전부 수집(유령/실계정 구분은 서버가 한다)
    void FetchPage(int start)
    {
        PlayFabClientAPI.GetLeaderboard(
            new GetLeaderboardRequest
            {
                StatisticName = sourceStatistic,
                StartPosition = start,
                MaxResultsCount = pageSize
            },
            result =>
            {
                targetIds.AddRange(result.Leaderboard.Select(e => e.PlayFabId));

                if (result.Leaderboard.Count >= pageSize)
                {
                    FetchPage(start + result.Leaderboard.Count); //다음 페이지
                }
                else
                {
                    Debug.Log($"[GhostCleaner] 총 {targetIds.Count}명 수집(실계정 포함). 유령 삭제 시작");
                    SendBatch(0);
                }
            },
            e =>
            {
                Debug.LogError($"[GhostCleaner] 순위표 조회 실패: {e.GenerateErrorReport()}");
                running = false;
            });
    }

    // 2단계: batchSize개씩 CloudScript로 삭제 요청
    void SendBatch(int offset)
    {
        if (offset >= targetIds.Count)
        {
            Finish();
            return;
        }

        List<string> batch = targetIds.Skip(offset).Take(batchSize).ToList();

        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = cloudFunction,
                FunctionParameter = new { ids = batch },
                GeneratePlayStreamEvent = false
            },
            result =>
            {
                if (result.Error != null) //핸들러 미배포/스크립트 자체 오류
                {
                    failCount += batch.Count;
                    Debug.LogError($"[GhostCleaner] CloudScript 오류: {result.Error.Error} / {result.Error.Message}");
                    DumpLogs(result);
                }
                else
                {
                    HandleBatchResult(result);
                }
                StartCoroutine(DelayNext(offset + batch.Count));
            },
            e =>
            {
                failCount += batch.Count;
                Debug.LogError($"[GhostCleaner] 배치 호출 실패(계속 진행): {e.GenerateErrorReport()}");
                StartCoroutine(DelayNext(offset + batch.Count));
            });
    }

    // 핸들러가 돌려준 개별 결과 집계: deleted / not ghost(스킵) / 오류(사유 로그)
    void HandleBatchResult(ExecuteCloudScriptResult result)
    {
        try
        {
            string json = PlayFabSimpleJson.SerializeObject(result.FunctionResult);
            DeleteBatchResult parsed = JsonUtility.FromJson<DeleteBatchResult>(json);

            foreach (DeleteEntryResult r in parsed.results)
            {
                if (r.deleted)
                {
                    deletedCount++;
                }
                else if (r.reason != null && r.reason.StartsWith("not ghost"))
                {
                    skippedCount++; //실계정 등
                }
                else
                {
                    failCount++;
                    Debug.LogError($"[GhostCleaner] {r.id} 삭제 실패: {r.reason}");
                }
            }
            Debug.Log($"[GhostCleaner] 진행: 삭제 {deletedCount} / 스킵 {skippedCount} / 실패 {failCount} (총 {targetIds.Count})");
        }
        catch (Exception ex)
        {
            failCount++;
            Debug.LogError($"[GhostCleaner] 결과 파싱 실패: {ex.Message}");
            DumpLogs(result);
        }
    }

    // CloudScript 실행 로그 출력(원인 진단용)
    void DumpLogs(ExecuteCloudScriptResult result)
    {
        if (result.Logs == null) return;
        foreach (LogStatement log in result.Logs)
        {
            string data = log.Data != null ? PlayFabSimpleJson.SerializeObject(log.Data) : "";
            Debug.LogError($"[GhostCleaner] CS {log.Level}: {log.Message} {data}");
        }
    }

    IEnumerator DelayNext(int nextOffset)
    {
        yield return new WaitForSeconds(delayBetweenCalls);
        SendBatch(nextOffset);
    }

    void Finish()
    {
        running = false;
        Debug.Log($"[GhostCleaner] 삭제 종료: 삭제 {deletedCount} / 스킵(실계정 등) {skippedCount} / 실패 {failCount}");
    }

    [Serializable]
    class DeleteBatchResult
    {
        public List<DeleteEntryResult> results;
    }

    [Serializable]
    class DeleteEntryResult
    {
        public string id;
        public bool deleted;
        public string reason;
    }
}

/* ===== PlayFab CloudScript(레거시)에 추가할 핸들러 =====
   Game Manager -> Automation -> Cloud Script(Legacy) 에 아래를 붙여넣고 배포(Deploy)하세요.
   (기존 DeleteGhostSelf 핸들러는 더 이상 안 쓰므로 지워도 됩니다)
   대상의 CustomId가 GHOST_ 로 시작할 때만 삭제하므로 실계정은 지워질 수 없습니다.

handlers.DeleteGhostBatch = function (args, context) {
    var ids = (args && args.ids) ? args.ids : [];
    var results = [];

    for (var i = 0; i < ids.length; i++) {
        var id = ids[i];
        try {
            var info = server.GetUserAccountInfo({ PlayFabId: id });
            var customId = (info.UserInfo && info.UserInfo.CustomIdInfo)
                ? info.UserInfo.CustomIdInfo.CustomId : null;

            if (!customId || customId.indexOf("GHOST_") !== 0) {
                results.push({ id: id, deleted: false, reason: "not ghost: " + customId });
                continue;
            }

            server.DeletePlayer({ PlayFabId: id });
            results.push({ id: id, deleted: true });
        } catch (ex) {
            results.push({ id: id, deleted: false, reason: JSON.stringify(ex) });
        }
    }

    return { results: results };
};
*/
