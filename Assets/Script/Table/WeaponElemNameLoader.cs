using System;
using System.Collections.Generic;
using System.Linq;

// WeaponElemName 시트 로더. v43b 무기 명명 = 속성×등급 사다리(프로토 REV_EL_NM).
// 행 = 등급(Id 0=E ~ 12=MR), 열 = Name:fire ~ Name:dark(속성별 이름).
// 표기는 "[등급코드] 사다리이름"(예: [MR] 라그나로크) — 조립은 WeaponGrades.DisplayName이 담당.
public class WeaponElemNameLoader : ISheetLoader
{
    public static WeaponElemNameLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=WeaponElemName";

    // 속성 Id → (등급 인덱스 → 이름)
    readonly Dictionary<string, Dictionary<int, string>> names = new();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public WeaponElemNameLoader() { Instance = this; }

    public void Parse(string csv)
    {
        names.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            UnityEngine.Debug.LogError("WeaponElemName CSV 데이터가 비어있음");
            return;
        }

        // 헤더의 "Name:속성" 열들을 속성 Id → 컬럼 인덱스로 매핑
        var header = TableLoaderTool.SplitCsvLine(lines[0]).Select(TableLoaderTool.CleanString).ToList();
        int idIdx = header.IndexOf("Id");
        Dictionary<string, int> elemCols = new();
        for (int c = 0; c < header.Count; c++)
        {
            if (header[c].StartsWith("Name:"))
                elemCols[header[c].Substring("Name:".Length)] = c;
        }

        if (idIdx < 0 || elemCols.Count == 0)
        {
            UnityEngine.Debug.LogError("[WeaponElemName] Id/Name:속성 컬럼이 없음 — WeaponElemName 탭이 스프레드시트에 있는지 확인 필요");
            return;
        }

        foreach (var kv in elemCols) names[kv.Key] = new Dictionary<int, string>();

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count <= idIdx) continue;

            int grade = TableLoaderTool.ToInt(cols[idIdx]);
            foreach (var kv in elemCols)
            {
                if (kv.Value >= cols.Count) continue;
                string n = TableLoaderTool.CleanString(cols[kv.Value]);
                if (!string.IsNullOrEmpty(n)) names[kv.Key][grade] = n;
            }
        }

        if (names.Values.All(d => d.Count == 0))
        {
            UnityEngine.Debug.LogError("[WeaponElemName] 파싱된 행이 없음 — WeaponElemName 탭 확인 필요");
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        UnityEngine.Debug.Log($"WeaponElemName Loaded: 속성 {names.Count}종 × 등급 {names.Values.Max(d => d.Count)}단");
    }

    // 속성×등급 이름(없으면 null — 호출부가 티어명 폴백)
    public string Get(string elementId, int grade)
        => !string.IsNullOrEmpty(elementId)
           && names.TryGetValue(elementId, out var byGrade)
           && byGrade.TryGetValue(grade, out var n) ? n : null;
}
