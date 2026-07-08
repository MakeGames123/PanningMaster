using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 연구소 패널. 자식 노드 UI들을 모아 갱신하고, 노드 선택 -> 하단 상세 패널을 조율한다.
// 페이지 시스템: 이전 페이지의 엔드 노드(LabEdge 시트의 To=EndNode)를 전부 마스터해야 다음 페이지가 열린다.
public class LaboratoryPanel : MonoBehaviour
{
    [Header("페이지")]
    [Tooltip("페이지 순서대로 등록. 각 페이지 루트 밑에 해당 페이지의 LabNodeUI 배치")]
    [SerializeField] List<GameObject> pages = new();
    [SerializeField] Button pageLeftButton;
    [SerializeField] Button pageRightButton;

    [SerializeField] LabDetailPanel detailPanel;

    // 페이지별 노드 UI 목록 (pages 와 같은 인덱스)
    readonly List<List<LabNodeUI>> pageNodeUIs = new();
    int currentPage;
    LabNodeUI selected;
    bool subscribed;

    void Awake()
    {
        foreach (GameObject page in pages)
        {
            List<LabNodeUI> uis = new();
            if (page != null) uis.AddRange(page.GetComponentsInChildren<LabNodeUI>(true));
            pageNodeUIs.Add(uis);

            foreach (LabNodeUI ui in uis)
                ui.OnClicked = HandleNodeClicked;
        }

        if (pageLeftButton != null) pageLeftButton.onClick.AddListener(() => MovePage(-1));
        if (pageRightButton != null) pageRightButton.onClick.AddListener(() => MovePage(1));
    }

    void OnEnable()
    {
        TrySubscribe();
        ShowPage(currentPage);
    }

    void Start()
    {
        TrySubscribe();
        ShowPage(currentPage);
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
        LaboratoryManager.Instance.onResearchCompleted.AddListener(UpdatePageButtons); //연구 완료 시에만 버튼 갱신
        DataManager.Instance.Gold.onValueChanged += OnGoldChanged;
        subscribed = true;
    }

    void OnDestroy()
    {
        if (!subscribed) return;
        if (LaboratoryManager.Instance != null)
        {
            LaboratoryManager.Instance.onTreeChanged.RemoveListener(RefreshAll);
            LaboratoryManager.Instance.onResearchCompleted.RemoveListener(UpdatePageButtons);
        }
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

    #region 페이지

    void MovePage(int dir)
    {
        int next = Mathf.Clamp(currentPage + dir, 0, Mathf.Max(0, pages.Count - 1));
        if (next == currentPage) return;
        if (dir > 0 && !IsPageUnlocked(next)) return; //안 열린 페이지로는 이동 불가

        ShowPage(next);
    }

    void ShowPage(int index)
    {
        currentPage = index;

        for (int i = 0; i < pages.Count; i++)
            if (pages[i] != null) pages[i].SetActive(i == currentPage);

        // 페이지가 바뀌면 선택 해제
        if (selected != null) { selected.SetSelected(false); selected = null; }
        if (detailPanel != null) detailPanel.Hide();

        RefreshAll();
        UpdatePageButtons(); //초기/페이지 전환 시 버튼 상태 반영
    }

    // 해당 페이지가 열렸는지: 이전 페이지의 엔드 노드를 전부 최대로 연구(마스터)해야 개방
    bool IsPageUnlocked(int index)
    {
        if (index <= 0) return true;
        if (index >= pages.Count) return false;

        LaboratoryManager mgr = LaboratoryManager.Instance;
        LabEdgeLoader edge = LabEdgeLoader.Instance;
        if (mgr == null || edge == null || !edge.IsLoaded) return false; //시트 로드 전에는 잠금

        bool hasEndNode = false;
        foreach (LabNodeUI ui in pageNodeUIs[index - 1])
        {
            if (ui == null || !edge.IsEndNode(ui.NodeId)) continue;

            hasEndNode = true;
            if (!mgr.IsMastered(ui.NodeId)) //엔드 노드 하나라도 마스터 전이면 잠금
            {
                Debug.Log($"[Lab] {index + 1}페이지 잠금: 엔드 노드 {ui.NodeId} 미마스터 ({mgr.GetLevel(ui.NodeId)}/{mgr.GetMaxLevel(ui.NodeId)})");
                return false;
            }
        }
        return hasEndNode; //엔드 노드가 지정 안 된 페이지는 열리지 않음(시트 설정 필요)
    }

    void UpdatePageButtons()
    {
        if (pageLeftButton != null)
            pageLeftButton.gameObject.SetActive(currentPage > 0);

        if (pageRightButton != null)
            pageRightButton.gameObject.SetActive(
                currentPage < pages.Count - 1 && IsPageUnlocked(currentPage + 1));
    }

    #endregion

    public void RefreshAll()
    {
        foreach (List<LabNodeUI> uis in pageNodeUIs)
            foreach (LabNodeUI ui in uis)
                if (ui != null) ui.Refresh();

        if (detailPanel != null) detailPanel.Refresh();
    }
}
