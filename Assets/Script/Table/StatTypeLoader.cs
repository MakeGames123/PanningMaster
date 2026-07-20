using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// StatType 시트 로더. Id(영문 스탯 키)/NameKo/Icon/Unit 컬럼을 읽는다.
// 스탯 enum명(예: FinalDamage)을 UI 라벨("최종 데미지")로 바꿀 때 사용.
public class StatTypeLoader : ISheetLoader
{
    public static StatTypeLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=StatType";

    // 탭 미존재 시 구글이 다른 탭으로 폴백하므로, 이 키가 없으면 잘못된 탭으로 판정
    const string SENTINEL_KEY = "Damage";

    readonly Dictionary<string, StatTypeData> dataDict = new();
    public bool IsLoaded { get; private set; }

    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public StatTypeLoader() { Instance = this; }

    public void Parse(string csv)
    {
        dataDict.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            Debug.LogError("StatType CSV 데이터가 비어있음");
            return;
        }

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 4) continue;

            StatTypeData d = new()
            {
                id = TableLoaderTool.CleanString(cols[0]),
                nameKo = TableLoaderTool.CleanString(cols[1]),
                icon = TableLoaderTool.CleanString(cols[2]),
                unit = TableLoaderTool.CleanString(cols[3]),
            };

            if (string.IsNullOrEmpty(d.id)) continue;
            dataDict[d.id] = d;
        }

        if (!dataDict.ContainsKey(SENTINEL_KEY))
        {
            Debug.LogError($"[StatType] '{SENTINEL_KEY}' 키가 없음 — StatType 탭이 스프레드시트에 없어 다른 탭이 로드된 것으로 보임. 시트 업로드 확인 필요.");
            dataDict.Clear();
            return; // IsLoaded false 유지 — EffectLabel은 하드코딩 폴백을 쓴다
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"StatType Loaded: {dataDict.Count}");
    }

    public StatTypeData Get(string id)
        => dataDict.TryGetValue(id, out var d) ? d : null;

    public StatTypeData Get(StatType effect) => Get(effect.ToString());

    // 라벨 조회. 시트에 없으면 fallback 반환(하드코딩 라벨 등)
    public string GetLabel(string id, string fallback = null)
        => dataDict.TryGetValue(id, out var d) ? d.nameKo : (fallback ?? id);
}

[System.Serializable]
public class StatTypeData
{
    public string id;      // 영문 스탯 키(C# enum명과 1:1, 예: FinalDamage)
    public string nameKo;  // 한글 표시명
    public string icon;    // 아이콘(임시 이모지 — 포팅 시 스프라이트 키로 치환)
    public string unit;    // 단위(% · 개 · 분)
}
