using System;
using System.Collections.Generic;
using UnityEngine;

// Dungeon 시트 로더. v47 던전 4종(gold/tk=누적딜 · gem/res=등반)의
// 표시명·타입·전용 열쇠·클리어 보상(단계 비례 RewardN:Type/Base/PerStage)을 읽는다.
public class DungeonLoader : ISheetLoader
{
    public static DungeonLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=Dungeon";

    // 탭 미존재 시 구글이 다른 탭으로 폴백하므로, 이 행이 없으면 잘못된(옛) 탭으로 판정
    const string SENTINEL_KEY = "tk"; // 열차 강탈 — v47 갱신 탭에만 존재

    readonly Dictionary<string, DungeonData> dataDict = new();
    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public DungeonLoader() { Instance = this; }

    public void Parse(string csv)
    {
        dataDict.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 14) continue;

            DungeonData d = new()
            {
                id = TableLoaderTool.CleanString(cols[0]),
                type = TableLoaderTool.CleanString(cols[1]),
                nameKo = TableLoaderTool.CleanString(cols[2]),
                emoji = TableLoaderTool.CleanString(cols[3]),
                keyId = TableLoaderTool.CleanString(cols[4]),
                keyEmoji = TableLoaderTool.CleanString(cols[5]),
                keyCap = ParseInt(cols[6]),
                reward1Type = TableLoaderTool.CleanString(cols[7]),
                reward1Base = ParseFloat(cols[8]),
                reward1PerStage = ParseFloat(cols[9]),
                reward2Type = TableLoaderTool.CleanString(cols[10]),
                reward2Base = ParseFloat(cols[11]),
                reward2PerStage = ParseFloat(cols[12]),
                subKo = TableLoaderTool.CleanString(cols[13]),
            };

            if (string.IsNullOrEmpty(d.id)) continue;
            dataDict[d.id] = d;
        }

        if (!dataDict.ContainsKey(SENTINEL_KEY))
        {
            Debug.LogError($"[Dungeon] '{SENTINEL_KEY}' 행이 없음 — Dungeon 탭이 v47 기준으로 갱신됐는지 확인 필요");
            dataDict.Clear();
            return; // IsLoaded false 유지 — 필드들은 하드코딩 폴백을 쓴다
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"Dungeon Loaded: {dataDict.Count}");
    }

    // dmg형 던전은 보상 컬럼이 공란(동적 산식)이라 경고 없이 0으로 처리
    static int ParseInt(string raw)
        => int.TryParse(TableLoaderTool.CleanString(raw), out var v) ? v : 0;

    static float ParseFloat(string raw)
        => float.TryParse(TableLoaderTool.CleanString(raw), out var v) ? v : 0f;

    public DungeonData Get(string id)
        => dataDict.TryGetValue(id, out var d) ? d : null;
}

[System.Serializable]
public class DungeonData
{
    public string id;         // 던전 키(gold/tk/gem/res)
    public string type;       // climb=등반 / dmg=30초 누적딜
    public string nameKo;     // 표시명
    public string emoji;      // 아이콘(임시 이모지)
    public string keyId;      // 전용 열쇠 재화 키(keyA~keyD)
    public string keyEmoji;   // 전용 열쇠 아이콘
    public int keyCap;        // 열쇠 보유 캡
    public string reward1Type;    // 등반 클리어 보상1 종류(Crate/Hourglass — dmg형은 공란)
    public float reward1Base;     // 보상1 기본량
    public float reward1PerStage; // 보상1 단계당 증가량 — 지급량=floor(Base+PerStage×단계)
    public string reward2Type;    // 등반 클리어 보상2 종류(Ticket)
    public float reward2Base;
    public float reward2PerStage;
    public string subKo;      // 부제
}
