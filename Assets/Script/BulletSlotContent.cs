using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Threading.Tasks;

public class BulletSlotContent
{
    public int index { get; private set; }
    public bool isDrag { get; private set; } = false;
    public bool IsEmpty => bulletInfo == null;
    public BulletInfo bulletInfo { get; private set; } = null;
    public Action<BulletInfo, int> onInfoChanged;
    public IBulletSlot ibulletSlot;
    public IBulletSlotUI ibulletSlotUI;
    public Action onBeginDragEvent;
    public Action onEndDragEvent;//onBeginDragEvent의 원상복구 이벤트
    public Action onStatChanged;
    public BulletSlotContent(int index, IBulletSlot ibulletSlot, IBulletSlotUI ibulletSlotUI)
    {
        this.index = index;
        bulletInfo = null;
        this.ibulletSlot = ibulletSlot;
        this.ibulletSlotUI = ibulletSlotUI;
    }
    public void UpdateBulletInfo(BulletInfo newInfo)//통상 변경
    {
        if (!IsEmpty) bulletInfo.onCountChanged = null;
        bulletInfo = newInfo;

        if (!IsEmpty)
        {
            ibulletSlot.ChangeRaycast(true);
            bulletInfo.onCountChanged += (val) => ibulletSlotUI.UpdateUI(bulletInfo);
            bulletInfo.onStatChanged += () => onStatChanged?.Invoke();
        }

        ibulletSlotUI.UpdateUI(newInfo);

        onInfoChanged?.Invoke(newInfo, index);
    }
    public void OnBulletDrop(BulletSlotContent inputContent)
    {
        BulletInfo info = inputContent.bulletInfo;

        inputContent.UpdateBulletInfo(bulletInfo);
        UpdateBulletInfo(info);
    }

    public void SetDragCondition(bool isDrag)
    {
        this.isDrag = isDrag;

        if (isDrag)
        {
            onBeginDragEvent?.Invoke();
            ibulletSlot.ChangeRaycast(false);
        }
        else
        {
            onEndDragEvent?.Invoke();
            ibulletSlot.ChangeRaycast(true);
        }
    }
}
