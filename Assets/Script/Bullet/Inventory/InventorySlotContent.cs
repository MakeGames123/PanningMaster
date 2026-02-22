using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Threading.Tasks;
public class InventorySlotContent : BulletSlotContent
{
    InventorySlotUI bulletSlotUI;
    private bool isActive = true;//사용 가능한지
    public InventorySlotContent(int newId, IBulletSlot ibulletSlot, InventorySlotUI bulletSlotUI)
    {
        id = newId;
        this.ibulletSlot = ibulletSlot;
        this.bulletSlotUI = bulletSlotUI;
    }
    public override void RefreshUI()
    {
        BulletInfo info = AllBulletList.Instance.GetBullet(id);
        bulletSlotUI.UpdateUI(info, isActive);
    }
    public void ChangeActive(bool flag)
    {
        isActive = flag;
        ibulletSlot.ChangeRaycast(flag);

        RefreshUI();
    }
    public override void RefreshInfo(int inputId)
    {
        if (id != inputId) return;

        BulletInfo info = AllBulletList.Instance.GetBullet(id);

        if (info.Count > 0 && isActive)
        {
            ibulletSlot.ChangeRaycast(true);
        }
        else
        {
            ibulletSlot.ChangeRaycast(false);
        }

        RefreshUI();
    }
}
