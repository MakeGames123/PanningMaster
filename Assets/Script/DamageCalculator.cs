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
    public float Calculate(BulletInfo info)
    {
        float tierBase = basicDamage[info.infoSO.tier];
        int level = info.ReturnLevel();

        float normalDamage = tierBase + level * tierBase;
        float normalTypeDamage = tierBase + level * tierBase;

        float normalDamageAmp =
            info.damage.GetValue()
            + DataManager.Instance.damage.GetValue()
            + DataManager.Instance.damageByType[(int)info.infoSO.bulletType].GetValue()
            + 1;

        float normalTypeDamageAmp =
            info.typeDamage.GetValue()
            + DataManager.Instance.typeDamage.GetValue()
            + DataManager.Instance.typeDamageByType[(int)info.infoSO.bulletType].GetValue()
            + 1;

        float finalDamage =
            info.finalDamage.GetValue()
            + DataManager.Instance.finalDamage.GetValue()
            + DataManager.Instance.finalDamageByType[(int)info.infoSO.bulletType].GetValue()
            + 1;

        float damage =
            (normalDamage * normalDamageAmp
            + normalTypeDamage * normalTypeDamageAmp)
            * finalDamage;

        Debug.Log(
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
        );

        return damage;
    }
}
/**/