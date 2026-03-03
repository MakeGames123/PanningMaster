using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class DrawSlotUI : MonoBehaviour
{
    [SerializeField] Image bulletImage;
    [SerializeField] TextMeshProUGUI bulletName;
    [SerializeField] TextMeshProUGUI levelOrCount;
    List<string> typeText = new() { "화염", "전기", "얼음", "독" };
    List<string> gradeTexts = new();

    void LoadData()
    {
        var grade = TierDataLoader.Instance.ReturnColumn(t => t.nameKR);
        gradeTexts = grade;
    }

    public void UpdateUI(int id, DrawInfo drawInfo)
    {
        if(gradeTexts.Count <= 0) LoadData();

        BulletInfo info = AllBulletList.Instance.bulletInfos[id];
        bool isLevelUp = info.Level - drawInfo.Level <= 0;

        bulletImage.sprite = info.infoSO.inventoryImage;
        bulletName.text = $"{gradeTexts[info.infoSO.tier]} {typeText[(int)info.infoSO.bulletType]}";

        if (isLevelUp)
        {
            levelOrCount.text = $"x{drawInfo.Gained}";
        }
        else
        {
            levelOrCount.text = $"{info.Level - drawInfo.Level} -> {drawInfo.Level}";
        }
    }
}
