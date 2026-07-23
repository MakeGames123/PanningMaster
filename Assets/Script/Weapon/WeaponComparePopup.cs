using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 무기 비교 팝업(신규 vs 현재 → 택1). WeaponManager.onPending을 직접 구독해 스스로 열리고,
// 버튼 선택(장착/판매)을 매니저에 위임한 뒤 닫힌다. 기본 패널(WeaponDrawPanel)과 독립.
// ※ 이 컴포넌트는 항상 활성인 오브젝트에 두고, 켜고 끄는 것은 popupRoot로 한다(Update 구독 재시도 유지).
public class WeaponComparePopup : MonoBehaviour
{
    [SerializeField] GameObject popupRoot;         // 팝업 루트(켜고 끔)
    [SerializeField] TextMeshProUGUI newNameText;  // 신규 무기 이름(등급색)
    [SerializeField] TextMeshProUGUI newInfoText;  // 신규 무기 스탯(주스탯+부옵)
    [SerializeField] TextMeshProUGUI curNameText;  // 현재 무기 이름(등급색)
    [SerializeField] TextMeshProUGUI curInfoText;  // 현재 무기 스탯
    [SerializeField] TextMeshProUGUI curPowerText; // 현재 전투력 "⚔️ 12.3천" (현재 무기 쪽)
    [SerializeField] TextMeshProUGUI newPowerText; // 장착 시 전투력 "⚔️ 13.1천 (+800)" (신규 무기 쪽, 변동량 색상)
    [SerializeField] Button equipButton;           // 신규 장착(기존 판매)
    [SerializeField] Button keepButton;            // 현재 유지(신규 판매)
    [SerializeField] Button rerollButton;          // 리롤: 신규 즉시 판매 + 새로 뽑기(🧰 1)

    bool subscribed;

    void Awake()
    {
        if (equipButton != null) equipButton.onClick.AddListener(OnEquipClicked);
        if (keepButton != null) keepButton.onClick.AddListener(OnKeepClicked);
        if (rerollButton != null) rerollButton.onClick.AddListener(OnRerollClicked);

        if (popupRoot != null) popupRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (subscribed && WeaponManager.Instance != null)
            WeaponManager.Instance.onPending.RemoveListener(Show);
    }

    void Update()
    {
        // Awake 시점엔 WeaponManager 싱글톤이 아직 없을 수 있어 구독 재시도
        if (!subscribed) TrySubscribe();
    }

    void TrySubscribe()
    {
        if (subscribed) return;
        if (WeaponManager.Instance == null) return;

        WeaponManager.Instance.onPending.AddListener(Show);
        subscribed = true;

        if (WeaponManager.Instance.Pending != null) // 이미 비교 대기 중이면 즉시 표시
            Show(WeaponManager.Instance.Equipped, WeaponManager.Instance.Pending);
    }

    // 비교 대상 표시(onPending 콜백)
    public void Show(WeaponData cur, WeaponData pend)
    {
        var mgr = WeaponManager.Instance;
        if (mgr == null || pend == null) return;

        if (popupRoot != null) popupRoot.SetActive(true);

        if (newNameText != null)
        {
            newNameText.text = WeaponGrades.DisplayName(pend);
            newNameText.color = WeaponGrades.Color(pend.grade);
        }
        if (newInfoText != null) newInfoText.text = WeaponGrades.InfoText(pend);

        if (curNameText != null)
        {
            curNameText.text = WeaponGrades.DisplayName(cur);
            curNameText.color = WeaponGrades.Color(cur.grade);
        }
        if (curInfoText != null) curInfoText.text = WeaponGrades.InfoText(cur);

        // 신규 장착 시 전투력 변동 미리보기(실제 장착 없이 시뮬레이션) — 현재/신규 분리 표시
        if (curPowerText != null || newPowerText != null)
        {
            float curPower = mgr.PowerCurrent();
            float newPower = mgr.PowerWith(pend);
            float diff = newPower - curPower;

            if (curPowerText != null)
                curPowerText.text = $"⚔️ {NumberFormatLoader.Abbrev(curPower)}";

            if (newPowerText != null)
            {
                string color = diff > 0 ? "#5eff7a" : diff < 0 ? "#ff6a6a" : "#8a93a8"; // 상승=초록/하락=빨강/동일=회색
                string sign = diff >= 0 ? "+" : "-";
                newPowerText.text =
                    $"⚔️ {NumberFormatLoader.Abbrev(newPower)} " +
                    $"<color={color}>({sign}{NumberFormatLoader.Abbrev(Mathf.Abs(diff))})</color>";
            }
        }

        // 리롤은 다음 뽑기용 상자가 있어야 가능
        if (rerollButton != null)
            rerollButton.interactable = DataManager.Instance != null && DataManager.Instance.crate >= 1;
    }

    void OnEquipClicked()
    {
        if (WeaponManager.Instance != null) WeaponManager.Instance.EquipPending();
        Hide();
    }

    void OnKeepClicked()
    {
        if (WeaponManager.Instance != null) WeaponManager.Instance.KeepCurrent();
        Hide();
    }

    // 리롤: 신규 즉시 판매 + 새로 뽑기 → onPending이 다시 발화해 팝업 내용이 갱신된다(닫지 않음)
    void OnRerollClicked()
    {
        if (WeaponManager.Instance == null) return;
        WeaponManager.Instance.RerollPending();
    }

    public void Hide()
    {
        if (popupRoot != null) popupRoot.SetActive(false);
    }
}
