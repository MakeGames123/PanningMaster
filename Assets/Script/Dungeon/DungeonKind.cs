// 던전 종류. Normal = 층 진행형 던전, Gold = 골드 던전(30초 채굴, 실패 없음), Train = 열차 강탈(30초 화물칸, 보상 🎫),
// Laboratory = 연구소 던전(등반 — 단일 정예 몬스터), Armory = 버려진 무기고(등반 — 연구소와 동일 동작, 보상 🧰)
public enum DungeonKind { Normal, Gold, Laboratory, Armory, Train }

public static class DungeonKindExt
{
    // Dungeon 시트의 Id 매핑(열쇠 캡·이름·보상 조회용). Normal은 유니티 전용이라 시트 행 없음(null)
    public static string SheetId(this DungeonKind kind) => kind switch
    {
        DungeonKind.Gold => "gold",
        DungeonKind.Train => "tk",
        DungeonKind.Laboratory => "res",
        DungeonKind.Armory => "gem",
        _ => null,
    };
}
