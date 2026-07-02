using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointerLoopMove : MonoBehaviour
{
    [Header("Component Settings")]
    [SerializeField] private RectTransform rectTransform;

    [Header("Loop Settings")]
    [SerializeField] private float width = 400f;       // 8자 루프의 가로 크기 (반지름)
    [SerializeField] private float height = 180f;      // 8자 루프의 세로 크기 (높이)
    [SerializeField] private float speed = 2.0f;       // 이동 속도

    private Vector2 _centerPosition;                   // 시작 시점의 중심 위치
    private float _timeCounter = 0f;

    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        // 스크립트가 시작된 현재 UI 위치를 기준으로 8자를 그리도록 중심점 저장
        if (rectTransform != null)
        {
            _centerPosition = rectTransform.anchoredPosition;
        }
    }

    private void Update()
    {
        if (rectTransform == null) return;

        // 시간 누적 (속도 반영)
        _timeCounter += Time.deltaTime * speed;

        // 베르누이의 레미니스케이트 공식을 활용한 ∞ 궤도 연산
        // X축은 기본 sin 그래프, Y축은 두 배 빠른 sin과 cos의 조합으로 8자를 형성합니다.
        float sinT = Mathf.Sin(_timeCounter);
        float cosT = Mathf.Cos(_timeCounter);

        // 분모 연산 (8자 중앙에서 교차할 때 왜곡 없이 깔끔하게 떨어지도록 보정)
        float denominator = 1f + sinT * sinT;

        // 실시간 8자 좌표 산출
        float x = (width * cosT) / denominator;
        float y = (height * sinT * cosT) / denominator;

        // 중심 좌표 기준으로 실시간 UI 위치 갱신
        rectTransform.anchoredPosition = _centerPosition + new Vector2(x, y);

        //클릭하면 비활성화
        if (Input.GetMouseButtonDown(0))
        {
            transform.parent.gameObject.SetActive(false);
        }
    }
}
