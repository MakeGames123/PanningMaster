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
    public AllBulletList allBulletList;
    List<int> multiple = new() { 1, 10, 50 };
    int multipleIndex = 0;
    int drawCount = 0;
    void Awake()
    {
        button.onClick.AddListener(DrawBullet);
        ChangeMultiple(0);
    }
    public void DrawBullet()
    {
        int level = DataManager.Instance.drawLevel;
        DrawData currentData = DrawPercentageLoader.Instance.ReturnData(level);

        DrawBullet(multiple[multipleIndex]);
        /*
                if (DataManager.Instance.UseTicket(multiple[multipleIndex]))
                {
                    Dictionary<int, DrawInfo> drawResult = new();

                    for (int i = 0; i < multiple[multipleIndex]; i++)
                    {
                        int tier = GetRandomTier(currentData);
                        var result = allBulletList.DrawBullet(tier);

                        int id = result.Item1;
                        bool isLevelUp = result.Item2;

                        if (!drawResult.TryGetValue(id, out var info))
                        {
                            info = new DrawInfo();
                            drawResult[id] = info;
                        }

                        info.TotalCount++;

                        if (isLevelUp)
                            info.LevelUpCount++;
                    }

                    if (drawResult.Count > 0) this.drawResult.SetCondition(drawResult);
                }
                */
    }
    public void DrawBullet(int drawCount)
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "DrawBullet",
            FunctionParameter = new
            {
                drawCount = 3
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

        DataManager.Instance.drawLevel = System.Convert.ToInt32(dict["drawLevel"]);
        DataManager.Instance.Ticket.ResetMinusPending(TicketUseType.Draw);

        Debug.Log("Draw Success");
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
                    Gained = gained,
                    Count = finalCount,
                    Level = finalLevel
                };
                drawResult[bulletId] = info;
            }

            Debug.Log($"ID: {bulletId} Count: {finalCount} Level: {finalLevel}");
        }

        if (drawResult.Count > 0) this.drawResult.SetCondition(drawResult);

        // TODO:
        // 여기서 UI 반영, 연출, 인벤 갱신 등 처리
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
        int currentCount = drawCount - DrawLevelUpLoader.Instance.GetRequiredXP(DataManager.Instance.drawLevel - 1);
        int req = DrawLevelUpLoader.Instance.GetRequiredXP(DataManager.Instance.drawLevel) - DrawLevelUpLoader.Instance.GetRequiredXP(DataManager.Instance.drawLevel - 1);
        levelText.text = $"Lv.{DataManager.Instance.drawLevel} {currentCount}/{req}";
    }
}
public class DrawInfo
{
    public int Level;
    public int Count;
    public int Gained;
}
