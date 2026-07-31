using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

// 내 전투력(Power)/스테이지(Stage)를 순위표 통계에 보고한다.
// 유령 유저만 있고 내 계정 값이 없으면 전투력/스테이지 순위표의 "내 주변"이 동작하지 않으므로 필요.
// 값이 바뀌면 dirty 표시만 하고, reportInterval 간격으로만 실제 전송한다(API 스팸 방지).
// 로그인 직후에는 1회 즉시 전송한다. (PvPRank는 결투 시스템이 따로 관리하므로 여기서 안 보냄)
public class PlayerStatReporter : MonoBehaviour
{
    [SerializeField] PlayFabLoginManager login;
    [SerializeField] string powerStatistic = "Power";
    [SerializeField] string stageStatistic = "Stage";
    [SerializeField] float reportInterval = 60f; //전송 최소 간격(초)

    bool dirty;
    bool subscribed;
    float lastReportTime = -999f;

    void Awake()
    {
        if (login != null) login.onLogined.AddListener(OnLogined);
    }

    void OnEnable() => TrySubscribe();
    void Start() => TrySubscribe();

    void TrySubscribe()
    {
        if (subscribed || DataManager.Instance == null) return;
        DataManager.Instance.onPowerChanged.AddListener(OnPowerChanged);
        DataManager.Instance.onStageChanged.AddListener(OnStageChanged);
        subscribed = true;
    }

    void OnDestroy()
    {
        if (login != null) login.onLogined.RemoveListener(OnLogined);
        if (subscribed && DataManager.Instance != null)
        {
            DataManager.Instance.onPowerChanged.RemoveListener(OnPowerChanged);
            DataManager.Instance.onStageChanged.RemoveListener(OnStageChanged);
        }
    }

    void OnPowerChanged(double _) => dirty = true;
    void OnStageChanged(int _) => dirty = true;

    void OnLogined()
    {
        dirty = true;
        lastReportTime = -999f; //로그인 직후 1회 즉시 전송
    }

    void Update()
    {
        if (!dirty || !PlayFabClientAPI.IsClientLoggedIn()) return;
        if (Time.unscaledTime - lastReportTime < reportInterval) return;
        Report();
    }

    void Report()
    {
        dirty = false;
        lastReportTime = Time.unscaledTime;

        //전투력은 순위표 표기 기준(PlayerData.Power). 통계는 int라 클램프
        int power = PlayerData.Instance != null
            ? (int)System.Math.Min(System.Math.Round((double)PlayerData.Instance.Power), int.MaxValue)
            : 0;
        int stage = DataManager.Instance != null ? DataManager.Instance.stage : 0;

        PlayFabClientAPI.UpdatePlayerStatistics(
            new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate { StatisticName = powerStatistic, Value = power },
                    new StatisticUpdate { StatisticName = stageStatistic, Value = stage }
                }
            },
            _ => Debug.Log($"[StatReporter] 통계 보고: 전투력 {power} / {stage}층"),
            e => Debug.LogError($"[StatReporter] 통계 보고 실패: {e.GenerateErrorReport()}"));
    }
}
