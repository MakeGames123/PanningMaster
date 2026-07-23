using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 무기 뽑기 기본 패널(내 리볼버). 장착 무기 이름/스탯 행·뽑기 버튼·상자/레벨 표시를 담당.
// 스탯 행 = 주스탯 1행 + 부옵 4행 고정(빈 슬롯 = 고스트로 해금 힌트 표시 — 프로토 revStatRows).
// 비교 팝업은 WeaponComparePopup이 onPending을 구독해 스스로 열린다 — 여기서 관여하지 않음.
public class WeaponDrawPanel : MonoBehaviour
{
    // 스탯 1행 뷰(이름 + 값). 씬에 미리 배치한 행의 텍스트를 연결
    [System.Serializable]
    public class StatRow
    {
        public TextMeshProUGUI nameText;  // "⚔️ 공격력" / "부옵션 3"
        public TextMeshProUGUI valueText; // "+53%" / "A등급부터"
    }

    [Header("장착 중 리볼버")]
    [SerializeField] TextMeshProUGUI equippedNameText; // "[C] 무쇠 리볼버 Lv.2" (등급 부분만 등급색)
    [SerializeField] StatRow mainStatRow;              // 주스탯(공격력) 행
    [SerializeField] List<StatRow> subStatRows = new(); // 부옵 행 4개(순서대로)
    [SerializeField] Color rowColor = Color.white;                       // 채워진 행 색
    [SerializeField] Color ghostColor = new(0.42f, 0.46f, 0.55f);        // 빈 슬롯(고스트) 행 색

    [Header("뽑기")]
    [SerializeField] Button drawButton;
    [SerializeField] TextMeshProUGUI levelText;   // "뽑기 Lv.3"
    [SerializeField] TextMeshProUGUI xpText;      // "13/31"
    [SerializeField] Slider xpSlider;             // 레벨 경험치 게이지(선택)

    // 빈 부옵 슬롯 해금 힌트(슬롯 1~4) — 프로토 v34e 13단 슬롯 문턱
    static readonly string[] slotHints = { "—", "C등급부터", "A등급부터", "SS·Lv5" };

    bool subscribed;

    void Awake()
    {
        if (drawButton != null) drawButton.onClick.AddListener(OnDrawClicked);
    }

    void OnDestroy()
    {
        if (!subscribed) return;
        if (WeaponManager.Instance != null)
            WeaponManager.Instance.onChanged.RemoveListener(Refresh);
        if (DataManager.Instance != null)
            DataManager.Instance.onCrateChanged.RemoveListener(OnCrateChanged);
    }

    void Update()
    {
        // OnEnable 시점엔 WeaponManager/DataManager 싱글톤이 아직 없을 수 있어 구독 재시도
        if (!subscribed) TrySubscribe();
    }

    void TrySubscribe()
    {
        if (subscribed) return;
        if (WeaponManager.Instance == null || DataManager.Instance == null) return;

        WeaponManager.Instance.onChanged.AddListener(Refresh);
        DataManager.Instance.onCrateChanged.AddListener(OnCrateChanged);
        subscribed = true;

        Refresh();
    }

    void OnCrateChanged(int _) => Refresh();

    void OnDrawClicked()
    {
        if (WeaponManager.Instance != null)
            WeaponManager.Instance.TryDraw(); // 비교가 필요하면 onPending → WeaponComparePopup이 열림
    }

    void Refresh()
    {
        var mgr = WeaponManager.Instance;
        if (mgr == null) return;

        RefreshEquipped(mgr.Equipped);
        
        int need = mgr.XpNeed();
        if (levelText != null) levelText.text = $"뽑기 Lv.{mgr.DrawLevel}";
        if (xpText != null) xpText.text = $"{mgr.DrawXp}/{need}";
        if (xpSlider != null) xpSlider.value = need > 0 ? Mathf.Clamp01((float)mgr.DrawXp / need) : 0f;

        // 상자가 없거나 비교 미해결이면 뽑기 비활성
        bool canDraw = mgr.IsReady && mgr.Pending == null
            && DataManager.Instance != null && DataManager.Instance.crate >= 1;
        if (drawButton != null) drawButton.interactable = canDraw;
    }

    void RefreshEquipped(WeaponData w)
    {
        // 이름: 등급 부분만 등급색 + 레벨 (프로토 revPaint)
        if (equippedNameText != null)
        {
            equippedNameText.text = w != null
                ? $"<color={WeaponGrades.ColorHex(w.grade)}>[{WeaponGrades.Code(w.grade)}]</color> {WeaponGrades.TierName(w)} <size=70%>Lv.{w.level}</size>"
                : "🧰 상자를 열어 첫 리볼버를 얻으세요";
        }

        // 주스탯 행
        SetRow(mainStatRow, "⚔️ 공격력", $"+{NumberFormatLoader.Abbrev(w != null ? w.atk : 0)}%", false);

        // 부옵 행 4개: 채워진 슬롯 = 스탯, 빈 슬롯 = 고스트(해금 힌트) — 프로토 revStatRows
        for (int i = 0; i < subStatRows.Count; i++)
        {
            var sub = (w != null && i < w.subs.Count) ? w.subs[i] : null;
            if (sub != null)
            {
                var d = WeaponSubStatLoader.Instance != null ? WeaponSubStatLoader.Instance.Get(sub.sid) : null;
                string name = d != null ? $"{d.icon} {d.nameKo}" : sub.sid;
                string sign = sub.sid == "reload" ? "-" : "+"; // 장전 속도는 감소가 이득
                SetRow(subStatRows[i], name, $"{sign}{sub.value}%", false);
            }
            else
            {
                SetRow(subStatRows[i], $"부옵션 {i + 1}", i < slotHints.Length ? slotHints[i] : "—", true);
            }
        }
    }

    void SetRow(StatRow row, string name, string value, bool ghost)
    {
        if (row == null) return;

        Color c = ghost ? ghostColor : rowColor;
        if (row.nameText != null)
        {
            row.nameText.text = name;
            row.nameText.color = c;
        }
        if (row.valueText != null)
        {
            row.valueText.text = value;
            row.valueText.color = c;
        }
    }
}
