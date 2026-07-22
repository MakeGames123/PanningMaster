using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// Achievement 시트 로더. 업적 래더(티어 6단, 보상=젬).
// 컬럼: Id/NameKo/LabelKo/EventKey/Judge/Tiers:1..6/Gems:1..6
public class AchievementLoader : ISheetLoader
{
    public static AchievementLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=Achievement";

    readonly Dictionary<string, AchievementData> dataDict = new();
    readonly List<string> order = new();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public AchievementLoader() { Instance = this; }

    public void Parse(string csv)
    {
        dataDict.Clear();
        order.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            UnityEngine.Debug.LogError("Achievement CSV 데이터가 비어있음");
            return;
        }

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 17) continue; // Id..Judge(5) + Tiers(6) + Gems(6)

            AchievementData d = new()
            {
                id = TableLoaderTool.CleanString(cols[0]),
                nameKo = TableLoaderTool.CleanString(cols[1]),
                labelKo = TableLoaderTool.CleanString(cols[2]),
                eventKey = TableLoaderTool.CleanString(cols[3]),
                judge = ParseJudge(cols[4]),
            };

            for (int t = 0; t < 6; t++)
            {
                string tierStr = TableLoaderTool.CleanString(cols[5 + t]);
                if (string.IsNullOrEmpty(tierStr)) continue; // 빈 티어는 없는 단계

                d.tiers.Add(ParseLong(tierStr));            // 1E+15 같은 지수 표기 지원
                d.gems.Add(TableLoaderTool.ToInt(cols[11 + t]));
            }

            if (string.IsNullOrEmpty(d.id) || d.tiers.Count == 0) continue;
            dataDict[d.id] = d;
            order.Add(d.id);
        }

        if (dataDict.Count == 0)
        {
            UnityEngine.Debug.LogError("[Achievement] 파싱된 행이 없음 — Achievement 탭이 스프레드시트에 있는지 확인 필요");
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        UnityEngine.Debug.Log($"Achievement Loaded: {dataDict.Count}");
    }

    static QuestJudge ParseJudge(string raw)
    {
        string s = TableLoaderTool.CleanString(raw);
        if (Enum.TryParse(s, true, out QuestJudge j)) return j;
        return QuestJudge.Counter;
    }

    static long ParseLong(string s)
        => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double v) ? (long)v : 0;

    public AchievementData Get(string id) => dataDict.TryGetValue(id, out var d) ? d : null;

    // 시트 순서대로 전체 목록
    public List<AchievementData> AllOrdered() => order.Select(id => dataDict[id]).ToList();

    public int Count => dataDict.Count;
}

[System.Serializable]
public class AchievementData
{
    public string id;          // 업적 키(kill/forge/power…)
    public string nameKo;      // 이름
    public string labelKo;     // 단위 라벨(클리어/탄환 뽑기 등)
    public string eventKey;    // QuestEventManager 카운터 키
    public QuestJudge judge;   // Counter=누적 / Absolute=현재값(전투력 등)
    public List<long> tiers = new(); // 단계별 목표값
    public List<int> gems = new();   // 단계별 보상 젬

    public int TierCount => tiers.Count;
}
