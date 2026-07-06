using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ReloadingUI : MonoBehaviour
{
    [SerializeField] GameObject root;   // 슬라이더 표시 루트(켜고 끔)
    [SerializeField] Slider slider;     // 남은 장전시간 표시

    void Awake()
    {
        if (root != null) root.SetActive(false);
    }

    //장전 취소: 슬라이더 UI 숨김
    public void Hide()
    {
        if (slider != null) slider.value = 0f;
        if (root != null) root.SetActive(false);
    }

    //남은 장전시간을 슬라이더로 표현(가득 -> 0). Player의 코루틴에서 yield로 대기
    public IEnumerator Reload(float duration)
    {
        if (root != null) root.SetActive(true);
        if (slider != null) slider.value = 1f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (slider != null) slider.value = Mathf.Clamp01(1f - t / duration);
            yield return null;
        }

        if (slider != null) slider.value = 0f;
        if (root != null) root.SetActive(false);
    }
}
