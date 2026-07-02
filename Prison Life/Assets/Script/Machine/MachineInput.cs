using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineInput : InputOutputSystem
{
    [SerializeField] private Machine machine;

    //주기에 맞춰 실행될 내용
    protected override void RoutineBehaviour(ItemInteractive itemInteractive)
    {
        //가져오는데 성공 했을때만 옮기기
        if (machine.CheckIronCount() && itemInteractive.GetItem(ItemType.Iron, stack.ReturnFrontPos())) machine.AddIronOre();
    }
}