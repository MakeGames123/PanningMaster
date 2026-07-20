using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// LabEdge 시트 로더. From/To 컬럼을 읽어 "To 노드의 선행(From) 목록"으로 보관한다.
public class LabEdgeLoader : ISheetLoader
{
    public static LabEdgeLoader Instance { get; private set; }
    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=ResearchNodeEdge";

    // To -> 선행 노드(From) 목록
    readonly Dictionary<int, List<int>> prereqOf = new();
    // 페이지의 마지막 노드들(To 가 -1 인 From 노드). 전부 연구해야 다음 페이지 개방
    readonly HashSet<int> endNodes = new();
    static readonly List<int> Empty = new();
    public bool IsLoaded { get; private set; }

    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public LabEdgeLoader() { Instance = this; }

    public void Parse(string csv)
    {
        prereqOf.Clear();
        endNodes.Clear();
        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            Debug.LogError("LabEdge CSV 데이터가 비어있음");
            return;
        }

        // 헤더(From, To) 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 2) continue;

            int from = TableLoaderTool.ToInt(cols[0]);
            if (from <= 0) continue; //빈/잘못된 행 스킵

            int to = TableLoaderTool.ToInt(cols[1]);

            // To 가 -1 이면 엣지가 아니라 페이지 마지막 노드(엔드 노드) 마킹
            if (to == -1)
            {
                endNodes.Add(from);
                continue;
            }

            if (to <= 0) continue; //빈/잘못된 행 스킵

            if (!prereqOf.TryGetValue(to, out var list))
            {
                list = new List<int>();
                prereqOf[to] = list;
            }
            list.Add(from);
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"LabEdge Loaded: {prereqOf.Count} nodes have prerequisites, {endNodes.Count} end nodes");
    }

    // 해당 노드가 열리기 위해 선행되어야 하는 노드 목록
    public List<int> GetPrerequisites(int nodeId)
        => prereqOf.TryGetValue(nodeId, out var list) ? list : Empty;

    // 해당 노드가 페이지의 엔드 노드인지
    public bool IsEndNode(int nodeId) => endNodes.Contains(nodeId);
}
