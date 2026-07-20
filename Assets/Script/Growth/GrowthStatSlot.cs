using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 성장 탭 한 줄(스탯 1개) 표시 뷰. 상태/판정 로직 없음 — GrowthPanel이 넘겨주는 값을 그리기만 한다.
// [아이콘] 공격력 Lv.20 / 현재 +40% → +42%   [비용버튼 3728]
public class GrowthStatSlot : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI iconText;       // "⚔️"
    [SerializeField] TextMeshProUGUI nameLevelText;  // "공격력 Lv.20"
    [SerializeField] TextMeshProUGUI effectText;     // "현재 +40% → +42%"
    [SerializeField] TextMeshProUGUI costText;       // "3728"
    [SerializeField] Button upgradeButton;           // 비용 버튼(누르면 레벨업)

    string statId;
    GrowthPanel owner;

    public void Bind(string statId, GrowthPanel owner)
    {
        this.statId = statId;
        this.owner = owner;

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => this.owner.OnUpgradeClicked(this.statId));
        }
    }

    public void Refresh()
    {
        var loader = GrowthStatLoader.Instance;
        var mgr = GrowthManager.Instance;
        if (loader == null || mgr == null) return;
        if (string.IsNullOrEmpty(statId)) return; // 아직 Bind 전 — 갱신할 스탯이 없음

        var d = loader.Get(statId);
        if (d == null) { gameObject.SetActive(false); return; }
        gameObject.SetActive(true);

        int level = mgr.GetLevel(statId);
        string u = d.unit;

        //if (iconText != null) iconText.text = d.icon;
        if (nameLevelText != null) nameLevelText.text = $"{d.nameKo} Lv.{level}";
        if (effectText != null)
            effectText.text = $"현재 +{mgr.GetCurrentEffect(statId):0.##}{u} -> +{mgr.GetNextEffect(statId):0.##}{u}";
        if (costText != null) costText.text = NumberFormatLoader.Abbrev(mgr.GetCost(statId));
        if (upgradeButton != null) upgradeButton.interactable = mgr.CanUpgrade(statId);
    }
}
