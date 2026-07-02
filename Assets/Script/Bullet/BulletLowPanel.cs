using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BulletLowPanel : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI bulletName;
    [SerializeField] TextMeshProUGUI BulletPower;
    [SerializeField] TextMeshProUGUI PossPower;
    [SerializeField] TextMeshProUGUI BulletStats;
    [SerializeField] TextMeshProUGUI ForgeReq;
    [SerializeField] Button forgeButton;
    [SerializeField] AllBulletList allBulletList;
    [SerializeField] RectTransform layoutRoot;
    [SerializeField] ForgePanel forgePanel;
    BulletInfo info;

    List<float> posses;
    List<float> baseDmgs;
    List<string> gradeTexts = new();
    List<long> goldReq = new();
    RectTransform rect;
    bool init = false;
    void Init()
    {
        if (init) return;
        init = true;
        posses = TierDataLoader.Instance.ReturnColumn(t => t.possScale);
        baseDmgs = TierDataLoader.Instance.ReturnColumn(t => t.baseDmg);

        var grade = TierDataLoader.Instance.ReturnColumn(t => t.nameKR);
        gradeTexts = grade;
        
        var req = TierDataLoader.Instance.ReturnColumn(t => t.craftCost);
        goldReq = req;

        forgeButton.onClick.AddListener(TryForge);

        forgePanel.onInfoUpdated.AddListener(UpdateUI);

        rect = GetComponent<RectTransform>();
    }
    public void TryForge()
    {
        if (DataManager.Instance.Gold.Use(GoldUseType.Forge, goldReq[info.infoSO.tier]))
        {
            forgePanel.gameObject.SetActive(true);
            forgePanel.SetCondition(info);
        }
    }
    public void UpdateUI(int id)
    {
        Init();

        rect.anchoredPosition = Vector2.zero;

        info = allBulletList.GetBullet(id);

        bulletName.text = info.infoSO.bulletName;
        BulletPower.text = $"공격력 {baseDmgs[info.infoSO.tier]}";
        PossPower.text = $"보유 효과: 전체 공격력 +{info.Level * posses[info.infoSO.tier]}%";
        ForgeReq.text = $"세공 :{goldReq[info.infoSO.tier]}";
        UpdateInfoText();
    }    
    private void UpdateInfoText()
    {
        string statText = "";
        List<BulletStat> bulletStats = info.stats;
        
        if (info != null)
        {
            for (int i = 0; i < bulletStats.Count; i++)
            {
                if(i != 0) statText += "\n";
                statText += BulletStatText.GetTargetText(bulletStats, i) + BulletStatText.GetRewardText(bulletStats, i, gradeTexts);
            }
        }

        BulletStats.text = statText;
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
    }
    public void DisablePanel()
    {
        rect.anchoredPosition = new Vector2(9999, 0);
    }
}
