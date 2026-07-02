using System.Collections;
using UnityEngine;

public class Iron : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private float disabledDuration = 1.5f; // 투명하게 유지될 시간 (초)

    private MeshRenderer meshRenderer;
    private Collider myCollider;
    // 현재 이 광석을 타겟으로 삼은 광부의 Collider (없으면 null)
    public Collider CurrentMiner { get; private set; }
    private bool isProcessing = false;

    private void Awake()
    {
        // 컴포넌트 자동 캐싱
        meshRenderer = GetComponent<MeshRenderer>();
        myCollider = GetComponent<Collider>();
    }

    /// <summary>
    /// 다른 광부가 이 광석을 선점할 수 있는지 확인하고, 가능하면 선점합니다.
    /// </summary>
    public bool TryClaim(Collider minerCollider)
    {
        if (CurrentMiner == null)
        {
            CurrentMiner = minerCollider;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 광부가 채굴을 포기(유실)하거나 완료했을 때 선점을 해제합니다.
    /// </summary>
    public void ReleaseClaim(Collider minerCollider)
    {
        if (CurrentMiner == minerCollider)
        {
            CurrentMiner = null;
        }
    }
    /// <summary>
    /// 외부(예: 곡괭이 스크립트 등)에서 이 오브젝트를 타격했을 때 호출할 메서드
    /// </summary>
    public bool TryMining()
    {
        // 중복 실행 방지
        if (isProcessing) return false;

        if (meshRenderer != null)
        {
            StartCoroutine(Respawn());
        }

        return true;
    }

    private IEnumerator Respawn()
    {
        isProcessing = true;

        // 완전히 보이지 않게 하고 충돌도 비활성화 (숨김 상태)
        meshRenderer.enabled = false;
        if (myCollider != null) myCollider.enabled = false;
        yield return new WaitForSeconds(disabledDuration);

        // 다시 원상태로 복구
        meshRenderer.enabled = true;
        if (myCollider != null) myCollider.enabled = true;

        isProcessing = false;
    }
}