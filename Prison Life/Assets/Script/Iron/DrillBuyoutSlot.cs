using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillBuyoutSlot : BuyoutSlot
{
    [SerializeField] GameObject minerSlot;
    [SerializeField] GameObject tractorSlot;
    protected override void OnPurchaseComplete()
    {
        player.UpgradeTool();
        base.OnPurchaseComplete();

        tractorSlot.SetActive(true);
        minerSlot.SetActive(true);
    }
}
