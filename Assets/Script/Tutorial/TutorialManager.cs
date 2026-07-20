using UnityEngine;
using UnityEngine.Events;

// 튜토리얼 시퀀스 진행기(최소 버전).
// - talk/click 스텝: UI(탭/클릭)에서 Advance() 호출로 다음 진행
// - await 스텝: QuestEventManager 이벤트를 구독해 목표 횟수 도달 시 자동 진행
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    public UnityEvent<TutorialStepData> onStepEnter = new(); // 스텝 진입 — UI가 대사/마커/타겟 표시
    public UnityEvent onSequenceComplete = new();            // 시퀀스 끝

    System.Collections.Generic.List<TutorialStepData> steps;
    int index = -1;

    // await 대기 상태
    bool waiting;
    string awaitKey;
    long awaitBase;   // 스텝 시작 시점 스냅샷(차분으로 진행 판정)
    int awaitTarget;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        StartSequence("main");
    }

    void OnDestroy() => StopWaiting();

    // 시퀀스 시작(seq = main/dungeon/craft…)
    public void StartSequence(string seq)
    {
        var loader = TutorialStepLoader.Instance;
        if (loader == null || !loader.IsLoaded)
        {
            Debug.LogWarning("[Tutorial] 시트 로드 전 — 시퀀스 시작 불가");
            return;
        }

        steps = loader.GetSequence(seq);
        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning($"[Tutorial] 시퀀스 없음: {seq}");
            return;
        }

        Debug.Log($"[Tutorial] 시퀀스 시작: '{seq}' ({steps.Count}스텝)");
        index = -1;
        Next();
    }

    // talk/click 스텝을 UI에서 진행시킬 때 호출(await 스텝에는 무효)
    public void Advance()
    {
        if (steps == null || index < 0 || index >= steps.Count) return;
        if (steps[index].type == TutorialStepType.Await)
        {
            Debug.Log($"[Tutorial] Advance 무시 — await 스텝은 이벤트로만 진행 (step {steps[index].stepId}, 대기 {awaitKey})");
            return;
        }
        Debug.Log($"[Tutorial] Advance 호출 → 다음 스텝 (현재 {steps[index].stepId})");
        Next();
    }

    void Next()
    {
        StopWaiting();

        index++;
        if (steps == null || index >= steps.Count)
        {
            Finish();
            return;
        }

        var step = steps[index];
        Debug.Log($"[Tutorial] 스텝 진입 [{step.stepNo}/{steps.Count}] {step.stepId} ({step.type})" +
                  (string.IsNullOrEmpty(step.tab) ? "" : $" tab={step.tab}") +
                  (string.IsNullOrEmpty(step.dialogKo) ? "" : $" | \"{step.dialogKo.Replace("\n", " ")}\""));
        onStepEnter.Invoke(step); // UI 표시(대사/마커/탭 이동 등)

        if (step.type == TutorialStepType.Await)
            BeginWait(step);
    }

    void BeginWait(TutorialStepData step)
    {
        awaitKey = step.awaitEvent;
        awaitTarget = Mathf.Max(1, step.count);
        awaitBase = QuestEventManager.Instance != null ? QuestEventManager.Instance.GetValue(awaitKey) : 0;

        if (QuestEventManager.Instance != null)
            QuestEventManager.Instance.OnEventChanged += OnEvent;

        waiting = true;
        Debug.Log($"[Tutorial] await 대기 시작 — 이벤트 '{awaitKey}' {awaitTarget}회 (기준값 {awaitBase})");
    }

    void OnEvent(string key, long value)
    {
        if (!waiting || key != awaitKey) return;

        long progress = value - awaitBase;
        Debug.Log($"[Tutorial] await 진행 '{key}' {progress}/{awaitTarget}");

        // 스텝 시작 이후 증가분이 목표에 도달하면 다음으로
        if (progress >= awaitTarget)
        {
            Debug.Log($"[Tutorial] await 완료 '{key}' — 다음 스텝으로");
            Next();
        }
    }

    void StopWaiting()
    {
        if (waiting && QuestEventManager.Instance != null)
            QuestEventManager.Instance.OnEventChanged -= OnEvent;
        waiting = false;
    }

    void Finish()
    {
        Debug.Log("[Tutorial] 시퀀스 완료");
        steps = null;
        index = -1;
        onSequenceComplete.Invoke();
    }
}
