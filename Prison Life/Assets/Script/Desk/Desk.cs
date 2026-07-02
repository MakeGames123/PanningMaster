using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Desk : MonoBehaviour
{
    [SerializeField] private PrisonerLine prisonerLine;  // 수감자들 라인
    [SerializeField] private HandCuffBubbleUI bubbleUI;  // 카운트
    [SerializeField] private Vector3 cuffPos;  // 수갑위치
    [SerializeField] private Vector3 prisonerPos;  // 수갑 줄 죄수 위치

    [Header("Inventory Stacks")]
    [SerializeField] private InventoryStack handcuffStack;  // 수갑 비주얼 스택
    [SerializeField] private InventoryStack moneyStack;     // 돈 비주얼 스택

    [Header("Exchange Settings")]
    [SerializeField] private int maxHandcuff = 20;     // 최대 스택
    [SerializeField] private int handcuffsPerMoney = 4;     // 돈 1개를 만들기 위한 수갑 필요 개수
    [SerializeField] private float takeInterval = 0.1f;     // 수갑을 하나씩 가져가는 시간 간격
    [SerializeField] private float prisonerInterval = 1f;     // 죄수들이 바뀌는 간격

    private Dictionary<ItemType, ItemData> itemInventory = new Dictionary<ItemType, ItemData>();
    // [핵심] 돈을 만들기 위해 현재 책상에 쌓인 수갑 개수

    public UnityEvent<int> onHandCuffChanged = new();
    private int accumulatedHandcuffs = 0;
    private Coroutine exchangeRoutine;

    void Awake()
    {
        // 딕셔너리 초기화 및 최대 수치 세팅
        itemInventory.Add(ItemType.Money, new ItemData("돈", 9999, moneyStack));
        itemInventory.Add(ItemType.Handcuff, new ItemData("수갑", maxHandcuff, handcuffStack));

        itemInventory[ItemType.Handcuff].OnCountChanged += TryStartExchange;
    }
    private void TryStartExchange(int count, int maxCount)
    {
        if (count > 0 && exchangeRoutine == null)
        {
            exchangeRoutine = StartCoroutine(ProcessHandcuffsRoutine());
        }
    }
    /// <summary>
    /// 외부에서 수갑 추가
    /// </summary>
    public void AddHandcuff()
    {
        itemInventory[ItemType.Handcuff].Add(1);
        onHandCuffChanged.Invoke(itemInventory[ItemType.Handcuff].CurrentCount);
    }
    /// <summary>
    /// 돈 가져오기
    /// </summary>
    public bool TryGetMoney()
    {
        return itemInventory[ItemType.Money].Remove(1);
    }

    /// <summary>
    /// 수갑을 1개씩 순차적으로 소모하며 돈을 조립하는 코루틴
    /// </summary>
    private IEnumerator ProcessHandcuffsRoutine()
    {
        // 책상 스택에 수갑이 남아있는 동안 계속 작동
        while (itemInventory[ItemType.Handcuff].CurrentCount > 0)
        {
            // 1. 가져가는 간격만큼 대기 (예: 0.5초마다 1개씩 쏙쏙 가져감)
            yield return new WaitForSeconds(takeInterval);

            // 대기 시간 직후 수갑이 정말 남아있는지 재체크
            if (itemInventory[ItemType.Handcuff].CurrentCount <= 0) break;

            // 2. 수갑을 스택에서 1개 차감하고 비주얼도 1개 제거
            itemInventory[ItemType.Handcuff].Remove(1);
            MovingItemPool.Instance.Spawn(ItemType.Handcuff, cuffPos, prisonerPos);
            onHandCuffChanged.Invoke(itemInventory[ItemType.Handcuff].CurrentCount);

            // 3. 내부 작업대로 수갑 1개 누적
            accumulatedHandcuffs++;
            bubbleUI.UpdateUI(handcuffsPerMoney, accumulatedHandcuffs);
            //Debug.Log($"[Desk] 수갑 1개 흡수 중... (현재 누적: {accumulatedHandcuffs} / {handcuffsPerMoney})");

            // 4. 누적된 수갑이 4개가 되었는지 확인
            if (accumulatedHandcuffs >= handcuffsPerMoney)
            {
                // 돈 1개 생산 및 비주얼 추가
                itemInventory[ItemType.Money].Add(4);
                prisonerLine.PopObject();

                // 누적 카운트 초기화 (혹시 모를 오버플로우 방지로 빼기 연산)
                accumulatedHandcuffs -= handcuffsPerMoney;

                bubbleUI.gameObject.SetActive(false);
                //Debug.Log($"<color=green>[Desk] 수갑 4개 도달! 돈 1개 생산 완료! (총 보유 금액: {MoneyCount})</color>");

                //다음 죄수 기다리기
                yield return new WaitForSeconds(prisonerInterval);

                bubbleUI.gameObject.SetActive(true);
                bubbleUI.UpdateUI(handcuffsPerMoney, accumulatedHandcuffs);
            }
        }

        // 스택에 있던 수갑을 다 쓰면 코루틴 종료 (남은 수갑 개수는 다음 수갑이 들어올 때까지 유지)
        exchangeRoutine = null;
    }

    private void OnDisable()
    {
        if (exchangeRoutine != null)
        {
            StopCoroutine(exchangeRoutine);
            exchangeRoutine = null;
        }
    }
}