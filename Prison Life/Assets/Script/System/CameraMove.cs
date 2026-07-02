using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CameraMove : MonoBehaviour
{
    [Header("Tracking Settings")]
    [SerializeField] private Transform target;   // 따라갈 대상 (플레이어)
    [SerializeField] private Vector3 offset;     // 카메라 위치 오프셋

    [Header("Highlight Settings")]
    [SerializeField] private float moveDuration = 1.0f; // 연출 지점까지 이동하는 시간
    [SerializeField] private float showDuration = 1.5f; // 해당 지점을 멈춰서 보여주는 시간
    [SerializeField] private DynamicJoystick joystick; //카메라 무브동안 클릭 방지
    [SerializeField] private Ease cameraEase = Ease.OutCubic; // 카메라 이동 이징

    private bool isTracking = true; // 현재 플레이어를 추적 중인지 여부
    private Sequence _cameraSequence;
    Camera mainCamera;
    private float _initialOrthographicSize; // 원래 카메라의 오르토 크기를 기억할 변수

    void Awake()
    {
        mainCamera = Camera.main;
        _initialOrthographicSize = mainCamera.orthographicSize;
    }

    void LateUpdate()
    {
        // 추적 모드가 켜져 있을 때만 플레이어를 고정해서 따라감
        if (!isTracking || target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = desiredPosition;
    }

    /// <summary>
    /// 외부에서 이 함수를 호출하면 플레이어 고정을 풀고 특정 위치를 연출한 뒤 복귀합니다.
    /// </summary>
    /// <param name="targetWorldPosition">보여주고 싶은 목적지의 3D 월드 좌표</param>
/// <summary>
    /// 외부에서 이 함수를 호출하면 카메라 이동과 동시에 지정한 orthographicSize로 줌 연출을 수행합니다.
    /// </summary>
    /// <param name="targetWorldPosition">보여주고 싶은 목적지 좌표</param>
    /// <param name="targetOrthoSize">하이라이트 시 변경할 카메라의 Orthographic Size (줌 크기)</param>
    public void ShowTargetPosition(Vector3 targetWorldPosition, float targetOrthoSize = 0)
    {
        if (mainCamera == null)
        {
            Debug.LogError("[CameraMove] Camera 컴포넌트가 연결되지 않았습니다.");
            return;
        }

        // 1. 기존에 돌고 있던 카메라 트윈 시퀀스가 있다면 안전하게 리턴 처리
        if (_cameraSequence != null && _cameraSequence.IsActive())
        {
            return;
        }

        //만약 디폴트값이면 그냥 유지
        if(targetOrthoSize == 0)
        {
            targetOrthoSize = _initialOrthographicSize;
        }

        // 2. 플레이어 추적 및 조이스틱 제어
        isTracking = false;
        joystick.ForceStop();

        Vector3 desiredViewPosition = targetWorldPosition;

        // 4. DOTween 시퀀스 생성 및 체이닝
        _cameraSequence = DOTween.Sequence();

        _cameraSequence
            // --- 1구간: 목적지로 이동하면서 동시에 카메라 줌 아웃/인 연출 ---
            // DOFieldOfView 처럼 Orthographic 카메라는 DOOrthoSize를 사용하여 부드럽게 크기를 바꿉니다.
            .Append(transform.DOMove(desiredViewPosition, moveDuration).SetEase(cameraEase))
            .Join(mainCamera.DOOrthoSize(targetOrthoSize, moveDuration).SetEase(cameraEase))

            // --- 2구간: 해당 지점에서 잠시 멈춰서 보여주기 ---
            .AppendInterval(showDuration)

            // --- 3구간: 다시 플레이어에게 복귀하면서 카메라 크기도 원래대로 복구 ---
            .Append(transform.DOMove(target.position + offset, moveDuration).SetEase(cameraEase))
            .Join(mainCamera.DOOrthoSize(_initialOrthographicSize, moveDuration).SetEase(cameraEase))

            // 모든 연출이 끝나 원래 자리로 복귀했다면 실행할 콜백
            .OnComplete(() =>
            {
                isTracking = true;
                joystick.gameObject.SetActive(true);
                Debug.Log("[Camera] 플레이어 타겟 추적 및 기본 줌 크기 복귀 완료.");
            });
    }

    private void OnDestroy()
    {
        if (_cameraSequence != null && _cameraSequence.IsActive())
        {
            _cameraSequence.Kill();
        }
    }
}