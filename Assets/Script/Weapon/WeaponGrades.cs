using UnityEngine;

// 무기 등급/티어 정적 테이블 (프로토 v1.0.40 REV_GRADES·REV_TIERS).
// 등급 배수는 v37 밸런스 개편의 ×2 균일 계단(E=1 ~ MR=4096, "유니크 무기 = 공격력 2배" 앵커).
// ※ WeaponGrade 시트(07)의 StatMul은 구 v35 계단(1~281)이라 사용하지 않음 — 시트 갱신 시 로더로 전환.
public static class WeaponGrades
{
    public const int Count = 13; // E~MR

    static readonly string[] codes =
        { "E", "D", "C", "B", "A", "S", "SS", "SSS", "SR", "SSR", "UR", "LR", "MR" };

    static readonly string[] colorHexes =
    {
        "#8f9aa8", "#b9c2d4", "#3fd06a", "#4aa3ff", "#b894ff", "#ffb84a", "#ff7ad9",
        "#7cf5ff", "#ffd75e", "#ffa63f", "#ff5252", "#d84bff", "#fffdf2"
    };

    // 티어(외형 서사) — 무기 레벨 1당 1티어, 마지막 티어 초과분은 ★로 표기
    static readonly string[] tierNames =
    {
        "녹슨 리볼버", "무쇠 리볼버", "강철 리볼버", "은장 리볼버", "황금 리볼버", "진홍 리볼버",
        "청염 리볼버", "자전 리볼버", "흑요 리볼버", "프리즘 리볼버", "성운 리볼버", "인피니티 리볼버"
    };

    static int Clamp(int g) => Mathf.Clamp(g, 0, Count - 1);

    public static string Code(int grade) => codes[Clamp(grade)];

    // 주스탯 등급 배수 = 2^grade (×2 균일 계단)
    public static double Mul(int grade) => System.Math.Pow(2, Clamp(grade));

    public static Color Color(int grade)
        => ColorUtility.TryParseHtmlString(colorHexes[Clamp(grade)], out var c) ? c : UnityEngine.Color.white;

    public static string ColorHex(int grade) => colorHexes[Clamp(grade)];

    // "흑요 리볼버 ★2" — 티어(외형) 이름만
    public static string TierName(WeaponData w)
    {
        if (w == null) return "리볼버 없음";
        int t = Mathf.Max(0, w.level - 1);
        int last = tierNames.Length - 1;
        int star = Mathf.Max(0, t - last);
        return tierNames[Mathf.Min(t, last)] + (star > 0 ? $" ★{star}" : "");
    }

    // "[SS] 흑요 리볼버 ★2" 형태 표시명
    public static string DisplayName(WeaponData w)
        => w == null ? "리볼버 없음" : $"[{Code(w.grade)}] {TierName(w)}";

    // 주스탯 + 부옵 목록 텍스트 (패널·팝업 공용)
    public static string InfoText(WeaponData w)
    {
        if (w == null) return "";

        var sb = new System.Text.StringBuilder();
        sb.Append($"⚔️ 공격력 {NumberFormatLoader.Abbrev(w.atk)}");

        var loader = WeaponSubStatLoader.Instance;
        foreach (var s in w.subs)
        {
            var d = loader != null ? loader.Get(s.sid) : null;
            sb.Append('\n');
            sb.Append(d != null ? $"{d.icon} {d.nameKo} +{s.value}%" : $"{s.sid} +{s.value}%");
        }
        return sb.ToString();
    }
}
