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
    public void Initialize(BulletSlotsRayController rayController, Workmanship workmanship)
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
            content.onStatChanged += CheckSlots;
        }
    }

    public void CheckSlots()
    {
        DataManager.Instance.damage.ResetValue();
        DataManager.Instance.typeDamage.ResetValue();
        DataManager.Instance.finalDamage.ResetValue();

        for (int k = 0; k < 4; k++)
        {
            DataManager.Instance.damageByType[k].ResetValue();
            DataManager.Instance.finalDamageByType[k].ResetValue();
            DataManager.Instance.typeDamageByType[k].ResetValue();
        }

        for (int i = 0; i < slotNum; i++)//초기화
        {
            BulletInfo info = AllBulletList.Instance.GetBullet(revolverSlotContents[i].id);
            if (info == null || info.Count == 0) continue;

            info.damage.ResetValue();
            info.finalDamage.ResetValue();
            info.typeDamage.ResetValue();
        }


        for (int i = 0; i < slotNum; i++)
        {
            BulletInfo info = AllBulletList.Instance.GetBullet(revolverSlotContents[i].id);
            if (info == null || info.Count == 0) continue;

            for (int j = 0; j < info.stats.Count; j++)
            {
                BulletStat stat = info.stats[j];
                switch (stat.target)
                {
                    case TargetType.Self:
                        ApplyRewardSlot(i, j, stat, info);
                        break;
                    case TargetType.SlotIndex:
                        ApplyRewardSlot(i, j, stat, AllBulletList.Instance.GetBullet(revolverSlotContents[(int)stat.targetCoef[0]].id));
                        break;
                    case TargetType.BulletType:

                        switch (stat.reward)
                        {
                            case RewardType.PowerIncrease:
                                DataManager.Instance.damageByType[(int)stat.targetCoef[0]].SetValue(i, j, stat.rewardCoef);
                                break;
                            case RewardType.FinalDamageIncrease:
                                DataManager.Instance.finalDamageByType[(int)stat.targetCoef[0]].SetValue(i, j, stat.rewardCoef);
                                break;
                            case RewardType.BulletTypeDamageIncrease:
                                DataManager.Instance.typeDamageByType[(int)stat.targetCoef[0]].SetValue(i, j, stat.rewardCoef);
                                break;
                        }

                        break;
                    case TargetType.All:

                        switch (stat.reward)
                        {
                            case RewardType.PowerIncrease:
                                DataManager.Instance.damage.SetValue(i, j, stat.rewardCoef);
                                break;
                            case RewardType.FinalDamageIncrease:
                                DataManager.Instance.finalDamage.SetValue(i, j, stat.rewardCoef);
                                break;
                            case RewardType.BulletTypeDamageIncrease:
                                DataManager.Instance.typeDamage.SetValue(i, j, stat.rewardCoef);
                                break;
                        }

                        break;
                }
            }
        }

        float power = 0;
        for (int i = 0; i < slotNum; i++)//초기화
        {
            BulletInfo info = AllBulletList.Instance.GetBullet(revolverSlotContents[i].id);
            if (info == null || info.Count == 0) continue;
            power += calculator.Calculate(info);
        }
        DataManager.Instance.UpdatePower(power);
    }
    public void ApplyRewardSlot(int infoFrom, int index, BulletStat stat, BulletInfo infoTarget = null)
    {
        if (infoTarget == null) return;

        switch (stat.reward)
        {
            case RewardType.PowerIncrease:
                infoTarget.damage.SetValue(infoFrom, index, stat.rewardCoef);
                break;
            case RewardType.FinalDamageIncrease:
                infoTarget.finalDamage.SetValue(infoFrom, index, stat.rewardCoef);
                break;
            case RewardType.BulletTypeDamageIncrease:
                infoTarget.damage.SetValue(infoFrom, index, stat.rewardCoef);
                break;
        }
    }
}
