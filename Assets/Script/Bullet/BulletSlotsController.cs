using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BulletSlotsController : MonoBehaviour
{
    [SerializeField] Inventory inventory;
    [SerializeField] RevolverSlots[] revolvers; // 사수별 전용 리볼버 3개(파티 슬롯 0~2와 1:1, 공유 안 함)
    BulletSlotsRayController rayController = new();
    DamageCalculator calculator = new(); // 자동장착 점수 계산용

    // Awake가 아니라 Start: 슬롯 초기화가 AllBulletList.Instance(Awake에서 세팅)를 참조하므로
    // 모든 Awake가 끝난 뒤에 돌아야 실행 순서가 보장된다
    void Start()
    {
        foreach (RevolverSlots revolver in revolvers)
            if (revolver != null) revolver.Initialize(rayController);
        inventory.Initialize(rayController);
    }

    // 자동장착(전 파티 일괄 분배) — 구 리볼버별 자동장착 대체(같은 탄환 중복 장착 문제 해결).
    // 1) 모든 리볼버를 비워 탄환을 전부 인벤토리로 반납(비활성 슬롯 포함 — 중복 원천 차단)
    // 2) 배치 중인 캐릭터의 슬롯에만 분배: 점수 = 탄환 단독 전투력(그 슬롯 무기 속성 매칭 반영)
    //    × 그 캐릭터 화력 배수(PowerMulOf) — (탄환×슬롯) 쌍을 점수 내림차순 그리디 배정
    public void AutoEquipAll()
    {
        var cm = CharacterManager.Instance;
        if (cm == null || !cm.IsReady) return;
        if (AllBulletList.Instance == null) return;

        // 배치 중인 캐릭터의 슬롯만 분배 대상(리볼버 인덱스 = 파티 슬롯)
        List<int> activeSlots = new();
        for (int i = 0; i < revolvers.Length; i++)
            if (revolvers[i] != null && cm.GetPartyMember(i) != null) activeSlots.Add(i);
        if (activeSlots.Count == 0) return;

        // 1. 전 리볼버 반납(비활성 슬롯 포함) — 이전 중복 장착 상태도 여기서 정리된다
        foreach (var revolver in revolvers)
            if (revolver != null) revolver.SetLoadout(null, inventory);

        // 2. 후보 탄환 + (탄환×슬롯) 점수표
        List<BulletInfo> candidates = AllBulletList.Instance.bulletInfos.Values
            .Where(b => b.Count >= 1)
            .ToList();
        if (candidates.Count == 0) return;

        float poss = PlayerData.Instance != null ? PlayerData.Instance.PossPower : 0f;

        var pairs = new List<(int bulletIdx, int slot, float score)>();
        foreach (int slot in activeSlots)
        {
            float charMul = cm.PowerMulOf(cm.GetPartyMember(slot));
            var weapon = WeaponManager.Instance != null ? WeaponManager.Instance.GetEquipped(slot) : null;

            for (int b = 0; b < candidates.Count; b++)
            {
                // 탄환 단독 전투력(세공은 자기 전용이라 다른 약실과 독립) — 무기 속성 매칭까지 반영
                var solo = new List<BulletInfo> { candidates[b], null, null, null, null, null };
                DamageModifier mod = calculator.CollectModifiers(solo);
                float power = calculator.CalculateDamage(candidates[b], mod, 0, poss, weapon).Item1;

                pairs.Add((b, slot, power * charMul));
            }
        }

        // 3. 점수 내림차순 그리디: 탄환은 1곳에만, 슬롯은 약실 수만큼
        pairs.Sort((a, b) => b.score.CompareTo(a.score));

        var assigned = new Dictionary<int, List<int>>(); // 슬롯 → 장착할 탄환 id 목록
        var capacity = new Dictionary<int, int>();       // 슬롯 → 남은 약실 수
        foreach (int slot in activeSlots)
        {
            assigned[slot] = new List<int>();
            capacity[slot] = revolvers[slot].revolverSlotContents.Count;
        }

        var usedBullets = new HashSet<int>();
        foreach (var (bulletIdx, slot, _) in pairs)
        {
            if (usedBullets.Contains(bulletIdx) || capacity[slot] <= 0) continue;

            usedBullets.Add(bulletIdx);
            assigned[slot].Add(candidates[bulletIdx].infoSO.bulletId);
            capacity[slot]--;
        }

        // 4. 슬롯별 장착 확정
        foreach (int slot in activeSlots)
            revolvers[slot].SetLoadout(assigned[slot], inventory);

        // 자동장착 → 퀘스트·튜토리얼 각각에 autoEq 발행(일괄 1회)
        if (QuestEventManager.Instance != null) QuestEventManager.Instance.AddEvent("autoEq");
        if (TutorialEventManager.Instance != null) TutorialEventManager.Instance.AddEvent("autoEq");
    }
}
