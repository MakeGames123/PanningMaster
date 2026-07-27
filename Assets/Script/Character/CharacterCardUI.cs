using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 동료 목록 카드 1장(프로토 renderChars의 .chg 셀 포팅).
// 보유 = 등급색 테두리 + 이모지 + 이름 + Lv/위력 배수 + 🃏 중복 카드 배지 / 미보유 = 실루엣(? + ??? + 등급명).
public class CharacterCardUI : MonoBehaviour
{
    [SerializeField] Image borderImage;          // 루트 이미지(등급색 테두리 역할 — 안쪽 BG가 덮어 프레임만 남음)
    [SerializeField] Image mainImage;            // 캐릭터 스프라이트(SO 소유 — 미보유·미등록이면 숨김)
    [SerializeField] TextMeshProUGUI emojiText;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI infoText;   // 보유: Lv.1 ×배수 / 미보유: 등급명
    [SerializeField] GameObject cardsBadge;      // 🃏 배지(중복 0장이면 숨김)
    [SerializeField] TextMeshProUGUI cardsText;
    [SerializeField] Slider cardProgress;        // 다음 레벨 진행도(보유 카드/요구치, 미보유 숨김)
    [SerializeField] TextMeshProUGUI cardProgressText; // 보유/요구 (예: 3/1)
    [SerializeField] Graphic clickArea;          // 드래그·클릭 판정용(탄환 clickArea 문법 — 보유만 켬)

    static readonly Color UnownedBorder = new(0.16f, 0.19f, 0.27f);
    static readonly Color UnownedInk = new(0.42f, 0.46f, 0.55f);
    static readonly Color OwnedInk = new(0.68f, 0.72f, 0.8f);

    // 이 카드에 고정 배정된 캐릭터(드래그 장착용 — 미보유 카드는 드래그 불가)
    public CharacterRosterData Data { get; private set; }
    public bool IsOwnedCard { get; private set; }

    CharacterGradeData grade;

    // 카드 ↔ 캐릭터 배정은 시작 시 1회 고정(CharacterListPanel.TryBind — 위치 불변 설계)
    public void SetCharacter(CharacterRosterData c, CharacterGradeData grade)
    {
        Data = c;
        this.grade = grade;
    }

    // 상태 변화 시 보유 여부에 맞게 다시 그린다(배정은 불변)
    public void Refresh()
    {
        var mgr = CharacterManager.Instance;
        if (Data == null || mgr == null || !mgr.IsReady) return;

        if (mgr.IsOwned(Data.id)) SetOwned(mgr.GetState(Data.id));
        else SetUnowned();
    }

    void SetOwned(CharacterManager.CharacterState state)
    {
        var c = Data;
        IsOwnedCard = true;

        if (clickArea != null) clickArea.raycastTarget = true;

        if (borderImage != null)
            borderImage.color = ParseColor(grade != null ? grade.colorHex : null, Color.white);

        if (mainImage != null)
        {
            var sprite = state != null && state.infoSO != null ? state.infoSO.characterSprite : null;
            mainImage.sprite = sprite;
            mainImage.enabled = sprite != null;
        }

        if (emojiText != null) emojiText.text = c.emoji;
        nameText.text = c.nameKo;
        nameText.color = Color.white;

        // 레벨 + 화력 배수(등급 × 성장 × ⚔️패시브 — 프로토 chMulOf)
        int lv = state != null ? state.level : 1;
        float mul = CharacterManager.Instance != null
            ? CharacterManager.Instance.PowerMulOf(c.id)
            : (grade != null ? grade.powerMul : 1f);
        infoText.text = $"Lv.{lv} <b>×{FormatMul(mul)}</b>";
        infoText.color = OwnedInk;

        bool hasCards = state != null && state.cards > 0;
        if (cardsBadge != null) cardsBadge.SetActive(hasCards);
        if (hasCards && cardsText != null) cardsText.text = "🃏" + state.cards;

        int cards = state != null ? state.cards : 0;
        int need = CharacterManager.LevelUpCost(lv); //다음 레벨 요구 🃏
        if (cardProgress != null)
        {
            cardProgress.gameObject.SetActive(true);
            cardProgress.maxValue = need;
            cardProgress.value = Mathf.Min(cards, need);
        }
        if (cardProgressText != null) cardProgressText.text = cards + "/" + need;
    }

    void SetUnowned()
    {
        IsOwnedCard = false;

        if (clickArea != null) clickArea.raycastTarget = false;

        if (borderImage != null) borderImage.color = UnownedBorder;

        if (mainImage != null) { mainImage.sprite = null; mainImage.enabled = false; }

        if (emojiText != null) emojiText.text = "?";
        nameText.text = "???";
        nameText.color = UnownedInk;

        // 미보유는 등급명을 등급색으로 — 어떤 급이 잠겨 있는지 예고(프로토 .gr)
        infoText.text = grade != null ? grade.nameKo : "";
        infoText.color = ParseColor(grade != null ? grade.colorHex : null, UnownedInk);

        if (cardsBadge != null) cardsBadge.SetActive(false);
        if (cardProgress != null) cardProgress.gameObject.SetActive(false);
        if (cardProgressText != null) cardProgressText.text = "";
    }

    public static string FormatMul(float mul)
        => mul % 1f == 0f ? ((int)mul).ToString() : mul.ToString("0.##");

    public static Color ParseColor(string hex, Color fallback)
    {
        if (string.IsNullOrEmpty(hex)) return fallback;
        if (!hex.StartsWith("#")) hex = "#" + hex;
        return ColorUtility.TryParseHtmlString(hex, out var color) ? color : fallback;
    }
}
