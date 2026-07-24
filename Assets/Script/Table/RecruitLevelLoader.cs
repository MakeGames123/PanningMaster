using System;
using System.Collections.Generic;

// RecruitLevel 시트 로더. 모집 레벨별 진입 누적 모집 수(프로토 CH_RXR).
// 컬럼: Id(모집 레벨)/CumulativeRecruits(이 레벨 진입에 필요한 누적 모집 수)
// 모집 레벨은 누적 모집 수에서 파생된다(무상태 — 세이브 마이그레이션 불요).
public class RecruitLevelLoader : ISheetLoader
{
    public static RecruitLevelLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=RecruitLevel";

    // (레벨, 진입 누적 모집 수) — 레벨 오름차순
    readonly List<(int level, int cumulative)> rows = new();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public RecruitLevelLoader() { Instance = this; }

    public void Parse(string csv)
    {
        rows.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            UnityEngine.Debug.LogError("RecruitLevel CSV 데이터가 비어있음");
            return;
        }

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 2) continue;
            if (string.IsNullOrEmpty(TableLoaderTool.CleanString(cols[0]))) continue;

            rows.Add((TableLoaderTool.ToInt(cols[0]), TableLoaderTool.ToInt(cols[1])));
        }

        rows.Sort((a, b) => a.level.CompareTo(b.level));

        if (rows.Count == 0)
        {
            UnityEngine.Debug.LogError("[RecruitLevel] 파싱된 행이 없음 — RecruitLevel 탭이 스프레드시트에 있는지 확인 필요");
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        UnityEngine.Debug.Log($"RecruitLevel Loaded: {rows.Count}");
    }

    public int MaxLevel => rows.Count > 0 ? rows[^1].level : 1;

    // 누적 모집 수 → 모집 레벨(도달한 마지막 행)
    public int GetRecruitLevel(int cumulativeRecruits)
    {
        int lv = 1;
        foreach (var r in rows)
            if (cumulativeRecruits >= r.cumulative) lv = r.level;
        return lv;
    }

    // 다음 레벨 진입까지 필요한 누적 모집 수(최대 레벨이면 -1)
    public int NextThreshold(int recruitLevel)
    {
        foreach (var r in rows)
            if (r.level == recruitLevel + 1) return r.cumulative;
        return -1;
    }

    public int Count => rows.Count;
}
