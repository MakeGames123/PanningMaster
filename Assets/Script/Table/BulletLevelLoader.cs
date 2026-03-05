using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BulletLevelLoader : MonoBehaviour, ITableLoader
{
    public static BulletLevelLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1uo6Tm2UDagmMJ09O3qIT6m4mfCTsakRTB5KVbSS0-DI/gviz/tq?tqx=out:csv&sheet=BulletLevelXP";

    private List<LevelData> levelList = new List<LevelData>();
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

        int cumulative = 0; // 누적값 직접 계산

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var cols = lines[i].Split(',');
            if (cols.Length < 3) continue;

            int from = TableLoaderTool.ToInt(cols[0]);
            int to = TableLoaderTool.ToInt(cols[1]);
            int required = TableLoaderTool.ToInt(cols[2]);

            // from ~ to 구간을 레벨 단위로 확장
            for (int lv = from; lv < to; lv++)
            {
                cumulative += required;

                LevelData data = new LevelData
                {
                    fromLv = lv,
                    toLv = lv + 1,
                    requiredXP = required,
                    cumulativeXP = cumulative
                };

                levelList.Add(data);
            }
        }

        levelList.Sort((a, b) => a.fromLv.CompareTo(b.fromLv));

        OnLoaded?.Invoke();

        Debug.Log($"LevelTable Expanded Loaded: {levelList.Count}");
    }
    public void GetProgress(int level, out int required)
    {
        LevelData currentLevel = null;
        currentLevel = levelList[level];
        required = currentLevel.requiredXP;
    }
    public int GetLevelByBulletCount(int bulletCount)
    {
        for (int i = levelList.Count - 1; i >= 0; i--)
        {
            if (bulletCount >= levelList[i].cumulativeXP)
                return levelList[i].toLv;
        }

        return 0;
    }
}
[System.Serializable]
public class LevelData
{
    public int fromLv;
    public int toLv;
    public int requiredXP;
    public int cumulativeXP;
}