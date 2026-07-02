using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Pool;

public class PrisonerLine : MonoBehaviour
{
    [Header("Line Settings")]
    [SerializeField] private Transform pointA;       // 맨 앞 자리 (Pop되는 위치)
    [SerializeField] private Transform pointB;       // 맨 뒤 자리 (새 오브젝트가 배치되는 기준)
    [SerializeField] private int maxCount = 5;       // 라인에 세워둘 최대 오브젝트 개수
    [SerializeField] private float moveDuration = 0.3f; // 앞으로 전진할 때 걸리는 시간
    [SerializeField] private Ease moveEase = Ease.OutQuad; // 이동 이징 스타일

    [Header("Pool Assignment")]
    [SerializeField] private PrisonerPool objectPool; // 단일 오브젝트 풀

    private List<GameObject> activeObjects = new List<GameObject>();
    private Vector3[] slotPositions;

    private void Start()
    {
        CalculateSlotPositions();
        FillLineOnStart(); // 시작할 때 라인 꽉 채우기
    }

    /// <summary>
    /// A지점부터 B지점까지 균일한 간격으로 오브젝트들이 서 있을 고정 좌표들을 계산합니다.
    /// </summary>
    private void CalculateSlotPositions()
    {
        slotPositions = new Vector3[maxCount];

        if (maxCount <= 1)
        {
            slotPositions[0] = pointA.position;
            return;
        }

        for (int i = 0; i < maxCount; i++)
        {
            float ratio = (float)i / (maxCount - 1);
            slotPositions[i] = Vector3.Lerp(pointA.position, pointB.position, ratio);
        }
    }

    /// <summary>
    /// 시작 시 라인을 최대 개수만큼 꽉 채워 배치합니다.
    /// </summary>
    private void FillLineOnStart()
    {
        for (int i = 0; i < maxCount; i++)
        {
            // 각 슬롯의 제자리에 즉시 생성 및 배치
            Vector3 targetPos = slotPositions[i];
            GameObject newObj = objectPool.Spawn(targetPos, Quaternion.identity);

            activeObjects.Add(newObj);
        }
    }

    /// <summary>
    /// [Push] 맨 뒤 자리에 여유가 있다면 오브젝트를 새로 배치합니다.
    /// </summary>
    public void PushObject()
    {
        if (activeObjects.Count >= maxCount)
        {
            Debug.LogWarning("[Line] 줄이 가득 차서 더 이상 배치할 수 없습니다.");
            return;
        }

        // 새 오브젝트는 현재 비어있는 가장 마지막 슬롯 위치에 스폰됩니다.
        int targetIndex = activeObjects.Count;
        Vector3 spawnPos = slotPositions[targetIndex];

        GameObject newObj = objectPool.Spawn(spawnPos, Quaternion.identity);
        activeObjects.Add(newObj);
    }

    /// <summary>
    /// [Pop] 맨 앞 오브젝트를 꺼내고, 뒤의 오브젝트들을 DOTween으로 일제히 한 칸씩 전진시킵니다.
    /// </summary>
    public GameObject PopObject()
    {
        if (activeObjects.Count == 0)
        {
            Debug.LogWarning("[Line] 줄이 텅 비어있어 꺼낼 오브젝트가 없습니다.");
            return null;
        }

        // 1. 맨 앞 오브젝트 뽑기
        GameObject poppedObj = activeObjects[0];
        activeObjects.RemoveAt(0);

        // 2. 뒤에 남은 오브젝트들을 DOTween으로 일제히 당기기
        MoveRemainingObjects();

        PushObject();

        if (poppedObj.TryGetComponent(out PrisonerMove move))
        {
            move.MoveAlongPath();
        }
        else objectPool.Despawn(poppedObj);
        return poppedObj;
    }

    /// <summary>
    /// Pop이 발생한 직후, 리스트에 남아있는 오브젝트들을 한 칸 전진된 슬롯 위치로 트윈시킵니다.
    /// </summary>
    private void MoveRemainingObjects()
    {
        for (int i = 0; i < activeObjects.Count; i++)
        {
            if (activeObjects[i] == null) continue;

            // 기존 트윈이 돌고 있다면 꼬이지 않게 킬(Kill) 처리
            activeObjects[i].transform.DOKill();

            // 리스트 인덱스가 당겨졌으므로(i), 바뀐 인덱스의 새 슬롯 좌표로 이동시킵니다.
            Vector3 targetPosition = slotPositions[i];
            activeObjects[i].transform.DOMove(targetPosition, moveDuration).SetEase(moveEase);
        }
    }
}