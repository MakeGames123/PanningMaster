using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 연구소 던전 필드(정예 열쇠 소모). 적 여러 마리가 오른쪽에서 일렬로 소환되어 왼쪽으로 다가온다.
// 표적은 항상 맨 앞(가장 왼쪽)의 살아있는 적: 적이 죽으면 다음 총알은 자동으로 다음 적을 노리고,
// 아직 소환된 적이 없으면 그 총알은 스킵된다(Player의 다중 표적 모드).
// 전부 처치하면 클리어, 한 마리라도 방어선(defeatX)에 도달하면 실패.
public class LabDungeonField : MonoBehaviour
{
    [SerializeField] BattleResult battleResult;
    [SerializeField] LabDungeonEnemy enemyPrefab;
    [SerializeField] RectTransform enemyParent; //적이 생성될 부모(필드 루트)

    [Header("소환")]
    [SerializeField] int enemyCount = 10;      //총 소환 수
    [SerializeField] float spawnInterval = 1f; //소환 간격(초)
    [SerializeField] float spawnX = 900f;      //소환 x(화면 오른쪽 밖)
    [SerializeField] float enemyY = 0f;        //일렬 y
    [SerializeField] float spawnYJitter = 30f; //소환 시 위아래 랜덤 편차(±)

    [Header("전투")]
    [SerializeField] float moveSpeed = 60f;    //왼쪽 이동 속도(px/초)
    [SerializeField] float defeatX = -450f;    //적이 이 x에 도달하면 실패
    [SerializeField] float hpMultiplier = 1f;  //적 1마리 체력 = 현재 스테이지 몬스터 체력 * 배수

    [Header("진행 UI")]
    [SerializeField] GameObject progressRoot;       //진행 UI 루트(켜고 끔)
    [SerializeField] TextMeshProUGUI progressText;  //"3/10"

    PlayerSquad player;
    readonly List<LabDungeonEnemy> enemies = new(); //죽어서 파괴되면 항목이 null이 된다
    int spawnedCount;
    Coroutine spawnCo;
    bool running;
    bool resolved;

    void Awake()
    {
        if (progressRoot != null) progressRoot.SetActive(false);
    }

    // FieldManager가 연구소 던전을 활성화할 때 호출
    public void Begin(PlayerSquad p)
    {
        player = p;
        resolved = false;
        running = true;
        spawnedCount = 0;
        ClearEnemies();

        if (progressRoot != null) progressRoot.SetActive(true);
        UpdateProgressUI();

        spawnCo = StartCoroutine(SpawnRoutine());
        player.StartCycle();
    }

    public void Stop()
    {
        StopAllCoroutines();
        spawnCo = null;
        running = false;
        if (progressRoot != null) progressRoot.SetActive(false);
        ClearEnemies();
    }

    // Player.onCycleComplete 에 FieldManager가 할당하는 유일한 핸들러.
    // 다중 표적 모드에서는 사이클 성패와 무관하게, 던전이 끝나기 전까지 장전 후 계속 공격한다.
    public void OnCycleComplete(bool _)
    {
        if (resolved) return;
        player.ReloadAndStartCycle();
    }

    // Player의 표적 공급자: 맨 앞(가장 왼쪽) 살아있는 적. 없으면 null(그 총알은 스킵됨)
    public IFireTarget GetCurrentTarget()
    {
        LabDungeonEnemy front = null;
        foreach (LabDungeonEnemy e in enemies)
        {
            if (e == null || !e.IsAlive) continue;
            if (front == null || e.X < front.X) front = e;
        }
        return front;
    }

    // 순차 소환 코루틴: 첫 마리는 즉시, 이후 spawnInterval 간격으로 enemyCount까지 소환
    IEnumerator SpawnRoutine()
    {
        while (spawnedCount < enemyCount)
        {
            Spawn();
            yield return new WaitForSeconds(spawnInterval);
        }
        spawnCo = null;
    }

    void Update()
    {
        if (!running || resolved) return;

        // 실패: 살아있는 적이 방어선 도달
        foreach (LabDungeonEnemy e in enemies)
        {
            if (e != null && e.IsAlive && e.X <= defeatX)
            {
                Resolve(false);
                return;
            }
        }

        // 클리어: 전부 소환됐고 살아있는 적이 없음
        if (spawnedCount >= enemyCount && GetCurrentTarget() == null)
            Resolve(true);

        UpdateProgressUI();
    }

    void Spawn()
    {
        LabDungeonEnemy e = Instantiate(enemyPrefab, enemyParent);
        float y = enemyY + Random.Range(-spawnYJitter, spawnYJitter);
        ((RectTransform)e.transform).anchoredPosition = new Vector2(spawnX, y);

        float hp = MonsterHpTableLoader.Instance.GetHP(DataManager.Instance.stage) * hpMultiplier;
        e.Init(hp, moveSpeed);

        enemies.Add(e);
        spawnedCount++;
    }

    int KilledCount()
    {
        int killed = 0;
        foreach (LabDungeonEnemy e in enemies)
            if (e == null || !e.IsAlive) killed++;
        return killed;
    }

    void UpdateProgressUI()
    {
        if (progressText != null) progressText.text = $"{KilledCount()}/{enemyCount}";
    }

    void Resolve(bool success)
    {
        if (resolved) return;
        resolved = true;
        running = false;

        if (spawnCo != null) { StopCoroutine(spawnCo); spawnCo = null; } //남은 소환 중단

        player.StopBattle();

        // 남은 적 정지(실패 시 화면 밖까지 계속 걸어가지 않게)
        foreach (LabDungeonEnemy e in enemies)
            if (e != null && e.IsAlive) e.Freeze();

        StartCoroutine(End(success));
    }

    IEnumerator End(bool success)
    {
        yield return new WaitForSeconds(0.5f);

        battleResult.ShowResult(success ? "연구소 던전 클리어" : "연구소 던전 실패");
        //TODO: 클리어 보상 지급 위치

        yield return new WaitForSeconds(2f);

        FieldManager.Instance.ReturnToNormal(); //일반 전투 복귀(복귀 과정에서 Stop이 호출돼 잔여 적 정리)
    }

    void ClearEnemies()
    {
        foreach (LabDungeonEnemy e in enemies)
            if (e != null) Destroy(e.gameObject);
        enemies.Clear();
    }
}
