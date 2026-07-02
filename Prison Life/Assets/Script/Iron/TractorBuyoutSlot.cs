using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TractorBuyoutSlot : BuyoutSlot
{
    protected override void OnPurchaseComplete()
    {
        player.UpgradeTool();
        base.OnPurchaseComplete();
    }
}
