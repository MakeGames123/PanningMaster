using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 던전 전투 필드. 기존 필드를 재활용하되 노말과 다른 매커니즘:
// 적 체력을 10배로 만들고 제한시간(15초) 안에 처치하면 클리어, 못 잡으면 실패.
// 끝나면 FieldManager를 통해 일반 전투로 복귀.
public class DungeonField : MonoBehaviour
{
    [SerializeField] BattleResult battleResult;
    [SerializeField] float hpMultiplier = 10f; //적 체력 배수
    [SerializeField] float timeLimit = 15f;    //제한시간(초)

    [Header("제한시간 UI")]
    [SerializeField] GameObject timerRoot;      //타이머 UI 루트(켜고 끔)
    [SerializeField] TextMeshProUGUI timerText; //남은 시간 텍스트
    [SerializeField] Slider timerSlider;        //남은 시간 게이지(선택)

    Player player;
    Enemy enumy;
    float savedMaxHp;
    Coroutine timerCo;
    bool resolved;
    int currentFloor;

    void Awake()
    {
        if (timerRoot != null) timerRoot.SetActive(false);
    }

    // FieldManager가 던전을 활성화할 때 호출
    public void Begin(Player p, int floor)
    {
        player = p;
        enumy = p.Enemy;
        resolved = false;
        currentFloor = floor;

        savedMaxHp = enumy.MaxHp;
        enumy.SetMaxHp(savedMaxHp * hpMultiplier); //체력 10배 + 풀피

        if (timerRoot != null) timerRoot.SetActive(true);

        timerCo = StartCoroutine(TimerRoutine());
        player.StartCycle();
    }

    public void Stop()
    {
        StopAllCoroutines();
        timerCo = null;
        if (timerRoot != null) timerRoot.SetActive(false);
    }

    // Player.onCycleComplete 에 FieldManager가 할당하는 유일한 핸들러
    public void OnCycleComplete(bool killed)
    {
        if (resolved) return;

        if (killed) Resolve(true);             //제한시간 내 처치 = 클리어
        else player.ReloadAndStartCycle();     //아직 시간 남음 -> 장전 후 계속 공격
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

        Resolve(false); //시간 초과 = 실패
    }

    void UpdateTimerUI(float remaining)
    {
        if (timerText != null) timerText.text = remaining.ToString("0.0");
        if (timerSlider != null) timerSlider.value = timeLimit > 0f ? remaining / timeLimit : 0f;
    }

    void Resolve(bool success)
    {
        if (resolved) return;
        resolved = true;

        if (timerCo != null) { StopCoroutine(timerCo); timerCo = null; }
        if (timerRoot != null) timerRoot.SetActive(false);

        player.StopBattle();

        StartCoroutine(End(success));
    }

    IEnumerator End(bool success)
    {
        yield return new WaitForSeconds(0.5f);

        battleResult.ShowResult(success ? "던전 클리어" : "던전 실패");

        if (success)
        {
            DataManager.Instance.ClearDungeonFloor(currentFloor); //클리어 층 기록
            if (QuestEventManager.Instance != null) QuestEventManager.Instance.AddEvent("dgClear"); //업적: 던전 클리어
        }

        yield return new WaitForSeconds(2f);

        enumy.SetMaxHp(savedMaxHp);             //체력 원상 복구
        FieldManager.Instance.ReturnToNormal(); //일반 전투 복귀
    }
}
