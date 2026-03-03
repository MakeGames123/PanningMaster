using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Events;
using Unity.Android.Gradle.Manifest;
public class RevolverSlots : MonoBehaviour
{
    [SerializeField] Transform revolverContent;
    public List<RevolverSlotContent> revolverSlotContents { get; private set; } = new();
    List<RevolverSlotUI> slotUIs = new();
    List<BulletSlotDrag> slotDrags = new();
    DamageCalculator calculator = new();
    int slotNum;
    public void Initialize(BulletSlotsRayController rayController, Forge workmanship)
    {
        slotNum = 6;

        for (int i = 0; i < slotNum; i++)
        {
            slotUIs.Add(revolverContent.GetChild(i).GetComponent<RevolverSlotUI>());
            slotDrags.Add(revolverContent.GetChild(i).GetComponent<BulletSlotDrag>());

            RevolverSlotContent content = new RevolverSlotContent(i, slotDrags[i], slotUIs[i]);
            revolverSlotContents.Add(content);
            content.RefreshUI();

            AllBulletList.Instance.onBulletAdded.AddListener(content.RefreshInfo);
            rayController.AddSlot(content);

            slotDrags[i].Initialize(content);
            slotDrags[i].onClick.AddListener(workmanship.UpdateInfo);

            content.onInfoChanged += (val1, val2) => CheckSlots();
        }

    }
    public void CheckSlots()
    {
        List<BulletInfo> revolverInfo = new();
        foreach (RevolverSlotContent content in revolverSlotContents)
        {
            revolverInfo.Add(AllBulletList.Instance.GetBullet(content.id));
        }

        DamageModifier mod = calculator.CollectModifiers(revolverInfo);
        float power = 0;
        for (int i = 0; i < 6; i++)
        {
            power += calculator.CalculateDamage(revolverInfo[i], mod, i, DataManager.Instance.possPower).Item1;
        }

        DataManager.Instance.UpdatePower(power);
    }
    public bool CheckEmpty()
    {
        foreach (RevolverSlotContent content in revolverSlotContents)
        {
            if(!content.IsEmpty) return false;
        }

        return true;
    }
}
