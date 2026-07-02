using UnityEngine;

public class NavigationArrow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("플레이어가 가야 할 목적지 Transform")]
    [SerializeField] private Vector3 targetDestination; 

    [Header("Rotation Settings")]
    [Tooltip("화살표가 회전하는 속도 (높을수록 기민하게 반응)")]
    [SerializeField] private float rotationSpeed = 10.0f;

    /// <summary>
    /// 외부(퀘스트 매니저 등)에서 실시간으로 목적지를 바꿔줄 때 사용합니다.
    /// </summary>
    public void SetTarget(Vector3 newTarget)
    {
        targetDestination = newTarget;
    }

    private void LateUpdate()
    {

        // 1. 화살표 위치에서 목적지 위치를 바라보는 방향 벡터 계산
        Vector3 direction = targetDestination - transform.position;

        // [중요] 화살표가 위아래(Y축)로 꺾이지 않도록 평면(X, Z) 연산으로 제한합니다.
        direction.y = 0f;

        // 2. 방향 벡터의 길이가 0에 가까우면(목적지에 거의 도달했으면) 회전 연산을 건너뜁니다.
        if (direction.sqrMagnitude > 0.01f)
        {
            // 3. 목표 방향을 바라보는 쿼터니언(회전값) 산출
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 4. 현재 회전값에서 목표 회전값까지 부드럽게 보간 이동 (Slerp)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}