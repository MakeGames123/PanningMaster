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
    List<int> multiple = new() { 1, 10, 50 };
    int multipleIndex = 0;
    void Awake()
    {
        dataManager.onDrawDataChanged.AddListener(UpdateLevelText);
        button.onClick.AddListener(() => DrawBullet(multiple[multipleIndex]));
        ChangeMultiple(0);
    }
    public void DrawBullet(int drawCount)
    {
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
            int finalLevel = System.Convert.ToInt32(entry["finalLevel"]);

            if (!drawResult.TryGetValue(bulletId, out var info))
            {
                info = new DrawInfo
                {
                    Id = bulletId,
                    Gained = gained,
                    Count = finalCount,
                    Level = finalLevel
                };
                drawResult[bulletId] = info;
            }

            //Debug.Log($"ID: {bulletId} Count: {finalCount} Level: {finalLevel}");
        }

        if (drawResult.Count > 0) this.drawResult.SetCondition(drawResult);

        foreach (var data in drawResult.Values) //뽑기 결과에서 기존 인포값 사용해야 하므로 연출 뒤에 인포 갱신
        {
            AllBulletList.Instance.AddBullet(data);
        }

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
        int req = DrawLevelUpLoader.Instance.GetReqData(DataManager.Instance.drawData.drawLevel);
        levelText.text = $"Lv.{DataManager.Instance.drawData.drawLevel} {DataManager.Instance.drawData.drawExp}/{req}";
    }
}
public class DrawInfo
{
    public int Id;
    public int Level;
    public int Count;
    public int Gained;
}
