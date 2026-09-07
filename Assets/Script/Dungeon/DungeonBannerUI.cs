using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 던전 목록의 큰 배너 버튼 1개. 요약(이름·부제·진행·보유 열쇠)만 표시하고,
// 누르면 DungeonDetailPopup이 열린다 — 상세 정보와 입장(열쇠 소모)은 팝업 몫.
public class DungeonBannerUI : MonoBehaviour
{
    [Header("던전")]
    [SerializeField] DungeonKind kind = DungeonKind.Normal;

    [Header("UI(널 허용)")]
    [SerializeField] Button button;                //배너 전체
    [SerializeField] TextMeshProUGUI nameText;     //"골드 던전"
    [SerializeField] TextMeshProUGUI subText;      //골드·열차 = 부제(시트 SubKo) / 나머지 = 진행("3단계 도전 · 최고 2")
    [SerializeField] TextMeshProUGUI keyText;      //"🔑 3/4" — 열쇠는 던전마다 개별 종류(kind가 곧 열쇠 종류)

    DungeonDetailPopup popup; //DungeonPanel이 주입

    public DungeonKind Kind => kind;
    public string DungeonId => kind.SheetId(); //Dungeon 시트 Id — kind에서 파생(Normal은 null)

    public void Init(DungeonDetailPopup p) => popup = p;

    void Awake()
    {
        if (button != null) button.onClick.AddListener(() => { if (popup != null) popup.Open(this); });
    }

    public DungeonData SheetData
        => (DungeonId != null && DungeonLoader.Instance != null)
            ? DungeonLoader.Instance.Get(DungeonId) : null;

    // 시트 NameKo 우선, 미로드 시 종류별 폴백
    public string DisplayName
    {
        get
        {
            DungeonData d = SheetData;
            if (d != null && !string.IsNullOrEmpty(d.nameKo)) return d.nameKo;
            return kind switch
            {
                DungeonKind.Gold => "골드 던전",
                DungeonKind.Train => "열차 강탈",
                DungeonKind.Laboratory => "연구소 던전",
                DungeonKind.Armory => "버려진 무기고",
                _ => "던전",
            };
        }
    }

    //시트 KeyEmoji 우선, 미로드 시 종류별 폴백(프로토 keyA=🗝️/keyD=🚂/그 외=🔑)
    public string KeyEmoji
    {
        get
        {
            DungeonData d = SheetData;
            if (d != null && !string.IsNullOrEmpty(d.keyEmoji)) return d.keyEmoji;
            return kind switch
            {
                DungeonKind.Gold => "🗝️",
                DungeonKind.Train => "🚂",
                _ => "🔑",
            };
        }
    }

    public void Refresh()
    {
        DataManager dm = DataManager.Instance;
        if (dm == null) return;

        DungeonData d = SheetData;

        if (nameText != null) nameText.text = DisplayName;

        if (subText != null)
        {
            if (kind == DungeonKind.Gold || kind == DungeonKind.Train)
            {
                //단계/층 개념 없음 → 부제 표시
                if (d != null) subText.text = d.subKo; //시트 미로드 시 에디터 입력값 유지
            }
            else
            {
                //등반·층 진행형 → 진행 표시
                int best = kind == DungeonKind.Normal ? dm.clearedDungeonFloor : dm.GetClimbBest(DungeonId);
                string unit = kind == DungeonKind.Normal ? "층" : "단계";
                subText.text = $"{best + 1}{unit} 도전 · 최고 {best}";
            }
        }

        if (keyText != null) keyText.text = $"{KeyEmoji} {dm.GetKey(kind)}/{dm.GetMaxKey(kind)}";
    }
}
