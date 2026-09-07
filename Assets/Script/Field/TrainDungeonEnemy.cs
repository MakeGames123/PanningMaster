using UnityEngine;
using UnityEngine.UI;

// 열차 강탈 전용 적(화물칸). 죽지 않는다 — 체력이 다 닳으면 화물칸 카운트 +1 후 같은 체력으로 리필(오버킬 이월).
// 화물칸 HP = 본편 몬스터 체력 × CartHpMul(시트) 고정. 황금 화물칸의 추가 🎫은 정산 산식이 일괄 계산(여긴 예고 표시만).
// IFireTarget으로 Player가 전용 표적으로 조준한다(죽지 않으므로 사이클이 끊기지 않음).
public class TrainDungeonEnemy : MonoBehaviour, IFireTarget
{
    [Header("체력 UI")]
    [SerializeField] Slider hpSlider;
    [SerializeField] DamageFloatingText damageText; //피격 데미지 플로팅
    [SerializeField] GameObject goldenMark;         //다음 칸이 ✨황금이면 켜는 예고 표시(선택 연결)

    float cartHp;
    float hp;
    int goldenEvery = 4;

    public int Carts { get; private set; } //부순 화물칸 수
    public Vector3 AimPosition => transform.position; // IFireTarget: 조준점(월드 좌표)

    // 던전 시작 시 초기화
    public void Begin(float cartHp, int goldenEvery)
    {
        this.cartHp = Mathf.Max(1f, cartHp);
        this.goldenEvery = Mathf.Max(1, goldenEvery);
        Carts = 0;
        hp = this.cartHp;
        UpdateUI();
    }

    // IFireTarget: 피격. 죽지 않으므로 false.
    public bool Attacked(float damage)
    {
        hp -= damage;
        if (damageText != null) damageText.Show(damage);

        while (hp <= 0f) //오버킬 이월 — 큰 한 방으로 여러 칸 가능
        {
            Carts++;
            hp += cartHp;
        }

        UpdateUI();
        return false;
    }

    void UpdateUI()
    {
        if (hpSlider != null) hpSlider.value = cartHp > 0f ? Mathf.Clamp01(hp / cartHp) : 0f;
        if (goldenMark != null) goldenMark.SetActive((Carts + 1) % goldenEvery == 0); //다음 칸 ✨황금 예고
    }
}
