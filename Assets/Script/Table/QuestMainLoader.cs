using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

// QuestMain 시트 로더. 메인 퀘스트 체인(Id 순서 = 진행 순서).
// 컬럼: Id/DescKo/EventKey/StatId/Goal/Judge/Reward:Type/Reward:Amount/Reward:Formula/NavTab/UnlockKey/TutorialSeq/$Memo
public class QuestMainLoader : ISheetLoader
{
    public static QuestMainLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=QuestMain";

    readonly Dictionary<int, QuestMainData> dataDict = new();
    public bool IsLoaded { get; private set; }

    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public QuestMainLoader() { Instance = this; }

    public void Parse(string csv)
    {
        dataDict.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            Debug.LogError("QuestMain CSV 데이터가 비어있음");
            return;
        }

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 12) continue;

            QuestMainData d = new()
            {
                id = TableLoaderTool.ToInt(cols[0]),
                descKo = TableLoaderTool.CleanString(cols[1]),
                eventKey = TableLoaderTool.CleanString(cols[2]),
                statId = TableLoaderTool.CleanString(cols[3]),
                goal = TableLoaderTool.ToInt(cols[4]),          // 빈 값(동적 목표)은 0
                judge = ParseJudge(cols[5]),
                rewardType = TableLoaderTool.CleanString(cols[6]),
                rewardAmount = TableLoaderTool.ToInt(cols[7]),  // 빈 값(수식 보상)은 0
                rewardFormula = TableLoaderTool.CleanString(cols[8]),
                navTab = TableLoaderTool.CleanString(cols[9]),
                unlockKey = TableLoaderTool.CleanString(cols[10]),
                tutorialSeq = TableLoaderTool.CleanString(cols[11]),
            };

            if (d.id <= 0) continue;
            dataDict[d.id] = d;
        }

        if (dataDict.Count == 0)
        {
            Debug.LogError("[QuestMain] 파싱된 행이 없음 — QuestMain 탭이 스프레드시트에 있는지 확인 필요");
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"QuestMain Loaded: {dataDict.Count}");
    }

    static QuestJudge ParseJudge(string raw)
    {
        string s = TableLoaderTool.CleanString(raw);
        if (Enum.TryParse(s, true, out QuestJudge j)) return j;

        Debug.LogWarning($"[QuestMain] 알 수 없는 Judge: '{raw}'");
        return QuestJudge.Counter;
    }

    public QuestMainData Get(int id) => dataDict.TryGetValue(id, out var d) ? d : null;

    public int Count => dataDict.Count;

    // Id 오름차순(=진행 순서) 전체 목록
    public List<QuestMainData> AllOrdered()
        => dataDict.Keys.OrderBy(k => k).Select(k => dataDict[k]).ToList();
}

// Absolute = 현재값이 목표 도달(스테이지·스탯 레벨) / Counter = 퀘스트 시작 시점 스냅샷 이후 N회
public enum QuestJudge
{
    Absolute,
    Counter
}

[System.Serializable]
public class QuestMainData
{
    public int id;                // 진행 순번(1~)
    public string descKo;         // 퀘스트 문구(빈 값 = 동적 생성)
    public string eventKey;       // QuestEventManager 카운터 키(stage/forge/craft/dgEnter…)
    public string statId;         // growStat 퀘스트의 대상 스탯(atk/elem…), 없으면 빈 값
    public int goal;              // 목표값(0 = 동적 목표)
    public QuestJudge judge;
    public string rewardType;     // Gold/Ticket/Gem/KeyGold/KeyResearch/KeyArmory/Hourglass/Crate
    public long rewardAmount;     // 고정 보상 수량(수식 보상이면 0)
    public string rewardFormula;  // 동적 보상 수식(예: qGoldAmt(8) = max(500, 클리어골드×8))
    public string navTab;         // 퀘스트 탭 클릭 시 이동할 탭
    public string unlockKey;      // 완료 시 해금할 컨텐츠(UnlockPopup.Id)
    public string tutorialSeq;    // 완료 시 재생할 튜토리얼 시퀀스

    // growStat 퀘스트는 "growStat_atk"처럼 스탯까지 붙인 키로 판정
    public string FullEventKey => string.IsNullOrEmpty(statId) ? eventKey : $"{eventKey}_{statId}";
}
