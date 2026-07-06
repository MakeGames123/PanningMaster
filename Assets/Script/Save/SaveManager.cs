using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;

public class SaveManager : MonoBehaviour
{
    [SerializeField] AllBulletList allBulletList;

    private const string BULLET_SAVE_KEY = "BulletInventory";
    private const string DRAW_LEVEL_KEY = "DrawLevelData";
    private const string LAB_DATA_KEY = "LabData";

    #region Save

    //단일 탄환만 서버에 저장. CloudScript(UpdateBulletStats)에서 해당 bulletId의 stats만 병합
    public void SaveBulletToServer(BulletInfo info)
    {
        BulletSaveData data = info.ToSaveData();
        string bulletJson = JsonUtility.ToJson(data);

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "UpdateBulletStats",
            FunctionParameter = new { bulletJson = bulletJson },
            GeneratePlayStreamEvent = false
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result => Debug.Log("Bullet Stats Save Success"),
            error => Debug.LogError("Bullet Stats Save Failed: " + error.GenerateErrorReport()));
    }

    // 연구소 스킬트리 노드 레벨을 서버(UserData "LabData")에 저장
    public void SaveLabToServer(LabSaveData data)
    {
        string labJson = JsonUtility.ToJson(data);

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "UpdateLabData",
            FunctionParameter = new { labJson = labJson },
            GeneratePlayStreamEvent = false
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result => Debug.Log("Lab Save Success"),
            error => Debug.LogError("Lab Save Failed: " + error.GenerateErrorReport()));
    }

    #endregion

    #region Load

    public void LoadBulletsFromServer()
    {
        var request = new GetUserDataRequest();

        PlayFabClientAPI.GetUserData(request,
            result =>
            {
                // BulletInventory 로드
                if (result.Data != null && result.Data.ContainsKey(BULLET_SAVE_KEY))
                {
                    string json = result.Data[BULLET_SAVE_KEY].Value;

                    BulletInventoryWrapper wrapper =
                        JsonUtility.FromJson<BulletInventoryWrapper>(json);

                    ApplyLoadedData(wrapper);
                }

                // DrawLevelData 로드
                if (result.Data != null && result.Data.ContainsKey(DRAW_LEVEL_KEY))
                {
                    string json = result.Data[DRAW_LEVEL_KEY].Value;
                    
                    DrawLevelData drawData =
                        JsonUtility.FromJson<DrawLevelData>(json);

                    ApplyDrawLevel(drawData);
                }

                // LabData 로드
                if (result.Data != null && result.Data.ContainsKey(LAB_DATA_KEY))
                {
                    string json = result.Data[LAB_DATA_KEY].Value;

                    LabSaveData labData =
                        JsonUtility.FromJson<LabSaveData>(json);

                    ApplyLabData(labData);
                }

                //onComplete?.Invoke();
            },
            error => Debug.LogError("Bullet Load Failed: " + error.GenerateErrorReport()));
    }

    private void ApplyLoadedData(BulletInventoryWrapper wrapper)
    {
        if (wrapper == null || wrapper.bullets == null)
            return;

        foreach (var save in wrapper.bullets)
        {
            if (!allBulletList.bulletInfoSODic.ContainsKey(save.bulletId))
                continue;

            BulletInfoSO so = allBulletList.bulletInfoSODic[save.bulletId];

            BulletInfo loadedInfo =
                BulletInfo.FromSaveData(save, so);

            allBulletList.bulletInfos[save.bulletId] = loadedInfo;
        }

        allBulletList.LoadRefresh();
    }

    private void ApplyDrawLevel(DrawLevelData data)
    {
        if (data == null)
            return;

        DataManager.Instance.drawData.drawLevel = data.drawLevel;
        DataManager.Instance.drawData.drawExp = data.drawExp;

        DataManager.Instance.onDrawDataChanged.Invoke();
    }

    private void ApplyLabData(LabSaveData data)
    {
        if (data == null)
            return;

        if (LaboratoryManager.Instance != null)
            LaboratoryManager.Instance.ApplyLoaded(data);
    }

    #endregion
}

[Serializable]
public class BulletInventoryWrapper
{
    public List<BulletSaveData> bullets;
}

[Serializable]
public class BulletSaveData
{
    public int bulletId;
    public int level;
    public int count;
    public List<BulletStat> stats;
}

[Serializable]
public class DrawLevelData
{
    public int drawLevel;
    public int drawExp;
}