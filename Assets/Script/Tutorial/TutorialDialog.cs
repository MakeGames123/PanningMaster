using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 튜토리얼 말풍선. 스텝 진입 시 대사를 표시하고, PosY로 말풍선 Y 위치를 조정한다.
// talk 스텝이면 우하단 "탭하여 계속" 안내를 켠다(다른 스텝은 행동으로 진행하므로 끔).
public class TutorialDialog : MonoBehaviour
{
    [SerializeField] TutorialManager tutorialManager;   // 비우면 Instance 사용
    [SerializeField] GameObject balloonRoot;            // 말풍선 전체 보이기/숨기기(비우면 balloon 오브젝트)
    [SerializeField] RectTransform balloon;             // 위치 이동 대상(PosY 적용)
    [SerializeField] TextMeshProUGUI nameText;          // "보안관 더스티"
    [SerializeField] TextMeshProUGUI dialogText;        // 대사 본문
    [SerializeField] GameObject tapToContinue;          // 우하단 "탭하여 계속" (talk일 때만)
    [SerializeField] RectTransform sizeFitter;          // ContentSizeFitter가 붙은 대상(비우면 balloon 재빌드)
    [SerializeField] string speakerName = "보안관 더스티";

    TutorialManager TM => tutorialManager != null ? tutorialManager : TutorialManager.Instance;

    bool subscribed;

    void OnEnable() => TrySubscribe();
    void OnDisable() => Unsubscribe();

    void Update()
    {
        // OnEnable 시점엔 TutorialManager 싱글톤이 아직 없을 수 있어 구독이 건너뛰어진다.
        // 준비되면 구독하도록 재시도(성공하면 이후 Update는 즉시 리턴).
        if (!subscribed) TrySubscribe();
    }

    void TrySubscribe()
    {
        if (subscribed) return;
        var tm = TM;
        if (tm == null) return;

        tm.onStepEnter.AddListener(OnStepEnter);
        tm.onSequenceComplete.AddListener(OnComplete);
        subscribed = true;

        Refresh(tm.CurrentStep); // 구독 성립 시점의 현재 스텝으로 동기화(없으면 숨김)
    }

    void Unsubscribe()
    {
        if (!subscribed) return;
        var tm = TM;
        if (tm != null)
        {
            tm.onStepEnter.RemoveListener(OnStepEnter);
            tm.onSequenceComplete.RemoveListener(OnComplete);
        }
        subscribed = false;
    }

    void OnStepEnter(TutorialStepData step) => Refresh(step);
    void OnComplete() => Show(false);

    void Refresh(TutorialStepData step)
    {
        // 대사 없는 스텝(또는 시작 전 null)이면 말풍선 숨김
        if (step == null || string.IsNullOrEmpty(step.dialogKo)) { Show(false); return; }

        Show(true);

        if (nameText != null) nameText.text = speakerName;
        if (dialogText != null) dialogText.text = step.dialogKo;
        if (tapToContinue != null) tapToContinue.SetActive(step.type == TutorialStepType.Talk);

        // 텍스트가 바뀌면 ContentSizeFitter가 같은 프레임에 반영 안 되므로 강제로 즉시 재빌드
        var rebuildTarget = sizeFitter != null ? sizeFitter : balloon;
        if (rebuildTarget != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rebuildTarget);

        // 말풍선 Y 위치를 시트 PosY로
        if (balloon != null)
        {
            var p = balloon.anchoredPosition;
            p.y = step.posY;
            balloon.anchoredPosition = p;
        }
    }

    void Show(bool on)
    {
        var go = balloonRoot != null ? balloonRoot
               : (balloon != null ? balloon.gameObject : null);
        if (go != null) go.SetActive(on);
    }
}
