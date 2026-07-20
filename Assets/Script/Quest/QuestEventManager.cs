using System;
using System.Collections.Generic;
using UnityEngine;

// 프로토의 qEvent() 대응 — 퀘스트 판정용 이벤트 값 저장소.
// Counter 퀘스트(누적 횟수): AddEvent("craft") / AddEvent("dgEnter_gold")
// Absolute 퀘스트(현재값 도달): SetValue("stage", 12) / SetValue("growStat_atk", 10)
// 키는 QuestMain 시트의 EventKey(+StatId) 규약을 따른다. (QuestMainData.FullEventKey 참조)
public class QuestEventManager : MonoBehaviour
{
    public static QuestEventManager Instance { get; private set; }

    readonly Dictionary<string, long> values = new();

    // (key, 바뀐 후 값) — 퀘스트 판정/UI 갱신 훅
    public event Action<string, long> OnEventChanged;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Counter형: 누적 증가. AddEvent("forge"), AddEvent("slotEnh", 2)
    public void AddEvent(string key, long amount = 1)
    {
        if (string.IsNullOrEmpty(key)) return;

        values.TryGetValue(key, out long cur);
        long next = cur + amount;
        values[key] = next;
        OnEventChanged?.Invoke(key, next);
    }

    // Absolute형: 현재값 갱신(스테이지·스탯 레벨 등). 낮아지지 않는 값이므로 max 유지
    public void SetValue(string key, long value)
    {
        if (string.IsNullOrEmpty(key)) return;

        values.TryGetValue(key, out long cur);
        if (value <= cur && values.ContainsKey(key)) return; // 변화 없으면 통지 생략

        values[key] = Math.Max(cur, value);
        OnEventChanged?.Invoke(key, values[key]);
    }

    public long GetValue(string key)
        => (!string.IsNullOrEmpty(key) && values.TryGetValue(key, out long v)) ? v : 0;

    // 퀘스트 데이터 기준 현재 진행값.
    // Counter 판정은 "수령 시점 스냅샷 이후 횟수"라서, 퀘스트 시작 시점에 GetValue(FullEventKey)를
    // 저장해두고 (현재값 - 스냅샷)으로 계산하는 것이 시트 규약([Judge] Counter=스냅샷 차분)과 일치한다.
    public long GetProgress(QuestMainData quest, long counterSnapshot = 0)
    {
        if (quest == null) return 0;

        long cur = GetValue(quest.FullEventKey);
        return quest.judge == QuestJudge.Counter ? cur - counterSnapshot : cur;
    }

    // ---- 저장 연동용 ----

    [Serializable]
    public class SaveData
    {
        public List<string> keys = new();
        public List<long> counts = new();
    }

    public SaveData GetSaveData()
    {
        var data = new SaveData();
        foreach (var kv in values)
        {
            data.keys.Add(kv.Key);
            data.counts.Add(kv.Value);
        }
        return data;
    }

    public void ApplySaveData(SaveData data)
    {
        values.Clear();
        if (data == null) return;

        int n = Math.Min(data.keys.Count, data.counts.Count);
        for (int i = 0; i < n; i++)
            values[data.keys[i]] = data.counts[i];
    }
}
