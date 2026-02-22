using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Workmanship : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI powerText;
    [SerializeField] List<TextMeshProUGUI> infoText;
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI reqText;
    [SerializeField] RevolverSlots revolver;
    BulletInfo info;
    DamageCalculator calculator = new();
    public RectTransform layoutRoot;
    List<int> goldReq = new() { 10, 100, 1000, 10000, 100000, 1000000 };
    List<string> gradeTexts = new() { "일반", "레어", "유니크", "에픽", "전설", "신화1", "신화2", "신화3", "신화4" };
    List<string> typeText = new() { "화염", "전기", "얼음", "독" };
    void Awake()
    {
        button.gameObject.SetActive(false);

        DataManager.Instance.onPowerChanged.AddListener(UpdateText);
    }
    public void UpdateInfo(int id)
    {
        info = AllBulletList.Instance.GetBullet(id);
        reqText.text = goldReq[info.infoSO.tier].ToString();
        UpdateText(0);
        button.gameObject.SetActive(true);
    }
    public void TryWorkmanship()
    {
        if (DataManager.Instance.TryUseGold(goldReq[info.infoSO.tier]))
        {
            info.RollStats();
            revolver.CheckSlots();
        }
    }
    private void UpdateText(double val)
    {
        for (int i = 0; i < 4; i++)
        {
            infoText[i].text = "";
        }

        if (info != null)
        {
            nameText.text = $"{gradeTexts[info.infoSO.tier]} {typeText[(int)info.infoSO.bulletType]}";

            powerText.text = $"{calculator.Calculate(info):F0}";

            for (int i = 0; i < info.stats.Count; i++)
            {
                infoText[i].text = info.GetTargetText(i) + info.GetRewardText(i);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
    }
}
