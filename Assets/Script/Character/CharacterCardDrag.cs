using UnityEngine;
using UnityEngine.EventSystems;

// 동료 목록 카드 드래그(탄환 BulletSlotDrag 문법 — 인벤토리→리볼버에 해당).
// 보유 카드를 장착 슬롯(CharacterPartySlotUI)에 끌어다 놓으면 장착, 클릭하면 상세 팝업. 미보유 카드는 드래그 불가.
// 고스트·팝업 참조는 CharacterListPanel이 주입(프리팹은 씬 오브젝트 참조를 저장 못 함).
[RequireComponent(typeof(CharacterCardUI))]
public class CharacterCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    CharacterCardUI card;
    CharacterDragGhost ghost;
    CharacterDetailPopup detailPopup;
    bool dragging;

    void Awake() => card = GetComponent<CharacterCardUI>();

    public void Inject(CharacterDragGhost ghost, CharacterDetailPopup popup)
    {
        this.ghost = ghost;
        detailPopup = popup;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = card.Data != null && card.IsOwnedCard;

        if (!dragging) return;

        if (ghost != null && CharacterManager.Instance != null)
            ghost.Show(CharacterManager.Instance.GetSprite(card.Data.id));
    }

    // 클릭 = 상세 팝업(프로토 chDetail — 드래그가 시작되면 클릭은 발화하지 않음)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (card.Data != null && detailPopup != null)
            detailPopup.Open(card.Data.id);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        if (ghost != null) ghost.Move(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;

        if (ghost != null) ghost.Hide();

        // 탄환 문법: 포인터가 놓인 위치의 레이캐스트 대상으로 드롭 판정
        var target = eventData.pointerCurrentRaycast.gameObject;
        var slot = target != null ? target.GetComponentInParent<CharacterPartySlotUI>() : null;
        if (slot != null && CharacterManager.Instance != null)
            CharacterManager.Instance.EquipParty(slot.SlotIndex, card.Data.id);
    }
}
