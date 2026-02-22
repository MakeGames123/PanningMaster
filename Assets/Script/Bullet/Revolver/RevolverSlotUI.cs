using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class RevolverSlotUI : MonoBehaviour
{
    [SerializeField] Image bulletImage;
    [SerializeField] Image background;
    [SerializeField] TextMeshProUGUI gageText;
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] Slider levelGage;
    public void UpdateUI(BulletInfo info)
    {
        if (info == null)
        {
            bulletImage.enabled = false;
            gageText.enabled = false;
            levelText.enabled = false;
            levelGage.gameObject.SetActive(false);
        }
        else
        {
            bulletImage.sprite = info.infoSO.inventoryImage;
            bulletImage.enabled = true;

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
