using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 열차 강탈 전용 필드(프로토 v39 tk). 골드 던전 문법 — 30초 · 실패 없음 · 죽지 않는 화물칸 조준.
// 정산 🎫 = TkBase + TkPerCart×칸 + TkGoldenBonus×floor(칸/TkGoldenEvery), 마지막 금고 룰렛 ×1~×3.
// 수치는 전부 DungeonConfig 시트(미로드 시 프로토 v1.0.47 기본값).
public class TrainDungeonField : MonoBehaviour
{
    [SerializeField] BattleResult battleResult;
    [SerializeField] TrainDungeonEnemy trainEnemy; // 열차 강탈 전용 적(화물칸)

    [Header("제한시간 UI")]
    [SerializeField] GameObject timerRoot;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] Slider timerSlider;

    PlayerSquad player;
    float timeLimit;
    int goldenEvery;
    Coroutine timerCo;
    bool ended;

    void Awake()
    {
        if (timerRoot != null) timerRoot.SetActive(false);
        if (trainEnemy != null) trainEnemy.gameObject.SetActive(false);
    }

    // FieldManager가 SetTargetProvider로 조준을 연결한다.
    public IFireTarget GetTarget() => trainEnemy;

    // FieldManager가 열차 강탈을 활성화할 때 호출
    public void Begin(PlayerSquad p)
    {
        player = p;
        ended = false;

        DungeonConfigLoader cfg = DungeonConfigLoader.Instance;
        timeLimit = cfg != null ? cfg.GetInt("DurationSec", 30) : 30;
        goldenEvery = cfg != null ? cfg.GetInt("TkGoldenEvery", 4) : 4;
        float cartHpMul = cfg != null ? cfg.GetFloat("CartHpMul", 2f) : 2f;

        trainEnemy.gameObject.SetActive(true);
        //화물칸 HP = 본편 몬스터 체력 × 배수(프로토 dgCartHP) — 리필돼도 같은 체력
        trainEnemy.Begin(MonsterHpTableLoader.Instance.GetHP(DataManager.Instance.stage) * cartHpMul, goldenEvery);

        if (timerRoot != null) timerRoot.SetActive(true);

        timerCo = StartCoroutine(TimerRoutine());
        player.StartCycle();
    }

    public void Stop()
    {
        StopAllCoroutines();
        timerCo = null;
        if (timerRoot != null) timerRoot.SetActive(false);
        if (trainEnemy != null) trainEnemy.gameObject.SetActive(false);
    }

    // 6발이 끝나면(적이 죽지 않으므로 사이클이 안 끊김) 정상적으로 장전 후 다시 발사.
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

        End(); // 시간 종료 = 습격 끝(실패 아님)
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

        int carts = trainEnemy != null ? trainEnemy.Carts : 0;
        int mult = RollJackpot();
        int gain = SettleTickets(carts, mult);

        DataManager.Instance.IncreaseTicket(gain);
        DataManager.Instance.SetBestTrainCarts(carts);
        if (carts > 0 && QuestEventManager.Instance != null) QuestEventManager.Instance.AddEvent("dgClear");

        battleResult.ShowResult(mult > 1
            ? $"💰 금고 룰렛 ×{mult}! 🎫 +{gain}"
            : $"열차 강탈 완료 🎫 +{gain}");

        yield return new WaitForSeconds(2f);

        FieldManager.Instance.ReturnToNormal(); // 일반 전투 복귀
    }

    // 🎫 = 기본 + 칸당 + 황금 화물칸(N칸마다) 보너스, 전체에 룰렛 배수
    int SettleTickets(int carts, int mult)
    {
        DungeonConfigLoader cfg = DungeonConfigLoader.Instance;
        int baseTk = cfg != null ? cfg.GetInt("TkBase", 20) : 20;
        int perCart = cfg != null ? cfg.GetInt("TkPerCart", 6) : 6;
        int goldenBonus = cfg != null ? cfg.GetInt("TkGoldenBonus", 12) : 12;

        return (baseTk + perCart * carts + goldenBonus * (carts / Mathf.Max(1, goldenEvery))) * mult;
    }

    // 금고 룰렛: ×1/×2/×3 확률(%)은 시트 TkJackpotProb1~3 — 프로토 기대값 ×1.5
    int RollJackpot()
    {
        DungeonConfigLoader cfg = DungeonConfigLoader.Instance;
        float p1 = cfg != null ? cfg.GetFloat("TkJackpotProb1", 60f) : 60f;
        float p2 = cfg != null ? cfg.GetFloat("TkJackpotProb2", 30f) : 30f;
        float p3 = cfg != null ? cfg.GetFloat("TkJackpotProb3", 10f) : 10f;

        float r = Random.Range(0f, p1 + p2 + p3);
        if (r < p1) return 1;
        if (r < p1 + p2) return 2;
        return 3;
    }
}
