using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeskInput : InputOutputSystem
{
    [SerializeField] private Desk desk;
    //주기에 맞춰 실행될 내용
    protected override void RoutineBehaviour(ItemInteractive itemInteractive)
    {
        if (itemInteractive.GetItem(ItemType.Handcuff, stack.ReturnFrontPos())) desk.AddHandcuff();
    }
}
