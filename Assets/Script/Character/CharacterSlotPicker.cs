using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 배치할 자리 피커(프로토 chPickOpen 포팅) — 상세 팝업의 [배치] 버튼이 연다.
// 슬롯 1~3번 버튼에 현재 배치된 캐릭터를 보여주고, 누르면 그 자리에 장착(이미 다른 자리면 스왑 = 이동).
// 바깥(백드롭) 터치 = 닫기.
public class CharacterSlotPicker : MonoBehaviour
{
    [SerializeField] GameObject view;           // 피커 루트(꺼진 채 시작 — 이 스크립트는 항상 켜진 부모에)
    [SerializeField] Button backdropButton;     // 바깥 터치 = 닫기(화면 전체 투명 버튼)
    [SerializeField] TextMeshProUGUI titleText; // "이름 — 배치할 자리"

    [SerializeField] Button[] slotButtons = new Button[CharacterManager.PartySize];  // 1~3번 자리
    [SerializeField] Image[] slotIcons = new Image[CharacterManager.PartySize];      // 자리의 현재 캐릭터(빈 자리 = 숨김)
    [SerializeField] TextMeshProUGUI[] slotLabels = new TextMeshProUGUI[CharacterManager.PartySize]; // "1번"… (선택 연결)

    string pendingId; // 배치 대상 캐릭터

    void Awake()
    {
        if (backdropButton != null) backdropButton.onClick.AddListener(Close);

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int idx = i; // 클로저 캡처용 복사
            if (slotButtons[i] != null)
                slotButtons[i].onClick.AddListener(() => Pick(idx));
        }

        if (view != null) view.SetActive(false);
    }

    public void Open(string id)
    {
        var mgr = CharacterManager.Instance;
        if (mgr == null || !mgr.IsOwned(id)) return;

        pendingId = id;

        var c = CharacterRosterLoader.Instance.Get(id);
        if (titleText != null) titleText.text = (c != null ? c.nameKo : id) + " — 배치할 자리";

        for (int i = 0; i < slotButtons.Length; i++)
        {
            string slotId = mgr.GetPartyMember(i);

            if (slotIcons.Length > i && slotIcons[i] != null)
            {
                var sprite = slotId != null ? mgr.GetSprite(slotId) : null;
                slotIcons[i].sprite = sprite;
                slotIcons[i].enabled = sprite != null; //빈 자리 = 아이콘 숨김
            }

            if (slotLabels.Length > i && slotLabels[i] != null)
                slotLabels[i].text = (i + 1) + "번";
        }

        if (view != null) view.SetActive(true);
    }

    public void Close()
    {
        if (view != null) view.SetActive(false);
        pendingId = null;
    }

    void Pick(int slot)
    {
        var mgr = CharacterManager.Instance;
        if (mgr != null && pendingId != null)
            mgr.EquipParty(slot, pendingId); //이미 다른 자리에 있으면 스왑(=이동)

        Close();
    }
}
