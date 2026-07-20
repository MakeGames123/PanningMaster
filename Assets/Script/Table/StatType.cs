// 전역 스탯 종류. StatType 시트의 Id, 그리고 GrowthStat/ResearchNode의 Effect 키와 이름이 1:1로 일치한다.
// 연구소·성장·부옵 등 스탯을 다루는 모든 시스템이 공용으로 사용한다.
public enum StatType
{
    Damage,           // 공격력
    ShootSpeed,       // 공격 속도
    ReloadSpeed,      // 장전 속도
    GoldAcq,          // 골드 획득
    CriticalChance,   // 크리티컬 확률
    CriticalDamage,   // 크리티컬 데미지
    FinalDamage,      // 최종 데미지
    TypeDamage,       // 속성 데미지
    TicketCap,        // 티켓 소지 한도 (+N개)
    OfflineGoldRate,  // 방치 채굴량 (+N%)
    OfflineTimeCap    // 방치캡 (+N분)
}
