using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class InventoryStack : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private GameObject objectPrefab;    // 풀링할 단 한 종류의 프리팹

    [Header("Pool Settings")]
    [SerializeField] private int poolSize = 30;          // 시작할 때 미리 만들어 둘 오브젝트 개수

    [Header("Multi-Stack Layout Settings")]
    [Tooltip("가로(X축)로 배치할 스택의 개수")]
    [SerializeField] private int columns = 3;
    [Tooltip("세로(Z축)로 배치할 스택의 개수")]
    [SerializeField] private int rows = 2;

    [Header("Spacing Settings")]
    [SerializeField] private float paddingBottom = 0.1f;  // 맨 아래 첫 번째 레이어의 시작 높이(Y)
    [SerializeField] private float objectHeight = 0.2f;   // 오브젝트 자체의 높이 두께
    [SerializeField] private float verticalSpacing = 0.05f; // 위로 쌓일 때의 Y축 간격
    [SerializeField] private float horizontalSpacingX = 0.5f; // 가로 스택 간의 X축 간격
    [SerializeField] private float horizontalSpacingZ = 0.5f; // 세로 스택 간의 Z축 간격

    // 풀에 의해 생성 및 정렬된 모든 오브젝트를 담는 리스트
    [SerializeField] private List<ItemAnim> objectPool = new List<ItemAnim>();
    //스택 관성 애니메이션
    [SerializeField] private ItemStackController controller;

    // 현재 활성화되어 눈에 보이는 오브젝트 개수 (외부에서 읽기 전용)
    public int ActiveCount { get; private set; } = 0;
    // 풀의 최대 용량
    public int MaxCapacity => objectPool.Count;

    // 현재 스택의 실제 개수 (Columns * Rows)
    private int TotalStacks => Mathf.Max(1, columns * rows);

    private void Awake()
    {
        if (Application.isPlaying)
        {
            InitializePool();
        }
    }

    private void OnValidate()
    {
        UpdateLayout();
    }

    private void InitializePool()
    {
        ClearAll();

        if (objectPrefab == null)
        {
            Debug.LogWarning("인스펙터에 Object Prefab이 할당되지 않았습니다.");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(objectPrefab, transform, false);
            obj.SetActive(false);
            objectPool.Add(obj.GetComponent<ItemAnim>());
        }

        UpdateLayout();
        ActiveCount = 0;
    }

    public void PushObject()
    {
        if (ActiveCount >= objectPool.Count)
        {
            Debug.LogWarning("오브젝트 풀이 가득 찼습니다! Pool Size를 늘려주세요.");
            return;
        }

        objectPool[ActiveCount].gameObject.SetActive(true);
        objectPool[ActiveCount].PlayScalePop();
        if(controller != null) controller.RefreshStackedItems();
        ActiveCount++;
    }

    public bool PopObject()
    {
        if (ActiveCount <= 0) return false;

        ActiveCount--;
        objectPool[ActiveCount].gameObject.SetActive(false);
        if(controller != null) controller.RefreshStackedItems();

        return true;
    }

    public Vector3 ReturnFrontPos()
    {
        return objectPool[ActiveCount].transform.position;
    }

    /// <summary>
    /// 풀 내부의 모든 오브젝트를 다중 스택 규칙(X, Z 평면 분배 후 Y축 적재)에 맞춰 나열합니다.
    /// </summary>
    public void UpdateLayout()
    {
        List<ItemAnim> targets = Application.isPlaying ? objectPool : GetEditorChildren();

        int totalStacks = TotalStacks;

        for (int i = 0; i < targets.Count; i++)
        {
            ItemAnim childObj = targets[i];
            if (childObj == null) continue;

            Transform child = childObj.transform;

            // 1. 몇 번째 스택 자리에 들어갈 것인가? (0 ~ totalStacks-1)
            int stackIndex = i % totalStacks;

            // 2. 해당 스택에서 위로 몇 번째 층(Layer)에 쌓일 것인가?
            int layerIndex = i / totalStacks;

            // 3. 스택 번호를 기반으로 가로(X), 세로(Z) 격자 좌표 계산
            int col = stackIndex % columns; // X축 인덱스
            int row = stackIndex / columns; // Z축 인덱스

            // 4. 로컬 좌표 최종 연산
            // (격자 중심 정렬을 원하시면 col - (columns-1)*0.5f 형태의 피벗 연산을 활용할 수도 있습니다)
            float targetX = col * horizontalSpacingX;
            float targetZ = row * horizontalSpacingZ;

            // Y축 높이: 바닥 여백 + (오브젝트 자체 높이 + 쌓임 간격) * 층수 + 피벗 보정(중앙 기준)
            float targetY = paddingBottom + (layerIndex * (objectHeight + verticalSpacing)) + (objectHeight * 0.5f);

            // 위치 지정
            child.localPosition = new Vector3(targetX, targetY, targetZ);
        }
    }

    public void ClearAll()
    {
        for (int i = objectPool.Count - 1; i >= 0; i--)
        {
            if (objectPool[i] != null)
            {
                if (Application.isPlaying) Destroy(objectPool[i]);
                else DestroyImmediate(objectPool[i]);
            }
        }
        objectPool.Clear();
        ActiveCount = 0;
    }

    private List<ItemAnim> GetEditorChildren()
    {
        List<ItemAnim> children = new List<ItemAnim>();
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).TryGetComponent(out ItemAnim anim))
            {
                children.Add(anim);
            }
        }
        return children;
    }
}