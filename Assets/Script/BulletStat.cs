using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public enum TargetType
{
    Self,
    SlotIndex,
    BulletType,
    All,
}
public enum RewardType
{
    PowerIncrease,
    FinalDamageIncrease,
    BulletTypeDamageIncrease,
}

public class BulletStat
{
    public TargetType target;
    public RewardType reward;
    public List<float> targetCoef = new();
    public List<float> rewardCoef = new();
    public float percentage;
    public BulletStat()
    {
        target = GetRandomEnumValue<TargetType>();
        reward = GetRandomEnumValue<RewardType>();

        target = TargetType.All;
        reward = RewardType.BulletTypeDamageIncrease;

        FillTargetCoef();
        FillRewardCoef();
    }

    public BulletStat(BulletStat other)
    {
        target = other.target;
        reward = other.reward;
        percentage = other.percentage;

        targetCoef = new List<float>(other.targetCoef);
        rewardCoef = new List<float>(other.rewardCoef);
    }
    private T GetRandomEnumValue<T>()
    {
        Array values = Enum.GetValues(typeof(T));
        return (T)values.GetValue(UnityEngine.Random.Range(0, values.Length));
    }

    private void FillTargetCoef()
    {
        switch (target)
        {
            case TargetType.Self:
                targetCoef.Add(1);
                break;
            case TargetType.SlotIndex:
                targetCoef.Add(UnityEngine.Random.Range(0, 6));
                targetCoef.Add(1.2f);
                break;
            case TargetType.BulletType:
                targetCoef.Add(UnityEngine.Random.Range(0, 4));
                targetCoef.Add(0.5f);
                break;
            case TargetType.All:
                targetCoef.Add(0.2f);
                break;
        }
    }
    private void FillRewardCoef()
    {
        float val1;
        float val2;
        switch (reward)
        {
            case RewardType.PowerIncrease:
                val1 = UnityEngine.Random.Range(0.5f, 1.5f);
                val2 = targetCoef.Last() * val1;
                rewardCoef.Add(val2);
                percentage = Normalize(val1, 0.5f, 1.5f) * 100;
                break;
            case RewardType.FinalDamageIncrease:
                val1 = UnityEngine.Random.Range(0.75f, 1.25f);
                val2 = targetCoef.Last() * val1;
                rewardCoef.Add(val2);
                percentage = Normalize(val1, 0.75f, 1.25f) * 100;
                break;
            case RewardType.BulletTypeDamageIncrease:
                val1 = UnityEngine.Random.Range(0.5f, 1.5f);
                val2 = targetCoef.Last() * val1;
                rewardCoef.Add(val2);
                percentage = Normalize(val1, 0.5f, 1.5f) * 100;
                break;
        }
    }
    public float Normalize(float value, float a, float b)
    {
        return Mathf.Clamp01((value - a) / (b - a));
    }
}
/**/