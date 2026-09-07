using UnityEngine;

// 전투 필드 관리자. 어떤 필드가 활성인지 관리하고, Player.onCycleComplete 핸들러를
// '=' 대입으로만 바꿔 항상 정확히 하나의 함수만 연결되도록 보장한다.
public class FieldManager : MonoBehaviour
{
    public static FieldManager Instance { get; private set; }

    [SerializeField] PlayerSquad player;
    [SerializeField] NormalField normalField;
    [SerializeField] DungeonField dungeonField;
    [SerializeField] GoldDungeonField goldDungeonField;
    [SerializeField] TrainDungeonField trainDungeonField; // 열차 강탈(v39 — 골드 던전 문법, 보상 🎫)
    [SerializeField] LabDungeonField labDungeonField;
    [SerializeField] ArmoryDungeonField armoryDungeonField; // 버려진 무기고(등반 — 연구소와 동일 동작)
    [SerializeField] WorldBossField worldBossField; // v45 월드보스(주간 펀치킹)

    bool inDungeon;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ActivateNormal(); //일반 전투로 시작
    }

    void ActivateNormal()
    {
        player.onCycleComplete = normalField.OnCycleComplete; //유일 핸들러 할당
        normalField.Begin(player);
    }

    // 던전 입장 (DungeonDetailPopup에서 호출)
    public void EnterDungeon(int floor)
    {
        if (inDungeon) return;
        inDungeon = true;

        normalField.Stop();
        player.ResetRevolver(); //장전 취소 + 약실 1번으로 초기화

        player.onCycleComplete = dungeonField.OnCycleComplete; //유일 핸들러 교체
        dungeonField.Begin(player, floor);
    }

    // 골드 던전 입장 (DungeonDetailPopup에서 호출). 층 없음 · 실패 없음 · 죽음/장전 없음 · 전용 적.
    public void EnterGoldDungeon()
    {
        if (inDungeon || goldDungeonField == null) return;
        inDungeon = true;

        normalField.Stop();
        player.ResetRevolver(); //장전 취소 + 약실 1번으로 초기화
        player.Enemy.gameObject.SetActive(false); //기본 적은 던전 동안 숨김

        player.onCycleComplete = goldDungeonField.OnCycleComplete; //유일 핸들러 교체
        player.SetTargetProvider(goldDungeonField.GetTarget); //골드 전용 적 조준(죽음으로 사이클 안 끊김)
        goldDungeonField.Begin(player);
    }

    // 열차 강탈 입장 (DungeonDetailPopup에서 호출). 골드 던전 문법 — 30초 · 실패 없음 · 화물칸 카운트, 보상 🎫.
    public void EnterTrainDungeon()
    {
        if (inDungeon || trainDungeonField == null) return;
        inDungeon = true;

        normalField.Stop();
        player.ResetRevolver(); //장전 취소 + 약실 1번으로 초기화
        player.Enemy.gameObject.SetActive(false); //기본 적은 던전 동안 숨김

        player.onCycleComplete = trainDungeonField.OnCycleComplete; //유일 핸들러 교체
        player.SetTargetProvider(trainDungeonField.GetTarget); //화물칸 조준(죽음으로 사이클 안 끊김)
        trainDungeonField.Begin(player);
    }

    // 연구소 던전 입장 (DungeonDetailPopup에서 호출). 정예 열쇠 소모, 일반 몬스터 재활용(단계 등반) 전투.
    public void EnterLabDungeon(int stage) => EnterClimbDungeon(labDungeonField, stage);

    // 버려진 무기고 입장 (DungeonDetailPopup에서 호출). 연구소와 동일 동작, 보상만 다름(🧰 무기 상자).
    public void EnterArmoryDungeon(int stage) => EnterClimbDungeon(armoryDungeonField, stage);

    void EnterClimbDungeon(ClimbDungeonField field, int stage)
    {
        if (inDungeon || field == null) return;
        inDungeon = true;

        normalField.Stop();
        player.ResetRevolver(); //장전 취소 + 약실 1번으로 초기화

        player.onCycleComplete = field.OnCycleComplete; //유일 핸들러 교체
        field.Begin(player, stage);
    }

    // 월드보스 입장 (WorldBossManager.TryBeginChallenge에서 호출) — 골드 던전 문법(불사신·다중 표적)
    public bool EnterWorldBoss()
    {
        if (inDungeon || worldBossField == null) return false;
        inDungeon = true;

        normalField.Stop();
        player.ResetRevolver(); //장전 취소 + 약실 1번으로 초기화
        player.Enemy.gameObject.SetActive(false); //기본 적은 전투 동안 숨김

        player.onCycleComplete = worldBossField.OnCycleComplete; //유일 핸들러 교체
        player.SetTargetProvider(worldBossField.GetTarget); //불사신 보스 조준(죽음으로 사이클 안 끊김)
        worldBossField.Begin(player);
        return true;
    }

    // 던전 종료 -> 일반 전투 복귀 (DungeonField/LabDungeonField에서 호출)
    public void ReturnToNormal()
    {
        if (!inDungeon) return;
        inDungeon = false;

        dungeonField.Stop();
        if (goldDungeonField != null) goldDungeonField.Stop();
        if (trainDungeonField != null) trainDungeonField.Stop();
        if (labDungeonField != null) labDungeonField.Stop();
        if (armoryDungeonField != null) armoryDungeonField.Stop();
        if (worldBossField != null) worldBossField.Stop();
        player.SetTargetProvider(null); //전용 표적 해제
        player.ResetRevolver(); //던전 종료 시에도 장전 취소 + 약실 1번으로 초기화
        player.Enemy.gameObject.SetActive(true); //숨겨둔 기존 적 복구

        ActivateNormal();
    }
}
