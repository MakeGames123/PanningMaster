using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

public class Machine : MonoBehaviour
{
    [Header("Max Texts")]
    [SerializeField] private GameObject handCuffMaxText;
    [SerializeField] private GameObject ironMaxText;

    [Header("Inventory Stacks")]
    [SerializeField] private InventoryStack ironStack;  // 철광석 비주얼 스택
    [SerializeField] private InventoryStack handcuffStack; // 수갑 스택

    [Header("Object Settings")]
    [SerializeField] private int maxHandcuffCount = 10;       // 보관 가능한 최대 수갑 개수
    [SerializeField] private int maxIronOreCount = 30;       // 보관 가능한 최대 수갑 개수

    [Header("Crafting Settings")]
    [SerializeField] private float craftInterval = 2.0f;     // 수갑 제작 걸리는 시간 (초)
    private Coroutine craftRoutine;

    private Dictionary<ItemType, ItemData> itemInventory = new Dictionary<ItemType, ItemData>();
    public UnityEvent<int, int> OnIronCountChanged;
    public UnityEvent<int, int> OnHandcuffCountChanged;
    private void Awake()
    {
        // 딕셔너리 초기화 및 최대 수치 세팅
        itemInventory.Add(ItemType.Iron, new ItemData("철광석", maxIronOreCount, ironStack));
        itemInventory.Add(ItemType.Handcuff, new ItemData("수갑", maxHandcuffCount, handcuffStack));

        itemInventory[ItemType.Handcuff].OnCountChanged += OnHandCuffChanged;
        itemInventory[ItemType.Iron].OnCountChanged += OnIronChanged;
    }
    private void OnHandCuffChanged(int currentCount, int maxCount)
    {
        TryStartCraftRoutine();

        handCuffMaxText.SetActive(currentCount >= maxCount);

        OnHandcuffCountChanged?.Invoke(currentCount, maxCount);
    }
    private void OnIronChanged(int currentCount, int maxCount)
    {
        TryStartCraftRoutine();

        ironMaxText.SetActive(currentCount >= maxCount);

        OnIronCountChanged?.Invoke(currentCount, maxCount);
    }
    /// <summary>
    /// 철광석이 있고, 수갑이 가득 찬 게 아니며, 코루틴이 안 돌고 있다면 시작
    /// </summary>
    private void TryStartCraftRoutine()
    {
        if (CanCraft() && craftRoutine == null)
        {
            craftRoutine = StartCoroutine(CraftHandcuffsRoutine());
        }
    }
    /// <summary>
    /// 철광석 개수 체크
    /// </summary>
    public bool CheckIronCount()
    {
        return !(itemInventory[ItemType.Iron].CurrentCount >= maxIronOreCount);
    }
    /// <summary>
    /// 외부에서 철광석을 이 머신에 넣어줄 때 호출
    /// </summary>
    public void AddIronOre()
    {
        itemInventory[ItemType.Iron].Add(1);
    }

    /// <summary>
    /// 수갑 가져오기
    /// </summary>
    public bool TryGetHandcuff()
    {
        return itemInventory[ItemType.Handcuff].Remove(1);
    }

    /// <summary>
    /// 수갑을 제작하는 반복 코루틴
    /// </summary>
    private IEnumerator CraftHandcuffsRoutine()
    {
        // 철광석이 남아있고, 수갑이 최대 보관 개수 미만일 때만 루프 작동
        while (CanCraft())
        {
            // 설정된 시간만큼 제작 대기
            yield return new WaitForSeconds(craftInterval);

            // 대기하는 시간 사이에 조건이 변했을 수 있으므로 다시 한번 안전장치 체크
            if (!CanCraft()) break;

            // 2. 철광석 데이터 감소 
            itemInventory[ItemType.Iron].Remove(1);
            // 3. 수갑 변수 증가 
            itemInventory[ItemType.Handcuff].Add(1);

            //Debug.Log($"[Machine] 수갑 제작 완료! (수갑: {HandcuffCount}/{maxHandcuffCount} | 남은 철광석: {IronOreCount})");
        }

        // 루프를 탈출했다는 것은 철광석이 없거나, 수갑이 가득 찼다는 뜻이므로 코루틴 참조 해제
        craftRoutine = null;
    }
    private bool CanCraft()
    {
        return itemInventory[ItemType.Iron].CurrentCount > 0
            && itemInventory[ItemType.Handcuff].CurrentCount < itemInventory[ItemType.Handcuff].MaxCount;
    }

    private void OnDisable()
    {
        if (craftRoutine != null)
        {
            StopCoroutine(craftRoutine);
            craftRoutine = null;
        }
    }
}