using System;
using System.Collections.Generic;

// WeaponGradeWindow 시트 로더. 뽑기 레벨별 등급 확률 창.
// 컬럼: Id(minLevel)/Prob:E..Prob:MR(13등급) — 각 행 합 100, 레벨업 = 창이 위로(상위 데뷔·하위 단종).
public class WeaponGradeWindowLoader : ISheetLoader
{
    public static WeaponGradeWindowLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=WeaponGradeWindow";

    public const int GradeCount = 13; // E~MR

    // (minLevel, 확률 13개) — minLevel 오름차순
    readonly List<(int minLevel, float[] probs)> rows = new();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public WeaponGradeWindowLoader() { Instance = this; }

    public void Parse(string csv)
    {
        rows.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            UnityEngine.Debug.LogError("WeaponGradeWindow CSV 데이터가 비어있음");
            return;
        }

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 1 + GradeCount) continue; // Id + 13등급 확률

            int minLevel = TableLoaderTool.ToInt(cols[0]);
            var probs = new float[GradeCount];
            for (int g = 0; g < GradeCount; g++)
                probs[g] = TableLoaderTool.ToFloat(cols[1 + g]);

            rows.Add((minLevel, probs));
        }

        rows.Sort((a, b) => a.minLevel.CompareTo(b.minLevel));

        if (rows.Count == 0)
        {
            UnityEngine.Debug.LogError("[WeaponGradeWindow] 파싱된 행이 없음 — WeaponGradeWindow 탭이 스프레드시트에 있는지 확인 필요");
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        UnityEngine.Debug.Log($"WeaponGradeWindow Loaded: {rows.Count}");
    }

    // 해당 뽑기 레벨의 확률 창(minLevel <= lv 인 마지막 행)
    public float[] GetWindow(int drawLevel)
    {
        if (rows.Count == 0) return null;

        var probs = rows[0].probs;
        foreach (var r in rows)
        {
            if (drawLevel >= r.minLevel) probs = r.probs;
            else break;
        }
        return probs;
    }

    public int Count => rows.Count;
}
