// 연구소 노드가 영향을 주는 능력치. 시트 Effect 열 문자열과 이름이 일치해야 파싱된다.
public enum LabEffectType
{
    Damage,
    ShootSpeed,
    ReloadSpeed,
    GoldAcq,
    CriticalChance,
    CriticalDamage,
    FinalDamage,
    TypeDamage
}

// LabNode 시트 한 행 = 노드 1개의 설계 정보.
public class LabNodeData
{
    public int id;
    public string name;
    public long cost;          // 연구 1회 비용(골드)
    public LabEffectType effect;
    public float amount;       // 레벨당 효과량
    public float timeSeconds;  // 연구 소요 시간(초)
    public int maxLevel;

    public string EffectLabel => effect switch
    {
        LabEffectType.Damage => "공격력",
        LabEffectType.ShootSpeed => "공격 속도",
        LabEffectType.ReloadSpeed => "장전 속도",
        LabEffectType.GoldAcq => "골드 획득",
        LabEffectType.CriticalChance => "치명타 확률",
        LabEffectType.CriticalDamage => "치명타 피해",
        LabEffectType.FinalDamage => "최종 데미지",
        LabEffectType.TypeDamage => "속성 데미지",
        _ => "효과"
    };

    // "공격력 +3" 형태
    public string FormatEffect(float value) => $"{EffectLabel} +{value:0.##}";
}
