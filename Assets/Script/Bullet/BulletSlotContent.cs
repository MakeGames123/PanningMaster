using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Threading.Tasks;

public abstract class BulletSlotContent
{
    public bool isDrag { get; private set; } = false;
    public int id;
    public IBulletSlot ibulletSlot;
    public Action onBeginDragEvent;
    public Action onEndDragEvent;//onBeginDragEvent의 원상복구 이벤트
    public Action onStatChanged;
    public abstract void RefreshInfo(int inputId);
    public abstract void RefreshUI();
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
