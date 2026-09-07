// 버려진 무기고 던전 필드 — Dungeon 시트 Id 'gem'(🧰 무기 상자 던전. 내부 id는 세이브 호환용 프로토 명명). 동작은 ClimbDungeonField 공통.
public class ArmoryDungeonField : ClimbDungeonField
{
    protected override string DungeonId => "gem";
    protected override string FallbackName => "버려진 무기고";
}
