using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineOutput : InputOutputSystem
{
    [SerializeField] private Machine machine;

    //주기에 맞춰 실행될 내용
    protected override void RoutineBehaviour(ItemInteractive itemInteractive)
    {
        if (itemInteractive.CheckItemCount(ItemType.Handcuff) && machine.TryGetHandcuff()) itemInteractive.AddItem(ItemType.Handcuff, stack.ReturnFrontPos());
    }
}
