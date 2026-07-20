using System.Collections.Generic;
using UnityEngine;

// 성장 탭 컨트롤러. 인스펙터에 미리 배치한 스탯 행(GrowthStatSlot)들을 시트 순서대로 바인딩하고,
// 레벨업 처리·골드/레벨 변화 시 갱신을 담당한다. 표시는 각 슬롯에 위임.
public class GrowthPanel : MonoBehaviour
{
    [SerializeField] List<GrowthStatSlot> slots = new(); // 씬에 미리 만들어 둔 스탯 행들(시트 순서대로 배치)

    bool bound;
    bool subscribed;

    void OnEnable()
    {
        TrySubscribe();
        TryBind();
        RefreshAll();
    }

    void OnDisable() => Unsubscribe();

    void Update()
    {
        // OnEnable 시점엔 DataManager/GrowthManager 싱글톤이 아직 없을 수 있어 구독이 건너뛰어진다.
        // 둘 다 생성되면 구독하도록 재시도(성공하면 이후 Update는 즉시 리턴).
        if (!subscribed) TrySubscribe();
    }

    void TrySubscribe()
    {
        if (subscribed) return;
        if (GrowthManager.Instance == null || DataManager.Instance == null) return;

        GrowthManager.Instance.onGrowthChanged.AddListener(RefreshAll);
        GrowthManager.Instance.onStatChanged.AddListener(OnStatChanged);
        DataManager.Instance.Gold.onValueChanged += OnGoldChanged; // 골드 변화 → 구매 가능 상태 갱신
        subscribed = true;

        RefreshAll(); // 구독 성립 시점의 최신 골드/레벨로 즉시 반영
    }

    void Unsubscribe()
    {
        if (!subscribed) return;

        if (GrowthManager.Instance != null)
        {
            GrowthManager.Instance.onGrowthChanged.RemoveListener(RefreshAll);
            GrowthManager.Instance.onStatChanged.RemoveListener(OnStatChanged);
        }
        if (DataManager.Instance != null)
            DataManager.Instance.Gold.onValueChanged -= OnGoldChanged;

        subscribed = false;
    }

    // 인스펙터 슬롯을 시트 순서(AllOrdered)의 스탯과 인덱스로 짝지어 바인딩한다.
    void TryBind()
    {
        if (bound) return;
        var loader = GrowthStatLoader.Instance;
        if (loader == null || !loader.IsLoaded) return;

        var ordered = loader.AllOrdered();
        if (slots.Count != ordered.Count)
            Debug.LogWarning($"[Growth] 슬롯 수({slots.Count})와 시트 스탯 수({ordered.Count})가 다름 — 적은 쪽 기준으로 바인딩됩니다.");

        int n = Mathf.Min(slots.Count, ordered.Count);
        for (int i = 0; i < n; i++)
        {
            if (slots[i] == null) continue;
            slots[i].Bind(ordered[i].id, this);
        }

        // 시트에 없는 여분 슬롯은 숨김
        for (int i = n; i < slots.Count; i++)
            if (slots[i] != null) slots[i].gameObject.SetActive(false);

        bound = true;
    }

    void RefreshAll()
    {
        TryBind(); // 로드 완료 콜백으로 처음 불릴 때 바인딩
        if (!bound) return; // 바인딩 전이면 슬롯 statId가 없어 갱신 불가(로더 미로드)
        foreach (var slot in slots)
            if (slot != null) slot.Refresh();
    }

    void OnStatChanged(string statId) => RefreshAll();
    void OnGoldChanged(long _)
    {
        if (!bound) return;
        foreach (var slot in slots)
            if (slot != null) slot.Refresh();
    }

    // GrowthStatSlot 버튼에서 호출
    public void OnUpgradeClicked(string statId)
    {
        if (GrowthManager.Instance == null) return;
        GrowthManager.Instance.TryUpgrade(statId);
        // 성공 시 onStatChanged/onGrowthChanged로 자동 갱신
    }
}
