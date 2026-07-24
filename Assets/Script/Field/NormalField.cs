using System.Collections;
using UnityEngine;

// 일반 전투 필드. Player의 사이클 결과(승/패)를 받아 보상/결과창/적 리스폰을 처리하고
// 장전 후 다음 사이클로 계속 이어간다. 활성/전환은 FieldManager가 관리한다.
public class NormalField : MonoBehaviour
{
    [SerializeField] BattleResult battleResult;

    PlayerSquad player;
    Enemy enumy;

    // FieldManager가 이 필드를 활성화할 때 호출
    public void Begin(PlayerSquad p)
    {
        player = p;
        enumy = p.Enemy;
        player.StartCycle();
    }

    // FieldManager가 이 필드를 비활성화할 때 호출
    public void Stop()
    {
        StopAllCoroutines(); //처리 중이던 승패 코루틴 정지
    }

    // Player.onCycleComplete 에 FieldManager가 할당하는 유일한 핸들러
    public void OnCycleComplete(bool win)
    {
        StartCoroutine(Handle(win));
    }

    IEnumerator Handle(bool win)
    {
        yield return new WaitForSeconds(0.5f);

        int stage = DataManager.Instance.stage;

        var cfg = GameConfigLoader.Instance;
        float gold = stage
            * cfg.GetFloat("ClearGoldPerStage")
            * (1 + Mathf.Max(0, stage - cfg.GetFloat("ClearGoldRampStage")) * cfg.GetFloat("ClearGoldRampPerStage"))
            * Mathf.Pow(cfg.GetFloat("ClearGoldExpoRate"), Mathf.Max(0, stage - cfg.GetFloat("ClearGoldExpoStage")));
        if (!win) gold *= cfg.GetFloat("FailGoldRatio");

        //골드 획득량 증가(연구소 GoldAcq + 무기 현상금 wgold — PlayerStatAggregator 합산, 값 1 = 1%)
        if (PlayerData.Instance != null) gold *= 1f + PlayerData.Instance.GoldAcq / 100f;

        DataManager.Instance.IncreaseGold((int)gold);
        DataManager.Instance.IncreaseTicket(cfg.GetInt(win ? "ClearTicket" : "FailTicket"));

        battleResult.SetCondition(win, gold);

        yield return new WaitForSeconds(2f);

        enumy.HandleEnumy(); //승리 시 업그레이드(다음 스테이지), 패배 시 리셋

        player.ReloadAndStartCycle(); //장전 후 다음 사이클
    }
}
