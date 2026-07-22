using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 업적 바 표시 전용 뷰(QuestUI와 동일 구조). 상태/판정 로직 없음 — AchievementPanel이 넘겨주는 값을 그리기만 한다.
// 수령 가능한(깨진) 업적 하나만 표시: [💎 xN] 백전노장 · 처치 2천/2천 [받기]
public class AchievementUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;         // "백전노장"
    [SerializeField] TextMeshProUGUI descText;         // "처치 2천/2천"
    [SerializeField] TextMeshProUGUI rewardIconText;   // "💎"
    [SerializeField] TextMeshProUGUI rewardCountText;  // "x50"
    [SerializeField] Button actionButton;              // 보상 수령
    [SerializeField] TextMeshProUGUI actionButtonText; // 버튼 라벨(옵션)

    // 버튼 클릭 통지 — AchievementPanel이 구독해서 보상 수령
    public event Action OnActionClicked;

    void Awake()
    {
        if (actionButton != null) actionButton.onClick.AddListener(() => OnActionClicked?.Invoke());
    }

    public void Show(AchievementData a, long progress, long threshold, int gems)
    {
        if (a == null) { Hide(); return; }
        gameObject.SetActive(true);

        if (nameText != null) nameText.text = a.nameKo;
        if (descText != null)
            descText.text = $"{a.labelKo} {NumberFormatLoader.Abbrev(progress)}/{NumberFormatLoader.Abbrev(threshold)}";
        if (rewardIconText != null) rewardIconText.text = "💎"; // 업적 보상은 젬 고정
        if (rewardCountText != null) rewardCountText.text = gems > 0 ? $"x{gems}" : "";
    }

    public void Hide() => gameObject.SetActive(false);
}
