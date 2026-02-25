using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public enum TargetType
{
    Self,
    BulletType,
    All,
}
public enum RewardType
{
    PowerIncrease,
    FinalDamageIncrease,
    BulletTypeDamageIncrease,
    CriticalChanceIncrease,
    CriticalDamageIncrease,
}

public class BulletStat
{
    public TargetType target;
    public RewardType reward;
    public float targetCoef;
    public float conditionCoef;
    public float rewardCoef;
    public float percentage;
    public int statTier;
    int tier;
    List<CraftConditionData> conditionData;
    Dictionary<string, List<StatRangeData>> statData;
    Dictionary<int, List<float>> weightDict;
    public BulletStat(int tier, List<CraftConditionData> conditionData, Dictionary<string, List<StatRangeData>> statData, Dictionary<int, List<float>> weightDict)
    {
        this.tier = tier;
        this.conditionData = conditionData;
        this.statData = statData;
        this.weightDict = weightDict;
        target = GetRandomEnumValue<TargetType>();
        reward = GetRandomEnumValue<RewardType>();

        FillTargetCoef();
        FillRewardCoef();
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
                conditionCoef = conditionData[0].multiplier;
                break;
            case TargetType.BulletType:
                targetCoef = UnityEngine.Random.Range(0, 4);
                conditionCoef = conditionData[1].multiplier;
                break;
            case TargetType.All:
                conditionCoef = conditionData[2].multiplier;
                break;
        }
    }
    private void FillRewardCoef()
    {
        float val1;
        float val2;

        statTier = GetRandomByWeight(weightDict[tier]);
        switch (reward)
        {
            case RewardType.PowerIncrease:
                val1 = UnityEngine.Random.Range(statData["atk"][statTier].min, statData["atk"][statTier].max);
                val2 = conditionCoef * val1;
                rewardCoef = val2;
                break;
            case RewardType.FinalDamageIncrease:
                val1 = UnityEngine.Random.Range(statData["finalDmg"][statTier].min, statData["finalDmg"][statTier].max);
                val2 = conditionCoef * val1;
                rewardCoef = val2;
                break;
            case RewardType.BulletTypeDamageIncrease:
                val1 = UnityEngine.Random.Range(statData["elemDmg"][statTier].min, statData["elemDmg"][statTier].max);
                val2 = conditionCoef * val1;
                rewardCoef = val2;
                break;
            case RewardType.CriticalChanceIncrease:
                val1 = UnityEngine.Random.Range(statData["critR"][statTier].min, statData["critR"][statTier].max);
                val2 = conditionCoef * val1;
                rewardCoef = val2;
                break;
            case RewardType.CriticalDamageIncrease:
                val1 = UnityEngine.Random.Range(statData["critD"][statTier].min, statData["critD"][statTier].max);
                val2 = conditionCoef * val1;
                rewardCoef = val2;
                break;
        }
    }
    public int GetRandomByWeight(List<float> weights)
    {
        float rand = UnityEngine.Random.Range(0, 100);

        float sum = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            sum += weights[i];
            if (rand <= sum)
                return i;
        }

        return 0;
    }
}
/**/