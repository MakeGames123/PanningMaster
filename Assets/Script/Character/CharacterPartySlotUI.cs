using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 캐릭터 장착 슬롯 1칸(프로토 배럭 G.party 포팅 — 총 3칸, slotIndex 0~2).
// 카드/다른 슬롯 드롭 = 장착·스왑, 슬롯을 드래그해 밖에 놓으면 해제.
// 드롭을 받으려면 루트에 Raycast Target 켜진 Image 필요.
public class CharacterPartySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] int slotIndex;               // 0~2
    [SerializeField] Image mainImage;             // 장착 캐릭터 스프라이트(빈 슬롯·미등록이면 숨김)
    [SerializeField] TextMeshProUGUI emojiText;   // 장착 이모지(빈 슬롯 = +)
    [SerializeField] TextMeshProUGUI nameText;    // 장착 이름(선택 연결)
    [SerializeField] TextMeshProUGUI infoText;    // Lv.1 ×위력 배수(카드와 동일 표기, 선택 연결)
    [SerializeField] Image borderImage;           // 등급색 테두리(선택 연결)

    static readonly Color EmptyBorder = new(0.16f, 0.19f, 0.27f);
    static readonly Color EmptyInk = new(0.42f, 0.46f, 0.55f);

    public int SlotIndex => slotIndex;

    bool bound;
    bool dragging;

    void Update()
    {
        if (!bound) TryBind();
    }

    void TryBind()
    {
        var mgr = CharacterManager.Instance;
        if (mgr == null || !mgr.IsReady) return;

        mgr.onChanged.AddListener(Refresh);
        bound = true;
        Refresh();
    }

    void OnDestroy()
    {
        if (bound && CharacterManager.Instance != null)
            CharacterManager.Instance.onChanged.RemoveListener(Refresh);
    }

    void Refresh()
    {
        string id = CharacterManager.Instance.GetPartyMember(slotIndex);
        var c = id != null ? CharacterRosterLoader.Instance.Get(id) : null;

        if (c == null)
        {
            if (mainImage != null) { mainImage.sprite = null; mainImage.enabled = false; }
            if (emojiText != null) { emojiText.text = "+"; emojiText.color = EmptyInk; }
            if (nameText != null) nameText.text = "";
            if (infoText != null) infoText.text = "";
            if (borderImage != null) borderImage.color = EmptyBorder;
            return;
        }

        if (mainImage != null)
        {
            var sprite = CharacterManager.Instance.GetSprite(c.id);
            mainImage.sprite = sprite;
            mainImage.enabled = sprite != null;
        }

        var grade = CharacterGradeLoader.Instance.Get(c.grade);
        if (emojiText != null) { emojiText.text = c.emoji; emojiText.color = Color.white; }
        if (nameText != null) nameText.text = c.nameKo;
        if (infoText != null)
            infoText.text = $"Lv.1 <b>×{CharacterCardUI.FormatMul(grade != null ? grade.powerMul : 1f)}</b>";
        if (borderImage != null)
            borderImage.color = CharacterCardUI.ParseColor(grade != null ? grade.colorHex : null, Color.white);
    }

    // ── 슬롯에서 드래그로 빼기(리볼버 슬롯 문법) ──

    public void OnBeginDrag(PointerEventData eventData)
    {
        var c = Current();
        dragging = c != null;
        if (!dragging) return;

        if (CharacterDragGhost.Instance != null && CharacterManager.Instance != null)
            CharacterDragGhost.Instance.Show(CharacterManager.Instance.GetSprite(c.id));
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        if (CharacterDragGhost.Instance != null)
            CharacterDragGhost.Instance.Move(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;

        if (CharacterDragGhost.Instance != null)
            CharacterDragGhost.Instance.Hide();

        var mgr = CharacterManager.Instance;
        string id = mgr.GetPartyMember(slotIndex);
        if (id == null) return;

        var target = eventData.pointerCurrentRaycast.gameObject;
        var slot = target != null ? target.GetComponentInParent<CharacterPartySlotUI>() : null;

        if (slot != null && slot != this) mgr.EquipParty(slot.SlotIndex, id); // 슬롯→슬롯 = 스왑
        else if (slot == null) mgr.UnequipParty(slotIndex);                   // 슬롯 밖 = 해제
    }

    CharacterRosterData Current()
    {
        var mgr = CharacterManager.Instance;
        if (mgr == null || !mgr.IsReady) return null;
        string id = mgr.GetPartyMember(slotIndex);
        return id != null ? CharacterRosterLoader.Instance.Get(id) : null;
    }
}
