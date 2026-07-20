using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// OnlineConfig 시트 로더. Id/Value/$Desc 키-값 구조.
// Value가 숫자면 configDict, 문자열(수식 스냅샷·스탯 키)이면 stringDict에 담긴다.
public class GameConfigLoader : ISheetLoader
{
    public static GameConfigLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=OnlineConfig";

    // 탭 미존재 시 구글이 비슷한 이름/첫 탭으로 폴백하므로, 이 키가 없으면 잘못된 탭으로 판정
    const string SENTINEL_KEY = "ClearGoldPerStage";

    private Dictionary<string, float> configDict
        = new Dictionary<string, float>();
    private Dictionary<string, string> stringDict
        = new Dictionary<string, string>();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public GameConfigLoader() { Instance = this; }

    public void Parse(string csv)
    {
        configDict.Clear();
        stringDict.Clear();

        var lines = csv.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var cols = SplitCsvLine(lines[i]);
            if (cols.Count < 2) continue;

            string key = TableLoaderTool.CleanString(cols[0]);
            string raw = TableLoaderTool.CleanString(cols[1]);
            if (string.IsNullOrEmpty(key)) continue;

            if (float.TryParse(raw, out float value))
                configDict[key] = value;
            else
                stringDict[key] = raw; // ClearGoldFormula(수식 스냅샷)·WeaponBountyStatId 등
        }

        if (!configDict.ContainsKey(SENTINEL_KEY))
        {
            Debug.LogError($"[GameConfig] '{SENTINEL_KEY}' 키가 없음 — OnlineConfig 탭이 스프레드시트에 없어 다른 탭(OfflineConfig/가이드)이 로드된 것으로 보임. 시트 업로드 확인 필요.");
            return; // IsLoaded false 유지 — 잘못된 값으로 게임이 돌지 않게 막는다
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"GameConfig Loaded: {configDict.Count} numeric, {stringDict.Count} string");
    }

    // 따옴표 안 쉼표를 필드 구분자로 취급하지 않는 CSV 한 줄 파서("" = 이스케이프된 따옴표)
    static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(c);
        }
        result.Add(sb.ToString());
        return result;
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

    public string GetString(string key)
    {
        if (stringDict.TryGetValue(key, out var value))
            return value;

        Debug.LogWarning($"Config string key not found: {key}");
        return string.Empty;
    }
}