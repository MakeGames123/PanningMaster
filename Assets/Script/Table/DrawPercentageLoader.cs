using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
public class DrawPercentageLoader : MonoBehaviour, ITableLoader
{
    public static DrawPercentageLoader Instance { get; private set; }
    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1uo6Tm2UDagmMJ09O3qIT6m4mfCTsakRTB5KVbSS0-DI/gviz/tq?tqx=out:csv&sheet=ForgeProb";

    private Dictionary<int, DrawData> dataDict
        = new Dictionary<int, DrawData>();

    public event Action OnLoaded;
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(StartLoad());
    }
    public IEnumerator StartLoad()
    {
        yield return StartCoroutine(LoadSheet());
    }
    public IEnumerator LoadSheet()
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
            Debug.LogError("CSV 데이터가 비어있음");
            return;
        }

        // 첫 줄 = 헤더
        var header = lines[0].Split(',');

        int rowCount = lines.Length - 1;      // tier 개수
        int columnCount = header.Length;      // 전체 열 개수

        // B열부터 끝까지 = forge레벨들
        for (int col = 1; col < columnCount; col++)
        {
            DrawData data = new DrawData();
            data.Index = col;   // forgelv1 → 1, forgelv2 → 2 ...
            data.weights = new List<float>();

            // 각 tier 행 돌기
            for (int row = 1; row < lines.Length; row++)
            {
                var cols = lines[row].Split(',');

                if (cols.Length <= col)
                {
                    data.weights.Add(0f);
                    continue;
                }

                float value = TableLoaderTool.ToFloat(cols[col]);
                data.weights.Add(value);
            }

            dataDict[data.Index] = data;
        }
        OnLoaded?.Invoke();

        Debug.Log($"DrawData Loaded (Column Based): {dataDict.Count}");
    }
    public DrawData ReturnData(int level)
    {
        return dataDict[level];
    }
}
[System.Serializable]
public class DrawData
{
    public int Index;                // 레벨
    public List<float> weights;        // 확률 리스트
}