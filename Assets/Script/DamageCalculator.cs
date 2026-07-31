using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.PlayerLoop;
public class DamageCalculator
{
    List<float> basicDamage = new();
    List<float> lvScale = new();
    public void Initialize()
    {
        var dmgList = TierDataLoader.Instance.ReturnColumn(t => t.baseDmg);
        basicDamage = dmgList;

        var scaleList = TierDataLoader.Instance.ReturnColumn(t => t.lvScale);
        lvScale = scaleList;
    }

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
    // v40: 세공 스탯은 무조건 자기 탄환(약실 index)에만 적용 — 조건부(전체·속성탄) 폐지로 단일 경로
    void ApplyStat(DamageModifier mod, BulletStat stat, int index)
    {
        switch (stat.reward)
        {
            case RewardType.PowerIncrease: mod.SelfPower[index] += stat.rewardCoef; break;
            case RewardType.BulletTypeDamageIncrease: mod.SelfTypePower[index] += stat.rewardCoef; break;
            case RewardType.FinalDamageIncrease: mod.SelfFinal[index] += stat.rewardCoef; break;
            case RewardType.CriticalChanceIncrease: mod.SelfCriticalChance[index] += stat.rewardCoef; break;
            case RewardType.CriticalDamageIncrease: mod.SelfCriticalDamage[index] += stat.rewardCoef; break;
        }
    }
    // weapon: 이 탄환을 쏘는 사수(파티 슬롯)의 장착 무기 — v43 무기 속성 매칭용(null=매칭 없음).
    // 무기와 같은 속성 탄환이면 최종 데미지 +ElemFinalDmg%(배럭 스코프 — 전역 gdmg와 같은 가산 축, 프로토 fB+=revElFd).
    public (float, float, bool) CalculateDamage(BulletInfo info, DamageModifier mod, int index, float poss, WeaponData weapon = null)
    {
        if (info == null) return (0, 0, false);

        if (basicDamage.Count == 0) Initialize();

        float tierBase = basicDamage[info.infoSO.tier] + (info.Level - 1) * basicDamage[info.infoSO.tier] * lvScale[info.infoSO.tier];

        // 전역 스탯 버킷(성장·연구소·무기 합산 — PlayerStatAggregator 경유, 값 1 = 1%)
        var p = PlayerData.Instance;
        float gDamage = p != null ? p.Damage / 100f : 0f;
        float gTypeDamage = p != null ? p.TypeDamage / 100f : 0f;
        float gFinal = p != null ? p.FinalDamage / 100f : 0f;
        float gCritChance = p != null ? p.CriticalChance : 0f;
        float gCritDamage = p != null ? p.CriticalDamage / 100f : 0f;

        float powerPart =
            tierBase *
            (1 + mod.SelfPower[index]) *
            (1 + gDamage);

        float typePart =
            tierBase *
            (1 + mod.SelfTypePower[index]) *
            (1 + gTypeDamage);

        // v43 무기 속성 매칭: 무기와 같은 속성 탄환 = 최종 데미지 가산(전역 gdmg와 같은 축)
        float weaponElemFd = 0f;
        if (weapon != null && !string.IsNullOrEmpty(weapon.element)
            && weapon.element == ElementLoader.IdFromBulletType(info.infoSO.bulletType))
            weaponElemFd = WeaponGrades.ElemFinalDmg(weapon.grade) / 100f;

        float finalAmp =
            (1 + mod.SelfFinal[index]) *
            (1 + gFinal + weaponElemFd);

        float criticalChance = mod.SelfCriticalChance[index] * 100 + gCritChance;
        criticalChance = Mathf.Min(100, criticalChance);

        // 상수 3 = 구 3항((1+자기)+(1+전체)+(1+속성탄)) 합의 보존 — 조건부 폐지 전과 수치 동일 유지
        float criticalDamage = 3 + mod.SelfCriticalDamage[index] + gCritDamage;

        bool isCritical = UnityEngine.Random.Range(0, 100) < criticalChance;

        float damage = !isCritical ? (powerPart + typePart) * finalAmp : (powerPart + typePart) * finalAmp * (1 + (criticalDamage + 50) / 100) * (1 + poss);
        float power = (powerPart + typePart) * finalAmp * (1 + criticalChance / 100 * (criticalDamage + 50) / 100) * (1 + poss);

        return (power, damage, isCritical);
    }
}
// v40: 조건부(전체 All·속성탄 ByType) 폐지 — 세공 스탯은 자기 약실 것만 남음
public class DamageModifier
{
    public List<float> SelfPower = new() { 0, 0, 0, 0, 0, 0 };
    public List<float> SelfTypePower = new() { 0, 0, 0, 0, 0, 0 };
    public List<float> SelfFinal = new() { 0, 0, 0, 0, 0, 0 };
    public List<float> SelfCriticalChance = new() { 0, 0, 0, 0, 0, 0 };
    public List<float> SelfCriticalDamage = new() { 0, 0, 0, 0, 0, 0 };
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