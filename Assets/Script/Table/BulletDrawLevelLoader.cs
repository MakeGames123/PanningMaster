using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BulletDrawLevelLoader : ISheetLoader
{
    public static BulletDrawLevelLoader Instance { get; private set; }
    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=BulletDrawLevel";

    private Dictionary<int, LevelUpData> dataDict
        = new Dictionary<int, LevelUpData>();

    public event Action OnLoaded;
    public string Url => SHEET_URL;

    public BulletDrawLevelLoader() { Instance = this; }

    public void Parse(string csv)
    {
        dataDict.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length <= 1)
        {
            Debug.LogError("CSV 데이터가 비어있음");
            return;
        }

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');

            if (cols.Length < 3) continue;

            LevelUpData data = new LevelUpData();

            data.FromLv = TableLoaderTool.ToInt(cols[0]);
            data.ToLv = TableLoaderTool.ToInt(cols[1]);
            data.RequiredXP = TableLoaderTool.ToInt(cols[2]);

            dataDict[data.FromLv] = data;
        }
        OnLoaded?.Invoke();

        Debug.Log($"LevelUpData Loaded: {dataDict.Count}");
    }

    public int GetReqData(int fromLv)
    {
        if (dataDict.TryGetValue(fromLv, out var data))
            return data.RequiredXP;

        Debug.LogWarning($"LevelUpData 없음: {fromLv}");
        return 0;
    }
}
[System.Serializable]
public class LevelUpData
{
    public int FromLv;
    public int ToLv;
    public int RequiredXP;
}