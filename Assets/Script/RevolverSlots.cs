using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
public class RevolverSlots : MonoBehaviour
{
    [SerializeField] Transform revolverContent;
    public List<BulletSlotContent> revolverSlotContents { get; private set; } = new();
    List<BulletSlotUI> slotUIs = new();
    List<BulletSlotDrag> slotDrags = new();
    int slotNum;
    public void Initialize(BulletSlotsRayController rayController, Workmanship workmanship)
    {
        slotNum = 6;

        for (int i = 0; i < slotNum; i++)
        {
            slotUIs.Add(revolverContent.GetChild(i).GetComponent<BulletSlotUI>());
            slotDrags.Add(revolverContent.GetChild(i).GetComponent<BulletSlotDrag>());

            BulletSlotContent content = new BulletSlotContent(i, slotDrags[i], slotUIs[i]);
            revolverSlotContents.Add(content);

            content.UpdateBulletInfo(null);
            rayController.AddSlot(content);

            slotDrags[i].Initialize(content);
            slotDrags[i].onClick.AddListener(workmanship.UpdateInfo);

            content.onInfoChanged += (val1, val2) => CheckSlots();
            content.onStatChanged += CheckSlots;
        }
    }

    public void CheckSlots()
    {
        Debug.Log("resert");
        for (int i = 0; i < slotNum; i++)//초기화
        {
            for (int j = 0; j < 3; j++)
            {
                DataManager.Instance.damage.SetValue(i, j, 0);
                DataManager.Instance.finalDamage.SetValue(i, j, 0);
                for (int k = 0; k < 4; k++)
                {
                    DataManager.Instance.damageByType[k].SetValue(i, j, 0);
                    DataManager.Instance.finalDamageByType[k].SetValue(i, j, 0);
                    DataManager.Instance.typeDamage[k].SetValue(i, j, 0);
                    DataManager.Instance.typeDamageByType[k].SetValue(i, j, 0);
                }


                BulletInfo info = revolverSlotContents[i].bulletInfo;
                if (info == null) continue;

                for (int k = 0; k < 6; k++)
                {
                    info.damage.SetValue(k, j, 0);
                    info.finalDamage.SetValue(k, j, 0);
                    info.typeDamage.SetValue(k, j, 0);
                }
            }
        }


        for (int i = 0; i < slotNum; i++)
        {
            BulletInfo info = revolverSlotContents[i].bulletInfo;
            if (info == null) continue;

            for (int j = 0; j < info.stats.Count; j++)
            {
                BulletStat stat = info.stats[j];
                switch (stat.target)
                {
                    case TargetType.Self:
                        ApplyRewardSlot(i, j, stat, info);
                        break;
                    case TargetType.SlotIndex:
                        ApplyRewardSlot(i, j, stat, revolverSlotContents[(int)stat.targetCoef[0]].bulletInfo);
                        break;
                    case TargetType.BulletType:

                        switch (stat.reward)
                        {
                            case RewardType.PowerIncrease:
                                DataManager.Instance.damageByType[(int)stat.targetCoef[0]].SetValue(i, j, stat.rewardCoef.Last());
                                break;
                            case RewardType.FinalDamageIncrease:
                                DataManager.Instance.finalDamageByType[(int)stat.targetCoef[0]].SetValue(i, j, stat.rewardCoef.Last());
                                break;
                            case RewardType.BulletTypeDamageIncrease:
                                DataManager.Instance.typeDamageByType[(int)stat.targetCoef[0]].SetValue(i, j, stat.rewardCoef.Last());
                                break;
                        }

                        break;
                    case TargetType.All:

                        switch (stat.reward)
                        {
                            case RewardType.PowerIncrease:
                                DataManager.Instance.damage.SetValue(i, j, stat.rewardCoef.Last());
                                break;
                            case RewardType.FinalDamageIncrease:
                                DataManager.Instance.finalDamage.SetValue(i, j, stat.rewardCoef.Last());
                                break;
                            case RewardType.BulletTypeDamageIncrease:
                                DataManager.Instance.typeDamage[(int)stat.targetCoef[0]].SetValue(i, j, stat.rewardCoef.Last());
                                break;
                        }

                        break;
                }
            }
        }
    }
    public void ApplyRewardSlot(int infoFrom, int index, BulletStat stat, BulletInfo infoTarget = null)
    {
        if (infoTarget == null) return;

        switch (stat.reward)
        {
            case RewardType.PowerIncrease:
                infoTarget.damage.SetValue(infoFrom, index, stat.rewardCoef.Last());
                break;
            case RewardType.FinalDamageIncrease:
                infoTarget.finalDamage.SetValue(infoFrom, index, stat.rewardCoef.Last());
                break;
            case RewardType.BulletTypeDamageIncrease:
                if ((int)infoTarget.infoSO.bulletType == stat.rewardCoef[0])
                    infoTarget.damage.SetValue(infoFrom, index, stat.rewardCoef.Last());
                break;
        }
    }
}
