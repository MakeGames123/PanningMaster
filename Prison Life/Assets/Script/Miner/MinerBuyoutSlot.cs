using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinerBuyoutSlot : BuyoutSlot
{
    [SerializeField] GameObject miners;
    [SerializeField] GameObject deliverSlot;
    protected override void OnPurchaseComplete()
    {
        miners.SetActive(true);
        deliverSlot.SetActive(true);
        base.OnPurchaseComplete();
    }
}
