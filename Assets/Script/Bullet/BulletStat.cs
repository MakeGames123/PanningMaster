using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public enum RewardType
{
    PowerIncrease,
    FinalDamageIncrease,
    BulletTypeDamageIncrease,
    CriticalChanceIncrease,
    CriticalDamageIncrease,
}

// 세공 스탯 1줄 — 무조건 이 탄환에만 적용(프로토 v40 확정: 조건부(전체·속성탄) 전면 폐지).
// 구세이브의 target/targetCoef 필드는 로드 시 무시되어 값 그대로 자기 전용으로 전환된다(mig40 문법).
[Serializable]
public class BulletStat
{
    public RewardType reward;
    public float rewardCoef;
    public int statTier;

    public BulletStat(
        RewardType reward,
        float rewardCoef,
        int statTier)
    {
        this.reward = reward;
        this.rewardCoef = rewardCoef;
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
        RewardType reward = GetRandomEnumValue<RewardType>();

        // 조건 배율은 self 행만 사용(v40: 세공 = 해당 탄환만 강화 — CraftCondition 시트의 self=1)
        float conditionCoef = SelfConditionCoef();
        int statTier = GetRandomStatTier(weightDict[tier]);
        float rewardCoef = CalculateRewardCoef(reward, conditionCoef, statTier);

        return new BulletStat(
            reward,
            rewardCoef,
            statTier);
    }
    float SelfConditionCoef()
    {
        if (conditionData == null) return 1f;
        foreach (var c in conditionData)
            if (c.condId == "self") return c.multiplier;
        return conditionData.Count > 0 ? conditionData[0].multiplier : 1f;
    }
    T GetRandomEnumValue<T>()
    {
        Array values = Enum.GetValues(typeof(T));
        return (T)values.GetValue(UnityEngine.Random.Range(0, values.Length));
    }
    private float CalculateRewardCoef(RewardType reward, float conditionCoef, int statTier)
    {
        float val1;

        switch (reward)
        {
            case RewardType.PowerIncrease:
                val1 = UnityEngine.Random.Range(statData["Damage"][statTier].min, statData["Damage"][statTier].max);
                return conditionCoef * val1;
            case RewardType.FinalDamageIncrease:
                val1 = UnityEngine.Random.Range(statData["FinalDamage"][statTier].min, statData["FinalDamage"][statTier].max);
                return conditionCoef * val1;
            case RewardType.BulletTypeDamageIncrease:
                val1 = UnityEngine.Random.Range(statData["TypeDamage"][statTier].min, statData["TypeDamage"][statTier].max);
                return conditionCoef * val1;
            case RewardType.CriticalChanceIncrease:
                val1 = UnityEngine.Random.Range(statData["CriticalChance"][statTier].min, statData["CriticalChance"][statTier].max);
                return conditionCoef * val1;
            case RewardType.CriticalDamageIncrease:
                val1 = UnityEngine.Random.Range(statData["CriticalDamage"][statTier].min, statData["CriticalDamage"][statTier].max);
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