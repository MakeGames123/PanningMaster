using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 연구소 던전의 적 1마리. 오른쪽에서 소환되어 왼쪽으로 걸어온다.
// 체력/피격/사망 연출을 스스로 처리하고, 죽으면 IsAlive=false가 되어 표적에서 제외된다.
public class LabDungeonEnemy : MonoBehaviour, IFireTarget
{
    [Header("체력 UI")]
    [SerializeField] Slider hpSlider;
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] GameObject healthBar;   //죽으면 끔

    [Header("사망 연출")]
    [SerializeField] Vector2 dieFlyOffset = new(400f, 600f); //오른쪽 위로 날아가는 이동량
    [SerializeField] float dieFlyDuration = 0.6f;
    [SerializeField] float dieSpins = 2f;    //회전 바퀴 수

    RectTransform rect;
    float maxHp;
    float hp;
    float moveSpeed;
    bool alive;

    public bool IsAlive => alive;
    public float X => rect.anchoredPosition.x;         //진행(왼쪽 이동) 판정용
    public Vector3 AimPosition => transform.position;  //IFireTarget: 조준점(월드 좌표)

    void Awake()
    {
        rect = (RectTransform)transform;
    }

    // 필드가 소환 직후 호출. 체력을 채우고 이동을 시작한다.
    public void Init(float maxHp, float moveSpeed)
    {
        this.maxHp = maxHp;
        this.moveSpeed = moveSpeed;
        hp = maxHp;
        alive = true;
        UpdateUI();
    }

    // 실패 확정 등으로 이동을 멈출 때
    public void Freeze() => moveSpeed = 0f;

    void Update()
    {
        if (!alive) return;
        rect.anchoredPosition += Vector2.left * (moveSpeed * Time.deltaTime);
    }

    // IFireTarget: 피격 처리. 죽었으면 true
    public bool Attacked(float damage)
    {
        if (!alive) return false;

        hp -= damage;
        UpdateUI();

        if (hp <= 0f)
        {
            Die();
            return true;
        }
        return false;
    }

    void UpdateUI()
    {
        float shown = Mathf.Max(0f, hp);
        if (hpSlider != null) hpSlider.value = maxHp > 0f ? shown / maxHp : 0f;
        if (hpText != null) hpText.text = $"{shown:F0}/{maxHp:F0}";
    }

    // 사망 연출: 체력바를 끄고 오른쪽 위로 빙글빙글 날아간 뒤 제거
    void Die()
    {
        alive = false;
        if (healthBar != null) healthBar.SetActive(false);

        rect.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Join(rect.DOAnchorPos(rect.anchoredPosition + dieFlyOffset, dieFlyDuration).SetEase(Ease.OutQuad));
        seq.Join(rect.DOLocalRotate(new Vector3(0f, 0f, -360f * dieSpins), dieFlyDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutQuad));
        seq.Join(rect.DOScale(rect.localScale * 0.4f, dieFlyDuration).SetEase(Ease.InQuad));
        seq.OnComplete(() => Destroy(gameObject));
    }

    void OnDestroy()
    {
        if (rect != null) rect.DOKill();
    }
}
