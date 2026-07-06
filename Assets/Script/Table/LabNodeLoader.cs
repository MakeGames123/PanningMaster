using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// LabNode 시트 로더. ID/Name/Cost/Effect/Amount/Time/MaxLevel 컬럼을 읽는다.
public class LabNodeLoader : MonoBehaviour, ITableLoader
{
    public static LabNodeLoader Instance { get; private set; }
    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1uo6Tm2UDagmMJ09O3qIT6m4mfCTsakRTB5KVbSS0-DI/gviz/tq?tqx=out:csv&sheet=LabNode";

    readonly Dictionary<int, LabNodeData> dataDict = new();
    public IReadOnlyDictionary<int, LabNodeData> All => dataDict;
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
        dataDict.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            Debug.LogError("LabNode CSV 데이터가 비어있음");
            return;
        }

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 7) continue;

            LabNodeData d = new()
            {
                id = TableLoaderTool.ToInt(cols[0]),
                name = TableLoaderTool.CleanString(cols[1]),
                cost = TableLoaderTool.ToInt(cols[2]),
                effect = ParseEffect(cols[3]),
                amount = TableLoaderTool.ToFloat(cols[4]),
                timeSeconds = ParseTime(cols[5]),
                maxLevel = TableLoaderTool.ToInt(cols[6]),
            };

            dataDict[d.id] = d;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"LabNode Loaded: {dataDict.Count}");
    }

    static LabEffectType ParseEffect(string raw)
    {
        string s = TableLoaderTool.CleanString(raw);
        if (Enum.TryParse(s, true, out LabEffectType e)) return e;

        Debug.LogWarning($"[LabNode] 알 수 없는 Effect: '{raw}'");
        return LabEffectType.Damage;
    }

    // "H:M" (앞=시, 뒤=분) -> 초. 예) "0:06" = 6분 = 360초. 초까지 있으면 함께 반영.
    // 콜론이 없으면 분 단위 숫자로 간주.
    static float ParseTime(string raw)
    {
        string s = TableLoaderTool.CleanString(raw);
        if (string.IsNullOrEmpty(s)) return 0f;

        if (!s.Contains(':'))
            return TableLoaderTool.ToFloat(s) * 60f;

        var parts = s.Split(':');
        float h = parts.Length > 0 ? TableLoaderTool.ToFloat(parts[0]) : 0f;
        float m = parts.Length > 1 ? TableLoaderTool.ToFloat(parts[1]) : 0f;
        float sec = parts.Length > 2 ? TableLoaderTool.ToFloat(parts[2]) : 0f;
        return h * 3600f + m * 60f + sec;
    }

    public LabNodeData Get(int id) => dataDict.TryGetValue(id, out var d) ? d : null;
}
