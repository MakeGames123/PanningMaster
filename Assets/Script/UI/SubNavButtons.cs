using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 하위 탭 전환기(스탯 성장 ↔ 연구 등). NavButtons식으로 패널을 anchoredPosition으로 옮겨 보이기/숨기기.
// buttons[i] 를 누르면 panels[i] 만 화면 안(zero), 나머지는 화면 밖(hiddenPos)으로 이동. SetActive를 쓰지 않는다.
public class SubNavButtons : MonoBehaviour
{
    [SerializeField] List<Button> buttons = new();          // 탭 버튼들
    [SerializeField] List<RectTransform> panels = new();    // 버튼과 같은 인덱스로 대응하는 패널들

    Color selectedColor = new(1f, 1f, 1f, 0.45f);              // 현재 탭
    Color normalColor = Color.white;        // 나머지 탭

    readonly Vector2 hiddenPos = new(-9999f, -9999f);
    bool initialized;

    public int Current { get; private set; } = -1;

    void Awake()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            int idx = i; // 클로저 캡처 고정
            if (buttons[i] != null) buttons[i].onClick.AddListener(() => Select(idx));
        }
    }

    void OnEnable() => TryInitialize();
    void Start() => TryInitialize();

    void TryInitialize()
    {
        if (initialized) return;
        if (buttons.Count == 0 || panels.Count == 0) return;
        initialized = true;
        Select(0); // 기본 첫 탭
    }

    public void Select(int index)
    {
        Current = index;

        // 대상 패널만 화면 안, 나머지는 화면 밖으로(NavButtons식)
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] == null) continue;
            panels[i].anchoredPosition = (i == index) ? Vector2.zero : hiddenPos;
        }

        // 버튼 하이라이트
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null || buttons[i].targetGraphic == null) continue;
            buttons[i].targetGraphic.color = (i == index) ? selectedColor : normalColor;
        }
    }
}
