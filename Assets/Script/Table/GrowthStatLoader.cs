using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

// GrowthStat 시트 로더. Id/NameKo/Icon/BaseCost/CostMul/EffectPerLevel/Unit 컬럼을 읽는다.
// 성장 탭(스탯 레벨업)의 원본 데이터.
public class GrowthStatLoader : ISheetLoader
{
    public static GrowthStatLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=GrowthStat";

    // 탭 미존재 시 구글이 다른 탭으로 폴백하므로, 이 키가 없으면 잘못된 탭으로 판정
    const string SENTINEL_KEY = "Damage";

    readonly Dictionary<string, GrowthStatData> dataDict = new();
    readonly List<string> order = new(); // 시트 행 순서(UI 표시 순서)

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public GrowthStatLoader() { Instance = this; }

    public void Parse(string csv)
    {
        dataDict.Clear();
        order.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            Debug.LogError("GrowthStat CSV 데이터가 비어있음");
            return;
        }

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 7) continue;

            GrowthStatData d = new()
            {
                id = TableLoaderTool.CleanString(cols[0]),
                nameKo = TableLoaderTool.CleanString(cols[1]),
                icon = TableLoaderTool.CleanString(cols[2]),
                baseCost = TableLoaderTool.ToInt(cols[3]),
                costMul = TableLoaderTool.ToFloat(cols[4]),
                effectPerLevel = TableLoaderTool.ToFloat(cols[5]),
                unit = TableLoaderTool.CleanString(cols[6]),
            };

            if (string.IsNullOrEmpty(d.id)) continue;
            dataDict[d.id] = d;
            order.Add(d.id);
        }

        if (!dataDict.ContainsKey(SENTINEL_KEY))
        {
            Debug.LogError($"[GrowthStat] '{SENTINEL_KEY}' 키가 없음 — GrowthStat 탭이 스프레드시트에 없어 다른 탭이 로드된 것으로 보임. 시트 업로드 확인 필요.");
            dataDict.Clear();
            order.Clear();
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"GrowthStat Loaded: {dataDict.Count}");
    }

    public GrowthStatData Get(string id) => !string.IsNullOrEmpty(id) && dataDict.TryGetValue(id, out var d) ? d : null;

    // 시트 행 순서대로 전체 목록(UI 표시용)
    public List<GrowthStatData> AllOrdered() => order.Select(id => dataDict[id]).ToList();

    public int Count => dataDict.Count;
}

[System.Serializable]
public class GrowthStatData
{
    public string id;             // 스탯 키(atk/elem/critR/critD/gdmg/reload)
    public string nameKo;         // 표시명(공격력 등)
    public string icon;           // 아이콘(임시 이모지)
    public long baseCost;         // 기본 비용
    public float costMul;         // 비용 배율/Lv
    public float effectPerLevel;  // 레벨당 효과(step)
    public string unit;           // 단위(%)

    // 다음 레벨 비용 = floor(baseCost × costMul^level)
    public long CostAt(int level) => (long)System.Math.Floor(baseCost * System.Math.Pow(costMul, level));
}
