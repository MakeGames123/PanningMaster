using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 약실 강화 전체 컨트롤러.
// - 6개 슬롯을 초기화하고 클릭 선택을 받아 하단 UI를 갱신
// - 강화 버튼으로 선택 슬롯을 강화(골드 소모)
// 강화 수치의 실제 데미지 반영은 아직 하지 않음(표시용 계산만).
public class ChamberEnforcePanel : MonoBehaviour
{
    [Header("슬롯")]
    [SerializeField] List<ChamberEnforceSlot> slots = new();

    [Header("하단 정보 / 버튼")]
    [SerializeField] CurrentSlotInfo currentSlotInfo;
    [SerializeField] Button enforceButton;

    [Header("자동 강화")]
    [SerializeField] Toggle autoToggle;          // 체크 시 골드 소진까지 자동 강화
    [SerializeField] float autoInterval = 0.1f;  // 자동 강화 시도 주기(초)

    [Header("설정(수치는 임시)")]
    [SerializeField] float baseSuccessRate = 90f;    // 기본 성공 확률
    [SerializeField] float bonusPerFail = 5f;         // 실패 시 다음 확률 +5%
    [SerializeField] float basePenaltyOnSuccess = 5f; // 성공 시 기본 확률 -5%
    [SerializeField] float effectPerLevel = 2f;       // 레벨당 최종 데미지 +2%(표시용)
    [SerializeField] int baseCost = 300;              // 기본 강화 비용
    [SerializeField] int costPerLevel = 100;          // 레벨당 비용 증가

    ChamberEnforceSlot selected;
    Coroutine autoRoutine;

    void Awake()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;
            slots[i].Init(i, baseSuccessRate);
            slots[i].OnClicked = Select;
        }

        if (enforceButton != null) enforceButton.onClick.AddListener(Enhance);
        if (autoToggle != null) autoToggle.onValueChanged.AddListener(OnAutoToggle);
    }

    void Start()
    {
        if (slots.Count > 0 && slots[0] != null) Select(slots[0]); // 기본 1번 슬롯 선택
    }

    void OnEnable()
    {
        if (autoToggle != null) autoToggle.isOn = false; // 패널이 켜질 때 자동 강화 해제
    }

    void Select(ChamberEnforceSlot slot)
    {
        if (selected != null) selected.SetSelected(false);
        selected = slot;
        if (selected != null) selected.SetSelected(true);

        RefreshInfo();
    }

    void Enhance()
    {
        if (selected == null) return;

        int cost = GetCost(selected);

        // 골드 소모(대충): DataManager가 있으면 차감, 부족하면 중단
        if (DataManager.Instance != null)
        {
            if (!DataManager.Instance.Gold.Use(GoldUseType.ChamberEnforce, cost)) return;
            //DataManager.Instance.Gold.GoldUseReq(GoldUseType.ChamberEnforce, cost);
        }

        // 성공 시 확률 -5%, 실패 시 다음 확률 +5% (레벨 유지)
        if (selected.TryEnhance(bonusPerFail, basePenaltyOnSuccess))
        {
            if (QuestEventManager.Instance != null) QuestEventManager.Instance.AddEvent("enhAny"); //업적: 강화 성공
        }
        // NOTE: 강화 수치의 실제 데미지 반영은 아직 하지 않음

        RefreshInfo();
    }

    void OnAutoToggle(bool on)
    {
        if (on)
        {
            if (autoRoutine == null) autoRoutine = StartCoroutine(AutoEnhance());
        }
        else if (autoRoutine != null)
        {
            StopCoroutine(autoRoutine);
            autoRoutine = null;
        }
    }

    // 체크되어 있는 동안 주기적으로 강화 시도. 골드가 부족해지면 체크 해제하고 중단.
    IEnumerator AutoEnhance()
    {
        WaitForSeconds wait = new(autoInterval);

        while (true)
        {
            if (selected == null || GetOwnedGold() < GetCost(selected))
            {
                if (autoToggle != null) autoToggle.isOn = false; // 골드 부족 -> 체크 해제(코루틴 정지)
                yield break;
            }

            Enhance();
            yield return wait;
        }
    }

    int GetCost(ChamberEnforceSlot slot) => baseCost + slot.Level * costPerLevel;

    long GetOwnedGold() => DataManager.Instance != null ? DataManager.Instance.Gold.GetValue() : 0;

    void RefreshInfo()
    {
        if (selected == null) return;

        int cost = GetCost(selected);
        long owned = GetOwnedGold();

        if (currentSlotInfo != null)
            currentSlotInfo.Show(selected, effectPerLevel, cost, owned);

        if (enforceButton != null)
            enforceButton.interactable = DataManager.Instance == null || owned >= cost;
    }
}
