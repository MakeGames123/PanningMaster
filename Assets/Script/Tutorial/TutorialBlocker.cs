using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 튜토리얼 클릭 차단 오버레이(컨트롤러). TutorialHoleGraphic과 같은 오브젝트에 붙인다.
// - talk 스텝: 전체 차단, 오버레이를 클릭하면 다음 스텝으로 진행
// - click/await 스텝(TargetSelector 있음): 타겟 사각형만 시각적으로 뚫고(구멍) 그 안만 클릭 통과
// - 그 외: 차단 해제
// 시각 구멍·레이캐스트 통과는 TutorialHoleGraphic이 담당한다.
[RequireComponent(typeof(TutorialHoleGraphic))]
public class TutorialBlocker : MonoBehaviour
{
    enum Mode { Off, TalkFull, Hole }

    [SerializeField] TutorialHoleGraphic graphic;     // 비우면 같은 오브젝트에서 자동
    [SerializeField] TutorialManager tutorialManager; // 비우면 Instance 사용

    public static TutorialBlocker Instance { get; private set; }

    Mode mode = Mode.Off;
    float baseAlpha = 1f; // 불투명 시 원래 알파(인스펙터 색 기준)

    TutorialManager TM => tutorialManager != null ? tutorialManager : TutorialManager.Instance;

    void Awake()
    {
        Instance = this;
        if (graphic == null) graphic = GetComponent<TutorialHoleGraphic>();
        if (graphic != null)
        {
            baseAlpha = graphic.color.a;
            graphic.enabled = true; // 씬에선 꺼둬(에디터에서 안 가림) 런타임에 켠다. 이후 표시는 알파로 제어
        }
        if (tutorialManager == null) tutorialManager = TutorialManager.Instance;
        SetMode(Mode.Off, null, false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 외부(드래그 컨트롤러 등)에서 blocker 레이캐스트를 켜고 끈다.
    public void SetRaycast(bool on)
    {
        if (graphic != null) graphic.raycastTarget = on;
    }

    void OnEnable()
    {
        if (TM != null)
        {
            TM.onStepEnter.AddListener(OnStepEnter);
            TM.onSequenceComplete.AddListener(OnComplete);
        }
        Apply(TM != null ? TM.CurrentStep : null); // 진행 중이던 스텝에 맞춰 초기 동기화
    }

    void OnDisable()
    {
        if (TM != null)
        {
            TM.onStepEnter.RemoveListener(OnStepEnter);
            TM.onSequenceComplete.RemoveListener(OnComplete);
        }
    }

    void OnStepEnter(TutorialStepData step) => Apply(step);
    void OnComplete() => SetMode(Mode.Off, null, false);

    void Apply(TutorialStepData step)
    {
        if (step == null) { SetMode(Mode.Off, null, false); return; }

        // talk: 전체 차단 + 탭하면 진행
        if (step.type == TutorialStepType.Talk) { SetMode(Mode.TalkFull, null, step.transparent); return; }

        // click/await: TargetSelector가 있으면 그 타겟들만 뚫고 나머지 차단
        if (!string.IsNullOrEmpty(step.targetSelector))
        {
            var targets = TutorialTargetRegistry.Get(step.targetSelector);
            if (targets != null && targets.Count > 0) { SetMode(Mode.Hole, targets, step.transparent); return; }

            // 매핑 안 된 타겟을 전체 차단하면 유저가 막히므로 차단 해제 + 경고
            Debug.LogWarning($"[Tutorial] TargetSelector 매핑 없음: '{step.targetSelector}' — 차단 해제(TutorialTarget 등록 필요)");
        }

        SetMode(Mode.Off, null, false);
    }

    void SetMode(Mode m, IReadOnlyList<RectTransform> targets, bool transparent)
    {
        mode = m;

        bool blocking = m != Mode.Off;
        if (graphic == null) return;

        // enabled는 런타임 내내 켜둔 채, 표시/숨김은 알파로 제어(off·투명 = 알파 0, 차단은 raycast로 유지)
        graphic.raycastTarget = blocking;

        var c = graphic.color;
        c.a = (blocking && !transparent) ? baseAlpha : 0f;
        graphic.color = c;

        if (m == Mode.Hole) graphic.ShowHoles(targets);
        else graphic.ShowFull();
    }

    // talk 전체 차단일 때만 탭으로 다음 스텝 진행.
    // Update 폴링이라 오버레이가 포인터 클릭을 소비하지 않음 → Hole 모드에선 구멍 안 타겟 클릭이 그대로 통과된다.
    void Update()
    {
        if (mode != Mode.TalkFull) return;

        var pointer = Pointer.current;
        if (pointer != null && pointer.press.wasPressedThisFrame && TM != null)
            TM.Advance();
    }
}
