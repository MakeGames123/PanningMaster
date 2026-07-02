using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakTool : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private string targetTag = "Iron"; // 파괴할 오브젝트 태그

    [Header("Destroy Rules")]
    [SerializeField] private float breakCooldown = 1.0f;       // 파괴 쿨타임 (초)

    [SerializeField] private Player player;       // 파괴 쿨타임 (초)
    private List<GameObject> contactingObjects = new List<GameObject>();
    private Coroutine breakRoutine;
    private float lastBreakTime = -999f;

    // 외부에서 쿨타임 상태를 확인할 수 있는 프로퍼티
    public bool IsOnCooldown => Time.time - lastBreakTime < breakCooldown;

    // 1. 트리거 범위 안으로 오브젝트가 들어왔을 때
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            if (!contactingObjects.Contains(other.gameObject))
            {
                contactingObjects.Add(other.gameObject);

                // 리스트가 비어있다가 처음으로 오브젝트가 추가된 상황이라면 코루틴 시작
                if (breakRoutine == null)
                {
                    breakRoutine = StartCoroutine(BreakSequenceRoutine());
                }
            }
        }
    }

    // 2. 트리거 범위 밖으로 오브젝트가 나갔을 때
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            contactingObjects.Remove(other.gameObject);

            // 더 이상 범위 안에 물체가 없다면 코루틴 안전하게 종료
            if (contactingObjects.Count == 0 && breakRoutine != null)
            {
                StopCoroutine(breakRoutine);
                breakRoutine = null;
            }
        }
    }

    // 3. 주기적 파괴를 담당하는 코루틴
    private IEnumerator BreakSequenceRoutine()
    {
        while (contactingObjects.Count > 0)
        {
            // 리스트 청소 (다른 요인으로 이미 파괴된 오브젝트 예외 처리)
            CleanUpList();

            if (contactingObjects.Count == 0) break;

            // 이전 파괴로 인한 남은 쿨타임이 있다면 실시간 계산하여 대기
            if (IsOnCooldown)
            {
                float remainingTime = breakCooldown - (Time.time - lastBreakTime);
                yield return new WaitForSeconds(remainingTime);
                continue;
            }

            // 첫 번째 오브젝트 꺼내기
            GameObject target = contactingObjects[0];
            contactingObjects.RemoveAt(0);

            // 파괴 시도 및 시간 기록
            if (target != null &&
                target.TryGetComponent<Iron>(out var iron) &&
                iron.TryMining())
            {
                player.AddItem(ItemType.Iron, Vector3.zero);
                lastBreakTime = Time.time;
            }

            // 파괴 직후 설정된 쿨타임만큼 대기
            yield return new WaitForSeconds(breakCooldown);
        }

        breakRoutine = null;
    }

    /// <summary>
    /// 다른 요인으로 인해 이미 null이 된 오브젝트들을 리스트에서 솎아냅니다.
    /// </summary>
    private void CleanUpList()
    {
        for (int i = contactingObjects.Count - 1; i >= 0; i--)
        {
            if (contactingObjects[i] == null)
            {
                contactingObjects.RemoveAt(i);
            }
        }
    }

    // 도구 해제/비활성화 시 데이터와 코루틴을 깔끔하게 리셋
    private void OnDisable()
    {
        if (breakRoutine != null)
        {
            StopCoroutine(breakRoutine);
            breakRoutine = null;
        }
        contactingObjects.Clear();
    }
}