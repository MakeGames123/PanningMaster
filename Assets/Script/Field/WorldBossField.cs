using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// v45 월드보스 전용 필드(골드 던전 필드 문법). 30초(WorldBossConfig.DurationSec) 동안
// 불사신 수배범에게 최대 딜 — 시간 종료 = 실패 아님, 총 데미지를 매니저에 정산.
// 전투 중 매칭 속성 탄환 ×2 집계는 Player.FireAt가 WorldBossManager.ElemMulFor로 처리.
public class WorldBossField : MonoBehaviour
{
    [SerializeField] BattleResult battleResult;
    [SerializeField] WorldBossEnemy boss; // 월드보스 전용 적

    [Header("제한시간/점수 UI")]
    [SerializeField] GameObject timerRoot;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] Slider timerSlider;
    [SerializeField] TextMeshProUGUI damageText; // 실시간 누적 데미지 "💥 1.2M" (선택)

    PlayerSquad player;
    float timeLimit = 30f;
    bool ended;

    void Awake()
    {
        if (timerRoot != null) timerRoot.SetActive(false);
        if (damageText != null) damageText.gameObject.SetActive(false); // 누적 데미지는 전투 중에만 표시
        if (boss != null) boss.gameObject.SetActive(false);
    }

    // FieldManager가 SetTargetProvider로 조준을 연결한다.
    public IFireTarget GetTarget() => boss;

    public void Begin(PlayerSquad p)
    {
        player = p;
        ended = false;

        var mgr = WorldBossManager.Instance;
        timeLimit = mgr != null ? mgr.DurationSec : 30f;

        boss.gameObject.SetActive(true);
        boss.Begin(mgr != null ? mgr.CurrentOutlaw() : null);

        if (mgr != null) mgr.SetBattleActive(true); // 속성 ×2 집계 켜기

        if (timerRoot != null) timerRoot.SetActive(true);
        if (damageText != null) damageText.gameObject.SetActive(true);

        StartCoroutine(TimerRoutine());
        player.StartCycle();
    }

    public void Stop()
    {
        StopAllCoroutines();
        if (WorldBossManager.Instance != null) WorldBossManager.Instance.SetBattleActive(false);
        if (timerRoot != null) timerRoot.SetActive(false);
        if (damageText != null) damageText.gameObject.SetActive(false);
        if (boss != null) boss.gameObject.SetActive(false);
    }

    // 6발이 끝나면(불사신이라 죽음으로 안 끊김) 장전 후 다시 발사.
    public void OnCycleComplete(bool killed)
    {
        if (ended) return;
        player.ReloadAndStartCycle();
    }

    IEnumerator TimerRoutine()
    {
        float t = timeLimit;
        UpdateTimerUI(t);

        while (t > 0f)
        {
            t -= Time.deltaTime;
            if (t < 0f) t = 0f;
            UpdateTimerUI(t);
            yield return null;
        }

        End(); // 시간 종료 = 정산(실패 아님)
    }

    void UpdateTimerUI(float remaining)
    {
        if (timerText != null) timerText.text = remaining.ToString("0.0");
        if (timerSlider != null) timerSlider.value = timeLimit > 0f ? remaining / timeLimit : 0f;
        if (damageText != null && boss != null)
            damageText.text = $"💥 {NumberFormatLoader.Abbrev(boss.TotalDamage)}";
    }

    void End()
    {
        if (ended) return;
        ended = true;

        if (WorldBossManager.Instance != null) WorldBossManager.Instance.SetBattleActive(false);
        if (timerRoot != null) timerRoot.SetActive(false);
        if (damageText != null) damageText.gameObject.SetActive(false);

        player.StopBattle();
        StartCoroutine(EndRoutine());
    }

    IEnumerator EndRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        long total = boss != null ? boss.TotalDamage : 0;
        long prevBest = WorldBossManager.Instance != null ? WorldBossManager.Instance.BestDamage : 0;
        if (WorldBossManager.Instance != null) WorldBossManager.Instance.SubmitResult(total);

        battleResult.ShowResult(total > prevBest
            ? $"💥 총 데미지 {NumberFormatLoader.Abbrev(total)} — 신기록!"
            : $"💥 총 데미지 {NumberFormatLoader.Abbrev(total)}");

        yield return new WaitForSeconds(2f);

        FieldManager.Instance.ReturnToNormal(); // 일반 전투 복귀
    }
}
