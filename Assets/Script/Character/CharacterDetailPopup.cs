using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 캐릭터 상세 팝업(프로토 chFullOpen/chFullRender 포팅) — 카드 클릭으로 열림.
// 보유: Lv/캡·🃏·개별 전투력·화력 ×배·고유 패시브·성장 게이지 + [성장]/[배치] 버튼.
// 미보유: 실루엣 + 잠금 안내. ◀▶로 로스터(시트순) 순환. 해방(★)·속성 배지는 미구현.
public class CharacterDetailPopup : MonoBehaviour
{
    [SerializeField] GameObject view; // 팝업 루트(꺼진 채 시작 — 이 스크립트는 항상 켜진 부모에)
    [SerializeField] PartyPlayerActivator partyPlayers; // 개별 전투력 조회용(인스펙터 연결)
    [SerializeField] CharacterSlotPicker slotPicker;    // [배치] → 자리 피커(인스펙터 연결)
    [SerializeField] Button closeButton;
    [SerializeField] Button prevButton;
    [SerializeField] Button nextButton;

    [Header("헤드")]
    [SerializeField] Image mainImage;
    [SerializeField] TextMeshProUGUI gradeText;  // 등급 배지(등급색)
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI starsText;  // 해방 미구현 — 빈 별 4개 고정

    [Header("본문")]
    [SerializeField] TextMeshProUGUI levelText;      // Lv.X / 캡
    [SerializeField] TextMeshProUGUI cardsText;      // 🃏 n
    [SerializeField] TextMeshProUGUI powerValueText;      // 개별 전투력 값(미배치 = —)
    [SerializeField] TextMeshProUGUI powerStateText; // "N번 자리" / "미배치" (선택 연결)
    [SerializeField] TextMeshProUGUI mulValueText;        // 화력 ×배
    [SerializeField] TextMeshProUGUI mulStateText;   // "리볼버 데미지 ×배" 설명(선택 연결)
    [SerializeField] TextMeshProUGUI passiveNameText;  // 스탯명(StatType 시트 — 아이콘은 UI 이미지 몫)
    [SerializeField] TextMeshProUGUI passiveValueText; // +n% 증가(Damage 패시브 = 화력 ×배)
    [SerializeField] Slider growthSlider;            // 성장 카드 게이지
    [SerializeField] TextMeshProUGUI growthText;     // 성장 카드 n / need

    [Header("버튼")]
    [SerializeField] Button growButton;
    [SerializeField] TextMeshProUGUI growCostText;   // 🃏 n / MAX
    [SerializeField] Button deployButton;
    [SerializeField] TextMeshProUGUI deployText;     // 배치 / N번 자리

    string currentId;
    bool bound;

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (prevButton != null) prevButton.onClick.AddListener(() => Nav(-1));
        if (nextButton != null) nextButton.onClick.AddListener(() => Nav(1));
        if (growButton != null) growButton.onClick.AddListener(Grow);
        if (deployButton != null) deployButton.onClick.AddListener(Deploy);

