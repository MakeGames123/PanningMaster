using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 퀘스트 바 표시 전용 뷰. 상태/판정 로직 없음 — QuestPanel이 넘겨주는 값을 그리기만 한다.
// 진행 중: [보상아이콘 xN] 퀘스트 6 / 약실 강화 2회 1/2 [»]
// 클리어:  버튼이 보상 수령 표시(받기)로 바뀐다
public class QuestUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI titleText;        // "퀘스트 6"
    [SerializeField] TextMeshProUGUI descText;         // "약실 강화 2회 1/2"
    [SerializeField] TextMeshProUGUI rewardIconText;   // "🧰" (임시 이모지)
    [SerializeField] TextMeshProUGUI rewardCountText;  // "x10"
    [SerializeField] Button actionButton;              // 진행 중=이동(») / 클리어=보상 받기
    [SerializeField] TextMeshProUGUI actionButtonText; // 버튼 라벨(옵션 — 없으면 라벨 유지)

    // 버튼 클릭 통지 — QuestPanel이 구독해서 네비/보상수령 분기
    public event Action OnActionClicked;

    void Awake()
    {
        if (actionButton != null) actionButton.onClick.AddListener(() => OnActionClicked?.Invoke());
    }

    public void Show(QuestMainData quest, long progress, bool claimable)
    {
        if (quest == null) { Hide(); return; }
        gameObject.SetActive(true);

        if (titleText != null) titleText.text = $"퀘스트 {quest.id}";
        if (descText != null) descText.text = $"{quest.descKo} {progress}/{quest.goal}";
        if (rewardIconText != null) rewardIconText.text = RewardIcon(quest.rewardType);
        if (rewardCountText != null)
            rewardCountText.text = quest.rewardAmount > 0 ? $"x{quest.rewardAmount}" : ""; // 수식 보상은 수량 미표기
        if (actionButtonText != null) actionButtonText.text = claimable ? "받기" : "»";
    }

    public void Hide() => gameObject.SetActive(false);

    // 보상 재화 → 임시 이모지(포팅 시 스프라이트로 치환)
    static string RewardIcon(string rewardType) => rewardType switch
    {
        "Gold" => "🪙",
        "Ticket" => "🎫",
        "Gem" => "💎",
        "KeyGold" => "🗝️",
        "KeyResearch" => "🔐",
        "KeyArmory" => "🔑",
        "Hourglass" => "⏳",
        "Crate" => "🧰",
        _ => "🎁"
    };
}
