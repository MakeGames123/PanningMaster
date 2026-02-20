using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] List<GameObject> slots = new();
    AllBulletList allBulletList;
    public List<BulletSlotContent> slotContents { get; private set; } = new();
    List<BulletSlotUI> slotUIs = new();
    List<BulletSlotDrag> slotDrags = new();
    public void Initialize(BulletSlotsRayController rayController, Workmanship workmanship)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slotUIs.Add(slots[i].GetComponent<BulletSlotUI>());
            slotDrags.Add(slots[i].GetComponent<BulletSlotDrag>());

            BulletSlotContent content = new BulletSlotContent(i, slotDrags[i], slotUIs[i]);
            slotContents.Add(content);

            content.UpdateBulletInfo(null);
            rayController.AddSlot(content);

            slotDrags[i].Initialize(content);
            slotDrags[i].onClick.AddListener(workmanship.UpdateInfo);
        }
    }
    public void AddBullet(BulletInfo info)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slotContents[i].IsEmpty)
            {
                slotContents[i].UpdateBulletInfo(info);
                break;
            }
        }
    }
}
