using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class DrawSlotUI : MonoBehaviour
{
    [SerializeField] Image bulletImage;
    [SerializeField] TextMeshProUGUI bulletName;
    [SerializeField] TextMeshProUGUI levelOrCount;
    [SerializeField] GameObject newImage; //새로 얻은 탄환 표시
    [SerializeField] GameObject levUpImage; //레벨업 표시(새 탄환이 아닐 때만)
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
        bool isLevelUp = drawInfo.LevelUp > 0;

        bulletImage.sprite = info.infoSO.inventoryImage;
        bulletName.text = $"{gradeTexts[info.infoSO.tier]} {typeText[(int)info.infoSO.bulletType]}";

        if (newImage != null) newImage.SetActive(drawInfo.IsNew); //새 탄환이면 new 이미지 활성화
        if (levUpImage != null) levUpImage.SetActive(isLevelUp && !drawInfo.IsNew); //새 탄환이 아니면서 레벨업 시 표시

        if (!isLevelUp)
        {
            if (levelOrCount != null) levelOrCount.text = $"x{drawInfo.Gained}";
        }
        else
        {
            //연출 시점에는 info가 이미 갱신된 상태이므로 LevelUp만큼 역산해 이전 레벨을 구함
            int newLevel = info.Level;
            int prevLevel = info.Level - drawInfo.LevelUp;
            if (levelOrCount != null) levelOrCount.text = $"{prevLevel} -> {newLevel}";
        }
    }
}
