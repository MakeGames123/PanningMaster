using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuyoutSlotUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI price;
    [SerializeField] Slider gage;

    public void UpdateUI(int originalPrice, int currentPrice)
    {
        gage.value = (float)currentPrice / originalPrice;

        price.text = "<sprite=0>" + ((originalPrice - currentPrice) * 5).ToString(); //돈 하나당 5원 취급
    }
}
