using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradePanel : MonoBehaviour
{
    [SerializeField] List<TextMeshProUGUI> percentages = new();
    [SerializeField] TextMeshProUGUI reqGold = new();
    [SerializeField] Button upgradeButton;
    List<string> gradeTexts = new() { "일반", "레어", "유니크", "에픽", "전설", "신화1", "신화2", "신화3", "신화4" };

    void OnEnable()
    {
        int level = DataManager.Instance.upgradeLevel;
        DrawData currentData = DrawLevelLoader.Instance.ReturnData(level);
        DrawData nextData = DrawLevelLoader.Instance.ReturnData(level + 1);

        for (int i = 0; i < 9; i++)
        {
            percentages[i].text = $"{gradeTexts[i]} 탄환: {currentData.weights[i]} -> {nextData.weights[i]}";
        }

        reqGold.text = nextData.price.ToString();
    }
    public void TryUpgrade()
    {
        DrawData nextData = DrawLevelLoader.Instance.ReturnData(DataManager.Instance.upgradeLevel + 1);
        DataManager.Instance.TryUseGold(nextData.price);
    }
}
