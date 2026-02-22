using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class WorkmanshipPanel : MonoBehaviour
{
    [SerializeField] List<TextMeshProUGUI> beforeInfoText;
    [SerializeField] RectTransform beforeLayoutRoot;
    [SerializeField] List<TextMeshProUGUI> afterInfoText;
    [SerializeField] TextMeshProUGUI afterPowerText;
    [SerializeField] RectTransform afterLayoutRoot;
    [SerializeField] RevolverSlots revolver;
    BulletInfo info;
    DamageCalculator calculator = new();

    List<BulletStat> newBulletStats = new();
    public void SetCondition(BulletInfo info)
    {
        this.info = info;
        RollStats();

        afterPowerText.text = $"{GetPower(newBulletStats).ToString():F1}";

        UpdateInfoText(info.stats, beforeInfoText, beforeLayoutRoot);
        UpdateInfoText(newBulletStats, afterInfoText, afterLayoutRoot);
    }
    public void Reroll()
    {
        RollStats();
        
        afterPowerText.text = GetPower(newBulletStats).ToString();

        UpdateInfoText(newBulletStats, afterInfoText, afterLayoutRoot);
    }
    public void RollStats()
    {
        newBulletStats.Clear();
        for (int i = 0; i < Mathf.Min((info.infoSO.tier + 1) / 2 + 1, 4); i++)
        {
            newBulletStats.Add(new BulletStat(info.infoSO.tier));
        }
    }
    private void UpdateInfoText(List<BulletStat> bulletStats, List<TextMeshProUGUI> infoText, RectTransform layout)
    {
        for (int i = 0; i < 4; i++)
        {
            infoText[i].text = "";
        }

        if (info != null)
        {
            for (int i = 0; i < bulletStats.Count; i++)
            {
                infoText[i].text = BulletStatText.GetTargetText(bulletStats, i) + BulletStatText.GetRewardText(bulletStats, i);
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(layout);
    }
    private float GetPower(List<BulletStat> bulletStats)
    {
        float power = 0;
        float afterPower = 0;

        if (info != null)
        {
            List<BulletInfo> revolverInfo = new();
            foreach (RevolverSlotContent content in revolver.revolverSlotContents)
            {
                revolverInfo.Add(AllBulletList.Instance.GetBullet(content.id));
            }

            DamageModifier mod = calculator.CollectModifiers(revolverInfo);


            int index = revolverInfo.IndexOf(info);

            if (index == -1)
            {
                List<float> powers = new();

                for (int i = 0; i < 6; i++)
                {
                    powers.Add(calculator.CalculateDamage(revolverInfo[i], mod, i));
                }

                int minIndex = powers.IndexOf(powers.Min());

                revolverInfo[minIndex] = info;

                DamageModifier mod2 = calculator.CollectModifiers(revolverInfo);

                power = calculator.CalculateDamage(info, mod2, minIndex);

                BulletInfo newInfo = new BulletInfo(info.infoSO);
                newInfo.stats = bulletStats;

                revolverInfo[minIndex] = newInfo;

                DamageModifier mod3 = calculator.CollectModifiers(revolverInfo);

                afterPower = calculator.CalculateDamage(newInfo, mod3, minIndex);
            }
            else
            {
                power = calculator.CalculateDamage(info, mod, index);

                BulletInfo newInfo = new BulletInfo(info.infoSO);
                newInfo.stats = bulletStats;

                revolverInfo[index] = newInfo;

                DamageModifier mod3 = calculator.CollectModifiers(revolverInfo);

                afterPower = calculator.CalculateDamage(newInfo, mod3, index);
            }
        }

        Debug.Log(afterPower + " " + power);
        return afterPower - power;
    }
}
