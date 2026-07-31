using UnityEngine;
using UnityEngine.UI;

// 순위표 보드 전환 탭(결투/전투력/스테이지 - 목업 하단 네비게이션).
// 각 보드는 RankBoard 인스턴스(statisticName/scoreSuffix만 다름)이고,
// NavButtons처럼 루트 좌표만 옮긴다(활성 (0,0) / 비활성 (9999,0)).
public class RankBoardTabs : MonoBehaviour
{
    [Header("보드 루트")]
    [SerializeField] RectTransform powerBoardRoot; //전투력(Power)
    [SerializeField] RectTransform stageBoardRoot; //스테이지(Stage)

    [Header("탭 버튼")]
    [SerializeField] Button powerButton;
    [SerializeField] Button stageButton;

    static readonly Vector2 hiddenPos = new(9999f, 0);
    static readonly Vector2 enablePos = new(0, 935);

    void Awake()
    {
        if (powerButton != null) powerButton.onClick.AddListener(ShowPower);
        if (stageButton != null) stageButton.onClick.AddListener(ShowStage);

        ShowPower(); //기본 탭: 결투
    }

    public void ShowPower() => Switch(powerBoardRoot);
    public void ShowStage() => Switch(stageBoardRoot);

    void Switch(RectTransform target)
    {
        Place(powerBoardRoot, target);
        Place(stageBoardRoot, target);
    }

    void Place(RectTransform root, RectTransform target)
    {
        if (root != null) root.anchoredPosition = root == target ? enablePos : hiddenPos;
    }
}
