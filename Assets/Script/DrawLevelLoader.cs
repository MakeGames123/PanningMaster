using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
public class DrawLevelLoader : MonoBehaviour
{
    public static DrawLevelLoader Instance { get; private set; }
    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1zPUOo0VfiUMGoo-iuPssETWltc9qkdnmYAOZoIdZO28/gviz/tq?tqx=out:csv&sheet=뽑기 확률표";

    private Dictionary<int, DrawData> dataDict
        = new Dictionary<int, DrawData>();

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
        var lines = csv.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var cols = lines[i].Split(',');

            if (cols.Length < 10) continue;

            DrawData data = new DrawData();
            data.Index = ParseLevel(cols[0]);   // "1레벨" → 1
            data.weights = new List<int>();
            data.price = ToDouble(cols[10]);

            // 1번 컬럼부터 끝까지 전부 리스트에 저장
            for (int j = 1; j < 10; j++)
            {
                data.weights.Add(ToInt(cols[j]));
            }

            dataDict[data.Index] = data;
        }

        Debug.Log($"DrawData Loaded: {dataDict.Count}");
    }
    int ToInt(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        string cleaned = value
            .Trim()
            .Trim('"');

        if (!int.TryParse(cleaned, out int result))
        {
            Debug.LogWarning($"Int Parse 실패: '{value}'");
            return 0;
        }

        return result;
    }
    double ToDouble(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        string cleaned = value
            .Trim()
            .Trim('"');

        if (!double.TryParse(cleaned, out double result))
        {
            Debug.LogWarning($"Int Parse 실패: '{value}'");
            return 0;
        }

        return result;
    }
    int ParseLevel(string value)
    {
        string cleaned = CleanString(value);
        cleaned = cleaned.Replace("레벨", "");
        return int.TryParse(cleaned, out int result) ? result : 0;
    }
    string CleanString(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        return value
            .Trim()
            .Trim('"')
            .Replace("\uFEFF", "")   // BOM
            .Replace("\u200B", "")   // Zero-width space
            .Normalize(System.Text.NormalizationForm.FormC);
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
    public List<int> weights;        // 확률 리스트
    public double price;        // 가격
}