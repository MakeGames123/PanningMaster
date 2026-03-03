using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public enum TargetType
{
    Self = 0,
    BulletType = 1,
    All = 2,
}
public enum RewardType
{
    PowerIncrease,
    FinalDamageIncrease,
    BulletTypeDamageIncrease,
    CriticalChanceIncrease,
    CriticalDamageIncrease,
}

[Serializable]
public class BulletStat
{
    public TargetType target;
    public RewardType reward;
    public float rewardCoef;
    public int targetCoef;
    public int statTier;

    public BulletStat(
        TargetType target,
        RewardType reward,
        float rewardCoef,
        int targetCoef,
        int statTier)
    {
        this.target = target;
        this.reward = reward;
        this.rewardCoef = rewardCoef;
        this.targetCoef = targetCoef;
        this.statTier = statTier;
    }
}
public class BulletStatGenerator
{
    List<CraftConditionData> conditionData;
    Dictionary<string, List<StatRangeData>> statData;
    Dictionary<int, List<float>> weightDict;

    public void Init(List<CraftConditionData> conditionData, Dictionary<string, List<StatRangeData>> statData, Dictionary<int, List<float>> weightDict)
    {
        this.conditionData = conditionData;
        this.statData = statData;
        this.weightDict = weightDict;
    }
    public BulletStat Generate(int tier)
    {
        TargetType target = GetRandomEnumValue<TargetType>();
        RewardType reward = GetRandomEnumValue<RewardType>();

        int targetCoef = CalculateTargetCoef(target);
        float conditionCoef = conditionData[(int)target].multiplier;
        int statTier = GetRandomStatTier(weightDict[tier]);
        float rewardCoef = CalculateRewardCoef(reward, conditionCoef, statTier);

        return new BulletStat(
            target,
            reward,
            rewardCoef,
            targetCoef,
            statTier);
    }
    T GetRandomEnumValue<T>()
    {
        Array values = Enum.GetValues(typeof(T));
        return (T)values.GetValue(UnityEngine.Random.Range(0, values.Length));
    }
    int CalculateTargetCoef(TargetType target)
    {
        if (target == TargetType.BulletType)
            return UnityEngine.Random.Range(0, 4);

        return -1;
    }
    private float CalculateRewardCoef(RewardType reward, float conditionCoef, int statTier)
    {
        float val1;

        switch (reward)
        {
            case RewardType.PowerIncrease:
                val1 = UnityEngine.Random.Range(statData["atk"][statTier].min, statData["atk"][statTier].max);
                return conditionCoef * val1;
            case RewardType.FinalDamageIncrease:
                val1 = UnityEngine.Random.Range(statData["finalDmg"][statTier].min, statData["finalDmg"][statTier].max);
                return conditionCoef * val1;
            case RewardType.BulletTypeDamageIncrease:
                val1 = UnityEngine.Random.Range(statData["elemDmg"][statTier].min, statData["elemDmg"][statTier].max);
                return conditionCoef * val1;
            case RewardType.CriticalChanceIncrease:
                val1 = UnityEngine.Random.Range(statData["critR"][statTier].min, statData["critR"][statTier].max);
                return conditionCoef * val1;
            case RewardType.CriticalDamageIncrease:
                val1 = UnityEngine.Random.Range(statData["critD"][statTier].min, statData["critD"][statTier].max);
                return conditionCoef * val1;
        }

        return -1;
    }
    public int GetRandomStatTier(List<float> weights)
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