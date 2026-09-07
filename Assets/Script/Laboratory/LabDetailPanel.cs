using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System.Collections.Generic;

// 노드를 클릭하면 하단에 뜨는 상세 패널. 레벨 개념 없음(노드당 1회 연구).
// 연구 가능: 이름/미연구/효과(현재 ▸ 연구 후)/비용 + [연구] 버튼
// 연구 중: 남은 시간 + ⏳단축 / 완료·잠김: 상태 문구
public class LabDetailPanel : MonoBehaviour
{
    [Header("공통")]
    [SerializeField] GameObject root;             // 패널 전체(보이기/숨기기)
    [SerializeField] TextMeshProUGUI nameText;    // 노드 이름
    [FormerlySerializedAs("levelText")]
    [SerializeField] TextMeshProUGUI researchedText; // "연구 완료"/"미연구"
    [SerializeField] TextMeshProUGUI effectText;  // "공격력 +0 ▸ +1"

    [Header("연구 가능 시")]
    [SerializeField] GameObject enhanceGroup;     // 비용 + 버튼 묶음
    [SerializeField] TextMeshProUGUI timeText;    // "연구 시간 2시간 30분"
    [SerializeField] Button researchButton;       // "연구"

    [Header("상태 표시")]
    [SerializeField] GameObject statusGroup;      // 상태 문구 묶음(연구 가능 아닐 때 표시)
    [FormerlySerializedAs("statusText")]
    [SerializeField] TextMeshProUGUI remainTimeText; // 연구 중일 때만: 남은 시간 "1시간 59분"
    [SerializeField] TextMeshProUGUI prereqText;     // 잠김일 때만: "선행연구 : A, B" (미완료 선행 노드 이름)

    [Header("연구 중 — ⏳ 시간 단축(프로토: 1개=30분)")]
    [SerializeField] GameObject speedupGroup;         // 진행바 + 단축 버튼 묶음(연구 중일 때만 표시)
    [SerializeField] List<Slider> progressSliders = new(); // 30분=슬라이더 1개(왼쪽부터). 총 시간 개수만큼 활성 — 1번이 다 차면 2번이 차는 식
    [SerializeField] Button reduceButton;             // "⏳1 -30분"
    [SerializeField] Button instantButton;            // "⏳N 즉시완료"
    [SerializeField] TextMeshProUGUI instantLabel;    // 즉시완료 버튼 라벨(비용 갱신)

    int currentId = -1;
    bool hasCurrent;
    float nextTickAt; // 연구 중 남은 시간 1초 갱신

    void Awake()
    {
        if (researchButton != null) researchButton.onClick.AddListener(OnResearch);
        if (reduceButton != null) reduceButton.onClick.AddListener(OnReduce);
        if (instantButton != null) instantButton.onClick.AddListener(OnInstant);
    }

    void Update()
    {
        // 연구 중이면 남은 시간/게이지를 1초 간격으로 갱신(이벤트 없이도 흐르는 시간 반영)
        if (!hasCurrent || Time.unscaledTime < nextTickAt) return;
        nextTickAt = Time.unscaledTime + 1f;

        var mgr = LaboratoryManager.Instance;
        if (mgr != null && mgr.IsResearching(currentId)) Refresh();
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

        LabNodeState state = mgr.GetState(currentId);
        bool available = state == LabNodeState.Available;
        bool researched = mgr.IsResearched(currentId);

        if (nameText != null) nameText.text = node.name;
        if (researchedText != null)
        {
            // 연구 중에는 숨김(진행 뷰가 대신 표시) — 그 외엔 연구 완료/미연구
            researchedText.gameObject.SetActive(state != LabNodeState.Researching);
            researchedText.text = researched ? "연구 완료" : "미연구";
        }

        // 보상 표기는 화살표 없이 항상 "공격력 +5%" 고정형
        if (effectText != null) effectText.text = node.FormatEffect(node.amount);

        if (enhanceGroup != null) enhanceGroup.SetActive(available);
        if (statusGroup != null) statusGroup.SetActive(!available);

        if (available)
        {
            long cost = mgr.GetCost(currentId);
            long owned = DataManager.Instance.Gold.GetValue();

            if (timeText != null) timeText.text = $"연구 시간 {FormatDuration(node.timeSeconds)}";
            if (researchButton != null) researchButton.interactable = owned >= cost;
        }
        else
        {
            // 연구 중 = 남은 시간 / 잠김 = 미완료 선행 노드 나열 / 완료 = 둘 다 숨김(researchedText가 "연구 완료" 표시)
            bool isResearching = state == LabNodeState.Researching;
            bool isLocked = state == LabNodeState.Locked;

            if (remainTimeText != null)
            {
                remainTimeText.gameObject.SetActive(isResearching);
                if (isResearching) remainTimeText.text = FormatDuration(mgr.GetRemaining(currentId));
            }
            if (prereqText != null)
            {
                prereqText.gameObject.SetActive(isLocked);
                if (isLocked) prereqText.text = $"선행연구 : {UnmetPrereqNames(mgr)}";
            }
        }

        RefreshSpeedup(mgr, state);
    }

