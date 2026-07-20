using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

// 튜토리얼 스텝 타입. talk=대사 / click=클릭 유도 / await=특정 이벤트 대기.
public enum TutorialStepType
{
    Talk,
    Click,
    Await
}

// TutorialStep 시트 로더. Seq(시퀀스)별로 StepNo 순서의 스텝 목록을 보관한다.
public class TutorialStepLoader : ISheetLoader
{
    public static TutorialStepLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=TutorialStep";

    const string SENTINEL_SEQ = "main";

    // seq -> StepNo 오름차순 스텝 목록
    readonly Dictionary<string, List<TutorialStepData>> bySeq = new();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public TutorialStepLoader() { Instance = this; }

    public void Parse(string csv)
    {
        bySeq.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            Debug.LogError("TutorialStep CSV 데이터가 비어있음");
            return;
        }

        // 1번째 줄은 헤더라서 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count < 13) continue;

            TutorialStepData d = new()
            {
                id = TableLoaderTool.CleanString(cols[0]),
                seq = TableLoaderTool.CleanString(cols[1]),
                stepNo = TableLoaderTool.ToInt(cols[2]),
                stepId = TableLoaderTool.CleanString(cols[3]),
                type = ParseType(cols[4]),
                awaitEvent = TableLoaderTool.CleanString(cols[5]),
                count = TableLoaderTool.ToInt(cols[6]),
                targetSelector = TableLoaderTool.CleanString(cols[7]),
                tab = TableLoaderTool.CleanString(cols[8]),
                markerKo = TableLoaderTool.CleanString(cols[9]),
                rig = TableLoaderTool.CleanString(cols[10]),
                free = TableLoaderTool.CleanString(cols[11]) == "1",
                dialogKo = TableLoaderTool.CleanString(cols[12]).Replace("\\n", "\n"), // \n 리터럴 → 실제 개행
            };

            if (string.IsNullOrEmpty(d.seq)) continue;

            if (!bySeq.TryGetValue(d.seq, out var list))
            {
                list = new List<TutorialStepData>();
                bySeq[d.seq] = list;
            }
            list.Add(d);
        }

        foreach (var list in bySeq.Values)
            list.Sort((a, b) => a.stepNo.CompareTo(b.stepNo));

        if (!bySeq.ContainsKey(SENTINEL_SEQ))
        {
            Debug.LogError($"[TutorialStep] '{SENTINEL_SEQ}' 시퀀스가 없음 — TutorialStep 탭이 스프레드시트에 없어 다른 탭이 로드된 것으로 보임. 시트 업로드 확인 필요.");
            bySeq.Clear();
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        Debug.Log($"TutorialStep Loaded: {bySeq.Count} sequences");
    }

    static TutorialStepType ParseType(string raw)
    {
        string s = TableLoaderTool.CleanString(raw);
        if (Enum.TryParse(s, true, out TutorialStepType t)) return t;

        Debug.LogWarning($"[TutorialStep] 알 수 없는 Type: '{raw}'");
        return TutorialStepType.Talk;
    }

    // 시퀀스의 스텝 목록(StepNo 순). 없으면 null.
    public List<TutorialStepData> GetSequence(string seq)
        => bySeq.TryGetValue(seq, out var list) ? list : null;

    public IEnumerable<string> AllSequences => bySeq.Keys;
}

[System.Serializable]
public class TutorialStepData
{
    public string id;             // 시퀀스_스텝 복합키
    public string seq;            // 시퀀스 키(main/dungeon/craft…)
    public int stepNo;            // 시퀀스 내 순번
    public string stepId;         // 스텝 이름
    public TutorialStepType type; // talk/click/await
    public string awaitEvent;     // await일 때 대기할 QuestEventManager 키
    public int count;             // await 대기 횟수
    public string targetSelector; // 강조 대상(프로토 CSS 셀렉터 — 유니티 포팅 시 치환 필요)
    public string tab;            // 강제 이동 탭
    public string markerKo;       // 손가락 마커 문구
    public string rig;            // 티켓 지급 등 리그
    public bool free;             // 터치 제한 해제 여부
    public string dialogKo;       // 보안관 대사(개행 반영됨)
}
