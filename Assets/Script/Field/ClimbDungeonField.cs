using System.Collections;
using UnityEngine;

// 등반형 던전 필드 공통(연구소 던전·버려진 무기고). 전용 몬스터 없이 일반 전투의 기본 몬스터(Enemy)를 재활용한다.
// 프로토 dgResolve 규칙: 도전 단계 = 최고 기록+1, 한 탄창(사이클 한 바퀴) 안에 처치하면 클리어, 못 죽이면 실패.
// 몬스터 체력 곡선·클리어 보상은 Dungeon/DungeonConfig 시트에서 읽는다(미로드 시 프로토 v1.0.47 기본값).
public abstract class ClimbDungeonField : MonoBehaviour
{
    [SerializeField] BattleResult battleResult;

    protected abstract string DungeonId { get; }    // Dungeon 시트의 Id(res/gem)
    protected abstract string FallbackName { get; } // 시트 미로드 시 표시명

    PlayerSquad player;
    Enemy enumy;
    float savedMaxHp;
    bool resolved;
    int stage; //이번 판 도전 단계(최고 기록+1)

    string DungeonName
    {
        get
        {
            DungeonData d = DungeonLoader.Instance != null ? DungeonLoader.Instance.Get(DungeonId) : null;
            return (d != null && !string.IsNullOrEmpty(d.nameKo)) ? d.nameKo : FallbackName;
        }
    }

    // FieldManager가 던전을 활성화할 때 호출. 도전 단계는 팝업에서 선택(1 ~ 최고 기록+1로 제한)
    public void Begin(PlayerSquad p, int challengeStage)
    {
        player = p;
        enumy = p.Enemy;
        resolved = false;
        stage = Mathf.Clamp(challengeStage, 1, DataManager.Instance.GetClimbBest(DungeonId) + 1);

        savedMaxHp = enumy.MaxHp;
        enumy.SetMaxHp(ClimbHp(stage)); //단계 기반 절대 곡선 + 풀피

        player.StartCycle();
    }

    public void Stop()
    {
        StopAllCoroutines();
    }

    // 프로토 dgHP(s) = floor(몬스터HP(Base + PerStage×s) × Mul) — 본편 스테이지 비종속 절대 곡선.
    // 상세 팝업(권장 전투력 표시)도 쓰므로 static 공개.
    public static float ClimbHpFor(int s)
    {
        DungeonConfigLoader cfg = DungeonConfigLoader.Instance;
        int hpBase = cfg != null ? cfg.GetInt("ClimbHpBase", 6) : 6;
        int hpPer = cfg != null ? cfg.GetInt("ClimbHpPerStage", 2) : 2;
        float mul = cfg != null ? cfg.GetFloat("ClimbHpMul", 1.35f) : 1.35f;

        return Mathf.Floor(MonsterHpTableLoader.Instance.GetHP(hpBase + hpPer * s) * mul);
    }

    float ClimbHp(int s) => ClimbHpFor(s);

    // Player.onCycleComplete 에 FieldManager가 할당하는 유일한 핸들러.
    // 한 탄창 승부: 처치 = 클리어, 전 약실 소진 = 실패.
    public void OnCycleComplete(bool killed)
    {
        if (resolved) return;
        Resolve(killed);
    }

    void Resolve(bool success)
    {
        if (resolved) return;
        resolved = true;

        player.StopBattle();

        StartCoroutine(End(success));
    }

    IEnumerator End(bool success)
    {
        yield return new WaitForSeconds(0.5f);

        if (success)
        {
            DataManager.Instance.SetClimbBest(DungeonId, stage); //최고 기록 갱신
            GiveRewards();
            if (QuestEventManager.Instance != null) QuestEventManager.Instance.AddEvent("dgClear"); //업적: 던전 클리어
            battleResult.ShowResult($"{DungeonName} {stage}단계 클리어");
        }
        else
        {
            battleResult.ShowResult($"{DungeonName} {stage}단계 실패");
        }

        yield return new WaitForSeconds(2f);

        enumy.SetMaxHp(savedMaxHp);             //체력 원상 복구
        FieldManager.Instance.ReturnToNormal(); //일반 전투 복귀
    }

    // Dungeon 시트의 RewardN — 지급량 = floor(Base + PerStage×단계)
    void GiveRewards()
    {
        DungeonData d = DungeonLoader.Instance != null ? DungeonLoader.Instance.Get(DungeonId) : null;
        if (d == null) return;

        Give(d.reward1Type, Mathf.FloorToInt(d.reward1Base + d.reward1PerStage * stage));
        Give(d.reward2Type, Mathf.FloorToInt(d.reward2Base + d.reward2PerStage * stage));
    }

    void Give(string type, int amount)
    {
        if (string.IsNullOrEmpty(type) || amount <= 0) return;

        DataManager dm = DataManager.Instance;
        switch (type)
        {
            case "Crate": dm.IncreaseCrate(amount); break;
            case "Hourglass": dm.IncreaseHourglass(amount); break;
            case "Ticket": dm.IncreaseTicket(amount); break;
            case "Gold": dm.IncreaseGold(amount); break;
            case "Gem": dm.IncreaseGem(amount); break;
            default: Debug.LogWarning($"[{DungeonName}] 알 수 없는 보상 타입: {type}"); break;
        }
    }
}
