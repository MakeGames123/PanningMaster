using System;
using System.Collections.Generic;
using System.Linq;

// WorldBossRankReward 시트 로더. v45 월드보스 주간 랭킹 보상 컷.
// 컬럼: Id(컷 순번)/RankMax(이 순위 이하까지 적용)/Red(🔴)/Ticket(🎫).
// 위에서부터 rank <= RankMax 첫 매칭 적용, 마지막 행 = 참가 전원.
public class WorldBossRankRewardLoader : ISheetLoader
{
    public static WorldBossRankRewardLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=WorldBossRankReward";

    readonly List<WorldBossRankRewardData> list = new();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public WorldBossRankRewardLoader() { Instance = this; }

    public void Parse(string csv)
    {
        list.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            UnityEngine.Debug.LogError("WorldBossRankReward CSV 데이터가 비어있음");
            return;
        }

        // 탭 미존재 폴백 방지 — 헤더로 판정
        var header = lines[0].Split(',').Select(TableLoaderTool.CleanString).ToList();
        if (!header.Contains("RankMax"))
        {
            UnityEngine.Debug.LogError("[WorldBossRankReward] RankMax 컬럼이 없음 — WorldBossRankReward 탭 확인 필요");
            return;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 4) continue;

            list.Add(new WorldBossRankRewardData
            {
                id = TableLoaderTool.ToInt(cols[0]),
                rankMax = TableLoaderTool.ToInt(cols[1]),
                red = TableLoaderTool.ToInt(cols[2]),
                ticket = TableLoaderTool.ToInt(cols[3]),
            });
        }

        if (list.Count == 0)
        {
            UnityEngine.Debug.LogError("[WorldBossRankReward] 파싱된 행이 없음 — WorldBossRankReward 탭 확인 필요");
            return;
        }

        list.Sort((a, b) => a.rankMax.CompareTo(b.rankMax)); // 컷 오름차순 보장

        IsLoaded = true;
        OnLoaded?.Invoke();

        UnityEngine.Debug.Log($"WorldBossRankReward Loaded: {list.Count}");
    }

    // 순위 → 적용 컷(위에서부터 rank <= RankMax 첫 매칭, 없으면 마지막 컷 = 참가 전원)
    public WorldBossRankRewardData GetForRank(int rank)
    {
        foreach (var c in list)
            if (rank <= c.rankMax) return c;
        return list.Count > 0 ? list[list.Count - 1] : null;
    }

    public List<WorldBossRankRewardData> AllOrdered() => new(list);

    public int Count => list.Count;
}

[System.Serializable]
public class WorldBossRankRewardData
{
    public int id;      // 컷 순번
    public int rankMax; // 이 순위 이하까지 적용
    public int red;     // 🔴 무료 다이아
    public int ticket;  // 🎫 티켓
}
