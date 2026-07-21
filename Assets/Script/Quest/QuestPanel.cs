using System;
using UnityEngine;
using UnityEngine.Events;

// 퀘스트 진행 상태 관리. 시트/이벤트 구독·진행도 계산·보상 지급·퀘스트 전환을 담당하고,
// 표시는 QuestUI에 위임한다.
// 버튼 하나로 통합: 클리어 전 = 해당 탭으로 이동 / 클리어 후 = 보상 지급 → 다음 퀘스트
public class QuestPanel : MonoBehaviour
{
    [SerializeField] QuestUI questUI;
    [SerializeField] NavButtons navButtons; // 퀘스트 이동 버튼 → 탭 전환 연계

    [Header("튜토리얼 연동")]
    [SerializeField] string tutorialCompleteStepId = "quest";      // 이 스텝 진입 시(=직전 autoeq 완료) 튜토리얼 완료 퀘스트 클리어
    [SerializeField] string tutorialCompleteEventKey = "tutorial"; // QuestMain 1번(튜토리얼 완료)의 EventKey

    // 탭 이동 추가 훅(연출 등). 실제 패널 전환은 navButtons가 담당
    public UnityEvent<string> onNavigate;
    // 보상 수령 시 통지 — 해금 팝업(UnlockKey)·튜토리얼(TutorialSeq) 연출 연결용
    public UnityEvent<QuestMainData> onQuestClaimed;

    int currentId = 1;
    long counterSnapshot;   // Counter 판정용 — 퀘스트 시작 시점 값

    QuestMainData Current => QuestMainLoader.Instance != null && QuestMainLoader.Instance.IsLoaded
        ? QuestMainLoader.Instance.Get(currentId) : null;

    void Start()
    {
        if (questUI != null) questUI.OnActionClicked += HandleActionClicked;

        if (QuestMainLoader.Instance != null)
            QuestMainLoader.Instance.OnLoaded += HandleSheetLoaded;
        if (QuestEventManager.Instance != null)
            QuestEventManager.Instance.OnEventChanged += HandleEventChanged;
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.onStepEnter.AddListener(HandleTutorialStep);

        Refresh();
    }

    void OnDestroy()
    {
        if (questUI != null) questUI.OnActionClicked -= HandleActionClicked;

        if (QuestMainLoader.Instance != null)
            QuestMainLoader.Instance.OnLoaded -= HandleSheetLoaded;
        if (QuestEventManager.Instance != null)
            QuestEventManager.Instance.OnEventChanged -= HandleEventChanged;
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.onStepEnter.RemoveListener(HandleTutorialStep);
    }

    // 튜토리얼 특정 스텝 진입 시 튜토리얼 완료 퀘스트(EventKey=tutorial)를 클리어 상태로 만든다.
    void HandleTutorialStep(TutorialStepData step)
    {
        if (step == null || step.stepId != tutorialCompleteStepId) return;
        if (QuestEventManager.Instance != null)
            QuestEventManager.Instance.SetValue(tutorialCompleteEventKey, 1); // Absolute goal 1 → 완료
    }

    void HandleSheetLoaded() => SetQuest(currentId);

    void HandleEventChanged(string key, long value)
    {
        var q = Current;
        if (q != null && key == q.FullEventKey) Refresh();
    }

    // 버튼 하나로 네비/보상 수령을 분기
    void HandleActionClicked()
    {
        var q = Current;
        if (q == null) return;

        if (IsCompleted(q))
        {
            GiveReward(q);
            onQuestClaimed?.Invoke(q);

            // 튜토리얼 main_12: 퀘스트 배너를 탭해 보상을 수령하면 questComplete 발행
            if (TutorialEventManager.Instance != null)
                TutorialEventManager.Instance.AddEvent("questComplete");

            NextQuest();
        }
        else
        {
            if (navButtons != null) navButtons.Navigate(q.navTab);
            onNavigate?.Invoke(q.navTab);
        }
    }

    // 현재 퀘스트 지정(저장 데이터 복원 등 외부에서 호출)
    public void SetQuest(int id)
    {
        currentId = id;

        var q = Current;
        counterSnapshot = (q != null && q.judge == QuestJudge.Counter && QuestEventManager.Instance != null)
            ? QuestEventManager.Instance.GetValue(q.FullEventKey) : 0;

        Refresh();
    }

    public void NextQuest() => SetQuest(currentId + 1);

    public int CurrentQuestId => currentId;

    long GetProgress(QuestMainData q)
    {
        long progress = QuestEventManager.Instance != null
            ? QuestEventManager.Instance.GetProgress(q, counterSnapshot) : 0;
        return Math.Clamp(progress, 0, q.goal);
    }

    bool IsCompleted(QuestMainData q) => q.goal > 0 && GetProgress(q) >= q.goal;

    void Refresh()
    {
        var q = Current;
        if (q == null)
        {
            if (questUI != null) questUI.Hide(); // 시트 로드 전이거나 퀘스트 전부 소진
            return;
        }

        if (questUI != null) questUI.Show(q, GetProgress(q), IsCompleted(q));
    }

    // ---- 보상 지급 ----

    void GiveReward(QuestMainData q)
    {
        long amount = q.rewardAmount;
        if (amount <= 0 && !string.IsNullOrEmpty(q.rewardFormula))
            amount = EvalGoldFormula(q.rewardFormula);
        if (amount <= 0) return;

        var dm = DataManager.Instance;
        switch (q.rewardType)
        {
            case "Gold": dm.IncreaseGold((int)amount); break;
            case "Ticket": dm.IncreaseTicket((int)amount); break;
            case "KeyGold": dm.AddKey(KeyType.Normal, (int)amount); break;   // 현재 열쇠 2종 체계 매핑
            case "KeyResearch":
            case "KeyArmory": dm.AddKey(KeyType.Elite, (int)amount); break;
            default:
                Debug.LogWarning($"[Quest] 미구현 보상 재화: {q.rewardType} x{amount} (퀘스트 {q.id})");
                break;
        }
    }

    // "qGoldAmt(8)" → max(500, 클리어골드(현재 스테이지) × 8)
    // 클리어골드 공식은 OnlineConfig 상수 기반(NormalField와 동일)
    static long EvalGoldFormula(string formula)
    {
        int open = formula.IndexOf('(');
        int close = formula.IndexOf(')');
        if (open < 0 || close <= open) return 0;

        if (!float.TryParse(formula.Substring(open + 1, close - open - 1), out float mul)) return 0;

        var cfg = GameConfigLoader.Instance;
        int stage = DataManager.Instance != null ? DataManager.Instance.stage : 1;

        float clearGold = stage
            * cfg.GetFloat("ClearGoldPerStage")
            * (1 + Mathf.Max(0, stage - cfg.GetFloat("ClearGoldRampStage")) * cfg.GetFloat("ClearGoldRampPerStage"))
            * Mathf.Pow(cfg.GetFloat("ClearGoldExpoRate"), Mathf.Max(0, stage - cfg.GetFloat("ClearGoldExpoStage")));

        return (long)Math.Max(500, Mathf.Floor(clearGold * mul));
    }
}
