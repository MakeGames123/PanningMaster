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

    #region Save

    public void SaveBulletsToServer()
    {
        List<BulletSaveData> saveList = new List<BulletSaveData>();

        foreach (var bullet in allBulletList.bulletInfos.Values)
        {
            // Count가 0인 건 굳이 저장 안 해도 됨 (선택)
            if (bullet.Count <= 0)
                continue;

            saveList.Add(bullet.ToSaveData());
        }

        BulletInventoryWrapper wrapper = new BulletInventoryWrapper
        {
            bullets = saveList
        };

        string json = JsonUtility.ToJson(wrapper);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { BULLET_SAVE_KEY, json }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("Bullet Save Success"),
            error => Debug.LogError("Bullet Save Failed: " + error.GenerateErrorReport()));
    }

    #endregion

    #region Load

    public void LoadBulletsFromServer()
    {
        var request = new GetUserDataRequest();

        PlayFabClientAPI.GetUserData(request,
            result =>
            {
                if (result.Data != null && result.Data.ContainsKey(BULLET_SAVE_KEY))
                {
                    string json = result.Data[BULLET_SAVE_KEY].Value;

                    BulletInventoryWrapper wrapper =
                        JsonUtility.FromJson<BulletInventoryWrapper>(json);

                    ApplyLoadedData(wrapper);
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