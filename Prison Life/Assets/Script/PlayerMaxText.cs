using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerMaxText : MonoBehaviour
{
    [Header("Component Settings")]
    [SerializeField] private TextMeshProUGUI textMesh;
    [SerializeField] private RectTransform rect;

    [Header("Tracking Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0); // 타겟 기준 초기 오프셋

    [Header("Animation Settings")]
    [SerializeField] private float moveSpeed = 1.2f;  // 위로 떠오르는 속도
    [SerializeField] private float fadeSpeed = 1.5f;  // 투명해지는 속도

    private Transform _target;
    private Camera _mainCam;
    private Color _initialColor;
    private Vector3 _currentOffset; // 애니메이션으로 인해 변하는 실시간 오프셋
    private Coroutine _animationCoroutine;
    private bool isRunning = false;

    private void Awake()
    {
        _mainCam = Camera.main;

        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshProUGUI>();
        }

        if (rect == null)
        {
            rect = GetComponent<RectTransform>();
        }

        if (textMesh != null)
        {
            _initialColor = textMesh.color;
        }
    }

    /// <summary>
    /// 오브젝트 풀에서 스폰할 때 이 함수에 추적할 대상(target)을 넘겨주며 호출합니다.
    /// </summary>
    public void Initialize(Transform targetTransform)
    {
        if(isRunning) return;

        isRunning = true;
        _target = targetTransform;

        // 1. 기존에 돌고 있던 애니메이션 코루틴이 있다면 안전하게 중지
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        // 2. 초기 상태 리셋
        _currentOffset = offset;
        textMesh.color = _initialColor;

        // 3. 첫 프레임 위치를 즉시 맞추기 위해 수동 호출
        UpdatePosition();

        // 4. 떠오르면서 투명해지는 코루틴 시작
        _animationCoroutine = StartCoroutine(AnimateTextRoutine());
    }

    private void LateUpdate()
    {
        // 코루틴이 도는 동안에도 타겟의 월드 좌표와 카메라 움직임을 실시간으로 반영
        UpdatePosition();
    }

    /// <summary>
    /// 월드 좌표를 스크린 좌표로 굽고 오프셋을 적용하는 핵심 연산 함수
    /// </summary>
    private void UpdatePosition()
    {
        if (_target == null) return;

        // 타겟의 현재 월드 위치에 실시간 오프셋(위로 떠오른 값)을 더해 스크린 좌표로 변환
        rect.position = _mainCam.WorldToScreenPoint(_target.position + _currentOffset);
    }

    /// <summary>
    /// DOTween 없이 순수 연산으로 처리하는 페이드아웃 및 상승 코루틴
    /// </summary>
    private IEnumerator AnimateTextRoutine()
    {
        Color txtColor = textMesh.color;

        // 알파값(투명도)이 0이 될 때까지 반복
        while (txtColor.a > 0f)
        {
            // 1. 오프셋의 Y축을 누적하여 위로 서서히 띄웁니다.
            _currentOffset.y += moveSpeed * Time.deltaTime;

            // 2. 알파값을 감소시켜 흐릿하게 만듭니다.
            txtColor.a -= fadeSpeed * Time.deltaTime;
            textMesh.color = txtColor;

            yield return null;
        }

        // 연출이 완전히 끝나면 오브젝트 비활성화 (풀로 반납 가능한 상태)
        isRunning = false;
        gameObject.SetActive(false);
    }
}