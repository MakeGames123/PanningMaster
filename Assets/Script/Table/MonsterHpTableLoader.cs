using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class MonsterHpTableLoader : MonoBehaviour, ITableLoader
{
    public static MonsterHpTableLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1uo6Tm2UDagmMJ09O3qIT6m4mfCTsakRTB5KVbSS0-DI/gviz/tq?tqx=out:csv&sheet=HPRateBrackets";

    private Dictionary<int, float> stageRotDict
        = new Dictionary<int, float>();
    private Dictionary<int, float> hpCache = new Dictionary<int, float>();

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

            int stageFrom = TableLoaderTool.ToInt(cols[1]);
            int stageTo = TableLoaderTool.ToInt(cols[2]);
            float rot = TableLoaderTool.ToFloat(cols[3]);

            // 🔥 351 ~ 999999 구간은 채우지 않음
            if (stageFrom >= 352)
                break;

            for (int stage = stageFrom; stage <= stageTo; stage++)
            {
                // 350까지만 채움
                if (stage >= 352)
                    break;

                stageRotDict[stage] = rot;
            }
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"StageBracket Loaded: {stageRotDict.Count}");
    }

    public float GetRot(int stage)
    {
        if (stageRotDict.TryGetValue(stage, out var rot))
            return rot;

        if (stage >= 351) return stageRotDict.Values.Last();

        return 1;
    }
    public float GetHP(int stage)
    {
        if (stage <= 1)
            return 10f;

        if (hpCache.TryGetValue(stage, out var cached))
            return cached;

        float prevHP = GetHP(stage - 1);

        float rot = GetRot(stage);

        float hp = prevHP * rot;

        // 50스테이지마다 3배
        if (stage % 50 == 0)
            hp *= 3f;

        hp = Mathf.Round(hp);

        hpCache[stage] = hp;


        return hp;
    }
}