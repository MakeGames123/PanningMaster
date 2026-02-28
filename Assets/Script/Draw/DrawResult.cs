using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class DrawResult : MonoBehaviour
{
    [SerializeField] List<DrawSlotUI> slotUIs = new();
    [SerializeField] RectTransform rect;

    public void SetCondition(Dictionary<int, DrawInfo> increasedBulletId)
    {
        rect.anchoredPosition = Vector2.zero;

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
    public void Disable()
    {
        rect.anchoredPosition = new Vector2(9999, 0);
    }
}
