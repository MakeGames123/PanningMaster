using UnityEngine;
using UnityEngine.UI;

// 리볼버 전환 스트립 — 리볼버 우측의 캐릭터 버튼 3개(파티 슬롯 순).
// 버튼 i를 누르면 사수 i의 리볼버 UI를 제자리에 보여주고, 나머지는 화면 밖으로 치운다.
// 비활성화(SetActive)가 아니라 위치 이동 — 리볼버의 리스너·상태를 살려두기 위함.
public class RevolverSwitcher : MonoBehaviour
{
    [SerializeField] RectTransform[] revolverRoots = new RectTransform[CharacterManager.PartySize]; // 사수 0~2 리볼버 UI 루트(에디터에서 전부 같은 위치에 배치)
    [SerializeField] Button[] characterButtons = new Button[CharacterManager.PartySize];
    [SerializeField] Image[] characterIcons = new Image[CharacterManager.PartySize]; // 버튼 초상(선택 연결)
    [SerializeField] Image[] selectFrames = new Image[CharacterManager.PartySize];   // 선택 표시 테두리(선택 연결)

    static readonly Vector2 HiddenOffset = new(100000f, 0f); // 안 보이는 곳
    static readonly Color SelectedFrame = new(1f, 0.8f, 0.2f);
    static readonly Color NormalFrame = new(0.16f, 0.19f, 0.27f);

    Vector2 shownPos;
    int selected;
    bool bound;

    void Awake()
    {
        // 보이는 자리 = 0번 리볼버가 에디터에서 놓인 위치
        if (revolverRoots.Length > 0 && revolverRoots[0] != null)
            shownPos = revolverRoots[0].anchoredPosition;

        for (int i = 0; i < characterButtons.Length; i++)
        {
            int idx = i; // 클로저 캡처용 복사
            if (characterButtons[i] != null)
                characterButtons[i].onClick.AddListener(() => Select(idx));
        }

        Select(0);
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

        mgr.onChanged.AddListener(RefreshButtons);
        bound = true;
        RefreshButtons();
    }

    void OnDestroy()
    {
        if (bound && CharacterManager.Instance != null)
            CharacterManager.Instance.onChanged.RemoveListener(RefreshButtons);
    }

    public void Select(int index)
    {
        selected = index;

        for (int i = 0; i < revolverRoots.Length; i++)
        {
            if (revolverRoots[i] == null) continue;
            revolverRoots[i].anchoredPosition = i == index ? shownPos : shownPos + HiddenOffset;
        }

        for (int i = 0; i < selectFrames.Length; i++)
            if (selectFrames[i] != null) selectFrames[i].color = i == index ? SelectedFrame : NormalFrame;
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

        if (mgr.GetPartyMember(selected) == null)
            for (int i = 0; i < CharacterManager.PartySize; i++)
                if (mgr.GetPartyMember(i) != null) { Select(i); break; }
    }
}
