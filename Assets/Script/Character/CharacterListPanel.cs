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
    [SerializeField] TextMeshProUGUI countText;   // 보유/전체 (예: 9/18)
    [SerializeField] TextMeshProUGUI scrollText;  // 🪪 보유 수
    [SerializeField] Button recruitButton;        // 모집(🪪 1)

    readonly List<CharacterCardUI> cards = new();
    bool bound;

    void Awake()
    {
        if (recruitButton != null) recruitButton.onClick.AddListener(Recruit);
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

    void Recruit()
    {
        var recruiter = CharacterRecruiter.Instance;
        if (recruiter == null || !recruiter.TryRecruit()) Debug.Log("[모집] 🪪 모집서가 부족합니다");
    }

    // 상태 변화마다 전체 재배치(로스터 18장 고정 — 카드는 재사용, 정렬만 다시)
    void Refresh()
    {
        var mgr = CharacterManager.Instance;
        var roster = CharacterRosterLoader.Instance.AllOrdered();

        // 보유 여부는 정렬에 안 씀 — 모집으로 획득해도 카드 위치가 고정되도록(등급 내림차순→시트 정의순, OrderBy 안정 정렬)
        var sorted = roster
            .OrderByDescending(c => c.grade)
            .ToList();

        if (cards.Count < sorted.Count)
            Debug.LogWarning($"[동료목록] gridContent의 카드가 부족합니다 ({cards.Count}/{sorted.Count})");

        for (int i = 0; i < cards.Count; i++)
        {
            if (i >= sorted.Count) { cards[i].gameObject.SetActive(false); continue; }

            var c = sorted[i];
            var grade = CharacterGradeLoader.Instance.Get(c.grade);
            cards[i].gameObject.SetActive(true);
            if (mgr.IsOwned(c.id)) cards[i].SetOwned(c, grade, mgr.GetState(c.id));
            else cards[i].SetUnowned(c, grade);
        }

        if (countText != null)
            countText.text = roster.Count(c => mgr.IsOwned(c.id)) + "/" + roster.Count;
    }
}
