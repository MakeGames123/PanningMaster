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
        maxHp = (int)(maxHp * 1.5f);
        hp = maxHp;
        UpdateUI();
    }
    public void ResetEnumy()
    {
        hp = maxHp;
        UpdateUI();
    }
}
