using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BulletSlotsRayController
{
    public List<RevolverSlotContent> slotContents { get; private set; } = new();

    public void AddSlot(BulletSlotContent slot)
    {
        if(slot is RevolverSlotContent content) slotContents.Add(content);

        slot.onEndDragEvent += DisableRayCastEmptySlots;
        slot.onBeginDragEvent += EnableRayCastSlots;
    }
    private void EnableRayCastSlots()
    {
        foreach (RevolverSlotContent content in slotContents)
        {
            if (!content.isDrag) content.ibulletSlot.ChangeRaycast(true);
        }
    }
    private void DisableRayCastEmptySlots()
    {
        foreach (RevolverSlotContent content in slotContents)
        {
            if (content.IsEmpty) content.ibulletSlot.ChangeRaycast(false);
        }
    }
}
