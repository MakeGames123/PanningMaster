using UnityEngine;

// 전투 필드 관리자. 어떤 필드가 활성인지 관리하고, Player.onCycleComplete 핸들러를
// '=' 대입으로만 바꿔 항상 정확히 하나의 함수만 연결되도록 보장한다.
public class FieldManager : MonoBehaviour
{
    public static FieldManager Instance { get; private set; }

    [SerializeField] Player player;
    [SerializeField] NormalField normalField;
    [SerializeField] DungeonField dungeonField;
    [SerializeField] LabDungeonField labDungeonField;

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

    // 던전 입장 (DungeonEntry에서 호출)
    public void EnterDungeon(int floor)
    {
        if (inDungeon) return;
        inDungeon = true;

        normalField.Stop();
        player.ResetRevolver(); //장전 취소 + 약실 1번으로 초기화

        player.onCycleComplete = dungeonField.OnCycleComplete; //유일 핸들러 교체
        dungeonField.Begin(player, floor);
    }

    // 연구소 던전 입장 (DungeonEntry에서 호출). 정예 열쇠 소모, 다중 표적 전투.
    public void EnterLabDungeon()
    {
        if (inDungeon || labDungeonField == null) return;
        inDungeon = true;

        normalField.Stop();
        player.ResetRevolver(); //장전 취소 + 약실 1번으로 초기화
        player.Enemy.gameObject.SetActive(false); //기존 적은 던전 동안 잠시 숨김

        player.onCycleComplete = labDungeonField.OnCycleComplete; //유일 핸들러 교체
        player.SetTargetProvider(labDungeonField.GetCurrentTarget); //다중 표적 모드 켜기
        labDungeonField.Begin(player);
    }

    // 던전 종료 -> 일반 전투 복귀 (DungeonField/LabDungeonField에서 호출)
    public void ReturnToNormal()
    {
        if (!inDungeon) return;
        inDungeon = false;

        dungeonField.Stop();
        if (labDungeonField != null) labDungeonField.Stop();
        player.SetTargetProvider(null); //다중 표적 모드 끄기
        player.ResetRevolver(); //던전 종료 시에도 장전 취소 + 약실 1번으로 초기화
        player.Enemy.gameObject.SetActive(true); //숨겨둔 기존 적 복구

        ActivateNormal();
    }
}
