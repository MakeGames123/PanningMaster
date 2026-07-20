using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class StatTableLoader : ISheetLoader
{
    public static StatTableLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=StatRanges";

    public Dictionary<string, List<StatRangeData>> statDict
        = new Dictionary<string, List<StatRangeData>>();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public StatTableLoader() { Instance = this; }

    public void Parse(string csv)
    {
        var lines = csv.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var cols = lines[i].Split(',');
            if (cols.Length < 4) continue;

            string statId = TableLoaderTool.CleanString(cols[0]);
            float min = TableLoaderTool.ToFloat(cols[2]) / 100;
            float max = TableLoaderTool.ToFloat(cols[3]) / 100;

            if (!statDict.ContainsKey(statId))
                statDict[statId] = new List<StatRangeData>();

            statDict[statId].Add(new StatRangeData
            {
                min = min,
                max = max
            });
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"StatTable Loaded: {statDict.Count}");
    }

    public List<StatRangeData> GetStat(string statId)
    {
        if (statDict.TryGetValue(statId, out var list))
            return list;

        Debug.LogWarning($"Stat not found: {statId}");
        return null;
    }
}
[System.Serializable]
public class StatRangeData
{
    public float min;
    public float max;
}