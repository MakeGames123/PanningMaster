using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] List<GameObject> slots = new();
    public List<InventorySlotContent> slotContents { get; private set; } = new();
    List<InventorySlotUI> slotUIs = new();
    List<BulletSlotDrag> slotDrags = new();
    public void Initialize(BulletSlotsRayController rayController)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slotUIs.Add(slots[i].GetComponent<InventorySlotUI>());
            slotDrags.Add(slots[i].GetComponent<BulletSlotDrag>());

            InventorySlotContent content = new InventorySlotContent(slots.Count - i + 999, slotDrags[i], slotUIs[i], slots[i]);
            slotContents.Add(content);
            content.ChangeActive(true);

            AllBulletList.Instance.onBulletChanged.AddListener(content.RefreshInfo);
            rayController.AddSlot(content);

            slotDrags[i].Initialize(content);
            slots[i].SetActive(false);
        }
    }
    //탄환 뽑기가 아닌 인벤토리로 돌려놓는 코드
    public void ReturnBullet(int id)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slotContents[i].id == id)
            {
                slotContents[i].ChangeActive(true);
                return;
            }
        }
    }
    //id에 해당하는 인벤토리 슬롯의 장착(active) 상태 설정 (true=인벤토리 사용가능, false=리볼버 장착중)
    public void SetActive(int id, bool flag)
    {
        for (int i = 0; i < slotContents.Count; i++)
        {
            if (slotContents[i].id == id)
            {
                slotContents[i].ChangeActive(flag);
                return;
            }
        }
    }
}
