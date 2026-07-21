using System.Collections.Generic;
using UnityEngine;

// 시트 TargetSelector 문자열 → Unity RectTransform 목록 매핑 레지스트리.
// 하나의 셀렉터(태그)에 여러 오브젝트를 등록할 수 있다(모두 구멍으로 뚫리고 클릭 통과).
public static class TutorialTargetRegistry
{
    static readonly Dictionary<string, List<RectTransform>> targets = new();

    public static void Register(string selector, RectTransform rt)
    {
        if (string.IsNullOrEmpty(selector) || rt == null) return;

        if (!targets.TryGetValue(selector, out var list))
        {
            list = new List<RectTransform>();
            targets[selector] = list;
        }
        if (!list.Contains(rt)) list.Add(rt);
    }

    public static void Unregister(string selector, RectTransform rt)
    {
        if (string.IsNullOrEmpty(selector)) return;

        if (targets.TryGetValue(selector, out var list))
        {
            list.Remove(rt);
            if (list.Count == 0) targets.Remove(selector);
        }
    }

    // 셀렉터에 등록된 오브젝트 목록(없으면 null)
    public static List<RectTransform> Get(string selector)
        => (!string.IsNullOrEmpty(selector) && targets.TryGetValue(selector, out var list)) ? list : null;
}

// 튜토리얼 타겟 UI에 붙여 셀렉터 키로 자신을 등록한다. selector = 시트 TargetSelector와 동일 문자열.
// 같은 selector를 여러 오브젝트에 붙이면 모두 등록된다.
public class TutorialTarget : MonoBehaviour
{
    [SerializeField] string selector; // 예: "#forge-btn", ".fm-btn[data-n=\"10\"]"

    RectTransform rt;

    void Awake() => rt = transform as RectTransform;

    void OnEnable() => TutorialTargetRegistry.Register(selector, rt);
    void OnDisable() => TutorialTargetRegistry.Unregister(selector, rt);
}
