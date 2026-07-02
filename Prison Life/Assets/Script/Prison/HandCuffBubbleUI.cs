using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HandCuffBubbleUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI count;
    [SerializeField] Slider gage;
    public void UpdateUI(int originalPrice, int currentPrice)
    {
        gage.value = (float)currentPrice / originalPrice;

        count.text = "<sprite=0> " + (originalPrice - currentPrice).ToString();
    }
}
