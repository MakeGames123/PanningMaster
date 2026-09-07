using System;
using System.Collections.Generic;

// DungeonConfig 시트 로더. v47 던전 상수(키-값 — WorldBossConfigLoader 문법).
// 등반 HP 곡선·열쇠 지급·열차 강탈 🎫·황금 마차·추가 입장·소탕 등 24키.
public class DungeonConfigLoader : ISheetLoader
{
    public static DungeonConfigLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=DungeonConfig";

    // 탭 미존재 시 구글이 다른 탭으로 폴백하므로, 이 키가 없으면 잘못된 탭으로 판정
    const string SENTINEL_KEY = "ClimbHpMul";

    readonly Dictionary<string, float> configDict = new();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public DungeonConfigLoader() { Instance = this; }

    public void Parse(string csv)
    {
        configDict.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 2) continue;

            string key = TableLoaderTool.CleanString(cols[0]);
            string raw = TableLoaderTool.CleanString(cols[1]);
            if (string.IsNullOrEmpty(key)) continue;

            if (float.TryParse(raw, out float value)) configDict[key] = value;
        }

        if (!configDict.ContainsKey(SENTINEL_KEY))
        {
            UnityEngine.Debug.LogError($"[DungeonConfig] '{SENTINEL_KEY}' 키가 없음 — DungeonConfig 탭이 스프레드시트에 있는지 확인 필요");
            return; // IsLoaded false 유지
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        UnityEngine.Debug.Log($"DungeonConfig Loaded: {configDict.Count}");
    }

    // 폴백 내장 조회 — 시트 미로드/키 누락 시 프로토 v1.0.47 기본값으로 동작
    public float GetFloat(string key, float fallback)
        => configDict.TryGetValue(key, out var v) ? v : fallback;

    public int GetInt(string key, int fallback)
        => configDict.TryGetValue(key, out var v) ? UnityEngine.Mathf.RoundToInt(v) : fallback;
}
