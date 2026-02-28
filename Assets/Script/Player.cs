using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    [SerializeField] RevolverSlots revolver;
    [SerializeField] Enumy enumy;
    [SerializeField] GameObject bulletLine;
    [SerializeField] BattleResult battleResult;
    GameObject bulletLineCpy;
    DamageCalculator calculator = new();
    int bulletIndex = 0;
    Coroutine attackCoroutine = null;

    void Start()
    {
        StartCoroutine(PlayerAttack());
    }
    private IEnumerator AttackStart()
    {
        yield return new WaitForSeconds(2.5f);

        StartCoroutine(PlayerAttack());
    }
    private IEnumerator PlayerAttack()
    {
        while (true)
        {
            if (revolver.CheckEmpty())
            {
                if (attackCoroutine != null) StopCoroutine(attackCoroutine);

                attackCoroutine = StartCoroutine(AttackStart());
                break;
            }

            if (Attack()) break;
            yield return new WaitForSeconds(0.1f);
        }
    }
    private bool Attack()
    {
        while (revolver.revolverSlotContents[bulletIndex].IsEmpty)
        {
            bulletIndex++;
            if (bulletIndex == 6)
            {
                StartCoroutine(Lose());
                return true;
            }
        }

        bulletLineCpy = Instantiate(bulletLine, transform.parent);
        bulletLineCpy.GetComponent<BulletLine>().AdjustLine(transform.localPosition, enumy.transform.localPosition + new Vector3(0, Random.Range(-40, 40)));

        List<BulletInfo> revolverInfo = new();
        foreach (RevolverSlotContent content in revolver.revolverSlotContents)
        {
            revolverInfo.Add(AllBulletList.Instance.GetBullet(content.id));
        }

        DamageModifier mod = calculator.CollectModifiers(revolverInfo);
        float damage = calculator.CalculateDamage(revolverInfo[bulletIndex], mod, bulletIndex, DataManager.Instance.possPower).Item2;
        if (enumy.Attacked(damage))
        {
            StartCoroutine(Win());
            return true;
        }
        else
        {
            bulletIndex++;
            if (bulletIndex == 6)
            {
                StartCoroutine(Lose());
                return true;
            }
        }

        return false;
    }

    private IEnumerator Lose()
    {
        yield return new WaitForSeconds(0.5f);

        bulletIndex = 0;
        int stage = DataManager.Instance.stage;

        float gold = stage
        * GameConfigLoader.Instance.GetFloat("goldBaseMultiplier")
        * (1 + Mathf.Max(0, stage - GameConfigLoader.Instance.GetFloat("goldScaleStartStage")) * GameConfigLoader.Instance.GetFloat("goldPostStageScale"))
        * GameConfigLoader.Instance.GetFloat("failGoldRatio");
        DataManager.Instance.IncreaseGold((int)gold);

        DataManager.Instance.IncreaseTicket(GameConfigLoader.Instance.GetInt("failBaseTickets"));

        battleResult.SetCondition(false, gold);

        if (attackCoroutine != null) StopCoroutine(attackCoroutine);

        attackCoroutine = StartCoroutine(AttackStart());

        yield return new WaitForSeconds(2f);

        enumy.HandleEnumy();
    }
    private IEnumerator Win()
    {
        yield return new WaitForSeconds(0.5f);

        bulletIndex = 0;
        int stage = DataManager.Instance.stage;

        float gold = stage
        * GameConfigLoader.Instance.GetFloat("goldBaseMultiplier")
        * (1 + Mathf.Max(0, stage - GameConfigLoader.Instance.GetFloat("goldScaleStartStage")) * GameConfigLoader.Instance.GetFloat("goldPostStageScale"));
        DataManager.Instance.IncreaseGold((int)gold);

        DataManager.Instance.IncreaseTicket(GameConfigLoader.Instance.GetInt("clearBaseTickets"));

        battleResult.SetCondition(true, gold);

        if (attackCoroutine != null) StopCoroutine(attackCoroutine);

        attackCoroutine = StartCoroutine(AttackStart());

        yield return new WaitForSeconds(2f);

        enumy.HandleEnumy();
    }
}
