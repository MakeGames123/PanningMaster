using UnityEngine;

// x10 뽑기 버튼(튜토리얼 main 8단계, await 스텝의 AwaitEvent=clickMul10)용.
// x10 버튼의 Button.onClick 에 OnClick 을 연결하면, 클릭 시 튜토리얼 이벤트를 발행해
// 해당 await 스텝을 다음으로 진행시킨다. (스텝 판정은 TutorialManager가 AwaitEvent로 처리)
public class Tutorialx10Button : MonoBehaviour
{
    [SerializeField] string awaitEvent = "clickMul10"; // 시트 AwaitEvent와 동일 문자열

    public void OnClick()
    {
        if (TutorialEventManager.Instance != null)
            TutorialEventManager.Instance.AddEvent(awaitEvent);
    }
}
