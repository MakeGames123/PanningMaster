using System;
using System.Collections.Generic;

// WeaponCeiling 시트 로더. 최상위 등급 미등장 천장(피티).
// 컬럼: Id(등급 인덱스)/Code/PityCount — 상위 등급 우선 순서(시트 순서 유지: MR→LR→UR).
public class WeaponCeilingLoader : ISheetLoader
{
    public static WeaponCeilingLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=WeaponCeiling";

    readonly List<WeaponCeilingData> list = new();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public WeaponCeilingLoader() { Instance = this; }

    public void Parse(string csv)
    {
        list.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            UnityEngine.Debug.LogError("WeaponCeiling CSV 데이터가 비어있음");
            return;
        }

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 3) continue;

            list.Add(new WeaponCeilingData
            {
                gradeId = TableLoaderTool.ToInt(cols[0]),
                code = TableLoaderTool.CleanString(cols[1]),
                pityCount = TableLoaderTool.ToInt(cols[2]),
            });
        }

        if (list.Count == 0)
        {
            UnityEngine.Debug.LogError("[WeaponCeiling] 파싱된 행이 없음 — WeaponCeiling 탭이 스프레드시트에 있는지 확인 필요");
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        UnityEngine.Debug.Log($"WeaponCeiling Loaded: {list.Count}");
    }

    // 시트 순서(상위 등급 우선)대로 전체 목록
    public List<WeaponCeilingData> AllOrdered() => new(list);

    public int Count => list.Count;
}

[System.Serializable]
public class WeaponCeilingData
{
    public int gradeId;   // 등급 인덱스(12=MR, 11=LR, 10=UR)
    public string code;   // 등급 코드
    public int pityCount; // 미등장 보증 카운트
}
