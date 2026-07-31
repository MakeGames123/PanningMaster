using System;
using System.Collections.Generic;
using System.Linq;

// WeaponGrade 시트 로더. 리볼버 13등급(E~MR).
// 컬럼: Id/Code/ColorHex/StatMul/SubSlotsBase/ElemFinalDmg ($로 시작하는 주석 열은 무시).
// StatMul = v37 ×2 균일 계단(E=1 ~ MR=4096) — 시트 미갱신(구 v35 계단) 상태로 쓰면 밸런스가 어긋나니 주의.
// ElemFinalDmg = v43 무기 속성: 매칭 속성 탄환 최종 데미지 +%(프로토 REV_EL_FD A안) — 열이 없으면 WeaponGrades가 하드코딩 폴백.
public class WeaponGradeLoader : ISheetLoader
{
    public static WeaponGradeLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=WeaponGrade";

    readonly Dictionary<int, WeaponGradeData> dataDict = new();

    public bool IsLoaded { get; private set; }
    public bool HasElemFinalDmg { get; private set; } // 시트에 ElemFinalDmg 열이 있는지(없으면 폴백 사용)
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public WeaponGradeLoader() { Instance = this; }

    public void Parse(string csv)
    {
        dataDict.Clear();
        HasElemFinalDmg = false;

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            UnityEngine.Debug.LogError("WeaponGrade CSV 데이터가 비어있음");
            return;
        }

        // 헤더명으로 컬럼 인덱스 매핑($ 주석 열이 끼어도 안전)
        var header = TableLoaderTool.SplitCsvLine(lines[0]).Select(TableLoaderTool.CleanString).ToList();
        int idIdx = header.IndexOf("Id");
        int codeIdx = header.IndexOf("Code");
        int colorIdx = header.IndexOf("ColorHex");
        int mulIdx = header.IndexOf("StatMul");
        int slotsIdx = header.IndexOf("SubSlotsBase");
        int elemIdx = header.IndexOf("ElemFinalDmg");
        if (idIdx < 0 || mulIdx < 0)
        {
            UnityEngine.Debug.LogError("[WeaponGrade] Id/StatMul 컬럼이 없음 — WeaponGrade 탭 확인 필요");
            return;
        }
        HasElemFinalDmg = elemIdx >= 0;

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count <= mulIdx) continue;

            WeaponGradeData d = new()
            {
                gradeId = TableLoaderTool.ToInt(cols[idIdx]),
                code = codeIdx >= 0 && codeIdx < cols.Count ? TableLoaderTool.CleanString(cols[codeIdx]) : "",
                colorHex = colorIdx >= 0 && colorIdx < cols.Count ? TableLoaderTool.CleanString(cols[colorIdx]) : "#ffffff",
                statMul = TableLoaderTool.ToFloat(cols[mulIdx]),
                subSlotsBase = slotsIdx >= 0 && slotsIdx < cols.Count ? TableLoaderTool.ToInt(cols[slotsIdx]) : 1,
                elemFinalDmg = elemIdx >= 0 && elemIdx < cols.Count ? TableLoaderTool.ToFloat(cols[elemIdx]) : 0f,
            };

            dataDict[d.gradeId] = d;
        }

        if (dataDict.Count == 0)
        {
            UnityEngine.Debug.LogError("[WeaponGrade] 파싱된 행이 없음 — WeaponGrade 탭이 스프레드시트에 있는지 확인 필요");
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        UnityEngine.Debug.Log($"WeaponGrade Loaded: {dataDict.Count} (ElemFinalDmg 열 {(HasElemFinalDmg ? "있음" : "없음 — 폴백 사용")})");
    }

    public WeaponGradeData Get(int gradeId) => dataDict.TryGetValue(gradeId, out var d) ? d : null;

    public float GetStatMul(int gradeId) => dataDict.TryGetValue(gradeId, out var d) ? d.statMul : 1f;

    public float GetElemFinalDmg(int gradeId) => dataDict.TryGetValue(gradeId, out var d) ? d.elemFinalDmg : 0f;

    public int Count => dataDict.Count;
}

[System.Serializable]
public class WeaponGradeData
{
    public int gradeId;      // 등급 인덱스(0=E ~ 12=MR)
    public string code;      // 등급 코드
    public string colorHex;  // UI 색
    public float statMul;    // 주스탯 배율(v37 ×2 계단)
    public int subSlotsBase; // 부옵 슬롯 기본치 = min(4, 1+floor(Id/2))
    public float elemFinalDmg; // v43 매칭 속성 탄환 최종 데미지 +%
}
