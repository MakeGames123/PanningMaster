using System.Collections.Generic;
using UnityEngine;

// 던전 탭 패널. 던전 배너 목록 관리 — 열쇠는 던전마다 개별 종류라 보유/충전 표시는 각 배너가 담당.
// 배너(DungeonBannerUI)를 누르면 상세 팝업(DungeonDetailPopup)이 열린다.
public class DungeonPanel : MonoBehaviour
{
    [Header("던전 목록")]
    [Tooltip("비우면 자식에서 자동 수집 — 순서 = 상세 팝업 ◀▶ 순환 순서")]
    [SerializeField] List<DungeonBannerUI> banners = new();
    [SerializeField] DungeonDetailPopup detailPopup;

    bool subscribed;

    void Awake()
    {
        if (banners.Count == 0)
            banners.AddRange(GetComponentsInChildren<DungeonBannerUI>(true));

        foreach (DungeonBannerUI b in banners)
            if (b != null) b.Init(detailPopup);

        if (detailPopup != null) detailPopup.SetBanners(banners);
    }

    void OnEnable()
    {
        TrySubscribe();
        Refresh();
    }

    void Start()
    {
        // NavButtons는 패널을 비활성화하지 않고 화면 밖으로 옮기므로 이벤트로도 갱신
        TrySubscribe();
        Refresh();

        // 시트(던전 이름·부제·열쇠 캡) 로드가 늦게 끝나면 다시 갱신
        if (DungeonLoader.Instance != null) DungeonLoader.Instance.OnLoaded += Refresh;
    }

    void TrySubscribe()
    {
        if (subscribed || DataManager.Instance == null) return;
        DataManager.Instance.onDungeonFloorChanged.AddListener(OnFloorChanged);
        DataManager.Instance.onKeyChanged.AddListener(Refresh);
        subscribed = true;
    }

    void OnDestroy()
    {
        if (subscribed && DataManager.Instance != null)
        {
            DataManager.Instance.onDungeonFloorChanged.RemoveListener(OnFloorChanged);
            DataManager.Instance.onKeyChanged.RemoveListener(Refresh);
        }
        if (DungeonLoader.Instance != null) DungeonLoader.Instance.OnLoaded -= Refresh;
    }

    void OnFloorChanged(int _) => Refresh();

    void Refresh()
    {
        foreach (DungeonBannerUI b in banners)
            if (b != null) b.Refresh();
    }
}
