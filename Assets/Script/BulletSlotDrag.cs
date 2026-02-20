using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public interface IBulletSlot
{
    void ChangeRaycast(bool flag);
}

public class BulletSlotDrag : MonoBehaviour,
    IDragHandler,
    IEndDragHandler,
    IBeginDragHandler,
    IPointerClickHandler,   // 추가
    IBulletSlot
{
    protected Vector2 originPos = new Vector2();
    public BulletSlotContent content;
    public Image clickArea;

    private Transform bulletUIDragSlot;
    private Image bulletUIDragSlotUI;

    public UnityEvent<BulletInfo> onClick = new();

    public void Initialize(BulletSlotContent slotContent)
    {
        bulletUIDragSlot = GameObject.Find("DragSlot").transform;
        bulletUIDragSlotUI = bulletUIDragSlot.GetComponent<Image>();
        content = slotContent;
        originPos = transform.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        content.ibulletSlotUI.UpdateUI(null);
        bulletUIDragSlotUI.sprite = content.bulletInfo.infoSO.inventoryImage;
        bulletUIDragSlotUI.enabled = true;

        content.SetDragCondition(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        bulletUIDragSlot.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        GameObject UIObject = eventData.pointerCurrentRaycast.gameObject;

        content.ibulletSlotUI.UpdateUI(content.bulletInfo);
        bulletUIDragSlotUI.sprite = null;
        bulletUIDragSlotUI.enabled = false;

        if (UIObject != null)
        {
            OnObjectDrop(UIObject);
        }

        content.SetDragCondition(false);
    }

    public void OnObjectDrop(GameObject UIObject)
    {
        if (UIObject.CompareTag("BulletSlot"))
        {
            UIObject.GetComponent<BulletSlotDrag>().content.OnBulletDrop(content);
        }
    }

    public void ChangeRaycast(bool flag)
    {
        clickArea.raycastTarget = flag;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke(content.bulletInfo);
    }
}
