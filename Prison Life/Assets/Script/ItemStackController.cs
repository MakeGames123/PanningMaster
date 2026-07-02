using System.Collections.Generic;
using UnityEngine;

public class ItemStackController : MonoBehaviour
{
    [Header("Stack Root")]
    [Tooltip("아이템들이 자식으로 쌓일 부모 오브젝트")]
    [SerializeField] private Transform stackRoot;

    [Header("Forward/Backward Leaning")]
    [Tooltip("가장 이쁜 기준값인 0.01로 세팅")]
    [SerializeField] private float leanSensitivity = 0.01f; 
    [SerializeField] private float maxLeanDistance = 2.0f;  

    [Header("Spring Physics (출렁임 세팅)")]
    [Tooltip("스프링의 탄성 강도 (값이 클수록 멈췄을 때 팅팅거리며 빠르게 출렁입니다)")]
    [SerializeField] private float springStiffness = 150f;
    [Tooltip("스프링의 감쇠력 (값이 작을수록 오랫동안 흔들리고, 크면 금방 멈춥니다)")]
    [SerializeField] private float springDamping = 10f;

    [Header("Exponential Sensitivity Settings")]
    [Tooltip("지수함수의 밑(Base) 값입니다.")]
    [SerializeField] private float exponentialBase = 1.35f;

    private List<Transform> _activeStackedItems = new List<Transform>();
    private List<float> _originalYPositions = new List<float>();
    private List<float> _layerLeanWeights = new List<float>();

    // [추가] 각 블록별 실시간 Z축 로컬 위치와 복귀 속도를 추적하기 위한 리스트
    private List<float> _currentLeanZs = new List<float>();
    private List<float> _leanZVelocitys = new List<float>();

    private Vector3 _previousPosition;
    private Vector3 _currentVelocity;

    private void Start()
    {
        _previousPosition = transform.position;
        if (stackRoot == null) stackRoot = this.transform;
        
        RefreshStackedItems();
    }

    private void Update()
    {
        // 1. 이번 프레임의 이동 속도 및 로컬 앞뒤(Z축) 속도 추출
        Vector3 currentPosition = transform.position;
        _currentVelocity = (currentPosition - _previousPosition) / Time.deltaTime;
        _previousPosition = currentPosition;

        Vector3 localVelocity = transform.InverseTransformDirection(_currentVelocity);
        float forwardVelocity = localVelocity.z;

        int totalCount = _activeStackedItems.Count;

        // 2. 활성화된 자식 스택들을 순회하며 탄성 및 출렁임 연산 적용
        for (int i = 0; i < totalCount; i++)
        {
            Transform item = _activeStackedItems[i];
            if (item == null) continue;

            float targetLeanZ = 0f;

            // 0번째(맨 바닥)는 무조건 0 고정 (어떤 상황에서도 절대 출렁이지 않음)
            if (i > 0)
            {
                int distanceFromTop = (totalCount - 1) - i;

                // 상위 10개 혹은 전체 개수가 10개 미만일 때 조건 검사
                if (totalCount >= 10 && distanceFromTop < 10 || totalCount < 10)
                {
                    int animIndex = (totalCount >= 10) ? (9 - distanceFromTop) : i;

                    if (animIndex > 0)
                    {
                        float exponentialSensitivity = leanSensitivity * Mathf.Pow(exponentialBase, animIndex);
                        float targetWeight = _layerLeanWeights[animIndex];

                        // 이동 중에 가야 할 목표 관성 Z 좌표값
                        targetLeanZ = -forwardVelocity * exponentialSensitivity * targetWeight;
                        targetLeanZ = Mathf.Clamp(targetLeanZ, -maxLeanDistance, maxLeanDistance);
                    }
                }
            }

            // [핵심 변경: 감쇠 진동 수동 물리 구현]
            // 현재 층의 실시간 위치와 속도 값을 가져옴
            float currentZ = _currentLeanZs[i];
            float velocityZ = _leanZVelocitys[i];

            // Hooke's Law (후크의 법칙) + Damping (감쇠력) 연산
            // 1. 스프링 힘 = (목표치 - 현재위치) * 탄성계수
            float springForce = (targetLeanZ - currentZ) * springStiffness;
            // 2. 댐핑 힘 = 현재 속도 * 감쇠계수 (반대 방향으로 작용하여 브레이크를 검)
            float dampingForce = velocityZ * springDamping;
            
            // 총 가속도를 구하고 가속도를 속도에 누적 반영
            float accelerationZ = springForce - dampingForce;
            velocityZ += accelerationZ * Time.deltaTime;
            
            // 속도를 현재 위치에 반영
            currentZ += velocityZ * Time.deltaTime;

            // 연산된 최종 값을 리스트에 백업
            _currentLeanZs[i] = currentZ;
            _leanZVelocitys[i] = velocityZ;

            // 3. X는 0 고정, Y축은 고유 높이 유지, Z축은 출렁임 물리 좌표 적용
            float originalY = _originalYPositions[i];
            item.localPosition = new Vector3(0f, originalY, currentZ);
        }
    }

    /// <summary>
    /// 자식 구조 변경 시 데이터 배열 초기화 및 캐싱
    /// </summary>
    public void RefreshStackedItems()
    {
        _activeStackedItems.Clear();
        _originalYPositions.Clear();
        _layerLeanWeights.Clear();
        
        // 물리 연산 리스트도 함께 초기화
        _currentLeanZs.Clear();
        _leanZVelocitys.Clear();

        float currentWeight = 0f;  
        float addValue = 0.1f;     

        for (int i = 0; i < stackRoot.childCount; i++)
        {
            Transform child = stackRoot.GetChild(i);
            if (child.gameObject.activeSelf)
            {
                _activeStackedItems.Add(child);
                _originalYPositions.Add(child.localPosition.y);
                
                // 생성 시점의 로컬 Z값을 기본값으로 세팅
                _currentLeanZs.Add(child.localPosition.z);
                _leanZVelocitys.Add(0f); // 초기 속도는 0

                if (i == 0)
                {
                    _layerLeanWeights.Add(0f); 
                }
                else
                {
                    currentWeight += addValue;
                    _layerLeanWeights.Add(currentWeight);
                    addValue += 0.01f; 
                }
            }
        }
    }
}