using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Workmanship : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI powerText;
    [SerializeField] List<TextMeshProUGUI> infoText;
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI reqText;
    [SerializeField] RevolverSlots revolver;
    [SerializeField] WorkmanshipPanel panel;
    BulletInfo info;
    DamageCalculator calculator = new();
    public RectTransform layoutRoot;
    List<int> goldReq = new();
    List<string> gradeTexts = new();
    List<string> typeText = new() { "화염", "전기", "얼음", "독" };
    void Awake()
    {
        button.gameObject.SetActive(false);
        panel.onInfoUpdated.AddListener(UpdateInfo);
    }
    void Start()
    {
        TierDataLoader.Instance.OnDataLoaded += LoadData;
    }
    void LoadData()
    {
        var req = TierDataLoader.Instance.ReturnColumn(t => t.craftCost);
        goldReq = req;

        var grade = TierDataLoader.Instance.ReturnColumn(t => t.nameKR);
        gradeTexts = grade;

        TierDataLoader.Instance.OnDataLoaded -= LoadData;
    }
    public void UpdateInfo(int id)
    {
        info = AllBulletList.Instance.GetBullet(id);
        reqText.text = goldReq[info.infoSO.tier].ToString();
        button.gameObject.SetActive(true);
        UpdateText();
    }
    public void TryWorkmanship()
    {
        if (DataManager.Instance.TryUseGold(goldReq[info.infoSO.tier]))
        {
            panel.gameObject.SetActive(true);
            panel.SetCondition(info);
        }
    }
    private void UpdateText()
    {
        for (int i = 0; i < 4; i++)
        {
            infoText[i].text = "";
        }

        if (info != null)
        {
            nameText.text = $"{gradeTexts[info.infoSO.tier]} {typeText[(int)info.infoSO.bulletType]}";

            List<BulletInfo> revolverInfo = new();
            foreach (RevolverSlotContent content in revolver.revolverSlotContents)
            {
                revolverInfo.Add(AllBulletList.Instance.GetBullet(content.id));
            }

            DamageModifier mod = calculator.CollectModifiers(revolverInfo);

            float power = 0;

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
            }
            else
            {
                power = calculator.CalculateDamage(info, mod, index);
            }

            powerText.text = $"{power:F0}";

            for (int i = 0; i < info.stats.Count; i++)
            {
                infoText[i].text = BulletStatText.GetTargetText(info.stats, i) + BulletStatText.GetRewardText(info.stats, i);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
    }
}
