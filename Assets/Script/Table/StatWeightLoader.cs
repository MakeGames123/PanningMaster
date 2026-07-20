using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class StatWeightLoader : ISheetLoader
{
    public static StatWeightLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=StatWeights";

    public Dictionary<int, List<float>> weightDict
        = new Dictionary<int, List<float>>();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public StatWeightLoader() { Instance = this; }

    public void Parse(string csv)
    {
        var lines = csv.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var cols = lines[i].Split(',');
            if (cols.Length < 2) continue;

            int bulletTier = TableLoaderTool.ToInt(cols[0]);

            List<float> probs = new List<float>();

            for (int j = 1; j < cols.Length; j++)
            {
                probs.Add(TableLoaderTool.ToFloat(cols[j]));
            }

            weightDict[bulletTier] = probs;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"StatWeight Loaded: {weightDict.Count}");
    }

    public List<float> GetWeights(int bulletTier)
    {
        if (weightDict.TryGetValue(bulletTier, out var list))
            return list;

        Debug.LogWarning($"Weight not found for tier: {bulletTier}");
        return null;
    }
}