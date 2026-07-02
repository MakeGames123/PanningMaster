using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DynamicJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI References")]
    [SerializeField] private RectTransform joystickArea;
    [SerializeField] private RectTransform handle;
    [SerializeField] private RectTransform handleRoot;
    [SerializeField] private GameObject pointerLoop;

    [Header("Settings")]
    [SerializeField] private float handleRange = 100f; // 핸들이 움직일 수 있는 최대 반지름 (픽셀 단위)
    [SerializeField] private float idleThresholdTime = 5.0f; // 방치 제한 시간 (5초)

    private Vector2 inputVector = Vector2.zero;
    private Canvas canvas;
    
    // [수정] 마지막으로 터치 입력이 있었던 시각을 기억할 변수
    private float lastTouchTime;
    private bool isHolding = false;
    private bool isIdleTriggered = false; // 빈 함수가 연속으로 중복 호출되는 것을 방지하는 플래그

    // 캐릭터가 가져갈 정규화된 입력 값 (X: 수평, Y: 수직 / -1.0 ~ 1.0)
    public Vector2 InputDirection => inputVector;
    // 얼마나 당겼는지 비율 (0.0 = 터치만 함, 1.0 = 끝까지 당김)
    public float InputMagnitude => inputVector.magnitude;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        // 게임 시작 시점부터 방치 카운트다운이 작동하도록 현재 시간으로 초기화
        ResetIdleTimer();
    }

    private void Update()
    {
        // 손가락을 대고 조작 중일 때는 방치 타임아웃을 체크하지 않음
        if (isHolding) return;
        
        // 이미 방치 함수가 호출되었다면 중복 실행 방지
        if (isIdleTriggered) return;

        // [핵심] 마지막 터치 시점으로부터 5초가 지났는지 실시간 체크
        if (Time.time - lastTouchTime >= idleThresholdTime)
        {
            isIdleTriggered = true;
            OnTouchIdleThreshold();
        }
    }
    public void ForceStop()
    {
        inputVector = Vector2.zero;
        gameObject.SetActive(false);
    }

    // 1. 화면을 터치했을 때 (조이스틱 생성)
    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        ResetIdleTimer(); // 터치하는 순간 방치 상태 리셋

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPoint
        );

        handleRoot.gameObject.SetActive(true);
        handleRoot.anchoredPosition = localPoint;
        handle.anchoredPosition = Vector2.zero;
    }

    // 2. 드래그 중일 때 (핸들 이동 및 입력값 계산)
    public void OnDrag(PointerEventData eventData)
    {
        // 드래그하며 움직이는 중에도 계속 마지막 터치 시각을 최신화하여 타이머 연장
        lastTouchTime = Time.time;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            handleRoot,
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPoint
        );

        inputVector = localPoint / handleRange;

        if (inputVector.magnitude > 1f)
        {
            inputVector = inputVector.normalized;
        }

        handle.anchoredPosition = inputVector * handleRange;
    }

    // 3. 손가락을 뗐을 때 (조이스틱 초기화 및 숨기기)
    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        handleRoot.gameObject.SetActive(false);

        isHolding = false;
        // 손가락을 떼는 순간부터 다시 5초 카운트다운이 새로 시작됨
        lastTouchTime = Time.time; 
    }

    /// <summary>
    /// 타이머를 초기 상태로 돌려놓는 함수
    /// </summary>
    private void ResetIdleTimer()
    {
        lastTouchTime = Time.time;
        isIdleTriggered = false;
    }

    /// <summary>
    /// [수정] 조이스틱을 5초 이상 만지지 않고 방치했을 때 단 한 번 호출될 빈 함수
    /// </summary>
    private void OnTouchIdleThreshold()
    {
        pointerLoop.SetActive(true);
    }

    private void OnEnable()
    {
        // UI가 껐다 켜질 때 타이머가 꼬여서 바로 호출되는 현상 방지
        ResetIdleTimer();
    }
}