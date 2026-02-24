using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
public class WorkmanshipPanel : MonoBehaviour
{
    [SerializeField] List<TextMeshProUGUI> beforeInfoText;
    [SerializeField] RectTransform beforeLayoutRoot;
    [SerializeField] List<TextMeshProUGUI> afterInfoText;
    [SerializeField] TextMeshProUGUI afterPowerText;
    [SerializeField] RectTransform afterLayoutRoot;
    [SerializeField] RevolverSlots revolver;
    [SerializeField] WorkmanshipButton button;
    BulletInfo info;
    DamageCalculator calculator = new();
    List<BulletStat> newBulletStats = new();
    Coroutine autoRerollRoutine;
    List<int> goldReq = new() { 10, 100, 1000, 10000, 100000, 1000000 };
    public UnityEvent<int> onInfoUpdated = new();
    float cachedBasePower;
    int cachedIndex;
    List<BulletInfo> cachedRevolverInfo;
    DamageModifier cachedModifier;
    bool isPowerCached;
    void Awake()
    {
        button.onReroll.AddListener(Reroll);
        button.onRerollStart.AddListener(StartAutoReroll);
        button.onRerollStop.AddListener(StopAutoReroll);
    }
    public void SetCondition(BulletInfo info)
    {
        this.info = info;

        CacheBasePower();

        Reroll();

        UpdateInfoText(info.stats, beforeInfoText, beforeLayoutRoot);
    }
    public void ApplyNewStats()
    {
        info.stats = new List<BulletStat>(newBulletStats);
        onInfoUpdated.Invoke(info.infoSO.bulletId);
        gameObject.SetActive(false);
    }
    public void Reroll()
    {
        RollStats();

        afterPowerText.text = $"{GetPower(newBulletStats):F1}";

        UpdateInfoText(newBulletStats, afterInfoText, afterLayoutRoot);
    }
    public void TryReroll()
    {
        if (DataManager.Instance.TryUseGold(goldReq[info.infoSO.tier]))
        {
            Reroll();
        }
    }
    void StartAutoReroll()
    {
        if (autoRerollRoutine == null)
            autoRerollRoutine = StartCoroutine(AutoReroll());
    }
    void StopAutoReroll()
    {
        if (autoRerollRoutine != null)
        {
            StopCoroutine(autoRerollRoutine);
            autoRerollRoutine = null;
        }
    }
    IEnumerator AutoReroll()
    {
        while (true)
        {
            if (!DataManager.Instance.TryUseGold(goldReq[info.infoSO.tier]))
                break;

            RollStats();

            float diff = GetPower(newBulletStats);

            afterPowerText.text = $"{diff:F1}";
            UpdateInfoText(newBulletStats, afterInfoText, afterLayoutRoot);

            if (diff > 0f)
            {
                break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        autoRerollRoutine = null;
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
    void CacheBasePower()
    {
        cachedRevolverInfo = new();

        foreach (RevolverSlotContent content in revolver.revolverSlotContents)
        {
            cachedRevolverInfo.Add(AllBulletList.Instance.GetBullet(content.id));
        }

        cachedModifier = calculator.CollectModifiers(cachedRevolverInfo);

        cachedIndex = cachedRevolverInfo.IndexOf(info);

        if (cachedIndex == -1)
        {
            List<float> powers = new();

            for (int i = 0; i < 6; i++)
                powers.Add(calculator.CalculateDamage(cachedRevolverInfo[i], cachedModifier, i));

            cachedIndex = powers.IndexOf(powers.Min());
            cachedRevolverInfo[cachedIndex] = info;

            cachedModifier = calculator.CollectModifiers(cachedRevolverInfo);
        }

        cachedBasePower = calculator.CalculateDamage(info, cachedModifier, cachedIndex);

        isPowerCached = true;
    }
    private float GetPower(List<BulletStat> bulletStats)
    {
        if (!isPowerCached)
            CacheBasePower();

        BulletInfo newInfo = new BulletInfo(info.infoSO);
        newInfo.stats = bulletStats;

        cachedRevolverInfo[cachedIndex] = newInfo;

        DamageModifier newMod = calculator.CollectModifiers(cachedRevolverInfo);

        float afterPower =
            calculator.CalculateDamage(newInfo, newMod, cachedIndex);

        return afterPower - cachedBasePower;
    }
}
