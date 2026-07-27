using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 사수(Player) 3명의 발사 사이클을 상위에서 지휘한다. 리볼버는 사수별 전용(공유 안 함).
// 약실 1칸마다: 그 칸에 탄환이 있는 활성 사수들이 차례로 발사(1발사→체크→2→체크→3→체크)→공격속도 딜레이→다음 약실.
// 6칸 다 돌면 실패 통지(필드가 재장전 호출). 승패 처리(보상/결과창/리스폰/던전 복귀)는 필드들이 이벤트로 받아 처리한다.
public class PlayerSquad : MonoBehaviour
{
    [SerializeField] Player[] players = new Player[CharacterManager.PartySize]; // 파티 슬롯 0~2 순서
    [SerializeField] Enemy enumy;
    [SerializeField] ReloadingUI reloadingUI;

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

    // 리볼버 초기화: 진행중 장전 취소 + 모든 사수의 실린더를 1번 약실(home)로 정렬
    public void ResetRevolver()
    {
        StopAllCoroutines(); //진행중 발사/장전 코루틴 정지
        foreach (Player p in players)
            if (p != null) p.ResetToHome();
        if (reloadingUI != null) reloadingUI.Hide(); //장전 UI 취소
    }

    IEnumerator ReloadThenCycle(bool showUI)
    {
        float reloadTime = PlayerData.Instance.ReloadTime; //ReloadSpeed(%) 반영된 실제 장전 시간

        foreach (Player p in ActivePlayers())
            p.PlayReload(reloadTime); //각자 전용 실린더 장전 연출

        if (showUI && reloadingUI != null) yield return reloadingUI.Reload(reloadTime);
        else yield return new WaitForSeconds(reloadTime);

        yield return CycleRoutine();
    }

    // 리볼버 한 바퀴: 약실 0~5를 순서대로, 그 칸에 탄환이 있는 활성 사수 전원이 차례로 발사.
    // 적을 죽이면 즉시 성공, 6칸 다 돌면 실패.
    IEnumerator CycleRoutine()
    {
        // 활성 사수 전원의 리볼버가 비어 있으면 탄환이 생길 때까지 대기(장전 안 함)
        while (!AnyBullets())
            yield return new WaitForSeconds(PlayerData.Instance.AttackSpeed);

        for (int i = 0; i < 6; i++)
        {
            //실린더는 발사 여부와 무관하게 약실 차례마다 1칸 전진(빈 약실 포함 — 한 바퀴 = 정확히 360°)
            foreach (Player p in ActivePlayers())
                p.StepCylinder();

            if (targetProvider != null) //다중 표적 모드(연구소 던전)
            {
                foreach (Player p in ActivePlayers())
                {
                    if (!p.HasBullet(i)) continue; //이 사수의 약실이 비었으면 스킵

                    IFireTarget target = targetProvider();
                    if (target != null) p.FireChamberAt(i, target); //죽여도 사이클 계속 -> 다음 사수는 다음 표적
                    //target == null: 아직 소환된 적이 없음 -> 이 총알은 스킵
                }
            }
            else
            {
                //1발사→체크→2발사→체크→3발사→체크
                foreach (Player p in ActivePlayers())
                {
                    if (!p.HasBullet(i)) continue; //이 사수의 약실이 비었으면 스킵

                    bool killed = p.FireChamber(i);
                    if (killed)
                    {
                        onCycleComplete?.Invoke(true); //적 처치 = 성공
                        yield break;
                    }
                }
            }

            //빈 약실도 공격속도만큼 대기 — 약실당 템포 고정(또각또각)
            yield return new WaitForSeconds(PlayerData.Instance.AttackSpeed); //ShootSpeed(%) 반영

            //남은 약실에 탄환이 하나도 없으면(전 리볼버) 빈 칸 돌지 않고 조기 종료
            if (!AnyBulletAfter(i)) break;
        }

        Debug.Log("[사이클] 전 약실 소진 — 실패 종료(장전 대기)"); //TODO: 약실 애니 검증 끝나면 삭제
        onCycleComplete?.Invoke(false); //탄환을 전부 사용했지만 못 죽임 = 실패
    }

    IEnumerable<Player> ActivePlayers()
    {
        foreach (Player p in players)
            if (p != null && p.gameObject.activeInHierarchy)
                yield return p;
    }

    bool AnyBullets()
    {
        foreach (Player p in ActivePlayers())
            if (p.HasAnyBullet()) return true;
        return false;
    }

    //index 이후 약실(index+1~5)에 탄환이 있는 활성 사수가 하나라도 있는가
    bool AnyBulletAfter(int index)
    {
        foreach (Player p in ActivePlayers())
            for (int i = index + 1; i < 6; i++)
                if (p.HasBullet(i)) return true;
        return false;
    }
}
