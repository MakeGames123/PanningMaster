using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Threading.Tasks;
public class InventorySlotContent : BulletSlotContent
{
    GameObject mySlot;
    InventorySlotUI bulletSlotUI;
    private bool isActive = true;//리볼버 슬롯에 등록 여부
    public InventorySlotContent(int newId, IBulletSlot ibulletSlot, InventorySlotUI bulletSlotUI, GameObject slot)
    {
        id = newId;
        this.ibulletSlot = ibulletSlot;
        this.bulletSlotUI = bulletSlotUI;
        mySlot = slot;
    }
    public override void RefreshUI()
    {
        BulletInfo info = AllBulletList.Instance.GetBullet(id);
        bulletSlotUI.UpdateUI(info, isActive);
    }
    //리볼버에 장착 여부 갱신
    public void ChangeActive(bool flag)
    {
        isActive = flag;
        RefreshInfo(id);

        RefreshUI();
    }
    //새로고침
    public override void RefreshInfo(int inputId)
    {
        if (id != inputId) return;

        BulletInfo info = AllBulletList.Instance.GetBullet(id);

        if(info.Count > 0 && !mySlot.activeSelf) mySlot.SetActive(true);

        if ((info.Count > 0 || info.Level > 0) && isActive)
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
