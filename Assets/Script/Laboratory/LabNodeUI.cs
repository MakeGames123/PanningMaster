using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스킬트리 노드 1개의 UI. 클릭하면 선택되어 하단 상세 패널이 뜬다.
// 시트에서 로드된 노드 id로 매니저를 조회한다.
public class LabNodeUI : MonoBehaviour
{
    [SerializeField] int nodeId;

    [Header("UI 참조")]
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI levelText;  // "5/5"
    [SerializeField] Image progressFill;         // 연구 진행 게이지(0~1, 선택)
    [SerializeField] GameObject selectedOutline; // 선택 시 강조(이중 링)

    [Header("상태별 색상")]
    [SerializeField] Color lockedColor = new(0.3f, 0.33f, 0.4f);
    [SerializeField] Color availableColor = new(0.4f, 0.75f, 1f);
    [SerializeField] Color researchingColor = new(0.5f, 0.9f, 0.6f);
    [SerializeField] Color masteredColor = new(1f, 0.85f, 0.3f);

    public int NodeId => nodeId;
    public System.Action<LabNodeUI> OnClicked;

    void Awake()
    {
        if (button != null) button.onClick.AddListener(() => OnClicked?.Invoke(this));
        SetSelected(false);
    }

    public void SetSelected(bool on)
    {
        if (selectedOutline != null) selectedOutline.SetActive(on);
    }

    public void Refresh()
    {
        LaboratoryManager mgr = LaboratoryManager.Instance;
        if (mgr == null) return;

        LabNodeState state = mgr.GetState(nodeId);
        int level = mgr.GetLevel(nodeId);
        int max = mgr.GetMaxLevel(nodeId);

        if (levelText != null) levelText.text = $"{level}/{max}";

        Color ringColor = state switch
        {
            LabNodeState.Locked => lockedColor,
            LabNodeState.Researching => researchingColor,
            LabNodeState.Mastered => masteredColor,
            _ => availableColor
        };

        if (progressFill != null)
            progressFill.fillAmount = state == LabNodeState.Researching ? mgr.GetProgress(nodeId) : 0f;

        if (button != null) button.interactable = true; // 항상 정보 확인 가능
    }
}
