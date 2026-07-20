using DG.Tweening;
using UnityEngine;

// 적 연출. 죽을 때 오른쪽 위 방향으로 빙글빙글 돌며 날아가고,
// 리스폰 시 원래 위치/상태로 복귀한다.
public class EnemyAnim : MonoBehaviour
{
    [SerializeField] RectTransform body;              // 날아갈 대상(비우면 자신)
    [SerializeField] GameObject healthBar;            // 죽으면 비활성, 리스폰 시 활성
    [SerializeField] Vector2 flyOffset = new(600f, 900f); // 오른쪽 위 방향 이동량
    [SerializeField] float flyDuration = 0.7f;
    [SerializeField] float spins = 2f;                // 회전 바퀴 수
    [SerializeField] float spinDirection = -1f;       // -1: 시계, +1: 반시계
    [SerializeField] float endScale = 0.4f;           // 날아가며 작아지는 정도

    Vector2 homePos;
    Quaternion homeRot;
    Vector3 homeScale;

    void Awake()
    {
        if (body == null) body = GetComponent<RectTransform>();
        if (body == null) return;

        homePos = body.anchoredPosition;
        homeRot = body.localRotation;
        homeScale = body.localScale;
    }

    // 죽을 때: 체력바를 끄고 오른쪽 위로 빙글빙글 돌며 날아감
    public void PlayDie()
    {
        if (body == null) return;

        if (healthBar != null) healthBar.SetActive(false);

        body.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Join(body.DOAnchorPos(homePos + flyOffset, flyDuration).SetEase(Ease.OutQuad));
        seq.Join(body.DOLocalRotate(new Vector3(0f, 0f, 360f * spins * spinDirection), flyDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutQuad));
        seq.Join(body.DOScale(homeScale * endScale, flyDuration).SetEase(Ease.InQuad));
    }

    // 리스폰 시: 원래 위치/회전/크기로 즉시 복귀하고 체력바를 다시 켬
    public void ResetAnim()
    {
        if (body == null) return;

        body.DOKill();

        body.anchoredPosition = homePos;
        body.localRotation = homeRot;
        body.localScale = homeScale;

        if (healthBar != null) healthBar.SetActive(true);
    }

    void OnDisable()
    {
        if (body != null) body.DOKill();
    }
}
