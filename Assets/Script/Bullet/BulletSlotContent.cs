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
    public bool IsEmpty => id == -1; // 리볼버 슬롯에서만 사용?
    public int id;
    public Action<int> onInfoRemoved;
    public Action<int, int> onInfoChanged;
    public IBulletSlot ibulletSlot;
    public IBulletSlotUI ibulletSlotUI;
    public Action onBeginDragEvent;
    public Action onEndDragEvent;//onBeginDragEvent의 원상복구 이벤트
    public Action onStatChanged;
    public BulletSlotContent(int index, IBulletSlot ibulletSlot, IBulletSlotUI ibulletSlotUI)
    {
        this.index = index;
        id = -1;
        this.ibulletSlot = ibulletSlot;
        this.ibulletSlotUI = ibulletSlotUI;
    }
    public void UpdateBulletInfo(int newId)//위치 변경
    {
        if(newId == -1 && id != -1) onInfoRemoved?.Invoke(id);
        id = newId;
        BulletInfo info = AllBulletList.Instance.GetBullet(id);

        if (!IsEmpty)
        {
            ibulletSlot.ChangeRaycast(true);
        }

        ibulletSlotUI.UpdateUI(info);

        onInfoChanged?.Invoke(id, index);
    }
    public void RefreshInfo(int inputId)
    {
        if (id != inputId) return;

        BulletInfo info = AllBulletList.Instance.GetBullet(id);

        if (!IsEmpty)
        {
            ibulletSlot.ChangeRaycast(true);
        }

        ibulletSlotUI.UpdateUI(info);
    }
    public void RefreshUI()
    {
        BulletInfo info = AllBulletList.Instance.GetBullet(id);
        ibulletSlotUI.UpdateUI(info);
    }
    public void OnBulletDrop(BulletSlotContent inputContent)
    {
        int infoId = inputContent.id;

        inputContent.UpdateBulletInfo(id);
        UpdateBulletInfo(infoId);
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
