using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public enum BulletType
{
    FlameBullet = 0,
    ElectricBullet = 1,
    IceBullet = 2,
    PoisonBullet = 3,
    All = 4,
}
[CreateAssetMenu(fileName = "BulletInfoSO", menuName = "Scriptable Object/BulletInfoSO")]
public class BulletInfoSO : ScriptableObject
{
    public string bulletName;
    public BulletType bulletType;
    public Sprite inventoryImage;
    public int tier;
    public int bulletId;
    public int particleID;
}

[Serializable]
public class BulletInfo
{
    public BulletInfoSO infoSO;
    public List<BulletStat> stats = new();
    public SpecificStat damage;
    public SpecificStat typeDamage;
    public SpecificStat finalDamage;
    private int count;

    public int Count
    {
        get => count;
        set
        {
            count = value;
        }
    }
    List<int> levelUpCount = new() { 2, 3, 5, 8, 12, 18, 30, 50, 100 };
    public BulletInfo(BulletInfoSO infoSO)
    {
        Count = 0;
        this.infoSO = infoSO;
        damage = new SpecificStat();
        typeDamage = new SpecificStat();
        finalDamage = new SpecificStat();
    }
    public void RollStats()
    {
        stats.Clear();
        for (int i = 0; i < infoSO.tier / 2 + 1; i++)
        {
            stats.Add(new BulletStat(infoSO.tier));
        }
    }
    public (int, int) ReturnLevelStatus()
    {
        int currentCount = Count;

        int level = 0;
        while (true)
        {
            if (currentCount - levelUpCount[level] < 0) break;
            currentCount -= levelUpCount[level];

            level++;
            if (level > 99) break;
        }

        return (currentCount, levelUpCount[level]);
    }
    public void IncreaseCount(int val)
    {
        Count += val;
    }
    public int ReturnLevel()
    {
        int ret = Count;
        int level = 0;
        for (level = 0; level < levelUpCount.Count; level++)
        {
            ret -= levelUpCount[level];
            if (ret < 0) break;
        }
        return level;
    }
    public int ReturnCount()
    {
        return Count;
    }


    List<string> typeString = new() { "화염", "전기", "얼음", "독" };
    public string GetTargetText(int index)
    {
        switch (stats[index].target)
        {
            case TargetType.Self:
                return "이 탄환의 ";
            case TargetType.SlotIndex:
                return $"{stats[index].targetCoef[0] + 1}번째 슬롯 탄환의 ";
            case TargetType.BulletType:
                return $"모든 {typeString[(int)stats[index].targetCoef[0]]}속성 탄환의 ";
            case TargetType.All:
                return $"모든 탄환의 ";
            default:
                return "";
        }
    }
    public string GetRewardText(int index)
    {
        switch (stats[index].reward)
        {
            case RewardType.PowerIncrease:
                float powerIncrease = stats[index].rewardCoef;
                return $"공격력 {powerIncrease * 100:F1}% 증가({stats[index].percentage:F0}%)";

            case RewardType.FinalDamageIncrease:
                float finalDamage = stats[index].rewardCoef;
                return $"최종 데미지 {finalDamage * 100:F1}% 증가({stats[index].percentage:F0}%)";

            case RewardType.BulletTypeDamageIncrease:
                float typeDamage = stats[index].rewardCoef;
                return $"속성 공격력 {typeDamage * 100:F1}% 증가({stats[index].percentage:F0}%)";

            default:
                return "";
        }
    }
}
/*



*/