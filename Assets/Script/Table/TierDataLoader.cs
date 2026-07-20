using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TierDataLoader : ISheetLoader
{
    public static TierDataLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=BulletTier";

    private Dictionary<int, TierData> tierDict
        = new Dictionary<int, TierData>();

    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public TierDataLoader() { Instance = this; }

    public void Parse(string csv)
    {

        var lines = csv.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var cols = lines[i].Split(',');

            if (cols.Length < 8) continue;

            TierData data = new TierData
            {
                tier = TableLoaderTool.ToInt(cols[0]),
                nameKR = TableLoaderTool.CleanString(cols[1]),
                baseDmg = TableLoaderTool.ToFloat(cols[2]),
                craftSlots = TableLoaderTool.ToInt(cols[3]),
                craftCost = TableLoaderTool.ToInt(cols[4]),
                lvScale = TableLoaderTool.ToFloat(cols[5]),
                possScale = TableLoaderTool.ToFloat(cols[6]),
                colorHex = TableLoaderTool.CleanString(cols[7])
            };

            tierDict[data.tier] = data;
        }
        OnLoaded?.Invoke();

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

    public float baseDmg;
    public int craftSlots;
    public long craftCost;

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