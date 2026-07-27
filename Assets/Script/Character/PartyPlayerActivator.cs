using UnityEngine;

// 파티 슬롯(0~2) ↔ 씬의 Player 3개를 1:1로 묶어, 캐릭터가 장착된 슬롯의 Player만 활성화한다.
// 스타터가 슬롯 0에 자동 배치되므로 시작 시 1번 Player는 항상 켜진다.
// 리볼버는 사수별 전용 — 각 Player의 revolver에 서로 다른 RevolverSlots를 연결(인스펙터).
// 전투력 합산도 여기서 담당: Σ(장착 슬롯의 리볼버 전투력 × 등급 PowerMul) — 파티 변경·탄환 변경 양쪽에서 재합산.
public class PartyPlayerActivator : MonoBehaviour
{
    [SerializeField] Player[] players = new Player[CharacterManager.PartySize]; // 파티 슬롯 순서대로 0~2

    bool bound;

    void Update()
    {
        // 매니저·시트가 준비되는 즉시 1회 바인딩(로드 순서 안전망 — CharacterListPanel과 동일 문법)
        if (!bound) TryBind();
    }

    void TryBind()
    {
        var mgr = CharacterManager.Instance;
        if (mgr == null || !mgr.IsReady) return;

        mgr.onChanged.AddListener(Refresh);

        foreach (var p in players)
            if (p != null && p.Revolver != null)
                p.Revolver.onChanged += RefreshPower; //탄환 장착/해제 → 전투력 재합산

        bound = true;
        Refresh();
    }

    void OnDestroy()
    {
        if (!bound) return;

        if (CharacterManager.Instance != null)
            CharacterManager.Instance.onChanged.RemoveListener(Refresh);

        foreach (var p in players)
            if (p != null && p.Revolver != null)
                p.Revolver.onChanged -= RefreshPower;
    }

    void Refresh()
    {
        var mgr = CharacterManager.Instance;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;

            string id = mgr.GetPartyMember(i);

            bool active = id != null;
            if (players[i].gameObject.activeSelf != active)
                players[i].gameObject.SetActive(active);

            //화력 배수 주입(등급 PowerMul × 레벨 성장 × ⚔️패시브 — 그 사수의 탄환 데미지에만 곱)
            players[i].PowerMul = id != null ? mgr.PowerMulOf(id) : 1f;
        }

        RefreshPower(); //파티가 바뀌면 전투력도 재합산
    }

    //슬롯 1칸의 개별 전투력 = 그 리볼버 전투력 × 화력 배수(미장착·미배선 = 0). 상세 팝업 표시용
    public float SlotPower(int slot)
    {
        var mgr = CharacterManager.Instance;
        if (mgr == null || !mgr.IsReady) return 0f;
        if (slot < 0 || slot >= players.Length || players[slot] == null || players[slot].Revolver == null) return 0f;

        string id = mgr.GetPartyMember(slot);
        if (id == null) return 0f;

        return players[slot].Revolver.ComputePower() * mgr.PowerMulOf(id);
    }

    //전투력 = Σ(장착 슬롯의 개별 전투력). 빈 슬롯의 리볼버는 제외
    void RefreshPower()
    {
        var mgr = CharacterManager.Instance;
        if (mgr == null || !mgr.IsReady || DataManager.Instance == null) return;

        float total = 0;
        for (int i = 0; i < players.Length; i++)
            total += SlotPower(i);

        DataManager.Instance.UpdatePower(total);
    }
}
