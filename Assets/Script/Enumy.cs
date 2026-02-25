using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Enumy : MonoBehaviour
{
    private float maxHp = 10;
    private float hp = 10;
    [SerializeField]private Slider slider;
    [SerializeField]private TextMeshProUGUI text;
    public bool Attacked(float damage)
    {
        hp -= damage;

        if (hp <= 0)
        {
            UpgradeEnumy();
            return true;
        }

        UpdateUI();
        return false;
    }
    void UpdateUI()
    {
        slider.value = hp / maxHp;
        text.text = $"{hp:F0}/{maxHp}";
    }
    public void UpgradeEnumy()
    {
        DataManager.Instance.IncreaseTicket(5);
        DataManager.Instance.IncreaseStage();
        maxHp = MonsterHpTableLoader.Instance.GetHP(DataManager.Instance.stage);
        hp = maxHp;
        UpdateUI();
    }
    public void ResetEnumy()
    {
        hp = maxHp;
        UpdateUI();
    }
}
