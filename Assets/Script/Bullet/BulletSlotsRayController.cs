using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BulletSlotsRayController
{
    public List<BulletSlotContent> slotContents { get; private set; } = new();

    public void AddSlot(BulletSlotContent slot)
    {
        slotContents.Add(slot);

        slot.onEndDragEvent += DisableRayCastEmptySlots;
        slot.onBeginDragEvent += EnableRayCastSlots;
    }
    private void EnableRayCastSlots()
    {
        foreach (BulletSlotContent content in slotContents)
        {
            if (!content.isDrag) content.ibulletSlot.ChangeRaycast(true);
        }
    }
    private void DisableRayCastEmptySlots()
    {
        foreach (BulletSlotContent content in slotContents)
        {
            if (content.IsEmpty) content.ibulletSlot.ChangeRaycast(false);
        }
    }
}
