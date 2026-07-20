using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// NumberFormat 시트 로더. index/suffix/exponent 를 읽어 큰 수를 K/M/B… 접미어로 축약한다.
// 프로토 fmt 규약과 동일: 선두값이 10 이상이 되는 가장 큰 단위를 골라 "1785K"처럼 표시.
public class NumberFormatLoader : ISheetLoader
{
    public static NumberFormatLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=NumberFormat";

    // (접미어, 지수). exponent 내림차순으로 정렬해 사용.
    readonly List<(string suffix, int exponent)> units = new();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    // 시트 로드 전 폴백(프로토 UNITS와 동일). exponent 내림차순.
    static readonly List<(string suffix, int exponent)> Fallback = new()
    {
        ("L", 63), ("J", 60), ("H", 57), ("X", 54), ("W", 51), ("V", 48), ("N", 45),
        ("D", 42), ("U", 39), ("S", 36), ("F", 33), ("R", 30), ("Y", 27), ("Z", 24),
        ("E", 21), ("P", 18), ("Q", 15), ("T", 12), ("B", 9), ("M", 6), ("K", 3),
    };

    List<(string suffix, int exponent)> Table => units.Count > 0 ? units : Fallback;

    public string Url => SHEET_URL;

    public NumberFormatLoader() { Instance = this; }

    public void Parse(string csv)
    {
        units.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            Debug.LogError("NumberFormat CSV 데이터가 비어있음");
            return;
        }

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 3) continue;

            string suffix = TableLoaderTool.CleanString(cols[1]);
            int exponent = TableLoaderTool.ToInt(cols[2]);
            if (string.IsNullOrEmpty(suffix) || exponent <= 0) continue;

            units.Add((suffix, exponent));
        }

        // 큰 단위부터 검사하도록 exponent 내림차순 정렬
        units.Sort((a, b) => b.exponent.CompareTo(a.exponent));

        // 폴백 판정: K 단위가 없으면 잘못된 탭으로 간주
        if (!units.Exists(u => u.suffix == "K"))
        {
            Debug.LogError("[NumberFormat] 'K' 단위가 없음 — NumberFormat 탭이 스프레드시트에 없어 다른 탭이 로드된 것으로 보임. 시트 업로드 확인 필요.");
            units.Clear();
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"NumberFormat Loaded: {units.Count}");
    }

    // 인스턴스 포맷(로드 전이면 폴백 표 사용)
    public string Format(double n) => FormatWith(n, Table);

    // 어디서든 호출 가능한 정적 축약(Instance 없거나 로드 전이면 폴백)
    public static string Abbrev(double n) => FormatWith(n, Instance != null ? Instance.Table : Fallback);

    // 프로토 fmt 규약: 10000 미만은 그대로, 이상은 선두값이 10 이상이 되는 최대 단위로 축약.
    static string FormatWith(double n, List<(string suffix, int exponent)> table)
    {
        if (double.IsNaN(n) || double.IsInfinity(n)) return "0";
        if (n < 0) return "-" + FormatWith(-n, table);
        if (n < 10 && n != Math.Floor(n)) return n.ToString("0.0"); // 10 미만 소수는 한 자리
        n = Math.Floor(n);
        if (n < 10000) return ((long)n).ToString();

        foreach (var (suffix, exponent) in table) // exponent 내림차순
        {
            double d = Math.Pow(10, exponent);
            double v = Math.Floor(n / d);
            if (v >= 10) return ((long)v).ToString() + suffix;
        }
        return ((long)n).ToString();
    }
}
