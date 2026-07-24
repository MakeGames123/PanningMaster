using UnityEngine;
using UnityEngine.EventSystems;

// 동료 목록 카드 드래그(탄환 BulletSlotDrag 문법 — 인벤토리→리볼버에 해당).
// 보유 카드를 장착 슬롯(CharacterPartySlotUI)에 끌어다 놓으면 장착. 미보유 카드는 드래그 불가.
[RequireComponent(typeof(CharacterCardUI))]
public class CharacterCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    CharacterCardUI card;
    bool dragging;

    void Awake() => card = GetComponent<CharacterCardUI>();

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = card.Data != null && card.IsOwnedCard;       
        
        if (!dragging) return;

        if (CharacterDragGhost.Instance != null && CharacterManager.Instance != null)
            CharacterDragGhost.Instance.Show(CharacterManager.Instance.GetSprite(card.Data.id));
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

        // 탄환 문법: 포인터가 놓인 위치의 레이캐스트 대상으로 드롭 판정
        var target = eventData.pointerCurrentRaycast.gameObject;
        var slot = target != null ? target.GetComponentInParent<CharacterPartySlotUI>() : null;
        if (slot != null && CharacterManager.Instance != null)
            CharacterManager.Instance.EquipParty(slot.SlotIndex, card.Data.id);
    }
}
