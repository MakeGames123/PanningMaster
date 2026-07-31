using System;
using System.Collections.Generic;
using System.Linq;

// WorldBossOutlaw 시트 로더. v45 월드보스 주간 로테이션 보스 6종.
// 컬럼: Id(로테이션 인덱스 = 주차%행수)/Element(Element.Id)/NameKo/Face(이모지)
//      /ElemMul(보스별 속성 약점 배율 — 열 없거나 0이면 WorldBossConfig.ElemDmgMul 폴백).
public class WorldBossOutlawLoader : ISheetLoader
{
    public static WorldBossOutlawLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=WorldBossOutlaw";

    readonly List<WorldBossOutlawData> list = new();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public WorldBossOutlawLoader() { Instance = this; }

    public void Parse(string csv)
    {
        list.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            UnityEngine.Debug.LogError("WorldBossOutlaw CSV 데이터가 비어있음");
            return;
        }

        // 탭 미존재 시 구글이 다른 탭으로 폴백하므로 헤더로 판정
        var header = lines[0].Split(',').Select(TableLoaderTool.CleanString).ToList();
        if (!header.Contains("Element") || !header.Contains("Face"))
        {
            UnityEngine.Debug.LogError("[WorldBossOutlaw] Element/Face 컬럼이 없음 — WorldBossOutlaw 탭이 스프레드시트에 있는지 확인 필요");
            return;
        }
        int elemMulIdx = header.IndexOf("ElemMul"); // 구버전 탭(열 없음) 호환 — -1이면 폴백 사용

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 4) continue;

            list.Add(new WorldBossOutlawData
            {
                rotationIndex = TableLoaderTool.ToInt(cols[0]),
                element = TableLoaderTool.CleanString(cols[1]),
                nameKo = TableLoaderTool.CleanString(cols[2]),
                face = TableLoaderTool.CleanString(cols[3]),
                elemMul = elemMulIdx >= 0 && elemMulIdx < cols.Count ? TableLoaderTool.ToFloat(cols[elemMulIdx]) : 0f,
            });
        }

        if (list.Count == 0)
        {
            UnityEngine.Debug.LogError("[WorldBossOutlaw] 파싱된 행이 없음 — WorldBossOutlaw 탭 확인 필요");
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        UnityEngine.Debug.Log($"WorldBossOutlaw Loaded: {list.Count}");
    }

    // 주차 → 이번 주 보스(로테이션 = 주차 % 보스 수)
    public WorldBossOutlawData GetByWeek(long weekIndex)
        => list.Count > 0 ? list[(int)(((weekIndex % list.Count) + list.Count) % list.Count)] : null;

    public List<WorldBossOutlawData> AllOrdered() => new(list);

    public int Count => list.Count;
}

[System.Serializable]
public class WorldBossOutlawData
{
    public int rotationIndex; // 주차%6
    public string element;    // 보스 속성(Element.Id) — 같은 속성 탄환이 약점
    public string nameKo;     // 수배범 이름
    public string face;       // 얼굴 이모지(포팅 시 스프라이트 대체 가능)
    public float elemMul;     // 보스별 약점 배율(0 = WorldBossConfig.ElemDmgMul 폴백)
}
