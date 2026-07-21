using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

public class DrawButton : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] Button button;
    [SerializeField] List<Image> multipleButtons;
    [SerializeField] DrawResult drawResult;
    public DataManager dataManager;//이벤트 할당용
    const int MaxDraw = -1; //Max 뽑기(보유 티켓 전부) 선택지
    List<int> multiple = new() { 1, 10, MaxDraw };
    int multipleIndex = 0;
    void Awake()
    {
        dataManager.onDrawDataChanged.AddListener(UpdateLevelText);
        button.onClick.AddListener(() => DrawBullet(GetDrawCount()));
        ChangeMultiple(0);
    }
    //현재 선택 배수의 실제 뽑기 횟수. Max는 보유 티켓 전부
    int GetDrawCount()
    {
        int m = multiple[multipleIndex];
        return m == MaxDraw ? DataManager.Instance.Ticket.GetValue() : m;
    }
    int pendingDrawCount; //서버 콜백에서 퀘스트 이벤트 발행에 쓸 이번 뽑기 수

    public void DrawBullet(int drawCount)
    {
        if (drawCount <= 0) return; //Max 뽑기인데 티켓이 없으면 요청 생략

        pendingDrawCount = drawCount;

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "DrawBullet",
            FunctionParameter = new
            {
                drawCount = drawCount
            },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(
            request,
            OnDrawSuccess,
            OnDrawError);
    }
    private void OnDrawSuccess(ExecuteCloudScriptResult result)
    {
        if (result.FunctionResult == null)
        {
            Debug.LogError("DrawBullet: FunctionResult is null");
            return;
        }

        var dict = result.FunctionResult as IDictionary<string, object>;

        if (dict == null)
        {
            Debug.LogError("DrawBullet: Result cast failed");
            return;
        }

        if (dict.ContainsKey("error"))
        {
            Debug.LogError("Draw Failed: " + result.FunctionResult);
            return;
        }

        DataManager.Instance.drawData.drawLevel = System.Convert.ToInt32(dict["drawLevel"]);
        DataManager.Instance.drawData.drawExp = System.Convert.ToInt32(dict["drawExp"]);
        DataManager.Instance.Ticket.ResetMinusPending(TicketUseType.Draw);

        //Debug.Log("Draw Success");
        var resultList = dict["results"] as IList<object>;
        Dictionary<int, DrawInfo> drawResult = new();

        foreach (var item in resultList)
        {
            var entry = item as IDictionary<string, object>;

            int bulletId = System.Convert.ToInt32(entry["bulletId"]);
            int gained = System.Convert.ToInt32(entry["gained"]);
            int finalCount = System.Convert.ToInt32(entry["finalCount"]);

            if (!drawResult.TryGetValue(bulletId, out var info))
            {
                //갱신 전 기존 레벨과 최종 카운트 기준 새 레벨을 비교해 상승 폭 계산
                int oldCount = AllBulletList.Instance.bulletInfos[bulletId].Count;
                int oldLevel = AllBulletList.Instance.bulletInfos[bulletId].Level;
                int newLevel = BulletLevelLoader.Instance.GetLevelByBulletCount(finalCount);

                info = new DrawInfo
                {
                    Id = bulletId,
                    Gained = gained,
                    Count = finalCount,
                    LevelUp = newLevel - oldLevel,
                    IsNew = oldCount <= 0, //뽑기 전 보유량이 0이면 새 탄환
                };
                drawResult[bulletId] = info;
            }

            //Debug.Log($"ID: {bulletId} Count: {finalCount} Level: {finalLevel}");
        }

        foreach (var data in drawResult.Values) //인포를 먼저 안전하게 갱신, 연출은 LevelUp으로 역산 처리
        {
            AllBulletList.Instance.UpdateBullet(data);
        }

        if (drawResult.Count > 0) this.drawResult.SetCondition(drawResult);

        //뽑은 탄환 수를 퀘스트·튜토리얼 각각의 이벤트 버스에 발행
        if (QuestEventManager.Instance != null)
            QuestEventManager.Instance.AddEvent("draw", pendingDrawCount);
        if (TutorialEventManager.Instance != null)
            TutorialEventManager.Instance.AddEvent("draw", pendingDrawCount);

        UpdateLevelText();
    }

    private void OnDrawError(PlayFabError error)
    {
        Debug.LogError("CloudScript Error: " + error.GenerateErrorReport());
    }
    public void ChangeMultiple(int index)
    {
        foreach (Image button in multipleButtons)
        {
            button.color = Color.white;
        }

        multipleButtons[index].color = Color.yellow;
        this.multipleIndex = index;
    }
    void UpdateLevelText()
    {
        int req = BulletDrawLevelLoader.Instance.GetReqData(DataManager.Instance.drawData.drawLevel);
        levelText.text = $"Lv.{DataManager.Instance.drawData.drawLevel} {DataManager.Instance.drawData.drawExp}/{req}";
    }
}
public struct DrawInfo
{
    public int Id;
    public int Count;
    public int Gained;
    public int LevelUp; //이번 뽑기로 오른 레벨 수 (0이면 레벨업 없음)
    public bool IsNew;  //이번 뽑기로 처음 획득한 탄환인지
}
