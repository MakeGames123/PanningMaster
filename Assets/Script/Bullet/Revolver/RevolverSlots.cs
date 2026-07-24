using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Events;
using Unity.Android.Gradle.Manifest;
public class RevolverSlots : MonoBehaviour
{
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

        DataManager.Instance.UpdatePower(ComputePower());
    }

    //현재 장착 탄환/전역 스탯 기준 전투력 계산(상태 변경 없음 — 무기 비교 시뮬레이션에서도 사용)
    public float ComputePower()
    {
        if (revolverSlotContents.Count == 0) return 0; // 아직 Initialize 전 — 계산 불가

        List<BulletInfo> revolverInfo = new();
        foreach (RevolverSlotContent content in revolverSlotContents)
            revolverInfo.Add(AllBulletList.Instance.GetBullet(content.id));

        DamageModifier mod = calculator.CollectModifiers(revolverInfo);
        float power = 0;
        for (int i = 0; i < revolverInfo.Count; i++) 
            power += calculator.CalculateDamage(revolverInfo[i], mod, i, PlayerData.Instance.PossPower).Item1;

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
    //자동장착: 슬롯을 전부 비운 뒤 count 1 이상인 탄환을 id 높은 순으로 슬롯 수만큼 장착(모자라면 있는 만큼만)
    public void AutoEquip()
    {
        suppressPowerUpdate = true; //일괄 변경 동안 전투력 연쇄 갱신 억제

        Inventory inventory = AllBulletList.Instance.inventory;

        //1. 슬롯 비우기 + 기존 장착 탄환은 인벤토리로 되돌림(active true)
        foreach (RevolverSlotContent content in revolverSlotContents)
        {
            if (!content.IsEmpty) inventory.SetActive(content.id, true);
            content.UpdateBulletInfo(-1);
        }

        //2. count 1 이상인 탄환을 id 내림차순으로 슬롯 수만큼
        List<BulletInfo> candidates = AllBulletList.Instance.bulletInfos.Values
            .Where(b => b.Count >= 1)
            .OrderByDescending(b => b.infoSO.bulletId)
            .Take(revolverSlotContents.Count)
            .ToList();

        //3. 장착 + 해당 인벤토리 슬롯은 장착중 표시(active false)
        for (int i = 0; i < candidates.Count; i++)
        {
            int id = candidates[i].infoSO.bulletId;
            revolverSlotContents[i].UpdateBulletInfo(id);
            inventory.SetActive(id, false);
        }

        suppressPowerUpdate = false;
        CheckSlots(); //전투력은 마지막에 한 번만 갱신

        //자동장착 → 퀘스트·튜토리얼 각각에 autoEq 발행
        if (QuestEventManager.Instance != null)
            QuestEventManager.Instance.AddEvent("autoEq");
        if (TutorialEventManager.Instance != null)
            TutorialEventManager.Instance.AddEvent("autoEq");
    }
}
