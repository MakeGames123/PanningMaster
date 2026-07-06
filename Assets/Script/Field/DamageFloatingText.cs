using DG.Tweening;
using TMPro;
using UnityEngine;

// Enumy 피격 데미지 플로팅 텍스트.
// 한 루틴(리볼버 6발)당 최대 6번만 뜨므로 풀 매니저 없이 6개만 생성해 순환 사용한다.
public class DamageFloatingText : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI textPrefab;   // 복제할 텍스트 프리팹
    [SerializeField] RectTransform spawnParent;    // 생성 위치 부모(비우면 자신)
    [SerializeField] int count = 6;
    [SerializeField] float floatDuration = 0.8f;   // 떠오르며 사라지는 시간
    [SerializeField] float floatDistance = 120f;   // 위로 떠오르는 거리(px)
    [SerializeField] float spreadX = 40f;          // 좌우 랜덤 퍼짐(px)
    [SerializeField] Color color = Color.white;

    TextMeshProUGUI[] texts;
    int index;

    void Awake()
    {
        if (spawnParent == null) spawnParent = GetComponent<RectTransform>();
        if (textPrefab == null) return;

        // 딱 count개만 생성해두고 돌려쓴다
        texts = new TextMeshProUGUI[count];
        for (int i = 0; i < count; i++)
        {
            TextMeshProUGUI t = Instantiate(textPrefab, spawnParent);
            SetAlpha(t, 0f);
            texts[i] = t;
        }
    }

    // 데미지 텍스트 1개 띄우기
    public void Show(float damage)
    {
        if (texts == null || texts.Length == 0) return;

        TextMeshProUGUI t = texts[index];
        index = (index + 1) % texts.Length; // 다음 텍스트로 순환

        RectTransform rt = t.rectTransform;
        rt.DOKill();

        t.text = $"{damage:F0}";
        SetAlpha(t, 1f);

        float x = Random.Range(-spreadX, spreadX);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.localScale = Vector3.one * 0.6f;

        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOAnchorPosY(floatDistance, floatDuration).SetEase(Ease.OutCubic));
        seq.Join(t.DOFade(0f, floatDuration).SetEase(Ease.InQuad));
        seq.Join(rt.DOScale(1f, 0.15f).SetEase(Ease.OutBack)); // 살짝 팝 되는 등장
    }

    void SetAlpha(TextMeshProUGUI t, float a)
    {
        if (t == null) return;
        Color c = color;
        c.a = a;
        t.color = c;
    }

    void OnDisable()
    {
        if (texts == null) return;
        foreach (TextMeshProUGUI t in texts)
            if (t != null) t.rectTransform.DOKill();
    }
}
