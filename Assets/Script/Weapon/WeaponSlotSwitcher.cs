using UnityEngine;
using UnityEngine.UI;

// 무기 패널 우측 캐릭터 버튼 3개(파티 슬롯 순) — 누르면 WeaponManager.SelectSlot으로 그 캐릭터의 무기를 패널에 표시.
// 패널은 1개, 내용만 갈아끼움(RevolverSwitcher와 같은 버튼 문법 — 위치 이동은 없음).
public class WeaponSlotSwitcher : MonoBehaviour
{
    [SerializeField] Button[] characterButtons = new Button[CharacterManager.PartySize];
    [SerializeField] Image[] characterIcons = new Image[CharacterManager.PartySize]; // 버튼 초상(선택 연결)
    [SerializeField] Image[] selectFrames = new Image[CharacterManager.PartySize];   // 선택 표시 테두리(선택 연결)

    static readonly Color SelectedFrame = new(1f, 0.8f, 0.2f);
    static readonly Color NormalFrame = new(0.16f, 0.19f, 0.27f);

    bool bound;

    void Awake()
    {
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int idx = i; // 클로저 캡처용 복사
            if (characterButtons[i] != null)
                characterButtons[i].onClick.AddListener(() => Select(idx));
        }
    }

    void Update()
    {
        // 매니저·시트가 준비되는 즉시 1회 바인딩(로드 순서 안전망 — CharacterListPanel과 동일 문법)
        if (!bound) TryBind();
    }

    void TryBind()
    {
        var mgr = CharacterManager.Instance;
        if (mgr == null || !mgr.IsReady) return;
        if (WeaponManager.Instance == null || !WeaponManager.Instance.IsReady) return;

        mgr.onChanged.AddListener(RefreshButtons);
        bound = true;
        RefreshButtons();
        RefreshFrames();
    }

    void OnDestroy()
    {
        if (bound && CharacterManager.Instance != null)
            CharacterManager.Instance.onChanged.RemoveListener(RefreshButtons);
    }

    void Select(int index)
    {
        if (WeaponManager.Instance != null) WeaponManager.Instance.SelectSlot(index);
        RefreshFrames(); // SelectSlot이 잠겨 있으면(비교 미해결) 프레임도 원래 선택을 유지
    }

    void RefreshFrames()
    {
        int selected = WeaponManager.Instance != null ? WeaponManager.Instance.SelectedSlot : 0;
        for (int i = 0; i < selectFrames.Length; i++)
            if (selectFrames[i] != null) selectFrames[i].color = i == selected ? SelectedFrame : NormalFrame;
    }

    // 파티 변경 → 버튼 초상 갱신(빈 슬롯 = 버튼 잠금). 선택 중인 슬롯이 비면 첫 장착 슬롯으로 전환.
    void RefreshButtons()
    {
        var mgr = CharacterManager.Instance;

        for (int i = 0; i < characterButtons.Length; i++)
        {
            string id = mgr.GetPartyMember(i);
            bool occupied = id != null;

            if (characterButtons[i] != null) characterButtons[i].interactable = occupied;

            if (characterIcons[i] != null)
            {
                var sprite = occupied ? mgr.GetSprite(id) : null;
                characterIcons[i].sprite = sprite;
                characterIcons[i].enabled = sprite != null;
            }
        }

        if (WeaponManager.Instance != null && mgr.GetPartyMember(WeaponManager.Instance.SelectedSlot) == null)
            for (int i = 0; i < CharacterManager.PartySize; i++)
                if (mgr.GetPartyMember(i) != null) { Select(i); break; }
    }
}
