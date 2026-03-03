using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
public class ForgePanel : MonoBehaviour
{
    [SerializeField] List<TextMeshProUGUI> beforeInfoText;
    [SerializeField] RectTransform beforeLayoutRoot;
    [SerializeField] List<TextMeshProUGUI> afterInfoText;
    [SerializeField] TextMeshProUGUI afterPowerText;
    [SerializeField] RectTransform afterLayoutRoot;
    [SerializeField] RevolverSlots revolver;
    [SerializeField] ForgeButton button;
    [SerializeField] TableLoaderManager table;
    BulletInfo info;
    DamageCalculator calculator = new();
    List<BulletStat> newBulletStats = new();
    Coroutine autoRerollRoutine;
    List<long> goldReq = new();
    List<int> slotCounts = new();
    public UnityEvent<int> onInfoUpdated = new();
    float cachedBasePower;
    int cachedIndex;
    List<BulletInfo> cachedRevolverInfo;
    List<CraftConditionData> conditionData = new();
    Dictionary<string, List<StatRangeData>> statData;
    Dictionary<int, List<float>> weightDict;
    List<string> gradeTexts = new();
    DamageModifier cachedModifier;
    bool isPowerCached;
    BulletStatGenerator statGenerator = new();
    long goldUsed = 0;
    void Awake()
    {
        button.onReroll.AddListener(Reroll);
        button.onRerollStart.AddListener(StartAutoReroll);
        button.onRerollStop.AddListener(StopAutoReroll);

        table.OnAllTablesLoaded.AddListener(LoadData);
    }
    public void LoadData()
    {
        var req = TierDataLoader.Instance.ReturnColumn(t => t.craftCost);
        goldReq = req;

        var slots = TierDataLoader.Instance.ReturnColumn(t => t.craftSlots);
        slotCounts = slots;

        var grade = TierDataLoader.Instance.ReturnColumn(t => t.nameKR);
        gradeTexts = grade;

        conditionData = CraftConditionLoader.Instance.GetAll();
        statData = StatTableLoader.Instance.statDict;
        weightDict = StatWeightLoader.Instance.weightDict;

        statGenerator.Init(conditionData, statData, weightDict);
    }
    public void SetCondition(BulletInfo info)
    {
        goldUsed = 0;
        this.info = info;
        GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        CacheBasePower();

        Reroll();

        UpdateInfoText(info.stats, beforeInfoText, beforeLayoutRoot);
    }
    public void DisablePanel()
    {
        GetComponent<RectTransform>().anchoredPosition = new Vector2(9999, 0);

        DataManager.Instance.Gold.GoldUseReq(GoldUseType.Forge, goldUsed);
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
        if (DataManager.Instance.Gold.Use(GoldUseType.Forge, goldReq[info.infoSO.tier]))
        {
            goldUsed += goldReq[info.infoSO.tier];
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
            if (!DataManager.Instance.Gold.Use(GoldUseType.Forge, goldReq[info.infoSO.tier]))
                break;

            goldUsed += goldReq[info.infoSO.tier];

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
        for (int i = 0; i < Mathf.Min(slotCounts[info.infoSO.tier], 4); i++)
        {
            newBulletStats.Add(statGenerator.Generate(info.infoSO.tier));
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
                infoText[i].text = BulletStatText.GetTargetText(bulletStats, i) + BulletStatText.GetRewardText(bulletStats, i, gradeTexts);
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
                powers.Add(calculator.CalculateDamage(cachedRevolverInfo[i], cachedModifier, i, DataManager.Instance.possPower).Item1);

            cachedIndex = powers.IndexOf(powers.Min());
            cachedRevolverInfo[cachedIndex] = info;

            cachedModifier = calculator.CollectModifiers(cachedRevolverInfo);
        }

        cachedBasePower = calculator.CalculateDamage(info, cachedModifier, cachedIndex, DataManager.Instance.possPower).Item1;

        isPowerCached = true;
    }
    private float GetPower(List<BulletStat> bulletStats)
    {
        if (!isPowerCached)
            CacheBasePower();

        BulletInfo newInfo = new BulletInfo(info);
        newInfo.stats = bulletStats;

        cachedRevolverInfo[cachedIndex] = newInfo;

        DamageModifier newMod = calculator.CollectModifiers(cachedRevolverInfo);

        float afterPower =
            calculator.CalculateDamage(newInfo, newMod, cachedIndex, DataManager.Instance.possPower).Item1;

        return afterPower - cachedBasePower;
    }
}
