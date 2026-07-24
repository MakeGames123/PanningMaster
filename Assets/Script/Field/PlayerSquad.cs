using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 사수(Player) 3명의 발사 사이클을 상위에서 지휘한다(구 Player.CycleRoutine 승격).
// 약실 1칸마다: 1발사→체크→2발사→체크→3발사→체크→공격속도 딜레이→다음 약실. 6칸 다 쏘면 실패 통지(필드가 재장전 호출).
// 승패 처리(보상/결과창/리스폰/던전 복귀)는 NormalField / DungeonField 가 이벤트로 받아 처리한다.
public class PlayerSquad : MonoBehaviour
{
    [SerializeField] Player[] players = new Player[CharacterManager.PartySize]; // 파티 슬롯 0~2 순서
    [SerializeField] RevolverSlots revolver; // 공유 리볼버
    [SerializeField] Enemy enumy;
    [SerializeField] ReloadingUI reloadingUI;
    [SerializeField] RevolverAnim revolverAnim;

    // 리볼버 한 바퀴(사이클)가 끝났을 때 호출. bool = 적을 죽였는지(성공 여부)
    // FieldManager만 = 로 할당하므로 항상 핸들러는 하나만 존재한다.
    public System.Action<bool> onCycleComplete;

    // 다중 표적 모드(연구소 던전 등). 설정되면 기본 enumy 대신 공급받은 표적에 발사한다.
    // 표적을 죽여도 사이클은 끝나지 않고 다음 표적으로 이어지며, 표적이 없으면 그 총알은 스킵된다.
    System.Func<IFireTarget> targetProvider;

    public Enemy Enemy => enumy;

    // 다중 표적 모드 설정/해제(null). FieldManager가 필드 전환 시 호출한다.
    public void SetTargetProvider(System.Func<IFireTarget> provider) => targetProvider = provider;

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

    // 리볼버 한 바퀴: 장착된 약실을 순서대로, 약실마다 활성 사수 전원이 차례로 발사.
    // 적을 죽이면 즉시 성공, 6칸 다 쏘면 실패.
    IEnumerator CycleRoutine()
    {
        // 탄환이 하나도 없거나 활성 사수가 없으면 생길 때까지 대기(장전 안 함)
        while (revolver.CheckEmpty() || !AnyActive())
            yield return new WaitForSeconds(PlayerData.Instance.AttackSpeed);

        for (int i = 0; i < 6; i++)
        {
            if (revolver.revolverSlotContents[i].IsEmpty) continue; //빈 약실 스킵

            if (revolverAnim != null) revolverAnim.Fire(); //실린더 회전은 약실당 1회(사수 수와 무관)

            if (targetProvider != null) //다중 표적 모드(연구소 던전)
            {
                foreach (Player p in ActivePlayers())
                {
                    IFireTarget target = targetProvider();
                    if (target != null) p.FireChamberAt(i, target); //죽여도 사이클 계속 -> 다음 사수는 다음 표적
                    //target == null: 아직 소환된 적이 없음 -> 이 총알은 스킵
                }

                yield return new WaitForSeconds(PlayerData.Instance.AttackSpeed);
                continue;
            }

            //1발사→체크→2발사→체크→3발사→체크
            foreach (Player p in ActivePlayers())
            {
                if (p.FireChamber(i))
                {
                    onCycleComplete?.Invoke(true); //적 처치 = 성공
                    yield break;
                }
            }

            yield return new WaitForSeconds(PlayerData.Instance.AttackSpeed); //ShootSpeed(%) 반영
        }

        onCycleComplete?.Invoke(false); //탄환을 전부 사용했지만 못 죽임 = 실패
    }

    IEnumerable<Player> ActivePlayers()
    {
        foreach (Player p in players)
            if (p != null && p.gameObject.activeInHierarchy)
                yield return p;
    }

    bool AnyActive()
    {
        foreach (Player p in players)
            if (p != null && p.gameObject.activeInHierarchy) return true;
        return false;
    }
}
