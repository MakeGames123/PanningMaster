using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] Image bulletImage;
    [SerializeField] Image bulletDisabled;
    [SerializeField] Image background;
    [SerializeField] TextMeshProUGUI gageText;
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] Slider levelGage;
    public void UpdateUI(BulletInfo info, bool isActive)
    {
        if (info.Count == 0 || !isActive)
        {
            bulletImage.sprite = info.infoSO.inventoryImage;
            bulletImage.enabled = true;
            if (bulletDisabled != null) bulletDisabled.enabled = true;

            gageText.enabled = false;
            levelText.enabled = false;
            levelGage.gameObject.SetActive(false);
        }
        else
        {
            bulletImage.sprite = info.infoSO.inventoryImage;
            bulletImage.enabled = true;
            if (bulletDisabled != null) bulletDisabled.enabled = false;

            var levelStatus = info.ReturnLevelStatus();

            gageText.text = $"{levelStatus.Item1}/{levelStatus.Item2}";
            gageText.enabled = true;

            levelText.text = "Lv." + (info.ReturnLevel() + 1).ToString();
            levelText.enabled = true;

            levelGage.value = (float)levelStatus.Item1 / levelStatus.Item2;
            levelGage.gameObject.SetActive(true);
        }
    }
}
