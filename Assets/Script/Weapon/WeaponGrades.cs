using UnityEngine;

// 무기 등급/티어 정적 테이블 (프로토 v1.0.45 REV_GRADES·REV_TIERS·REV_EL_FD).
// 등급 배수는 v37 밸런스 개편의 ×2 균일 계단(E=1 ~ MR=4096, "유니크 무기 = 공격력 2배" 앵커).
// WeaponGrade 시트가 로드되면 StatMul·ElemFinalDmg는 시트 우선, 미로드 시 하드코딩 폴백.
// ※ 구글 시트 WeaponGrade 탭이 구 v35 계단(1~281)인 채로 로드되면 그대로 반영되니 탭 갱신 필수.
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

    // v43 무기 속성: 등급별 매칭 탄환 최종 데미지 +% (프로토 REV_EL_FD A안 — 시트 미로드 폴백)
    static readonly float[] elemFinalDmgs =
        { 40, 55, 75, 100, 130, 165, 205, 250, 300, 360, 430, 510, 600 };

    static int Clamp(int g) => Mathf.Clamp(g, 0, Count - 1);

    public static string Code(int grade) => codes[Clamp(grade)];

    // 주스탯 등급 배수 — WeaponGrade 시트(StatMul) 우선, 미로드 시 2^grade (×2 균일 계단)
    public static double Mul(int grade)
    {
        var loader = WeaponGradeLoader.Instance;
        if (loader != null && loader.IsLoaded) return loader.GetStatMul(Clamp(grade));
        return System.Math.Pow(2, Clamp(grade));
    }

    // v43 무기 속성: 무기와 같은 속성 탄환의 최종 데미지 +% — 시트(ElemFinalDmg) 우선
    public static float ElemFinalDmg(int grade)
    {
        var loader = WeaponGradeLoader.Instance;
        if (loader != null && loader.IsLoaded && loader.HasElemFinalDmg)
            return loader.GetElemFinalDmg(Clamp(grade));
        return elemFinalDmgs[Clamp(grade)];
    }

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

    // v43b 무기 기본 이름 = 속성×등급 사다리(WeaponElemName 시트). 속성 없거나 시트 미로드 = 구 티어명 폴백
    public static string BaseName(WeaponData w)
    {
        if (w == null) return "리볼버 없음";
        if (!string.IsNullOrEmpty(w.element)
            && WeaponElemNameLoader.Instance != null && WeaponElemNameLoader.Instance.IsLoaded)
        {
            string n = WeaponElemNameLoader.Instance.Get(w.element, Clamp(w.grade));
            if (!string.IsNullOrEmpty(n)) return n;
        }
        return TierName(w);
    }

    // "[MR] 라그나로크" / (속성 없으면) "[SS] 흑요 리볼버 ★2" 형태 표시명
    public static string DisplayName(WeaponData w)
        => w == null ? "리볼버 없음" : $"[{Code(w.grade)}] {BaseName(w)}";

    // 주스탯 + 속성 + 부옵 목록 텍스트 (패널·팝업 공용)
    public static string InfoText(WeaponData w) => InfoText(w, null);

    // cmp = 비교 상대 무기(비교 팝업의 신규 쪽에서 현재 무기를 넘김) — 속성이 다르면 '변경' 뱃지(프로토 v43)
    public static string InfoText(WeaponData w, WeaponData cmp)
    {
        if (w == null) return "";

        var sb = new System.Text.StringBuilder();
        sb.Append($"⚔️ 공격력 {NumberFormatLoader.Abbrev(w.atk)}");

        // v43 속성 행: 매칭 속성 탄환의 최종 데미지 가산(배럭 스코프)
        var el = ElementLoader.Instance != null ? ElementLoader.Instance.Get(w.element) : null;
        if (el != null)
        {
            sb.Append($"\n<color={el.colorHex}>{el.emoji} {el.nameKo}</color>탄환 최종 데미지 +{ElemFinalDmg(w.grade):0.#}%");
            if (cmp != null && !string.IsNullOrEmpty(w.element) && cmp.element != w.element)
                sb.Append(" <color=#5eff7a>변경</color>");
        }
        else
        {
            sb.Append("\n속성 —"); // 속성 없는 구식 무기/시트 미로드 — 고스트 표기
        }

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
