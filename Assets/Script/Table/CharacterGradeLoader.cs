using System;
using System.Collections.Generic;

// ChracterGrade 시트 로더. 캐릭터 6단 등급(일반~신화) 정의.
// 컬럼: Id/NameKo/ColorHex/Prob/LevelCap/DebutRecruitLv/PowerMul
// ※ 시트 탭명이 'ChracterGrade'(오타 철자)로 생성돼 있어 URL도 그 철자를 따른다.
public class CharacterGradeLoader : ISheetLoader
{
    public static CharacterGradeLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=CharacterGrade";

    readonly List<CharacterGradeData> list = new(); // 등급 인덱스 순(0=일반 ~ 5=신화)

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public CharacterGradeLoader() { Instance = this; }

    public void Parse(string csv)
    {
        list.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            UnityEngine.Debug.LogError("ChracterGrade CSV 데이터가 비어있음");
            return;
        }

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 7) continue;

            list.Add(new CharacterGradeData
            {
                id = TableLoaderTool.ToInt(cols[0]),
                nameKo = TableLoaderTool.CleanString(cols[1]),
                colorHex = TableLoaderTool.CleanString(cols[2]),
                prob = TableLoaderTool.ToFloat(cols[3]),
                levelCap = TableLoaderTool.ToInt(cols[4]),
                debutRecruitLv = TableLoaderTool.ToInt(cols[5]),
                powerMul = TableLoaderTool.ToFloat(cols[6]),
            });
        }

        list.Sort((a, b) => a.id.CompareTo(b.id));

        if (list.Count == 0)
        {
            UnityEngine.Debug.LogError("[ChracterGrade] 파싱된 행이 없음 — ChracterGrade 탭이 스프레드시트에 있는지 확인 필요");
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        UnityEngine.Debug.Log($"ChracterGrade Loaded: {list.Count}");
    }

    public CharacterGradeData Get(int gradeId)
        => gradeId >= 0 && gradeId < list.Count ? list[gradeId] : null;

    public List<CharacterGradeData> AllOrdered() => new(list);

    public int Count => list.Count;
}

[System.Serializable]
public class CharacterGradeData
{
    public int id;             // 등급 인덱스(0=일반 ~ 5=신화)
    public string nameKo;      // 등급명
    public string colorHex;    // 등급색
    public float prob;         // 정상 창 확률(%) — 데뷔 전 등급 0 처리 후 잔여 정규화
    public int levelCap;       // 등급 기본 레벨캡(해방 ★당 +5)
    public int debutRecruitLv; // 이 모집 레벨부터 풀에 등장
    public float powerMul;     // 위력(곱연산) 등급 배수
}
