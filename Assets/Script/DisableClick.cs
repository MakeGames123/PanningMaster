using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Update에서 클릭(마우스 왼쪽 버튼/터치 탭)을 감지해 onClick 을 발화한다.
public class DisableClick : MonoBehaviour
{
    public UnityEvent onClick;

    void Update()
    {
        var pointer = Pointer.current; // 마우스·터치 통합
        if (pointer != null && pointer.press.wasPressedThisFrame)
            onClick.Invoke();
    }
}
