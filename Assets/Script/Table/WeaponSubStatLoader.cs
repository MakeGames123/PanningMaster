using System;
using System.Collections.Generic;
using System.Linq;

// WeaponSubStat 시트 로더. 무기 부옵 스탯 정의.
// 컬럼: Id/NameKo/Icon/Step/Scope(Global=전역 버킷, Weapon=무기 전용 상황부 스탯).
// 부옵 롤 풀은 atk 제외(주스탯과 중복 — 프로토 v36d 확정).
public class WeaponSubStatLoader : ISheetLoader
{
    public static WeaponSubStatLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=WeaponSubStat";

    readonly Dictionary<string, WeaponSubStatData> dataDict = new();
    readonly List<string> order = new();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public WeaponSubStatLoader() { Instance = this; }

    public void Parse(string csv)
    {
        dataDict.Clear();
        order.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            UnityEngine.Debug.LogError("WeaponSubStat CSV 데이터가 비어있음");
            return;
        }

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 5) continue;

            WeaponSubStatData d = new()
            {
                id = TableLoaderTool.CleanString(cols[0]),
                nameKo = TableLoaderTool.CleanString(cols[1]),
                icon = TableLoaderTool.CleanString(cols[2]),
                step = TableLoaderTool.ToFloat(cols[3]),
                scope = TableLoaderTool.CleanString(cols[4]),
            };

            if (string.IsNullOrEmpty(d.id)) continue;
            dataDict[d.id] = d;
            order.Add(d.id);
        }

        if (dataDict.Count == 0)
        {
            UnityEngine.Debug.LogError("[WeaponSubStat] 파싱된 행이 없음 — WeaponSubStat 탭이 스프레드시트에 있는지 확인 필요");
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        UnityEngine.Debug.Log($"WeaponSubStat Loaded: {dataDict.Count}");
    }

    public WeaponSubStatData Get(string id) => dataDict.TryGetValue(id, out var d) ? d : null;

    // 시트 순서대로 전체 목록
    public List<WeaponSubStatData> AllOrdered() => order.Select(id => dataDict[id]).ToList();

    // 부옵 롤 풀 — 주스탯과 중복되는 atk 제외(프로토 v36d "공격력 2줄" 리포트 확정)
    public List<WeaponSubStatData> SubPool() => order.Where(id => id != "atk").Select(id => dataDict[id]).ToList();

    public int Count => dataDict.Count;
}

[System.Serializable]
public class WeaponSubStatData
{
    public string id;     // 스탯 키(elem/critR/…/wgold/wboss/wdgn)
    public string nameKo; // 이름
    public string icon;   // 아이콘 이모지
    public float step;    // 부옵 값 = step × (1+등급) × (1+rand)
    public string scope;  // Global=전역 버킷 / Weapon=무기 전용(상황부)
}
