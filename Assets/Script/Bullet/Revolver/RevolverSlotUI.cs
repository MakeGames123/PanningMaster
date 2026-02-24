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

            int level = info.Level;

            int current;
            int req;

            BulletLevelLoader.Instance.GetProgress(info.Count, out current, out req);

            gageText.text = $"{current}/{req}";
            gageText.enabled = true;

            levelText.text = "Lv." + level.ToString();
            levelText.enabled = true;

            levelGage.value = (float)current / req;
            levelGage.gameObject.SetActive(true);
        }
    }
}
