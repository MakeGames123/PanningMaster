using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Workmanship : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI infoText;
    BulletInfo info;
    List<float> basicDamage = new() { 2, 10, 50, 250, 1000, 4000, 15000, 50000, 120000 };
    public void UpdateInfo(BulletInfo info)
    {
        this.info = info;
        UpdateText();
    }
    public void TryWorkmanship()
    {
        info.RollStats();
        UpdateText();
    }
    private void UpdateText()
    {
        string text = "";

        if (info != null)
        {
            text += basicDamage[info.infoSO.tier].ToString() + "\n";
            for (int i = 0; i < info.stats.Count; i++)
            {
                text += info.GetTargetText(i) + info.GetRewardText(i);
                text += "\n";
            }
        }
        infoText.text = text;
    }
}
