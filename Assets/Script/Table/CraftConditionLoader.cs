using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CraftConditionLoader : MonoBehaviour, ITableLoader
{
    public static CraftConditionLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1uo6Tm2UDagmMJ09O3qIT6m4mfCTsakRTB5KVbSS0-DI/gviz/tq?tqx=out:csv&sheet=CraftConditions";

    private Dictionary<string, CraftConditionData> conditionDict
        = new Dictionary<string, CraftConditionData>();

    public event Action OnLoaded;
    public bool IsLoaded { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

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
        var lines = csv.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var cols = lines[i].Split(',');
            if (cols.Length < 4) continue;

            CraftConditionData data = new CraftConditionData
            {
                condId = TableLoaderTool.CleanString(cols[0]),
                nameKR = TableLoaderTool.CleanString(cols[1]),
                multiplier = TableLoaderTool.ToFloat(cols[2]),
                detailType = TableLoaderTool.CleanString(cols[3]),
            };

            conditionDict[data.condId] = data;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"CraftCondition Loaded: {conditionDict.Count}");
    }

    public List<CraftConditionData> GetAll()
    {
        return new List<CraftConditionData>(conditionDict.Values);
    }
}
[System.Serializable]
public class CraftConditionData
{
    public string condId;
    public string nameKR;
    public float multiplier;
    public string detailType;
}