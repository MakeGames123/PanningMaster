using System.Collections.Generic;
using UnityEngine;

// 연구소 패널. 자식 노드 UI들을 모아 갱신하고, 노드 선택 -> 하단 상세 패널을 조율한다.
public class LaboratoryPanel : MonoBehaviour
{
    [Tooltip("비워두면 자식에서 자동으로 수집")]
    [SerializeField] List<LabNodeUI> nodeUIs = new();
    [SerializeField] LabDetailPanel detailPanel;

    LabNodeUI selected;
    bool subscribed;

    void Awake()
    {
        if (nodeUIs.Count == 0)
            nodeUIs.AddRange(GetComponentsInChildren<LabNodeUI>(true));

        foreach (LabNodeUI ui in nodeUIs)
            if (ui != null) ui.OnClicked = HandleNodeClicked;
    }

    void OnEnable()
    {
        TrySubscribe();
        RefreshAll();
    }

    void Start()
    {
        TrySubscribe();
        RefreshAll();
    }

    void Update()
    {
        // 연구 진행 중에는 남은 시간/게이지를 매 프레임 갱신
        if (LaboratoryManager.Instance != null && LaboratoryManager.Instance.AnyResearching)
            RefreshAll();
    }

    void TrySubscribe()
    {
        if (subscribed || LaboratoryManager.Instance == null) return;

        LaboratoryManager.Instance.onTreeChanged.AddListener(RefreshAll);
        DataManager.Instance.Gold.onValueChanged += OnGoldChanged;
        subscribed = true;
    }

    void OnDestroy()
    {
        if (!subscribed) return;
        if (LaboratoryManager.Instance != null)
            LaboratoryManager.Instance.onTreeChanged.RemoveListener(RefreshAll);
        if (DataManager.Instance != null)
            DataManager.Instance.Gold.onValueChanged -= OnGoldChanged;
    }

    void OnGoldChanged(long _) => RefreshAll();

    void HandleNodeClicked(LabNodeUI ui)
    {
        if (selected != null) selected.SetSelected(false);
        selected = ui;
        selected.SetSelected(true);

        if (detailPanel != null) detailPanel.Show(ui.NodeId);
    }

    public void RefreshAll()
    {
        foreach (LabNodeUI ui in nodeUIs)
            if (ui != null) ui.Refresh();

        if (detailPanel != null) detailPanel.Refresh();
    }
}