    // 아직 완료하지 못한 선행 노드 이름들 — "A, B" 형식
    string UnmetPrereqNames(LaboratoryManager mgr)
    {
        if (LabEdgeLoader.Instance == null) return "";

        var names = new System.Collections.Generic.List<string>();
        foreach (int pre in LabEdgeLoader.Instance.GetPrerequisites(currentId))
        {
            if (mgr.IsResearched(pre)) continue;
            var node = mgr.GetNode(pre);
            names.Add(node != null ? node.name : pre.ToString());
        }
        return string.Join(", ", names);
    }

    // ⏳ 단축 UI — 연구 중일 때만: 진행 게이지 + [⏳1 -30분] + [⏳N 즉시완료]
    void RefreshSpeedup(LaboratoryManager mgr, LabNodeState state)
    {
        bool researching = state == LabNodeState.Researching;
        if (speedupGroup != null) speedupGroup.SetActive(researching);
        if (!researching) return;

        int owned = DataManager.Instance != null ? DataManager.Instance.hourglass : 0;
        int instantCost = mgr.InstantCompleteCost(currentId); // = ceil(남은 시간/30분)

        // 슬라이더 1개 = 30분 구간. 총 시간만큼만 활성(1시간 반 = 3개)하고,
        // 경과 시간이 자기 구간에 들어오면 그 슬라이더가 차오른다(1번 완료 → 2번 진행 → …).
        var node = mgr.GetNode(currentId);
        float totalSec = node != null ? node.timeSeconds : 0f;
        float elapsed = Mathf.Max(0f, totalSec - mgr.GetRemaining(currentId));
        int cells = totalSec > 0f
            ? Mathf.Max(1, Mathf.CeilToInt(totalSec / LaboratoryManager.HourglassSeconds))
            : 0;

        for (int i = 0; i < progressSliders.Count; i++)
        {
            var s = progressSliders[i];
            if (s == null) continue;

            bool on = i < cells;
            if (s.gameObject.activeSelf != on) s.gameObject.SetActive(on);
            if (!on) continue;

            float chunkStart = i * LaboratoryManager.HourglassSeconds;
            float chunkDur = Mathf.Min(LaboratoryManager.HourglassSeconds, totalSec - chunkStart);
            s.value = chunkDur > 0f ? Mathf.Clamp01((elapsed - chunkStart) / chunkDur) : 0f;
        }

        if (instantLabel != null) instantLabel.text = $"⏳{instantCost} 즉시완료";
        if (reduceButton != null) reduceButton.interactable = owned >= 1;
        if (instantButton != null) instantButton.interactable = owned >= instantCost;
    }

    void OnResearch()
    {
        if (!hasCurrent) return;
        LaboratoryManager.Instance.TryResearch(currentId);
        // onTreeChanged -> LaboratoryPanel.RefreshAll -> 이 패널 Refresh
    }

    void OnReduce()
    {
        if (!hasCurrent) return;
        LaboratoryManager.Instance.TryReduceTime(currentId); // ⏳1 = -30분(30분 이하면 즉시 완료)
        Refresh();
    }

    void OnInstant()
    {
        if (!hasCurrent) return;
        LaboratoryManager.Instance.TryInstantComplete(currentId); // ⏳ceil(남은/30분) 소모
        Refresh();
    }

    // 총 소요 초 -> "2시간 30분" / "30분" / "45초"
    static string FormatDuration(float seconds)
    {
        int total = Mathf.CeilToInt(seconds);
        int h = total / 3600;
        int m = (total % 3600) / 60;
        int s = total % 60;

        if (h > 0) return m > 0 ? $"{h}시간 {m}분" : $"{h}시간";
        if (m > 0) return s > 0 ? $"{m}분 {s}초" : $"{m}분";
        return $"{s}초";
    }
}
