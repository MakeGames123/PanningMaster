using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PrisonBuyout : BuyoutSlot
{
    [SerializeField] GameObject expansion;
    [SerializeField] GameObject removeWall;
    [SerializeField] CameraMove cameraMove;
    [SerializeField] Vector3 cameraPos;
    [SerializeField] float cameraOrth;
    public UnityEvent onExpansion;
    
    protected override void OnPurchaseComplete()
    {
        onExpansion.Invoke();
        expansion.SetActive(true);
        removeWall.SetActive(false);
        cameraMove.ShowTargetPosition(cameraPos, cameraOrth);
        base.OnPurchaseComplete();
    }
}
