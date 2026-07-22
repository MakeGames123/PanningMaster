using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    private float maxHp = 10;
    private float hp = 10;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private EnemyAnim anim;
    [SerializeField] private DamageFloatingText damageText;

    public bool Attacked(float damage)
    {
        hp -= damage;

        if (damageText != null) damageText.Show(damage); //피격 데미지 플로팅

        if (hp <= 0)
        {
            UpdateUI();
            if (anim != null) anim.PlayDie(); //사망 연출: 오른쪽 위로 빙글빙글 날아감
            return true;
        }

        UpdateUI();
        return false;
    }
    void UpdateUI()
    {
        if (hp <= 0) hp = 0;
        slider.value = hp / maxHp;
        text.text = $"{hp:F0}/{maxHp}";
    }
    public void HandleEnumy()
    {
        if (hp <= 0) UpgradeEnumy();
        else ResetEnumy();
    }
    public void UpgradeEnumy()
    {
        DataManager.Instance.IncreaseTicket(2);
        DataManager.Instance.IncreaseStage();

        if (QuestEventManager.Instance != null) QuestEventManager.Instance.AddEvent("kill"); //업적: 클리어(처치)
        maxHp = MonsterHpTableLoader.Instance.GetHP(DataManager.Instance.stage);
        hp = maxHp;
        if (anim != null) anim.ResetAnim(); //리스폰: 원위치/원상태 복귀
        UpdateUI();
    }
    public void ResetEnumy()
    {
        hp = maxHp;
        UpdateUI();
    }

    public float MaxHp => maxHp;

    //최대 체력을 지정하고 가득 채움(던전 등). 죽음 연출 상태도 초기화.
    public void SetMaxHp(float value)
    {
        maxHp = value;
        hp = value;
        if (anim != null) anim.ResetAnim();
        UpdateUI();
    }
}
