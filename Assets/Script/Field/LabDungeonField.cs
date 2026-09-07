// 연구소 던전 필드(정예 열쇠 소모) — Dungeon 시트 Id 'res'(⏳ 모래시계 던전). 동작은 ClimbDungeonField 공통.
public class LabDungeonField : ClimbDungeonField
{
    protected override string DungeonId => "res";
    protected override string FallbackName => "연구소 던전";
}
