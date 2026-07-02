using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Miner : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private string targetTag = "Destructible";
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float scanInterval = 0.3f;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Destroy Settings")]
    [SerializeField] private float destroyDuration = 2.0f;

    [Header("Iron Destination")]
    [SerializeField] private Machine machine;
    [SerializeField] private Vector3 ironPos;

    private Collider currentTarget;
    private Coroutine activeRoutine;
    private Rigidbody rb;
    private Collider myCollider; // 선점 등록을 위한 본인 콜라이더 캐싱

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        myCollider = GetComponent<Collider>();
        rb.freezeRotation = true;
        StartCoroutine(ScanForTargetsRoutine());
    }

    private IEnumerator ScanForTargetsRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(scanInterval);
        while (true)
        {
            if (currentTarget == null && activeRoutine == null)
            {
                FindNearestTarget();
            }
            yield return wait;
        }
    }

    private void FindNearestTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        float closestDistance = float.MaxValue;
        Collider closestTarget = null;
        Iron targetIronComponent = null;

        Vector3 myPosFlat = new Vector3(transform.position.x, 0f, transform.position.z);

        foreach (var col in hitColliders)
        {
            if (col.CompareTag(targetTag))
            {
                // [개선] Iron 컴포넌트가 있고, 이미 다른 광부가 찜했는지 확인
                if (col.TryGetComponent(out Iron iron))
                {
                    if (iron.CurrentMiner != null && iron.CurrentMiner != myCollider)
                    {
                        continue; // 다른 광부가 선점한 광석은 스킵
                    }

                    // [수정] 스캔 단계에서도 Y축을 제외한 수평 거리 기준 최단거리 계산
                    Vector3 targetPosFlat = new Vector3(col.transform.position.x, 0f, col.transform.position.z);
                    float distance = Vector3.Distance(myPosFlat, targetPosFlat);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTarget = col;
                        targetIronComponent = iron;
                    }
                }
            }
        }

        // 가장 가까운 선점 안 된 광석을 찾았다면 선점(Claim) 시도
        if (closestTarget != null && targetIronComponent != null)
        {
            if (targetIronComponent.TryClaim(myCollider))
            {
                currentTarget = closestTarget;
                activeRoutine = StartCoroutine(ApproachAndDestroyRoutine(currentTarget.gameObject, targetIronComponent));
            }
        }
    }

    private IEnumerator ApproachAndDestroyRoutine(GameObject target, Iron ironComponent)
    {
        // PHASE 1: Rigidbody 3D 직선 이동
        while (true)
        {
            if (target == null || !target.activeInHierarchy)
            {
                Debug.LogWarning("[Miner] 이동 중 타겟 유실.");
                ResetState(ironComponent); // 유실 시 선점 해제 포함 초기화
                yield break;
            }

            Vector3 myPosSameHeight = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 targetPosSameHeight = new Vector3(target.transform.position.x, 0f, target.transform.position.z);

            float horizontalDistance = Vector3.Distance(myPosSameHeight, targetPosSameHeight);

            if (horizontalDistance <= stoppingDistance)
            {
                rb.velocity = Vector3.zero;
                break;
            }

            Vector3 direction = (targetPosSameHeight - myPosSameHeight).normalized;
            rb.velocity = direction * moveSpeed;

            transform.LookAt(new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z));

            yield return null;
        }

        // PHASE 2: 파괴 (채굴)
        float elapsedTime = 0f;
        while (elapsedTime < destroyDuration)
        {
            if (target == null || !target.activeInHierarchy)
            {
                Debug.LogWarning("[Miner] 파괴 중 타겟 유실.");
                ResetState(ironComponent);
                yield break;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 최종 채굴 집행
        if (ironComponent != null && ironComponent.TryMining())
        {
            machine.AddIronOre();
            MovingItemPool.Instance.SimpleSpawn(ItemType.Iron, transform.position, ironPos);
        }
        
        ResetState(ironComponent);
    }

    /// <summary>
    /// 광부 상태를 초기화하고 선점했던 광석을 풀어줍니다.
    /// </summary>
    private void ResetState(Iron ironComponent = null)
    {
        rb.velocity = Vector3.zero;

        // 선점 해제 처리
        if (ironComponent != null)
        {
            ironComponent.ReleaseClaim(myCollider);
        }
        else if (currentTarget != null && currentTarget.TryGetComponent(out Iron residualIron))
        {
            residualIron.ReleaseClaim(myCollider);
        }

        currentTarget = null;
        activeRoutine = null;
    }
}