using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Events;
public class RevolverSlots : MonoBehaviour
{
    // 슬롯 구성이 바뀌었을 때 통지(장착/해제/자동장착 등) — 전투력 합산은 PartyPlayerActivator가 담당
    public event Action onChanged;

    [SerializeField] List<Transform> revolverSlotTransforms = new();
    public List<RevolverSlotContent> revolverSlotContents { get; private set; } = new();
    List<RevolverSlotUI> slotUIs = new();
    List<BulletSlotDrag> slotDrags = new();
    DamageCalculator calculator = new();
    int slotNum;
    bool suppressPowerUpdate; //자동장착 등 일괄 변경 중 전투력 연쇄 갱신 억제
    public void Initialize(BulletSlotsRayController rayController)
    {
        slotNum = revolverSlotTransforms.Count;

        for (int i = 0; i < slotNum; i++)
        {
            slotUIs.Add(revolverSlotTransforms[i].GetComponent<RevolverSlotUI>());
            slotDrags.Add(revolverSlotTransforms[i].GetComponent<BulletSlotDrag>());

            RevolverSlotContent content = new RevolverSlotContent(i, slotDrags[i], slotUIs[i]);
            revolverSlotContents.Add(content);
            content.RefreshUI();

            AllBulletList.Instance.onBulletChanged.AddListener(content.RefreshInfo);
            rayController.AddSlot(content);

            slotDrags[i].Initialize(content);

            content.onInfoChanged += (val1, val2) => CheckSlots();
        }

    }
    public void CheckSlots()
    {
        if (suppressPowerUpdate) return; //일괄 변경 중에는 마지막에 한 번만 갱신

        onChanged?.Invoke();
    }

    //현재 장착 탄환/전역 스탯 기준 전투력 계산(상태 변경 없음 — 무기 비교 시뮬레이션에서도 사용)
    //weapon = 이 리볼버 사수(파티 슬롯)의 장착 무기 — v43 속성 매칭 반영(null=매칭 없음)
    public float ComputePower(WeaponData weapon = null)
    {
        if (revolverSlotContents.Count == 0) return 0; // 아직 Initialize 전 — 계산 불가

        List<BulletInfo> revolverInfo = new();
        foreach (RevolverSlotContent content in revolverSlotContents)
            revolverInfo.Add(AllBulletList.Instance.GetBullet(content.id));

        DamageModifier mod = calculator.CollectModifiers(revolverInfo);
        float power = 0;
        for (int i = 0; i < revolverInfo.Count; i++)
            power += calculator.CalculateDamage(revolverInfo[i], mod, i, PlayerData.Instance.PossPower, weapon).Item1;

        return power;
    }
    public bool CheckEmpty()
    {
        foreach (RevolverSlotContent content in revolverSlotContents)
        {
            if (!content.IsEmpty) return false;
        }

        return true;
    }
    //자동장착 버튼 호환용 — 전 리볼버 일괄 분배(BulletSlotsController)로 위임.
    //(구) 리볼버 1개만 채우던 방식은 다른 리볼버와 같은 탄환이 중복 장착되는 문제로 폐지.
    public void AutoEquip()
    {
        var controller = FindAnyObjectByType<BulletSlotsController>();
        if (controller != null) controller.AutoEquipAll();
    }

    //이 리볼버의 로드아웃을 통째로 교체: 전부 비우고(인벤토리 반납) 주어진 id 순서대로 장착.
    //전투력 연쇄 갱신은 억제하고 마지막에 1회만. ids가 null/빈 리스트면 비우기만 한다.
    public void SetLoadout(List<int> bulletIds, Inventory inventory)
    {
        suppressPowerUpdate = true;

        foreach (RevolverSlotContent content in revolverSlotContents)
        {
            if (!content.IsEmpty) inventory.SetActive(content.id, true);
            content.UpdateBulletInfo(-1);
        }

        if (bulletIds != null)
        {
            int n = Mathf.Min(bulletIds.Count, revolverSlotContents.Count);
            for (int i = 0; i < n; i++)
            {
                revolverSlotContents[i].UpdateBulletInfo(bulletIds[i]);
                inventory.SetActive(bulletIds[i], false);
            }
        }

        suppressPowerUpdate = false;
        CheckSlots();
    }
}
