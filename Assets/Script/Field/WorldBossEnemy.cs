using TMPro;
using UnityEngine;
using UnityEngine.UI;

// v45 월드보스 전용 적(수배범). 사실상 불사신 — 죽음 판정 없음, 체력바는 연출용이고
// 다 닳으면 다음 체력(×배수)으로 리필된다. 넣은 총 데미지(TotalDamage)가 곧 점수.
// 체력바 연출은 골드 던전 적과 동일: 논리(점수·이월)는 즉시 반영하고, 표시용 바가 실제 상태를
// 향해 매우 빠르게 감소(던파식). 바를 넘길 때마다 뒤에 깔린 다음 바 색이 드러난다.
// IFireTarget으로 Player가 다중 표적 모드로 조준한다(죽음 판정으로 사이클이 끊기지 않음).
public class WorldBossEnemy : MonoBehaviour, IFireTarget
{
    [Header("표시")]
    [SerializeField] TextMeshProUGUI faceText; // 수배범 얼굴 이모지(선택 — Begin 시 이번 주 보스로 교체)
    [SerializeField] DamageFloatingText damageText; // 피격 데미지 플로팅

    [Header("체력 UI")]
    [SerializeField] Slider hpSlider;
    [SerializeField] Image fillImage;        // 현재 바 색(앞)
    [SerializeField] Image backgroundImage;  // 다음 바 색(뒤 — 닳으면 드러남). 바 넘길 때마다 랜덤색

    [Header("설정")]
    [SerializeField] float baseHp = 100f;          // 첫 체력바 값(연출용)
    [SerializeField] float hpMultiplier = 2f;      // 다음 체력바 = 현재 × 이 배수
    [SerializeField] int maxDoubling = 100;        // 체력 배수 상한(float 오버플로 방지)
    [SerializeField] float catchUpFactor = 8f;     // 남은 감소량 × 이 값 = 초당 감소 속도(많이 남을수록 빠름)
    [SerializeField] float minDrainPerSecond = 2f; // 최소 감소 속도(적게 남았을 때의 하한 — 끝맺음용)

    // 실제(논리) 상태 — 점수·이월은 즉시 반영
    float maxHp;
    float hp;
    int depletions;

    // 표시(연출) 상태 — 실제를 향해 빠르게 감소
    int dispBar;
    float dispFill; // 0~1
    Color nextColor; // 다음 바(background) 색

    public long TotalDamage { get; private set; } // 누적 데미지 = 점수
    public Vector3 AimPosition => transform.position; // IFireTarget: 조준점

    // 전투 시작 시 초기화 — 이번 주 수배범 얼굴로 교체
    public void Begin(WorldBossOutlawData outlaw)
    {
        TotalDamage = 0;
        depletions = 0;
        maxHp = HpForBar(0);
        hp = maxHp;

        dispBar = 0;
        dispFill = 1f;

        if (faceText != null && outlaw != null) faceText.text = outlaw.face;

        // 색 초기화: fill = 현재 바 색, background = 다음 바 색(랜덤)
        Color first = RandomBarColor(Color.clear);
        if (fillImage != null) fillImage.color = first;
        nextColor = RandomBarColor(first);
        if (backgroundImage != null) backgroundImage.color = nextColor;

        UpdateUI();
    }

    // IFireTarget: 피격. 논리(점수·이월)는 즉시 처리, 체력바 연출은 Update에서. 죽지 않으므로 false.
    public bool Attacked(float damage)
    {
        TotalDamage += (long)damage;
        hp -= damage;

        if (damageText != null) damageText.Show(damage); //피격 데미지 플로팅

        // 체력바가 다 닳으면 죽지 않고 다음 체력바(×배수)로 리필. 이월분으로 연쇄 소진 가능.
        while (hp <= 0f)
        {
            depletions++;
            float overflow = -hp;         // 초과 데미지
            maxHp = HpForBar(depletions); // 다음 체력바
            hp = maxHp - overflow;        // 이월 적용
        }

        return false;
    }

    // 표시 바가 실제 상태를 향해 감소 — 남은 감소량이 클수록(넘길 바가 많을수록) 빠르게, 적을수록 느리게.
    void Update()
    {
        float targetFill = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;

        if (dispBar == depletions && Mathf.Approximately(dispFill, targetFill)) return; // 이미 도달

        // 남은 감소량(바-fill 단위)에 비례한 속도 → 많이 남으면 빠르고, 적게 남으면 느림
        float remaining = RemainingDrain(targetFill);
        float step = Mathf.Max(minDrainPerSecond, remaining * catchUpFactor) * Time.deltaTime;

        // step만큼 감소하되 바 경계를 넘으면 다음 바로 flip(한 프레임에 여러 바 가능)
        while (step > 0f)
        {
            if (dispBar < depletions)
            {
                if (dispFill > step) { dispFill -= step; step = 0f; }
                else { step -= dispFill; dispBar++; dispFill = 1f; AdvanceBarColor(); }
            }
            else
            {
                dispFill = Mathf.Max(targetFill, dispFill - step); // 목표 fill까지만
                step = 0f;
            }
        }

        UpdateUI();
    }

    // 표시 바가 실제 상태까지 감소해야 할 총량(바-fill 단위)
    float RemainingDrain(float targetFill)
    {
        if (dispBar < depletions)
            return dispFill + (depletions - dispBar - 1) + (1f - targetFill);
        return Mathf.Max(0f, dispFill - targetFill);
    }

    // 바를 넘길 때: 드러났던 다음 색(background)이 새 현재 색(fill)이 되고, background는 새 랜덤색.
    void AdvanceBarColor()
    {
        if (fillImage != null) fillImage.color = nextColor;
        nextColor = RandomBarColor(nextColor);
        if (backgroundImage != null) backgroundImage.color = nextColor;
    }

    // avoid와 색상(Hue)이 충분히 다른 선명한 랜덤색
    Color RandomBarColor(Color avoid)
    {
        Color.RGBToHSV(avoid, out float avoidH, out _, out _);
        float h = Random.value;
        for (int i = 0; i < 8 && Mathf.Abs(Mathf.DeltaAngle(h * 360f, avoidH * 360f)) < 60f; i++)
            h = Random.value;
        return Color.HSVToRGB(h, Random.Range(0.65f, 0.95f), Random.Range(0.8f, 1f));
    }

    // 소진 순번별 체력바 값: 0번=초기, 이후 ×hpMultiplier씩(오버플로 방지 상한)
    float HpForBar(int index) => baseHp * Mathf.Pow(hpMultiplier, Mathf.Min(index, maxDoubling));

    void UpdateUI()
    {
        if (hpSlider != null) hpSlider.value = Mathf.Clamp01(dispFill);
    }
}
