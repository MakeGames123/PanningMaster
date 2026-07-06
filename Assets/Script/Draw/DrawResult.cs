using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class DrawResult : MonoBehaviour
{
    [SerializeField] List<DrawSlotUI> slotUIs = new();
    [SerializeField] DrawSlotUI singleNewSlot; //새 탄환을 딱 1개만 뽑았을 때의 단독 연출 슬롯
    [SerializeField] RectTransform rect;

    public void SetCondition(Dictionary<int, DrawInfo> increasedBulletId)
    {
        rect.anchoredPosition = Vector2.zero;

        //isNew 이면서 딱 한 개의 탄환만 뽑은 경우 -> 별개의 단독 슬롯 연출
        if (singleNewSlot != null && IsSingleNew(increasedBulletId, out DrawInfo single))
        {
            singleNewSlot.gameObject.SetActive(true);
            singleNewSlot.UpdateUI(single.Id, single);

            foreach (DrawSlotUI slot in slotUIs) //일반 슬롯은 모두 숨김
                slot.gameObject.SetActive(false);
            return;
        }

        if (singleNewSlot != null) singleNewSlot.gameObject.SetActive(false);

        int index = 0;

        foreach (KeyValuePair<int, DrawInfo> kvp in increasedBulletId)
        {
            slotUIs[index].gameObject.SetActive(true);
            slotUIs[index++].UpdateUI(kvp.Key, kvp.Value);
        }

        for (int i = index; i < slotUIs.Count; i++)
        {
            slotUIs[i].gameObject.SetActive(false);
        }
    }

    //결과가 새 탄환 1종이고 획득 수량도 1개(= 단일 뽑기)인지
    bool IsSingleNew(Dictionary<int, DrawInfo> dict, out DrawInfo single)
    {
        single = default;
        if (dict.Count != 1) return false;

        foreach (DrawInfo v in dict.Values) single = v;
        return single.IsNew && single.Gained == 1;
    }
    public void Disable()
    {
        rect.anchoredPosition = new Vector2(9999, 0);
    }
}
