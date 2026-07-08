using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 던전 종류. Normal = 기존 층 진행형 던전, Laboratory = 연구소 던전(층 없음, 다중 적)
public enum DungeonKind { Normal, Laboratory }

// 던전 목록의 한 항목(예: 골드 던전). 층 선택(< n >) + 입장 버튼 + 입장 비용 표시.
// 선택 가능 최고 층 = 클리어 층 + 1. 다음 층이 안 열렸으면 오른쪽, 1층이면 왼쪽 버튼 비활성.
public class DungeonEntry : MonoBehaviour
{
    [Header("던전 종류")]
    [SerializeField] DungeonKind kind = DungeonKind.Normal;

    [Header("층 선택")]
    [SerializeField] TextMeshProUGUI floorText;   // "1"
    [SerializeField] Button leftButton;           // 이전 층
    [SerializeField] Button rightButton;          // 다음 층

    [Header("입장")]
    [SerializeField] Button enterButton;
    [SerializeField] TextMeshProUGUI costText;    // "일반 열쇠 1개"
    [SerializeField] KeyType keyType = KeyType.Normal;
    [SerializeField] string keyName = "일반 열쇠";
    [SerializeField] int keyCost = 1;

    int selectedFloor = 1;

    void Awake()
    {
        if (leftButton != null) leftButton.onClick.AddListener(() => ChangeFloor(-1));
        if (rightButton != null) rightButton.onClick.AddListener(() => ChangeFloor(1));
        if (enterButton != null) enterButton.onClick.AddListener(Enter);
    }

    void OnEnable() => Refresh();

    void ChangeFloor(int dir)
    {
        selectedFloor = Mathf.Clamp(selectedFloor + dir, 1, MaxSelectableFloor());
        Refresh();
    }

    // 선택 가능한 최고 층 = 클리어 층 + 1 (다음 도전 층까지 열림)
    // 연구소 던전은 층 개념이 없으므로 항상 1층 고정(양쪽 화살표 자동 숨김)
    int MaxSelectableFloor()
    {
        if (kind == DungeonKind.Laboratory) return 1;

        int cleared = DataManager.Instance != null ? DataManager.Instance.clearedDungeonFloor : 0;
        return cleared + 1;
    }

    public void Refresh()
    {
        int max = MaxSelectableFloor();
        selectedFloor = Mathf.Clamp(selectedFloor, 1, max);

        if (floorText != null) floorText.text = selectedFloor.ToString();
        if (costText != null) costText.text = $"{keyName} {keyCost}개";

        // 1층이면 왼쪽 숨김 / 다음 층이 안 열렸으면 오른쪽 숨김 (아예 보이거나 안 보이게)
        if (leftButton != null) leftButton.gameObject.SetActive(selectedFloor > 1);
        if (rightButton != null) rightButton.gameObject.SetActive(selectedFloor < max);
    }

    void Enter()
    {
        // 열쇠가 부족하면 입장 불가
        if (!DataManager.Instance.UseKey(keyType, keyCost))
        {
            Debug.Log($"열쇠 부족: {keyName} {keyCost}개 필요");
            return;
        }

        Debug.Log($"던전 입장: {selectedFloor}층 (비용 {keyName} {keyCost}개)");

        if (FieldManager.Instance == null) return;

        if (kind == DungeonKind.Laboratory) FieldManager.Instance.EnterLabDungeon();
        else FieldManager.Instance.EnterDungeon(selectedFloor);
    }
}
