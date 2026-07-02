using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    [SerializeField] RevolverSlots revolver;
    [SerializeField] Enumy enumy;
    [SerializeField] GameObject bulletLine;
    [SerializeField] BattleResult battleResult;
    [SerializeField] ReloadingUI reloadingUI;
    [SerializeField] float reloadTime = 2.5f;
    GameObject bulletLineCpy;
    DamageCalculator calculator = new();
    int bulletIndex = 0;
    Coroutine attackCoroutine = null;

    void Start()
    {
        StartCoroutine(PlayerAttack());
    }
    //장전: 남은 장전시간을 ReloadingUI 슬라이더로 표시하고, 끝나면 공격 재개
    //리볼버가 비어있을 때(showUI=false)는 장전 UI를 띄우지 않음
    private IEnumerator Reload(bool showUI = true)
    {
        if (showUI && reloadingUI != null) yield return reloadingUI.Reload(reloadTime);
        else yield return new WaitForSeconds(reloadTime);

        StartCoroutine(PlayerAttack());
    }
    private IEnumerator PlayerAttack()
    {
        while (true)
        {
            if (revolver.CheckEmpty())
            {
                if (attackCoroutine != null) StopCoroutine(attackCoroutine);

                attackCoroutine = StartCoroutine(Reload(false)); //리볼버가 비어있으면 장전 UI 미표시
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
        float damage = calculator.CalculateDamage(revolverInfo[bulletIndex], mod, bulletIndex, DataManager.Instance.PossPower).Item2;
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

        yield return new WaitForSeconds(2f);

        enumy.HandleEnumy();

        //Lose 코루틴이 끝난 뒤 장전
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);

        attackCoroutine = StartCoroutine(Reload());
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

        yield return new WaitForSeconds(2f);

        enumy.HandleEnumy();

        //Win 코루틴이 끝난 뒤 장전
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);

        attackCoroutine = StartCoroutine(Reload());
    }
}
