using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DeliveryMan : MonoBehaviour, ItemInteractive
{
    public enum DeliveryState { Idle, MovingToSource, MovingToDestination }

    [Header("State Info")]
    [SerializeField] private DeliveryState currentState = DeliveryState.Idle;

    [Header("Source Place")]
    [SerializeField] private Transform sourcePlace;

    [Header("Deliver Destination")]
    [SerializeField] private Transform destination;

    [Header("Stacks")]
    [SerializeField] private InventoryStack handCuffInventory;

    [Header("Inventory Settings")]
    [SerializeField] private int maxHandcuffCount = 10;
    [SerializeField] private float interactiveInterval = 0.5f;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float stoppingDistance = 1.2f;

    private ItemData handCuffData;
    private Rigidbody rb;
    private Coroutine moveRoutine;

    public int HandcuffCount => handCuffData.CurrentCount;

    private void Awake()
    {
        handCuffData = new ItemData("수갑", maxHandcuffCount, handCuffInventory);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        // 1. 이벤트 구독 등록
        handCuffData.OnCountChanged += HandleHandcuffCountChanged;

        // 2. 시작 시 첫 행동 결정 (수갑이 비어있으므로 공급처로)
        EvaluateBehavior(handCuffData.CurrentCount);
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위한 이벤트 해제
        if (handCuffData != null)
        {
            handCuffData.OnCountChanged -= HandleHandcuffCountChanged;
        }
    }

    /// <summary>
    /// ItemData에서 이벤트가 들어오면 실행되는 구독 함수
    /// </summary>
    private void HandleHandcuffCountChanged(int currentCount, int maxCount)
    {
        EvaluateBehavior(currentCount);
    }

    /// <summary>
    /// 개수 상태를 체크해서 그에 맞는 행동(목적지 변경)을 수행하는 핵심 함수
    /// </summary>
    private void EvaluateBehavior(int currentCount)
    {
        // 손이 다 비었다면 -> 공급처로 이동 시작
        if (currentCount <= 0)
        {
            SetStateAndMove(DeliveryState.MovingToSource, sourcePlace);
        }
        // 손이 가득 찼다면 -> 목적지로 이동 시작
        else if (currentCount >= handCuffData.MaxCount)
        {
            SetStateAndMove(DeliveryState.MovingToDestination, destination);
        }
        // 중간 수치일 때는 이동 중이므로 가만히 둡니다 (외부 상호작용으로 개수가 바뀔 때를 대비)
    }

    /// <summary>
    /// 상태를 바꾸고 기존 이동을 취소한 뒤 새로운 목적지로 이동을 명령합니다.
    /// </summary>
    private void SetStateAndMove(DeliveryState newState, Transform targetTransform)
    {
        currentState = newState;

        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveRoutine(targetTransform));
    }

    /// <summary>
    /// 오직 '이동 목적지'까지 평면 이동만 수행하는 순수 이동 코루틴 (Y축 배제)
    /// </summary>
    private IEnumerator MoveRoutine(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            rb.velocity = Vector3.zero;
            yield break;
        }

        while (true)
        {
            Vector3 myPosFlat = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 targetPosFlat = new Vector3(targetTransform.position.x, 0f, targetTransform.position.z);

            float horizontalDistance = Vector3.Distance(myPosFlat, targetPosFlat);

            if (horizontalDistance <= stoppingDistance)
            {
                rb.velocity = Vector3.zero;
                // [참고] 여기에 도착 후 상호작용 트리거(예: 충돌 구역에서 AddItem/GetItem 실행)를 연동하시면 됩니다.
                yield break; 
            }

            Vector3 direction = (targetPosFlat - myPosFlat).normalized;
            rb.velocity = direction * moveSpeed;

            transform.LookAt(new Vector3(targetTransform.position.x, transform.position.y, targetTransform.position.z));

            yield return null;
        }
    }

    public bool CheckItemCount(ItemType type)
    {
        if (type != ItemType.Handcuff) return false;
        return handCuffData.CurrentCount < handCuffData.MaxCount;
    }

    public void AddItem(ItemType type, Vector3 pos)
    {
        if (type != ItemType.Handcuff) return;
        MovingItemPool.Instance.Spawn(type, pos, handCuffInventory.ReturnFrontPos());
        handCuffData.Add(1);
    }

    public bool GetItem(ItemType type, Vector3 pos)
    {
        if (type != ItemType.Handcuff) return false;
        MovingItemPool.Instance.Spawn(type, handCuffInventory.ReturnFrontPos(), pos);
        return handCuffData.Remove(1);
    }
    public float ReturnInterval()
    {
        return interactiveInterval;
    }

}