using UnityEngine;

// 파티 슬롯(0~2) ↔ 씬의 Player 3개를 1:1로 묶어, 캐릭터가 장착된 슬롯의 Player만 활성화한다.
// 스타터가 슬롯 0에 자동 배치되므로 시작 시 1번 Player는 항상 켜진다.
// 탄환은 공유 — 세 Player의 revolver에 같은 RevolverSlots를 연결(인스펙터).
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
        bound = true;
        Refresh();
    }

    void OnDestroy()
    {
        if (bound && CharacterManager.Instance != null)
            CharacterManager.Instance.onChanged.RemoveListener(Refresh);
    }

    void Refresh()
    {
        var mgr = CharacterManager.Instance;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;

            bool active = mgr.GetPartyMember(i) != null;
            if (players[i].gameObject.activeSelf != active)
                players[i].gameObject.SetActive(active);
        }
    }
}
