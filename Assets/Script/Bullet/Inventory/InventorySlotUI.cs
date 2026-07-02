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
    [SerializeField] TextMeshProUGUI equipText;
    [SerializeField] Slider levelGage;
    public void UpdateUI(BulletInfo info, bool isActive)
    {
        //탄환 안가지고 있을 경우
        if (info.Count == 0 && info.Level == 0)
        {
            bulletImage.sprite = info.infoSO.inventoryImage;
            bulletImage.enabled = true;
            if (bulletDisabled != null) bulletDisabled.enabled = true;

            gageText.enabled = false;

            levelText.enabled = false;

            if (equipText != null) equipText.enabled = false;

            levelGage.gameObject.SetActive(false);
        }
        else if (!isActive)
        {
            bulletImage.sprite = info.infoSO.inventoryImage;
            bulletImage.enabled = true;
            if (bulletDisabled != null) bulletDisabled.enabled = true;

            int level = info.Level;
            (int, int) data;
            
            BulletLevelLoader.Instance.GetProgress(info.Level, out data);

            gageText.enabled = false;

            levelText.text = "Lv." + level.ToString();
            levelText.enabled = true;

            if (equipText != null) equipText.enabled = true;

            levelGage.gameObject.SetActive(false);
        }
        else
        {
            bulletImage.sprite = info.infoSO.inventoryImage;
            bulletImage.enabled = true;

            bulletDisabled.enabled = false;

            int level = info.Level;
            (int, int) data;

            BulletLevelLoader.Instance.GetProgress(info.Level, out data);
            int req = data.Item1;
            int cum = data.Item2;

            gageText.text = $"{info.Count - cum}/{req}";
            gageText.enabled = true;

            levelText.text = "Lv." + level.ToString();
            levelText.enabled = true;

            if (equipText != null) equipText.enabled = false;

            levelGage.value = (float)(info.Count - cum) / req;
            levelGage.gameObject.SetActive(true);
        }
    }
}
