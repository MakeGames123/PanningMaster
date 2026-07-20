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
    private Inventory inventory;
    private BulletLowPanel lowPanel;

    public void Initialize(BulletSlotContent slotContent)
    {
        inventory = FindAnyObjectByType<Inventory>();
        bulletUIDragSlot = GameObject.Find("DragSlot").transform;
        lowPanel = FindFirstObjectByType<BulletLowPanel>();
        bulletUIDragSlotUI = bulletUIDragSlot.GetComponent<Image>();
        content = slotContent;
        originPos = transform.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        bulletUIDragSlotUI.sprite = AllBulletList.Instance.bulletInfoSODic[content.id].inventoryImage;
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

        bulletUIDragSlotUI.sprite = null;
        bulletUIDragSlotUI.enabled = false;

        content.SetDragCondition(false);

        if (UIObject != null)
        {
            OnObjectDrop(UIObject);
        }
    }

    public void OnObjectDrop(GameObject UIObject)
    {
        if (UIObject.CompareTag("BulletSlot"))
        {
            BulletSlotContent oppoContent = UIObject.GetComponent<BulletSlotDrag>().content;

            if (oppoContent is RevolverSlotContent revolverSlot)
            {
                revolverSlot.OnBulletDrop(content);

                //인벤토리 탄환을 약실로 드롭 = 수동 장착 (퀘스트/튜토리얼 equipAct)
                if (content is InventorySlotContent && QuestEventManager.Instance != null)
                    QuestEventManager.Instance.AddEvent("equipAct");
            }

            if (content is InventorySlotContent inventorySlot)
            {
                inventorySlot.ChangeActive(false);
            }
        }
        else //인벤토리로 돌아가기
        {
            int id = content.id;

            if (content is RevolverSlotContent revolverSlot)
            {
                revolverSlot.UpdateBulletInfo(-1);
            }
            
            inventory.ReturnBullet(id);
        }
    }

    public void ChangeRaycast(bool flag)
    {
        clickArea.raycastTarget = flag;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        lowPanel.gameObject.SetActive(true);
        lowPanel.UpdateUI(content.id);
    }
}
