using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] List<GameObject> slots = new();
    public List<BulletSlotContent> slotContents { get; private set; } = new();
    List<BulletSlotUI> slotUIs = new();
    List<BulletSlotDrag> slotDrags = new();
    List<int> bulletIds = new();
    BulletType typeTag = BulletType.All;
    public void Initialize(BulletSlotsRayController rayController, Workmanship workmanship)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slotUIs.Add(slots[i].GetComponent<BulletSlotUI>());
            slotDrags.Add(slots[i].GetComponent<BulletSlotDrag>());

            BulletSlotContent content = new BulletSlotContent(i + 6, slotDrags[i], slotUIs[i]);
            slotContents.Add(content);

            content.onInfoChanged += (val1, val2) => SortInventory();
            content.onInfoRemoved += (id) => bulletIds.Remove(id);
            content.RefreshUI();
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
                SortInventory();
                return;
            }
        }

        for (int i = 0; i < slots.Count; i++)//신규 탄환
        {
            if (slotContents[i].IsEmpty)
            {
                bulletIds.Add(id);
                slotContents[i].UpdateBulletInfo(id);
                SortInventory();
                return;
            }
        }
    }
    public void ChangeTag(int index)
    {
        typeTag = (BulletType)index;
        SortInventory();
    }
    public void SortInventory()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slotContents[i].id = -1;
        }
        int index = 0;
        for (int i = 0; i < bulletIds.Count; i++)
        {
            BulletInfo info = AllBulletList.Instance.GetBullet(bulletIds[i]);

            if(info.infoSO.bulletType == typeTag || typeTag == BulletType.All)
            {
                slotContents[index++].id = bulletIds[i];
            }
        }

        for (int i = 0; i < slots.Count; i++)
        {
            slotContents[i].RefreshUI();
        }
    }
}
