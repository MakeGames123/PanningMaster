using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
public class DamageCalculator
{
    List<float> basicDamage = new() { 2, 10, 50, 250, 1000, 4000, 15000, 50000, 120000 };

    public DamageModifier CollectModifiers(List<BulletInfo> infos)
    {
        DamageModifier mod = new DamageModifier();

        for (int i = 0; i < infos.Count; i++)
        {
            var info = infos[i];
            if (info == null) continue;

            foreach (var stat in info.stats)
            {
                ApplyStat(mod, stat, i);
            }
        }

        return mod;
    }
    void ApplyStat(DamageModifier mod, BulletStat stat, int index)
    {
        switch (stat.target)
        {
            case TargetType.Self:
                ApplyReward(mod, stat.reward, stat.rewardCoef, -1, index);
                break;

            case TargetType.All:
                ApplyReward(mod, stat.reward, stat.rewardCoef, -2, index);
                break;

            case TargetType.BulletType:
                int type = (int)stat.targetCoef[0];
                ApplyReward(mod, stat.reward, stat.rewardCoef, type, index);
                break;
        }
    }
    void ApplyReward(DamageModifier mod, RewardType reward, float value, int type, int index)
    {
        switch (reward)
        {
            case RewardType.PowerIncrease:
                if (type == -1) mod.SelfPower[index] += value;
                else if (type == -2) mod.AllPower += value;
                else mod.PowerByType[type] += value;
                break;

            case RewardType.BulletTypeDamageIncrease:
                if (type == -1) mod.SelfTypePower[index] += value;
                else if (type == -2) mod.AllTypePower += value;
                else mod.TypePowerByType[type] += value;
                break;

            case RewardType.FinalDamageIncrease:
                if (type == -1) mod.SelfFinal[index] += value;
                else if (type == -2) mod.AllFinal += value;
                else mod.FinalByType[type] += value;
                break;
        }
    }
    public float CalculateDamage(BulletInfo info, DamageModifier mod, int index)
    {
        if(info == null) return 0;
        
        int type = (int)info.infoSO.bulletType;
        float tierBase = basicDamage[info.infoSO.tier];

        float powerPart =
            tierBase *
            (1 + mod.SelfPower[index]) *
            (1 + mod.AllPower) *
            (1 + mod.PowerByType[type]);

        float typePart =
            tierBase *
            (1 + mod.SelfTypePower[index]) *
            (1 + mod.AllTypePower) *
            (1 + mod.TypePowerByType[type]);

        float finalAmp =
            (1 + mod.SelfFinal[index]) *
            (1 + mod.AllFinal) *
            (1 + mod.FinalByType[type]);

        return (powerPart + typePart) * finalAmp;
    }
}
public class DamageModifier
{
    public List<float> SelfPower = new() { 0, 0, 0, 0, 0, 0 };
    public List<float> SelfTypePower = new() { 0, 0, 0, 0, 0, 0 };
    public List<float> SelfFinal = new() { 0, 0, 0, 0, 0, 0 };

    public float AllPower;
    public float AllTypePower;
    public float AllFinal;

    public List<float> PowerByType = new() { 0, 0, 0, 0 };
    public List<float> TypePowerByType = new() { 0, 0, 0, 0 };
    public List<float> FinalByType = new() { 0, 0, 0, 0 };
}
/*Debug.Log(
            $"[DamageCalculator]\n" +
            $"Bullet: {info.infoSO.bulletName}\n" +
            $"Tier Base: {tierBase}\n" +
            $"Count: {level}\n" +
            $"NormalDamage: {normalDamage}\n" +
            $"NormalTypeDamage: {normalTypeDamage}\n" +
            $"NormalDamageAmp: {normalDamageAmp}\n" +
            $"NormalTypeDamageAmp: {info.typeDamage.GetValue()}\n" +
            $"NormalTypeDamageAmp: {DataManager.Instance.typeDamage.GetValue()}\n" +
            $"NormalTypeDamageAmp: {DataManager.Instance.typeDamageByType[(int)info.infoSO.bulletType].GetValue()}\n" +
            $"NormalTypeDamageAmp: {normalTypeDamageAmp}\n" +
            $"FinalDamageAmp1: {info.finalDamage.GetValue()}\n" +
            $"FinalDamageAmp2: {DataManager.Instance.finalDamage.GetValue()}\n" +
            $"FinalDamageAmp3: {DataManager.Instance.finalDamageByType[(int)info.infoSO.bulletType].GetValue()}\n" +
            $"FinalDamageAmp: {finalDamage}\n" +
            $"---------------------------------\n" +
            $"Final Result: {damage}"
        );*/