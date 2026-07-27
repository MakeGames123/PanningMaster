using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 동료 목록 패널(프로토 renderChars의 chg-grid 포팅).
// 정렬 = 등급 좋은순 → 시트 정의순. 보유 여부와 무관한 고정 배치(유저 확정 — 획득해도 카드가 안 움직임).
public class CharacterListPanel : MonoBehaviour
{
    [SerializeField] RectTransform gridContent;
    [SerializeField] CharacterDragGhost dragGhost;     // 카드들에 주입(프리팹은 씬 참조를 저장 못 함)
    [SerializeField] CharacterDetailPopup detailPopup; // 〃 — 카드 클릭 시 열림
    [SerializeField] RecruitCeremonyPanel recruitCeremony; // 모집 결과창(인스펙터 연결)
    [SerializeField] TextMeshProUGUI countText;   // 보유/전체 (예: 9/18)
    [SerializeField] TextMeshProUGUI scrollText;  // 🪪 보유 수
    [SerializeField] Button recruitButton;        // 모집 x1(🪪 1)
    [SerializeField] Button recruit10Button;      // 모집 x10(🪪 10)

    readonly List<CharacterCardUI> cards = new();
    bool bound;

    void Awake()
    {
        if (recruitButton != null) recruitButton.onClick.AddListener(() => Recruit(1));
        if (recruit10Button != null) recruit10Button.onClick.AddListener(() => Recruit(10));
    }

    void Update()
    {
        // 매니저·시트가 준비되는 즉시 1회 바인딩(로드 순서 안전망 — GrowthManager와 동일 문법)
        if (!bound) TryBind();
    }

    void TryBind()
    {
        var mgr = CharacterManager.Instance;
        if (mgr == null || !mgr.IsReady) return;

        // 에디터에서 gridContent 밑에 미리 배치해둔 카드들을 수집(생성 안 함, 비활성 포함)
        gridContent.GetComponentsInChildren(true, cards);

        // 카드 ↔ 캐릭터 1회 고정 배정: 등급 내림차순 → 동급 내 시트 역순(유저 확정 — 시트 맨 밑이 1번 슬롯, 레온이 맨 끝).
        // 보유 여부와 무관한 고정 배치 — 이후 Refresh는 상태만 다시 그린다.
        var reversed = CharacterRosterLoader.Instance.AllOrdered();
        reversed.Reverse();
        var sorted = reversed
            .OrderByDescending(c => c.grade) //안정 정렬이라 동급 내에선 역순이 유지된다
            .ToList();

        if (cards.Count < sorted.Count)
            Debug.LogWarning($"[동료목록] gridContent의 카드가 부족합니다 ({cards.Count}/{sorted.Count})");

        for (int i = 0; i < cards.Count; i++)
        {
            if (i >= sorted.Count) { cards[i].gameObject.SetActive(false); continue; }

            cards[i].SetCharacter(sorted[i], CharacterGradeLoader.Instance.Get(sorted[i].grade));

            // 드래그/클릭에 고스트·팝업 참조 주입 — 카드 18장을 일일이 배선하지 않기 위함
            var drag = cards[i].GetComponent<CharacterCardDrag>();
            if (drag != null) drag.Inject(dragGhost, detailPopup);
        }

        mgr.onChanged.AddListener(Refresh);

        if (DataManager.Instance != null)
        {
            DataManager.Instance.onScrollChanged.AddListener(OnScrollChanged);
            OnScrollChanged(DataManager.Instance.scroll);
        }

        bound = true;
        Refresh();
    }

    void OnDestroy()
    {
        if (!bound) return;
        if (CharacterManager.Instance != null) CharacterManager.Instance.onChanged.RemoveListener(Refresh);
        if (DataManager.Instance != null) DataManager.Instance.onScrollChanged.RemoveListener(OnScrollChanged);
    }

    void OnScrollChanged(int value)
    {
        if (scrollText != null) scrollText.text = "🪪 " + value;
    }

    void Recruit(int count)
    {
        // 결과창이 연결돼 있으면 모집 + 세리머니 표시, 없으면 직접 모집(폴백)
        if (recruitCeremony != null) { recruitCeremony.RecruitAndShow(count); return; }

        var recruiter = CharacterRecruiter.Instance;
        if (recruiter == null || recruiter.TryRecruitMany(count) == null) Debug.Log("[모집] 🪪 모집서가 부족합니다");
    }

    // 상태 변화마다 각 카드가 자기 상태만 다시 그린다(배정·위치는 TryBind에서 1회 고정)
    void Refresh()
    {
        foreach (var card in cards) card.Refresh();

        if (countText != null)
        {
            var mgr = CharacterManager.Instance;
            var roster = CharacterRosterLoader.Instance.AllOrdered();
            countText.text = roster.Count(c => mgr.IsOwned(c.id)) + "/" + roster.Count;
        }
    }
}
