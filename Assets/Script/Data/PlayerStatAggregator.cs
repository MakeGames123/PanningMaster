using System.Collections.Generic;

// 여러 소스(성장·연구소·향후 장비 등)가 각자의 스탯 기여분을 등록하면
// 그 합을 PlayerData의 전역 스탯 필드에 반영한다.
// 각 소스는 자기 몫을 SetContribution(소스이름, StatSet)으로 통째 교체 → 소스끼리 서로 덮어쓰지 않는다.
public static class PlayerStatAggregator
{
    static readonly Dictionary<string, StatSet> sources = new();

    // source 예: "growth", "lab". 같은 이름으로 다시 부르면 그 소스 기여분만 갱신된다.
    public static void SetContribution(string source, StatSet set)
    {
        sources[source] = set;
        Apply();
    }

    static void Apply()
    {
        var p = PlayerData.Instance;
        if (p == null) return;

        StatSet sum = default;
        foreach (var s in sources.Values) sum.Add(s);

        p.Damage = sum.Damage;
        p.TypeDamage = sum.TypeDamage;
        p.CriticalChance = sum.CriticalChance;
        p.CriticalDamage = sum.CriticalDamage;
        p.FinalDamage = sum.FinalDamage;
        p.ReloadSpeed = sum.ReloadSpeed;
        p.ShootSpeed = sum.ShootSpeed;
        p.GoldAcq = sum.GoldAcq;

        // 스탯 변화 → 전투력 재계산 → onPowerChanged 변화량이 PowerFloatingText로 연출됨
        var revolver = UnityEngine.Object.FindAnyObjectByType<RevolverSlots>();
        if (revolver != null) revolver.CheckSlots();
    }
}

// PlayerData의 전역 스탯 8종 묶음(합산 단위)
public struct StatSet
{
    public float Damage;          // 공격력(atk)
    public float TypeDamage;      // 속성 데미지(elem)
    public float CriticalChance;  // 크리티컬 확률(critR)
    public float CriticalDamage;  // 크리티컬 데미지(critD)
    public float FinalDamage;     // 최종/전체 데미지(gdmg)
    public float ReloadSpeed;     // 장전 속도(reload)
    public float ShootSpeed;      // 공격 속도
    public float GoldAcq;         // 골드 획득

    public void Add(StatSet o)
    {
        Damage += o.Damage;
        TypeDamage += o.TypeDamage;
        CriticalChance += o.CriticalChance;
        CriticalDamage += o.CriticalDamage;
        FinalDamage += o.FinalDamage;
        ReloadSpeed += o.ReloadSpeed;
        ShootSpeed += o.ShootSpeed;
        GoldAcq += o.GoldAcq;
    }

    // StatType 기준으로 해당 필드에 값 누적. 성장·연구소 공용 분류점.
    public void AddEffect(StatType type, float value)
    {
        switch (type)
        {
            case StatType.Damage: Damage += value; break;
            case StatType.TypeDamage: TypeDamage += value; break;
            case StatType.CriticalChance: CriticalChance += value; break;
            case StatType.CriticalDamage: CriticalDamage += value; break;
            case StatType.FinalDamage: FinalDamage += value; break;
            case StatType.ReloadSpeed: ReloadSpeed += value; break;
            case StatType.ShootSpeed: ShootSpeed += value; break;
            case StatType.GoldAcq: GoldAcq += value; break;
            // TicketCap/OfflineGoldRate/OfflineTimeCap 은 전투 스탯 8종이 아니라 각 시스템이 별도 처리 — 여기선 무시
            default: break;
        }
    }
}
