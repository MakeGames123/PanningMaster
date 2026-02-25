using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    [SerializeField] RevolverSlots revolver;
    [SerializeField] Enumy enumy;
    [SerializeField] GameObject bulletLine;
    GameObject bulletLineCpy;
    DamageCalculator calculator = new();
    int bulletIndex = 0;

    void Start()
    {
        StartCoroutine(PlayerAttack());
    }
    private IEnumerator AttackStart()
    {
        yield return new WaitForSeconds(1f);
        StartCoroutine(PlayerAttack());
    }
    private IEnumerator PlayerAttack()
    {
        while (true)
        {
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
                Lose();
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
            Win();
            return true;
        }
        else
        {
            bulletIndex++;
            if (bulletIndex == 6)
            {
                Lose();
                return true;
            }
        }

        return false;
    }

    private void Lose()
    {
        bulletIndex = 0;
        enumy.ResetEnumy();
        DataManager.Instance.IncreaseGold((int)DataManager.Instance.Power / 4);
        StartCoroutine(AttackStart());
    }
    private void Win()
    {
        bulletIndex = 0;
        DataManager.Instance.IncreaseGold((int)DataManager.Instance.Power / 4);
        StartCoroutine(AttackStart());
    }
}
