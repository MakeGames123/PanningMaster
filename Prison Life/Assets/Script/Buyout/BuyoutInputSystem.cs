using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BuyoutInputSystem : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] protected string targetTag = "Player"; // 감지할 태그 이름

    [Header("Interval Settings")]
    [SerializeField] protected float interval = 0.05f;       // 코루틴 반복 주기 (초)
    
    [Header("Protection Settings")]
    [SerializeField] private float protectionDuration = 1.0f; // [추가] 오브젝트 켜진 후 구매 불가 보호 시간 (초)

    [SerializeField] protected BuyoutSlot buyoutSlot;
    protected Coroutine routine;
    protected Player player;

    // [추가] 보호 기간이 끝나는 시점을 기록할 타임스탬프
    private float protectionEndTime;

    void OnEnable()
    {
        // [추가] 활성화된 현재 시간 기준으로 보호 기간 설정 (예: 현재시간 + 1초)
        protectionEndTime = Time.time + protectionDuration;
    }

    // 트리거 범위 안으로 무언가 들어왔을 때
    private void OnTriggerEnter(Collider other)
    {
        // 들어온 오브젝트의 태그가 Player 인지 확인
        if (other.CompareTag(targetTag))
        {
            // player 캐싱
            if (player == null && other.TryGetComponent<Player>(out var playerScript))
            {
                player = playerScript;
            }

            // 혹시 이미 코루틴이 돌고 있다면 중복 실행 방지 차원에서 종료 후 재시작
            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(ProcessRoutine());
        }
    }

    // 트리거 범위 밖으로 나갔을 때
    private void OnTriggerExit(Collider other)
    {
        // 나간 오브젝트의 태그가 Player 인지 확인
        if (other.CompareTag(targetTag))
        {
            // 안전하게 코루틴을 종료하고 참조를 초기화
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }
    }

    // 주기에 맞춰 반복 실행될 코루틴
    private IEnumerator ProcessRoutine()
    {
        // [추가] 현재 시간이 보호 종료 시점보다 작다면, 그 차이만큼 기다렸다가 돈을 뺍니다.
        if (Time.time < protectionEndTime)
        {
            float remainingProtectionTime = protectionEndTime - Time.time;
            yield return new WaitForSeconds(remainingProtectionTime);
        }

        int count = buyoutSlot.GetRemainingPrice();

        while (count-- > 0)
        {
            if (player.GetItem(ItemType.Money, transform.position)) buyoutSlot.AddMoney();
            // 인스펙터에서 설정한 interval 시간만큼 대기
            yield return new WaitForSeconds(interval);
        }
    }

    // 오브젝트가 비활성화되거나 파괴될 때 코루틴이 유령처럼 남아있지 않도록 예외 처리
    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }
}