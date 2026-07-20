// LabNode 시트 한 행 = 노드 1개의 설계 정보.
public class LabNodeData
{
    public int id;
    public string name;
    public long cost;          // 연구 1회 비용(골드) — 신규 시트에 Cost 컬럼이 없어 항상 0(시간만 소모)
    public StatType effect;    // 이 노드가 올려주는 스탯(전역 StatType)
    public float amount;       // 레벨당 효과량
    public float timeSeconds;  // 연구 소요 시간(초)
    public int maxLevel;

    // StatType 시트(NameKo)가 로드됐으면 시트 라벨, 아니면 하드코딩 폴백
    public string EffectLabel
    {
        get
        {
            var loader = StatTypeLoader.Instance;
            if (loader != null && loader.IsLoaded)
                return loader.GetLabel(effect.ToString(), FallbackLabel);

            return FallbackLabel;
        }
    }

    string FallbackLabel => effect switch
    {
        StatType.Damage => "공격력",
        StatType.ShootSpeed => "공격 속도",
        StatType.ReloadSpeed => "장전 속도",
        StatType.GoldAcq => "골드 획득",
        StatType.CriticalChance => "치명타 확률",
        StatType.CriticalDamage => "치명타 피해",
        StatType.FinalDamage => "최종 데미지",
        StatType.TypeDamage => "속성 데미지",
        StatType.TicketCap => "티켓 소지 한도",
        StatType.OfflineGoldRate => "방치 채굴량",
        StatType.OfflineTimeCap => "방치캡",
        _ => "효과"
    };

    // StatType 시트의 단위 문자열(% · 개 · 분). 시트 미로드 시 빈 문자열
    public string Unit
    {
        get
        {
            var loader = StatTypeLoader.Instance;
            var data = (loader != null && loader.IsLoaded) ? loader.Get(effect) : null;
            return data != null ? data.unit : "";
        }
    }

    // "+3%" 형태 — 증가량 숫자 + 시트 단위
    public string FormatValue(float value) => $"+{value:0.##}{Unit}";

    // "공격력 +3%" 형태 — 시트 라벨 + 증가량
    public string FormatEffect(float value) => $"{EffectLabel} {FormatValue(value)}";
}
