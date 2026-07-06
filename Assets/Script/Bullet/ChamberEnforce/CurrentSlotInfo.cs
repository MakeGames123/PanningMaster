using TMPro;
using UnityEngine;

// 하단 정보 패널. 선택된 슬롯의 강화 수치/효과/확률/비용을 표시한다.
public class CurrentSlotInfo : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI enforceAmount; // 큰 "+0"
    [SerializeField] TextMeshProUGUI effect;        // "최종 데미지 +x%"
    [SerializeField] TextMeshProUGUI slotNum;       // "1번 슬롯"
    [SerializeField] TextMeshProUGUI percentage;    // "강화 성공 확률 90.0%"
    [SerializeField] TextMeshProUGUI failText;      // "실패해도 레벨은 유지됩니다"
    [SerializeField] TextMeshProUGUI cost;          // "강화 비용 300 (보유 11k)"

    public void Show(ChamberEnforceSlot slot, float effectPerLevel, int costValue, long ownedGold)
    {
        if (slot == null) return;

        if (enforceAmount != null) enforceAmount.text = $"+{slot.Level}";
        if (effect != null) effect.text = $"최종 데미지 +{slot.Level * effectPerLevel:0.#}%";
        if (slotNum != null) slotNum.text = $"{slot.SlotIndex + 1}번 슬롯";
        if (percentage != null) percentage.text = $"강화 성공 확률 {slot.SuccessRate:0.0}%";
        if (failText != null) failText.text = "실패해도 레벨은 유지됩니다";
        if (cost != null) cost.text = $"강화 비용 {costValue} (보유 {Abbrev(ownedGold)})";
    }

    static string Abbrev(long v)
    {
        if (v >= 1_000_000) return (v / 1_000_000f).ToString("0.#") + "M";
        if (v >= 1_000) return (v / 1_000f).ToString("0.#") + "k";
        return v.ToString();
    }
}
