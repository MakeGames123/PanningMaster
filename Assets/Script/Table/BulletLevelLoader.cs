using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BulletLevelLoader : ISheetLoader
{
    public static BulletLevelLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=BulletLevelXP";

    private List<LevelData> levelList = new List<LevelData>();
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public BulletLevelLoader() { Instance = this; }

    public void Parse(string csv)
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
            // 시트의 requiredXP는 구간 상한(마지막 레벨 to-1 -> to)의 요구치이므로,
            // 구간 안에서는 레벨이 낮을수록 1씩 줄여서 램프 형태로 풀어준다.
            for (int lv = from; lv < to; lv++)
            {
                int req = required - (to - 1 - lv);
                cumulative += req;

                LevelData data = new LevelData
                {
                    fromLv = lv,
                    toLv = lv + 1,
                    requiredXP = req,
                    cumulativeXP = cumulative
                };

                levelList.Add(data);
            }
        }

        levelList.Sort((a, b) => a.fromLv.CompareTo(b.fromLv));

        OnLoaded?.Invoke();

        Debug.Log($"LevelTable Expanded Loaded: {levelList.Count}");
    }
    public void GetProgress(int level, out (int, int) data)
    {
        data.Item1 = levelList[level].requiredXP;
        data.Item2 = level == 0 ? 0 : levelList[level - 1].cumulativeXP;
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

    //해당 레벨이 되기 위한 최소 탄환 개수(GetLevelByBulletCount의 역함수). 테이블 범위 밖이면 -1
    public int GetMinCountForLevel(int level)
    {
        if (level <= 0) return 0;

        foreach (LevelData data in levelList)
            if (data.toLv == level) return data.cumulativeXP;

        return -1;
    }

    //테이블이 지원하는 최대 레벨
    public int MaxLevel => levelList.Count > 0 ? levelList[levelList.Count - 1].toLv : 0;
}
[System.Serializable]
public class LevelData
{
    public int fromLv;
    public int toLv;
    public int requiredXP;
    public int cumulativeXP;
}