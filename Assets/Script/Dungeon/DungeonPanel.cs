using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 던전 탭 패널. 보유 열쇠 표시 + 던전 목록 갱신 관리.
public class DungeonPanel : MonoBehaviour
{
    [Header("보유 열쇠")]
    [SerializeField] TextMeshProUGUI normalKeyText;        // 일반 열쇠 (보유/최대)
    [SerializeField] TextMeshProUGUI eliteKeyText;         // 정예 열쇠 (보유/최대)
    [SerializeField] TextMeshProUGUI normalChargeTimerText; // 다음 일반 열쇠 충전 남은 시간
    [SerializeField] TextMeshProUGUI eliteChargeTimerText;  // 다음 정예 열쇠 충전 남은 시간

    [Header("던전 목록")]
    [Tooltip("비우면 자식에서 자동 수집")]
    [SerializeField] List<DungeonEntry> entries = new();

    bool subscribed;

    void Awake()
    {
        if (entries.Count == 0)
            entries.AddRange(GetComponentsInChildren<DungeonEntry>(true));
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
    }

    void OnFloorChanged(int _) => Refresh();

    void Update()
    {
        UpdateChargeTimer(); //충전 남은 시간 카운트다운 갱신
    }

    void Refresh()
    {
        DataManager dm = DataManager.Instance;
        if (dm == null) return;

        if (normalKeyText != null) normalKeyText.text = $"{dm.normalKey}/{dm.maxNormalKey}";
        if (eliteKeyText != null) eliteKeyText.text = $"{dm.eliteKey}/{dm.maxEliteKey}";

        foreach (DungeonEntry e in entries)
            if (e != null) e.Refresh();
    }

    void UpdateChargeTimer()
    {
        DataManager dm = DataManager.Instance;
        if (dm == null) return;

        SetTimerText(normalChargeTimerText, dm, KeyType.Normal);
        SetTimerText(eliteChargeTimerText, dm, KeyType.Elite);
    }

    void SetTimerText(TextMeshProUGUI text, DataManager dm, KeyType type)
    {
        if (text == null) return;

        if (dm.IsKeyFull(type))
        {
            text.text = "MAX";
            return;
        }

        int total = Mathf.CeilToInt(dm.GetKeyChargeRemaining(type));
        text.text = $"{total / 60:00}:{total % 60:00}"; //mm:ss
    }
}
