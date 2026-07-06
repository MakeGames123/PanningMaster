using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 노드를 클릭하면 하단에 뜨는 상세 패널.
// 연구 가능: 이름/레벨/효과(현재 ▸ 다음)/비용 + [연구] 버튼
// 연구 중: 남은 시간 표시 / 마스터·잠김: 상태 문구
public class LabDetailPanel : MonoBehaviour
{
    [Header("공통")]
    [SerializeField] GameObject root;             // 패널 전체(보이기/숨기기)
    [SerializeField] TextMeshProUGUI nameText;    // 노드 이름
    [SerializeField] TextMeshProUGUI levelText;   // "Lv.0/3"
    [SerializeField] TextMeshProUGUI effectText;  // "공격력 +0 ▸ +1"

    [Header("연구 가능 시")]
    [SerializeField] GameObject enhanceGroup;     // 비용 + 버튼 묶음
    [SerializeField] TextMeshProUGUI costText;    // "30 (보유 11k)"
    [SerializeField] Button researchButton;       // "연구"

    [Header("상태 표시")]
    [SerializeField] GameObject statusGroup;      // 상태 문구 묶음
    [SerializeField] TextMeshProUGUI statusText;  // 연구중/마스터/잠김 문구

    int currentId = -1;
    bool hasCurrent;

    void Awake()
    {
        if (researchButton != null) researchButton.onClick.AddListener(OnResearch);
        Hide();
    }

    public void Show(int nodeId)
    {
        currentId = nodeId;
        hasCurrent = true;
        if (root != null) root.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        hasCurrent = false;
        if (root != null) root.SetActive(false);
    }

    public void Refresh()
    {
        if (!hasCurrent) return;

        LaboratoryManager mgr = LaboratoryManager.Instance;
        LabNodeData node = mgr != null ? mgr.GetNode(currentId) : null;
        if (node == null) return;

        int level = mgr.GetLevel(currentId);
        LabNodeState state = mgr.GetState(currentId);
        bool available = state == LabNodeState.Available;

        if (nameText != null) nameText.text = node.name;
        if (levelText != null) levelText.text = $"Lv.{level}/{node.maxLevel}";

        float cur = level * node.amount;
        if (effectText != null)
        {
            effectText.text = available
                ? $"{node.FormatEffect(cur)} -> +{(level + 1) * node.amount:0.##}"
                : node.FormatEffect(cur);
        }

        if (enhanceGroup != null) enhanceGroup.SetActive(available);
        if (statusGroup != null) statusGroup.SetActive(!available);

        if (available)
        {
            long cost = mgr.GetCost(currentId);
            long owned = DataManager.Instance.Gold.GetValue();

            if (costText != null) costText.text = $"{cost} (보유 {Abbrev(owned)})";
            if (researchButton != null) researchButton.interactable = owned >= cost;
        }
        else if (statusText != null)
        {
            statusText.text = state switch
            {
                LabNodeState.Researching => $"연구 중 {FormatTime(mgr.GetRemaining(currentId))}",
                LabNodeState.Mastered => "최대 연구 도달",
                _ => "선행 연구 필요"
            };
        }
    }

    void OnResearch()
    {
        if (!hasCurrent) return;
        LaboratoryManager.Instance.TryResearch(currentId);
        // onTreeChanged -> LaboratoryPanel.RefreshAll -> 이 패널 Refresh
    }

    // 남은 초 -> "mm:ss"
    static string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(seconds);
        return $"{total / 60:00}:{total % 60:00}";
    }

    static string Abbrev(long v)
    {
        if (v >= 1_000_000_000) return (v / 1_000_000_000f).ToString("0.#") + "B";
        if (v >= 1_000_000) return (v / 1_000_000f).ToString("0.#") + "M";
        if (v >= 1_000) return (v / 1_000f).ToString("0.#") + "k";
        return v.ToString();
    }
}
