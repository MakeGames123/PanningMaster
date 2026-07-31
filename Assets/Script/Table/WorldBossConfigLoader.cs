using System;
using System.Collections.Generic;

// WorldBossConfig 시트 로더. v45 월드보스 상수(키-값 — GameConfigLoader 문법).
// Ghost* 키는 프로토 더미 참가자 전용 — 유니티는 PlayFab 실랭킹이라 미사용.
public class WorldBossConfigLoader : ISheetLoader
{
    public static WorldBossConfigLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=WorldBossConfig";

    // 탭 미존재 시 구글이 다른 탭으로 폴백하므로, 이 키가 없으면 잘못된 탭으로 판정
    const string SENTINEL_KEY = "BossHpMul";

    readonly Dictionary<string, float> configDict = new();
    readonly Dictionary<string, string> stringDict = new();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public WorldBossConfigLoader() { Instance = this; }

    public void Parse(string csv)
    {
        configDict.Clear();
        stringDict.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 2) continue;

            string key = TableLoaderTool.CleanString(cols[0]);
            string raw = TableLoaderTool.CleanString(cols[1]);
            if (string.IsNullOrEmpty(key)) continue;

            if (float.TryParse(raw, out float value)) configDict[key] = value;
            else stringDict[key] = raw; // ScoreRule 등 문자열 키
        }

        if (!configDict.ContainsKey(SENTINEL_KEY))
        {
            UnityEngine.Debug.LogError($"[WorldBossConfig] '{SENTINEL_KEY}' 키가 없음 — WorldBossConfig 탭이 스프레드시트에 있는지 확인 필요");
            return; // IsLoaded false 유지
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        UnityEngine.Debug.Log($"WorldBossConfig Loaded: {configDict.Count} numeric, {stringDict.Count} string");
    }

    // 폴백 내장 조회 — 시트 미로드/키 누락 시 프로토 v1.0.45 기본값으로 동작
    public float GetFloat(string key, float fallback)
        => configDict.TryGetValue(key, out var v) ? v : fallback;

    public int GetInt(string key, int fallback)
        => configDict.TryGetValue(key, out var v) ? UnityEngine.Mathf.RoundToInt(v) : fallback;

    public string GetString(string key, string fallback = "")
        => stringDict.TryGetValue(key, out var v) ? v : fallback;
}