        if (view != null) view.SetActive(false);
    }

    void Update()
    {
        // 매니저·시트가 준비되는 즉시 1회 바인딩(로드 순서 안전망 — CharacterListPanel과 동일 문법)
        if (!bound) TryBind();
    }

    void TryBind()
    {
        var mgr = CharacterManager.Instance;
        if (mgr == null || !mgr.IsReady) return;

        mgr.onChanged.AddListener(OnChanged);
        bound = true;
    }

    void OnDestroy()
    {
        if (bound && CharacterManager.Instance != null)
            CharacterManager.Instance.onChanged.RemoveListener(OnChanged);
    }

    void OnChanged()
    {
        if (view != null && view.activeSelf) Refresh(); //열려 있는 동안 성장/모집/배치 반영
    }

    public void Open(string id)
    {
        if (CharacterRosterLoader.Instance == null || CharacterRosterLoader.Instance.Get(id) == null) return;

        currentId = id;
        if (view != null) view.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (view != null) view.SetActive(false);
        currentId = null;
    }

    void Nav(int dir)
    {
        var all = CharacterRosterLoader.Instance.AllOrdered();
        int i = all.FindIndex(c => c.id == currentId);
        if (i < 0) return;

        currentId = all[(i + dir + all.Count) % all.Count].id;
        Refresh();
    }

    void Grow()
    {
        var mgr = CharacterManager.Instance;
        if (mgr != null && currentId != null) mgr.TryLevelUp(currentId); //성공 시 onChanged → Refresh
    }

    // 배치: 자리 피커를 연다(프로토 chPickOpen). 이미 배치된 캐릭터도 피커로 자리 이동 가능
    void Deploy()
    {
        var mgr = CharacterManager.Instance;
        if (mgr == null || currentId == null || !mgr.IsOwned(currentId)) return;

        if (slotPicker != null) slotPicker.Open(currentId);
    }

    void Refresh()
    {
        var mgr = CharacterManager.Instance;
        var c = CharacterRosterLoader.Instance.Get(currentId);
        if (mgr == null || c == null) return;

        var grade = CharacterGradeLoader.Instance.Get(c.grade);
        var gradeColor = CharacterCardUI.ParseColor(grade != null ? grade.colorHex : null, Color.white);
        var st = mgr.GetState(currentId);
        bool owned = st != null;

        if (gradeText != null) { gradeText.text = grade != null ? grade.nameKo : ""; gradeText.color = gradeColor; }
        if (nameText != null) nameText.text = owned ? c.nameKo : "? ? ?";
        if (starsText != null) starsText.text = "★★★★"; //해방 미구현 — 빈 별 표시

        if (mainImage != null)
        {
            var sprite = mgr.GetSprite(c.id);
            mainImage.sprite = sprite;
            mainImage.enabled = sprite != null;
            mainImage.color = owned ? Color.white : Color.black; //미보유 = 실루엣
        }

        //고유 패시브 라벨(StatType 시트 한글명, 폴백 = 영문 키 — 아이콘 삽입 안 함)
        var statType = StatTypeLoader.Instance != null ? StatTypeLoader.Instance.Get(c.passiveStatId) : null;
        if (passiveNameText != null)
            passiveNameText.text = statType != null ? statType.nameKo : c.passiveStatId;

        if (!owned)
        {
            if (levelText != null) levelText.text = "아직 만나지 못한 동료";
            if (cardsText != null) cardsText.text = "";
            if (powerValueText != null) powerValueText.text = "—";
            if (powerStateText != null) powerStateText.text = "미보유";
            if (mulValueText != null) mulValueText.text = "";
            if (mulStateText != null) mulStateText.text = "";
            if (passiveValueText != null) passiveValueText.text = "";
            if (growthSlider != null) growthSlider.gameObject.SetActive(false);
            if (growthText != null) growthText.text = "";
            if (growButton != null) growButton.gameObject.SetActive(false);
            if (deployButton != null) deployButton.gameObject.SetActive(false);
            return;
        }

        int cap = mgr.LevelCapOf(c.id);
        int need = CharacterManager.LevelUpCost(st.level);
        bool atCap = st.level >= cap;
        int inParty = mgr.PartySlotOf(c.id);

        if (levelText != null) levelText.text = $"Lv.{st.level} <size=70%>/ {cap}</size>";
        if (cardsText != null) cardsText.text = st.cards.ToString();

        //개별 전투력 = 배치된 슬롯의 리볼버 전투력 × 화력 배수(미배치 = —)
        float power = inParty >= 0 && partyPlayers != null ? partyPlayers.SlotPower(inParty) : 0f;
        if (powerValueText != null) powerValueText.text = inParty >= 0 ? Mathf.Round(power).ToString("N0") : "—";
        if (powerStateText != null) powerStateText.text = inParty >= 0 ? (inParty + 1) + "번 자리" : "미배치";

        if (mulValueText != null) mulValueText.text = "x" + CharacterCardUI.FormatMul(mgr.PowerMulOf(c.id));
        if (mulStateText != null) mulStateText.text = "리볼버 데미지 x배"; //화력 = 이 캐릭터 리볼버 데미지에 곱하는 배수

        //Damage 패시브 = 화력 정액 배율(개인) / 그 외 = 보유 전역 +n%
        if (passiveValueText != null)
            passiveValueText.text = c.passiveStatId == "Damage"
                ? "화력 x" + CharacterCardUI.FormatMul(1f + c.passiveBase / 100f)
                : "+" + mgr.PassiveValueOf(c.id) + "% 증가";

        if (growthSlider != null)
        {
            growthSlider.gameObject.SetActive(!atCap);
            growthSlider.maxValue = need;
            growthSlider.value = Mathf.Min(st.cards, need);
        }
        if (growthText != null) growthText.text = atCap ? "레벨 캡 도달" : $"성장 카드 {st.cards} / {need}";

        if (growButton != null)
        {
            growButton.gameObject.SetActive(true);
            growButton.interactable = !atCap && st.cards >= need;
        }
        if (growCostText != null) growCostText.text = atCap ? "MAX" : need.ToString();

        if (deployButton != null)
        {
            deployButton.gameObject.SetActive(true);
            deployButton.interactable = true; //배치됨 상태에서도 피커로 자리 이동 가능
        }
        if (deployText != null) deployText.text = inParty >= 0 ? (inParty + 1) + "번 자리" : "배치";
    }
}
