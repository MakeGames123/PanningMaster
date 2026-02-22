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
    }
    public (int, int) ReturnLevelStatus()
    {
        int currentCount = Count;

        int level = 0;
        while (true)
        {
            int req = level > levelUpCount.Count ? 100 : levelUpCount[level];
            if (currentCount - req < 0) break;
            currentCount -= req;

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
}
public static class BulletStatText
{
    private static List<string> typeString = new() { "화염", "전기", "얼음", "독" };
    public static string GetTargetText(List<BulletStat> stats, int index)
    {
        switch (stats[index].target)
        {
            case TargetType.Self:
                return "이 탄환의 ";
            case TargetType.BulletType:
                return $"모든 {typeString[(int)stats[index].targetCoef[0]]}속성 탄환의 ";
            case TargetType.All:
                return $"모든 탄환의 ";
            default:
                return "";
        }
    }
    public static string GetRewardText(List<BulletStat> stats, int index)
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