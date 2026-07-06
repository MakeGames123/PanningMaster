using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 전투 매커니즘(발사/장전)만 담당한다.
// 승패 처리(보상/결과창/리스폰/던전 복귀)는 NormalField / DungeonField 가 이벤트로 받아 처리한다.
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [SerializeField] RevolverSlots revolver;
    [SerializeField] Enumy enumy;
    [SerializeField] GameObject bulletLine;
    [SerializeField] ReloadingUI reloadingUI;
    [SerializeField] RevolverAnim revolverAnim;

    GameObject bulletLineCpy;
    DamageCalculator calculator = new();

    // 리볼버 한 바퀴(사이클)가 끝났을 때 호출. bool = 적을 죽였는지(성공 여부)
    // FieldManager만 = 로 할당하므로 항상 핸들러는 하나만 존재한다.
    public System.Action<bool> onCycleComplete;

    public Enumy Enemy => enumy;

    void Awake()
    {
        Instance = this;
    }

    // 한 사이클 발사 시작
    public void StartCycle()
    {
        StopAllCoroutines();
        StartCoroutine(CycleRoutine());
    }

    // 장전 후 다음 사이클 시작 (필드가 승패 처리 후 호출)
    public void ReloadAndStartCycle(bool showReloadUI = true)
    {
        StopAllCoroutines();
        StartCoroutine(ReloadThenCycle(showReloadUI));
    }

    public void StopBattle()
    {
        StopAllCoroutines();
    }

    // 리볼버 초기화: 진행중 장전 취소 + 실린더를 1번 약실(home)로 정렬
    public void ResetRevolver()
    {
        StopAllCoroutines(); //진행중 발사/장전 코루틴 정지
        if (revolverAnim != null) revolverAnim.ResetToHome(); //약실 1번으로, 회전 취소
        if (reloadingUI != null) reloadingUI.Hide();          //장전 UI 취소
    }

    IEnumerator ReloadThenCycle(bool showUI)
    {
        float reloadTime = PlayerData.Instance.ReloadTime; //ReloadSpeed(%) 반영된 실제 장전 시간

        if (revolverAnim != null) revolverAnim.PlayReload(reloadTime); //장전시간 동안 반대방향 3바퀴

        if (showUI && reloadingUI != null) yield return reloadingUI.Reload(reloadTime);
        else yield return new WaitForSeconds(reloadTime);

        yield return CycleRoutine();
    }

    // 리볼버 한 바퀴: 장착된 약실을 순서대로 발사. 적을 죽이면 성공, 다 쏘면 실패.
    IEnumerator CycleRoutine()
    {
        // 탄환이 하나도 없으면 장착될 때까지 대기(장전 안 함)
        while (revolver.CheckEmpty())
            yield return new WaitForSeconds(PlayerData.Instance.AttackSpeed);

        for (int i = 0; i < 6; i++)
        {
            if (revolver.revolverSlotContents[i].IsEmpty) continue; //빈 약실 스킵

            if (FireChamber(i))
            {
                onCycleComplete?.Invoke(true); //적 처치 = 성공
                yield break;
            }

            yield return new WaitForSeconds(PlayerData.Instance.AttackSpeed); //ShootSpeed(%) 반영
        }

        onCycleComplete?.Invoke(false); //탄환을 전부 사용했지만 못 죽임 = 실패
    }

    // 약실 1칸 발사 -> 적이 죽었는지 반환
    bool FireChamber(int index)
    {
        bulletLineCpy = Instantiate(bulletLine, transform.parent);
        bulletLineCpy.GetComponent<BulletLine>().AdjustLine(transform.localPosition, enumy.transform.localPosition + new Vector3(0, Random.Range(-40, 40)));

        if (revolverAnim != null) revolverAnim.Fire(); //발사 1발 -> 실린더 시계방향 60도

        List<BulletInfo> revolverInfo = new();
        foreach (RevolverSlotContent content in revolver.revolverSlotContents)
        {
            revolverInfo.Add(AllBulletList.Instance.GetBullet(content.id));
        }

        DamageModifier mod = calculator.CollectModifiers(revolverInfo);
        float damage = calculator.CalculateDamage(revolverInfo[index], mod, index, PlayerData.Instance.PossPower).Item2;

        return enumy.Attacked(damage);
    }
}
