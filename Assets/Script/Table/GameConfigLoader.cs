using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameConfigLoader : MonoBehaviour, ITableLoader
{
    public static GameConfigLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1uo6Tm2UDagmMJ09O3qIT6m4mfCTsakRTB5KVbSS0-DI/gviz/tq?tqx=out:csv&sheet=BattleConfig";

    private Dictionary<string, float> configDict
        = new Dictionary<string, float>();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

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
            if (cols.Length < 2) continue;

            string key = TableLoaderTool.CleanString(cols[0]);
            float value = TableLoaderTool.ToFloat(cols[1]);

            configDict[key] = value;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"GameConfig Loaded: {configDict.Count}");
    }

    public float GetFloat(string key)
    {
        if (configDict.TryGetValue(key, out var value))
            return value;

        Debug.LogWarning($"Config key not found: {key}");
        return 0f;
    }

    public int GetInt(string key)
    {
        return Mathf.RoundToInt(GetFloat(key));
    }
}