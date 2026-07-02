using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryBuyoutSlot : BuyoutSlot
{
    [SerializeField] GameObject deliveryMen;
    protected override void OnPurchaseComplete()
    {
        deliveryMen.SetActive(true);
        base.OnPurchaseComplete();
    }
}
