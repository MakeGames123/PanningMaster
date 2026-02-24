using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TierDataLoader : MonoBehaviour
{
    public static TierDataLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1uo6Tm2UDagmMJ09O3qIT6m4mfCTsakRTB5KVbSS0-DI/gviz/tq?tqx=out:csv&sheet=TierData";

    private Dictionary<int, TierData> tierDict
        = new Dictionary<int, TierData>();

    public event Action OnDataLoaded;

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
        var lines = csv.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var cols = lines[i].Split(',');

            if (cols.Length < 9) continue;

            TierData data = new TierData
            {
                tier = TableLoaderTool.ToInt(cols[0]),
                nameKR = TableLoaderTool.CleanString(cols[1]),
                nameEN = TableLoaderTool.CleanString(cols[2]),
                baseDmg = TableLoaderTool.ToFloat(cols[3]),
                craftSlots = TableLoaderTool.ToInt(cols[4]),
                craftCost = TableLoaderTool.ToInt(cols[5]),
                lvScale = TableLoaderTool.ToFloat(cols[6]),
                possScale = TableLoaderTool.ToFloat(cols[7]),
                colorHex = TableLoaderTool.CleanString(cols[8])
            };

            tierDict[data.tier] = data;
        }
        OnDataLoaded?.Invoke();

        Debug.Log($"TierData Loaded: {tierDict.Count}");
    }
    public List<T> ReturnColumn<T>(System.Func<TierData, T> selector)
    {
        List<T> list = new List<T>();

        foreach (var key in tierDict.Keys.OrderBy(k => k))
        {
            list.Add(selector(tierDict[key]));
        }

        return list;
    }
    public TierData GetTier(int tier)
    {
        if (tierDict.TryGetValue(tier, out var data))
            return data;

        return null;
    }
}
[System.Serializable]
public class TierData
{
    public int tier;
    public string nameKR;
    public string nameEN;

    public float baseDmg;
    public int craftSlots;
    public int craftCost;

    public float lvScale;
    public float possScale;

    public string colorHex;

    public Color Color
    {
        get
        {
            if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
                return color;

            return Color.white;
        }
    }
}