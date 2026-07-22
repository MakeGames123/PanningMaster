using UnityEngine;

// 업적 바 컨트롤러(QuestPanel과 동일 구조). 슬롯은 하나뿐 —
// 지금 수령 가능한(깨진) 업적 하나만 AchievementUI에 표시하고, 버튼으로 보상 수령한다.
// 수령하면 다음으로 깨진 업적을 표시하고, 없으면 바를 숨긴다.
public class AchievementPanel : MonoBehaviour
{
    [SerializeField] AchievementUI achievementUI;

    bool subscribed;

    void Start()
    {
        if (achievementUI != null) achievementUI.OnActionClicked += HandleActionClicked;
        TrySubscribe();
        Refresh();
    }

    void OnDestroy()
    {
        if (achievementUI != null) achievementUI.OnActionClicked -= HandleActionClicked;
        if (subscribed && AchievementManager.Instance != null)
            AchievementManager.Instance.onChanged.RemoveListener(Refresh);
    }

    void Update()
    {
        // Start 시점엔 AchievementManager 싱글톤/시트가 아직 없을 수 있어 구독이 건너뛰어진다.
        // 준비되면 구독하도록 재시도(성공하면 이후 Update는 즉시 리턴).
        if (!subscribed) TrySubscribe();
    }

    void TrySubscribe()
    {
        if (subscribed) return;
        if (AchievementManager.Instance == null) return;

        AchievementManager.Instance.onChanged.AddListener(Refresh);
        subscribed = true;

        Refresh(); // 구독 성립 시점의 최신 진행도로 즉시 반영
    }

    // 시트 순서상 지금 수령 가능한(깨진) 첫 업적
    AchievementData FindClaimable()
    {
        var loader = AchievementLoader.Instance;
        var mgr = AchievementManager.Instance;
        if (loader == null || !loader.IsLoaded || mgr == null) return null;

        foreach (var a in loader.AllOrdered())
            if (mgr.IsClaimable(a)) return a;
        return null;
    }

    void Refresh()
    {
        var mgr = AchievementManager.Instance;
        var a = FindClaimable();

        if (a == null || mgr == null)
        {
            if (achievementUI != null) achievementUI.Hide(); // 깨진 업적이 없으면 바 숨김
            return;
        }

        if (achievementUI != null)
            achievementUI.Show(a, mgr.GetProgress(a), mgr.NextThreshold(a), mgr.NextGems(a));
    }

    // AchievementUI 버튼 클릭 → 보상 수령 후 다음 깨진 업적 표시(없으면 숨김)
    void HandleActionClicked()
    {
        var a = FindClaimable();
        if (a == null || AchievementManager.Instance == null) return;

        AchievementManager.Instance.Claim(a.id); // onChanged → Refresh 자동 호출
    }
}
