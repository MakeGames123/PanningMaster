using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// LabEdge 시트 로더. From/To 컬럼을 읽어 "To 노드의 선행(From) 목록"으로 보관한다.
public class LabEdgeLoader : MonoBehaviour, ITableLoader
{
    public static LabEdgeLoader Instance { get; private set; }
    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1uo6Tm2UDagmMJ09O3qIT6m4mfCTsakRTB5KVbSS0-DI/gviz/tq?tqx=out:csv&sheet=LabEdge";

    // To -> 선행 노드(From) 목록
    readonly Dictionary<int, List<int>> prereqOf = new();
    static readonly List<int> Empty = new();
    public bool IsLoaded { get; private set; }

    public event Action OnLoaded;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        StartCoroutine(LoadSheet());
    }

    IEnumerator LoadSheet()
    {
        UnityWebRequest req = UnityWebRequest.Get(SHEET_URL);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            yield break;
        }

        ParseCSV(req.downloadHandler.text);
    }

    void ParseCSV(string csv)
    {
        prereqOf.Clear();

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
            int to = TableLoaderTool.ToInt(cols[1]);

            if (!prereqOf.TryGetValue(to, out var list))
            {
                list = new List<int>();
                prereqOf[to] = list;
            }
            list.Add(from);
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"LabEdge Loaded: {prereqOf.Count} nodes have prerequisites");
    }

    // 해당 노드가 열리기 위해 선행되어야 하는 노드 목록
    public List<int> GetPrerequisites(int nodeId)
        => prereqOf.TryGetValue(nodeId, out var list) ? list : Empty;
}
