using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 골드 던전 전용 필드. 단계 없음 · 실패 없음 · 죽음 판정 없음 · 장전 없음.
// 전용 적(GoldDungeonEnemy)을 다중 표적 모드로 조준해 계속 발사한다.
// 적은 죽지 않고 체력바만 닳으면 다음 체력(2배)으로 리필된다. 시간이 끝나면 채굴 종료(실패 아님).
public class GoldDungeonField : MonoBehaviour
{
    [SerializeField] BattleResult battleResult;
    [SerializeField] GoldDungeonEnemy goldEnemy; // 골드 던전 전용 적
    [SerializeField] float timeLimit = 30f;      // 채굴 시간(초). 종료 = 실패 아님

    [Header("제한시간 UI")]
    [SerializeField] GameObject timerRoot;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] Slider timerSlider;

    PlayerSquad player;
    Coroutine timerCo;
    bool ended;

    void Awake()
    {
        if (timerRoot != null) timerRoot.SetActive(false);
        if (goldEnemy != null) goldEnemy.gameObject.SetActive(false);
    }

    // FieldManager가 SetTargetProvider로 조준을 연결한다.
    public IFireTarget GetTarget() => goldEnemy;

    // FieldManager가 골드 던전을 활성화할 때 호출
    public void Begin(PlayerSquad p)
    {
        player = p;
        ended = false;

        goldEnemy.gameObject.SetActive(true);
        goldEnemy.Begin();

        if (timerRoot != null) timerRoot.SetActive(true);

        timerCo = StartCoroutine(TimerRoutine());
        player.StartCycle();
    }

    public void Stop()
    {
        StopAllCoroutines();
        timerCo = null;
        if (timerRoot != null) timerRoot.SetActive(false);
        if (goldEnemy != null) goldEnemy.gameObject.SetActive(false);
    }

    // 6발이 끝나면(다중 표적 모드라 죽음으로 안 끊김) 정상적으로 장전 후 다시 발사.
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

        End(); // 시간 종료 = 채굴 끝(실패 아님)
    }

    void UpdateTimerUI(float remaining)
    {
        if (timerText != null) timerText.text = remaining.ToString("0.0");
        if (timerSlider != null) timerSlider.value = timeLimit > 0f ? remaining / timeLimit : 0f;
    }

    void End()
    {
        if (ended) return;
        ended = true;

        if (timerCo != null) { StopCoroutine(timerCo); timerCo = null; }
        if (timerRoot != null) timerRoot.SetActive(false);

        player.StopBattle();
        StartCoroutine(EndRoutine());
    }

    IEnumerator EndRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        battleResult.ShowResult($"골드 채굴 완료 +{goldEnemy.EarnedGold}");

        yield return new WaitForSeconds(2f);

        FieldManager.Instance.ReturnToNormal(); // 일반 전투 복귀
    }
}
