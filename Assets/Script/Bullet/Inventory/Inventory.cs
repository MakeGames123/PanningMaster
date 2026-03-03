using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] List<GameObject> slots = new();
    public List<InventorySlotContent> slotContents { get; private set; } = new();
    List<InventorySlotUI> slotUIs = new();
    List<BulletSlotDrag> slotDrags = new();
    public void Initialize(BulletSlotsRayController rayController, Forge workmanship)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slotUIs.Add(slots[i].GetComponent<InventorySlotUI>());
            slotDrags.Add(slots[i].GetComponent<BulletSlotDrag>());

            InventorySlotContent content = new InventorySlotContent(i + 1000, slotDrags[i], slotUIs[i]);
            slotContents.Add(content);
            content.ChangeActive(false);

            AllBulletList.Instance.onBulletAdded.AddListener(content.RefreshInfo);
            rayController.AddSlot(content);

            slotDrags[i].Initialize(content);
            slotDrags[i].onClick.AddListener(workmanship.UpdateInfo);
        }
    }
    public void AddBullet(int id)
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
    public void ActiveAll()//탄환 로드후 호출
    {
        for (int i = 0; i < slots.Count; i++)
        {
            BulletInfo info = AllBulletList.Instance.bulletInfos[slotContents[i].id];
            if (info.Count > 0 || info.Level > 0)
            {
                slotContents[i].ChangeActive(true);
            }
        }
    }
}
