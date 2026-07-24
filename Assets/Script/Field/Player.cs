using UnityEngine;
using System.Collections.Generic;

// 발사(조준·탄줄·데미지 계산)만 담당하는 사수 1명. 파티 슬롯당 1개, 총 3개(활성화 = PartyPlayerActivator).
// 사이클(발사 순서/장전/승패 통지)은 PlayerSquad가 상위에서 돌린다. 리볼버는 셋이 공유.
public class Player : MonoBehaviour
{
    [SerializeField] RevolverSlots revolver;
    [SerializeField] Enemy enumy;
    [SerializeField] GameObject bulletLine;

    GameObject bulletLineCpy;
    DamageCalculator calculator = new();

    public Enemy Enemy => enumy;

    // 약실 1칸 발사 -> 적이 죽었는지 반환
    public bool FireChamber(int index)
    {
        return FireAt(index, enumy.transform.localPosition, enumy.Attacked);
    }

    // 다중 표적 모드 발사: 표적의 월드 좌표를 필드(부모) 로컬 좌표로 변환해 조준
    public bool FireChamberAt(int index, IFireTarget target)
    {
        Vector3 targetLocal = transform.parent.InverseTransformPoint(target.AimPosition);
        return FireAt(index, targetLocal, target.Attacked);
    }

    bool FireAt(int index, Vector3 targetLocalPos, System.Func<float, bool> attacked)
    {
        bulletLineCpy = Instantiate(bulletLine, transform.parent);
        bulletLineCpy.GetComponent<BulletLine>().AdjustLine(transform.localPosition, targetLocalPos + new Vector3(0, Random.Range(-40, 40)));

        List<BulletInfo> revolverInfo = new();
        foreach (RevolverSlotContent content in revolver.revolverSlotContents)
        {
            revolverInfo.Add(AllBulletList.Instance.GetBullet(content.id));
        }

        DamageModifier mod = calculator.CollectModifiers(revolverInfo);
        float damage = calculator.CalculateDamage(revolverInfo[index], mod, index, PlayerData.Instance.PossPower).Item2;

        return attacked(damage);
    }
}
