using UnityEngine;
using System.Collections.Generic;

// 발사(조준·탄줄·데미지 계산)만 담당하는 사수 1명. 파티 슬롯당 1개, 총 3개(활성화 = PartyPlayerActivator).
// 사수마다 전용 리볼버·실린더 연출을 소유(공유 안 함) — 사이클(발사 순서/장전/승패 통지)은 PlayerSquad가 상위에서 돌린다.
public class Player : MonoBehaviour
{
    [SerializeField] RevolverSlots revolver;    // 이 사수 전용 리볼버
    [SerializeField] RevolverAnim revolverAnim; // 전용 실린더 연출(선택 연결)
    [SerializeField] Enemy enumy;
    [SerializeField] GameObject bulletLine;

    GameObject bulletLineCpy;
    DamageCalculator calculator = new();

    // 장착 캐릭터의 등급 위력 배수(CharacterGrade.PowerMul) — 이 사수의 탄환 데미지에만 곱한다.
    // PartyPlayerActivator가 장착 변경 시 주입(빈 슬롯 = 1).
    public float PowerMul { get; set; } = 1f;

    public Enemy Enemy => enumy;
    public RevolverSlots Revolver => revolver; // 배선 검증용(PlayerSquad가 중복 연결 감지)

    public bool HasAnyBullet() => !revolver.CheckEmpty();

    public bool HasBullet(int index)
        => index < revolver.revolverSlotContents.Count && !revolver.revolverSlotContents[index].IsEmpty;

    // 실린더 1칸 전진 — 발사 여부와 무관하게 스쿼드가 약실 차례마다 호출(빈 약실도 전진해야 표시가 안 어긋남)
    public void StepCylinder()
    {
        if (revolverAnim != null) revolverAnim.Fire();
    }

    // 장전 연출(전용 실린더 반대방향 3바퀴)
    public void PlayReload(float reloadTime)
    {
        if (revolverAnim != null) revolverAnim.PlayReload(reloadTime);
    }

    // 실린더를 1번 약실(home)로 정렬
    public void ResetToHome()
    {
        if (revolverAnim != null) revolverAnim.ResetToHome();
    }

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

        return attacked(damage * PowerMul); //캐릭터 등급 위력 배수(사수 개인 적용)
    }
}
