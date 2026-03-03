using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Threading.Tasks;
public class RevolverSlotContent : BulletSlotContent
{
    public int index { get; private set; }
    public bool IsEmpty => id == -1; // 리볼버 슬롯에서만 사용 인벤토리 절대 사용 금지
    public Action<int, int> onInfoChanged;
    RevolverSlotUI bulletSlotUI;
    public RevolverSlotContent(int index, IBulletSlot ibulletSlot, RevolverSlotUI bulletSlotUI)
    {
        this.index = index;
        id = -1;
        this.ibulletSlot = ibulletSlot;
        this.bulletSlotUI = bulletSlotUI;
    }
    public void UpdateBulletInfo(int newId)//리볼버 슬롯 위치변경, 맨 처음 초기화에만 사용
    {
        id = newId;
        BulletInfo info = AllBulletList.Instance.GetBullet(id);

        if (!IsEmpty)
        {
            ibulletSlot.ChangeRaycast(true);
        }

        bulletSlotUI.UpdateUI(info);

        onInfoChanged?.Invoke(id, index);
    }
    public override void RefreshInfo(int inputId)
    {
        if (id != inputId) return;

        BulletInfo info = AllBulletList.Instance.GetBullet(id);

        if (!IsEmpty)
        {
            ibulletSlot.ChangeRaycast(true);
        }

        bulletSlotUI.UpdateUI(info);
    }
    public override void RefreshUI()
    {
        BulletInfo info = AllBulletList.Instance.GetBullet(id);
        bulletSlotUI.UpdateUI(info);
    }
    public void OnBulletDrop(BulletSlotContent inputContent)
    {
        int infoId = id;

        UpdateBulletInfo(inputContent.id);

        if(inputContent is RevolverSlotContent revolverSlot)
        {
            revolverSlot.UpdateBulletInfo(infoId);
        }
    }
}
