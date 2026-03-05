using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UpgradePanel : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] List<TextMeshProUGUI> percentages = new();
    int index;
    List<string> gradeTexts = new() { "일반", "레어", "유니크", "에픽", "전설", "신화1", "신화2", "신화3", "신화4" };

    void OnEnable()
    {
        int level = DataManager.Instance.drawData.drawLevel;
        index = level - 1;

        UpdateUI();
    }
    public void UpdateUI()
    {
        DrawData currentData = DrawPercentageLoader.Instance.ReturnData(index + 1);
        levelText.text = (index + 1).ToString();

        for (int i = 0; i < 9; i++)
        {
            percentages[i].text = $"{gradeTexts[i]} 탄환: {currentData.weights[i]}";
        }
    }
    public void Right()
    {
        index++;
        if (index >= 9) index = 8;
        UpdateUI();
    }
    public void Left()
    {
        index--;
        if (index < 0) index = 0;
        UpdateUI();
    }
}
