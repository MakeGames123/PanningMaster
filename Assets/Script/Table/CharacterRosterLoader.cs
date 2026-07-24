using System;
using System.Collections.Generic;
using System.Linq;

// ChracterRoster 시트 로더. 모집 가능한 캐릭터(총잡이) 풀 정의.
// 컬럼: Id/NameKo/Emoji/Grade/Passive:StatId/Passive:Base/$ProtoKey($ = 주석 컬럼, 무시)
// ※ 시트 탭명이 'ChracterRoster'(오타 철자)로 생성돼 있어 URL도 그 철자를 따른다.
public class CharacterRosterLoader : ISheetLoader
{
    public static CharacterRosterLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=CharacterRoster";

    readonly Dictionary<string, CharacterRosterData> dataDict = new();
    readonly List<string> order = new();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public CharacterRosterLoader() { Instance = this; }

    public void Parse(string csv)
    {
        dataDict.Clear();
        order.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            UnityEngine.Debug.LogError("ChracterRoster CSV 데이터가 비어있음");
            return;
        }

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 6) continue;

            CharacterRosterData d = new()
            {
                id = TableLoaderTool.CleanString(cols[0]),
                nameKo = TableLoaderTool.CleanString(cols[1]),
                emoji = TableLoaderTool.CleanString(cols[2]),
                grade = TableLoaderTool.ToInt(cols[3]),
                passiveStatId = TableLoaderTool.CleanString(cols[4]),
                passiveBase = TableLoaderTool.ToFloat(cols[5]),
            };

            if (string.IsNullOrEmpty(d.id)) continue;
            dataDict[d.id] = d;
            order.Add(d.id);
        }

        if (dataDict.Count == 0)
        {
            UnityEngine.Debug.LogError("[ChracterRoster] 파싱된 행이 없음 — ChracterRoster 탭이 스프레드시트에 있는지 확인 필요");
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        UnityEngine.Debug.Log($"ChracterRoster Loaded: {dataDict.Count}");
    }

    public CharacterRosterData Get(string id) => dataDict.TryGetValue(id, out var d) ? d : null;

    // 시트 순서대로 전체 목록
    public List<CharacterRosterData> AllOrdered() => order.Select(id => dataDict[id]).ToList();

    // 해당 등급의 캐릭터들(시트 순서 유지 — 천장 미보유 우선 선택의 결정론 기준)
    public List<CharacterRosterData> ByGrade(int grade)
        => order.Select(id => dataDict[id]).Where(d => d.grade == grade).ToList();

    public int Count => dataDict.Count;
}

[System.Serializable]
public class CharacterRosterData
{
    public string id;            // 캐릭터 키(rusty/mae/…)
    public string nameKo;        // 이름
    public string emoji;         // 아이콘 이모지
    public int grade;            // 등급 인덱스(0=일반 ~ 5=신화)
    public string passiveStatId; // 고유 패시브 스탯(StatType 이름 — Damage는 위력 배율로 전환, BossDamage는 미적용)
    public float passiveBase;    // 패시브 기본값(값 = base × 레벨)
}
