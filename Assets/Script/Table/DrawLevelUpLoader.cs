using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DrawLevelUpLoader : MonoBehaviour
{
    public static DrawLevelUpLoader Instance { get; private set; }
    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1uo6Tm2UDagmMJ09O3qIT6m4mfCTsakRTB5KVbSS0-DI/gviz/tq?tqx=out:csv&sheet=ForgeLevelXP";

    private Dictionary<int, LevelUpData> dataDict
        = new Dictionary<int, LevelUpData>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(LoadSheet());
    }

    IEnumerator LoadSheet()
    {
        UnityWebRequest req = UnityWebRequest.Get(SHEET_URL);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            yield break;
        }

        ParseCSV(req.downloadHandler.text);
    }

    void ParseCSV(string csv)
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

        Debug.Log($"LevelUpData Loaded: {dataDict.Count}");
    }

    public LevelUpData GetData(int fromLv)
    {
        if (dataDict.TryGetValue(fromLv, out var data))
            return data;

        Debug.LogWarning($"LevelUpData 없음: {fromLv}");
        return null;
    }

    public int GetRequiredXP(int fromLv)
    {
        if(fromLv < 0) return 0; 
        int total = 0;

        for (int lv = 1; lv <= fromLv; lv++)
        {
            if (dataDict.TryGetValue(lv, out var data))
            {
                total += data.RequiredXP;
            }
        }

        return total;
    }
}
[System.Serializable]
public class LevelUpData
{
    public int FromLv;
    public int ToLv;
    public int RequiredXP;
}