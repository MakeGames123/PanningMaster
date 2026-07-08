using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

// PVP 유령 유저 시딩 도구(개발용). 플레이 모드에서 컴포넌트 우클릭 -> "Upload Ghost Users".
// 전투력 숫자를 그냥 넣는 게 아니라, 세공이 안 된(스탯 없는) 탄환 로드아웃을 실제 저장 포맷
// (UserData "BulletInventory")으로 저장하고, 그 로드아웃으로 게임과 동일한 수식으로 계산한
// 전투력을 Power 통계에 등록한다.
//   - 목표 전투력 분포: basePower(10)부터 10배 간격 구간 bucketCount(10)개 × perBucket(10)명 = 100명
//   - 로드아웃 탐색: 티어별 탄환 equipCount(6)개를 같은 레벨로 장착한다고 보고,
//     목표 전투력에 가장 가까운 (티어, 레벨) 조합을 이분 탐색으로 찾는다.
//   - count는 레벨 역산치(GetMinCountForLevel)로 저장 -> 로드 시 LoadRefresh가 같은 레벨로 복원
//   - 유령은 장착할 탄환만 보유하므로 보유 효과(PossPower)도 로드아웃 그대로 반영된다.
// 절차: GHOST_0001~ 로그인(생성) -> 닉네임 -> BulletInventory 저장 -> Power 통계 등록 반복.
// 주의:
// - 테이블(TierData/BulletLevelXP)이 로드된 뒤 실행해야 한다("All Tables Loaded" 로그 이후).
// - 실행 중 세션이 유령 계정으로 바뀌므로 끝나면 기기 계정으로 자동 재로그인한다.
// - Game Manager에서 "클라이언트의 플레이어 통계 게시 허용"이 켜져 있어야 한다.
// - 통계값은 int라 약 21억을 넘는 전투력은 클램프된다. 탄환/레벨 테이블로 도달 불가능한
//   구간은 가장 가까운 도달 가능 전투력으로 대체된다(로그로 확인).
public class PvpGhostSeeder : MonoBehaviour
{
    [SerializeField] PlayFabLoginManager login; //완료 후 원래(기기) 계정 재로그인용

    [Header("등록 설정")]
    [SerializeField] string statisticName = "Power";
    [SerializeField] string pvpRankStatisticName = "PvPRank"; //PVP 점수 순위표
    [SerializeField] string stageStatisticName = "Stage";     //스테이지 순위표
    [SerializeField] string customIdPrefix = "GHOST_";
    [SerializeField] float delayBetweenUsers = 5f; //API 스로틀 방지 간격(초)

    [Header("목표 전투력 분포: 구간당 perBucket명, basePower부터 10배씩 bucketCount개 구간")]
    [SerializeField] int perBucket = 10;
    [SerializeField] int bucketCount = 10;
    [SerializeField] double basePower = 10;

    [Header("로드아웃")]
    [SerializeField] int equipCount = 6; //장착 탄환 수(리볼버 슬롯 수)

    [Header("순위표 분산 범위 (전투력이 강한 유령일수록 높은 값)")]
    [SerializeField] int pvpScoreMax = 1000; //PVP 점수: 1 ~ 이 값으로 분산
    [SerializeField] int stageMax = 1000;    //스테이지: 1층 ~ 이 값으로 분산

    const string BULLET_SAVE_KEY = "BulletInventory"; //SaveManager와 동일 키

    static readonly string[] Names =
    {
        "무법자 잭", "빠른손 케이트", "황야의 콜트", "저격수 로사", "도박꾼 에이스", "검은모자 카일",
        "쌍권총 맥스", "은탄환 실버", "현상금 사냥꾼 밥", "붉은노을 진", "방랑자 하울", "턱수염 더건"
    };

    class Ghost
    {
        public string customId;
        public string displayName;
        public List<BulletSaveData> bullets; //세공 없는 로드아웃(보유 = 장착)
        public int power;                    //로드아웃 기반 계산 전투력
        public int pvpScore;                 //PVP 점수(1~pvpScoreMax 분산)
        public int stage;                    //스테이지(1~stageMax 분산)
        public string loadoutDesc;           //로그용 요약
    }

    DamageCalculator calculator = new();
    List<float> possScales;                       //티어별 possScale
    Dictionary<int, List<BulletInfoSO>> soByTier; //티어별 탄환 SO

    List<Ghost> ghosts;
    int index;
    int failCount;
    bool uploading;

#if UNITY_EDITOR
    void Update()
    {
        // 디버그: F2 -> 유령 유저 업로드
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.f2Key.wasPressedThisFrame)
            Upload();
    }
#endif

    [ContextMenu("Upload Ghost Users")]
    public void Upload()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[GhostSeeder] 플레이 모드에서 실행하세요");
            return;
        }
        if (uploading) return;

        if (!PrepareTables()) return;

        uploading = true;
        BuildGhosts();
        index = 0;
        failCount = 0;
        Debug.Log($"[GhostSeeder] 유령 유저 {ghosts.Count}명 등록 시작");
        ProcessNext();
    }

    // 테이블/탄환 목록 준비. 아직 로드 전이면 중단.
    bool PrepareTables()
    {
        if (TierDataLoader.Instance == null || BulletLevelLoader.Instance == null || AllBulletList.Instance == null)
        {
            Debug.LogError("[GhostSeeder] 테이블/탄환 매니저가 없습니다. 플레이 중인지 확인하세요");
            return false;
        }

        possScales = TierDataLoader.Instance.ReturnColumn(t => t.possScale);
        if (possScales.Count == 0 || BulletLevelLoader.Instance.MaxLevel <= 0)
        {
            Debug.LogError("[GhostSeeder] 테이블이 아직 로드되지 않았습니다. 'All Tables Loaded' 이후 실행하세요");
            return false;
        }

        soByTier = AllBulletList.Instance.bulletInfoSOs
            .Where(so => so.tier >= 0 && so.tier < possScales.Count)
            .GroupBy(so => so.tier)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (soByTier.Count == 0)
        {
            Debug.LogError("[GhostSeeder] 탄환 목록이 비어 있습니다");
            return false;
        }
        return true;
    }

    // 구간별 목표 전투력 -> 로드아웃 탐색 -> 유령 목록 생성
    void BuildGhosts()
    {
        ghosts = new List<Ghost>();
        int n = 1;
        int total = bucketCount * perBucket;

        for (int b = 0; b < bucketCount; b++)
        {
            double min = basePower * System.Math.Pow(10, b);
            double max = min * 10;

            for (int i = 0; i < perBucket; i++)
            {
                double target = min + (max - min) * Random.value;
                Loadout loadout = FindLoadout(target);

                List<BulletSaveData> bullets = loadout.sos.Select(so => new BulletSaveData
                {
                    bulletId = so.bulletId,
                    level = loadout.level,
                    count = BulletLevelLoader.Instance.GetMinCountForLevel(loadout.level),
                    stats = new List<BulletStat>() //세공 안 됨
                }).ToList();

                ghosts.Add(new Ghost
                {
                    customId = $"{customIdPrefix}{n:0000}",
                    displayName = $"{Names[Random.Range(0, Names.Length)]}#{n:000}", //중복 방지 번호
                    bullets = bullets,
                    power = (int)System.Math.Min(System.Math.Round((double)loadout.power), int.MaxValue),
                    pvpScore = DistributedValue(n, total, 1, pvpScoreMax), //강한 유령일수록 높은 점수
                    stage = DistributedValue(n, total, 1, stageMax),      //강한 유령일수록 높은 층
                    loadoutDesc = $"목표 {target:F0} -> 티어{loadout.tier} Lv{loadout.level} x{loadout.sos.Count}"
                });
                n++;
            }
        }
    }

    // n번째(1부터) 유령에게 [min, max] 범위를 총원수로 등분해 분산 배정.
    // 유령은 약한 순서로 생성되므로 전투력 순위와 대체로 일치하고, 자기 구간 안에서만 랜덤이 섞인다.
    int DistributedValue(int n, int total, int min, int max)
    {
        float t = (n - 1 + Random.value) / total;
        return Mathf.Clamp(Mathf.RoundToInt(min + (max - min) * t), min, max);
    }

    class Loadout
    {
        public int tier;
        public int level;
        public List<BulletInfoSO> sos;
        public float power;
    }

    // 목표 전투력에 가장 가까운 (티어, 레벨) 로드아웃 탐색.
    // 전투력은 레벨에 대해 단조 증가하므로 티어마다 이분 탐색 후 로그 스케일로 가장 가까운 것 선택.
    Loadout FindLoadout(double target)
    {
        int maxLevel = BulletLevelLoader.Instance.MaxLevel;
        Loadout best = null;
        double bestErr = double.MaxValue;

        foreach (var kv in soByTier)
        {
            //같은 티어 안에서는 아무 탄환이나 골라도 동일 스펙이므로 랜덤으로 다양성만 준다
            List<BulletInfoSO> sos = kv.Value.OrderBy(_ => Random.value).Take(equipCount).ToList();

            //목표 이상이 되는 최소 레벨
            int lo = 1, hi = maxLevel;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (ComputePower(sos, mid) >= target) hi = mid;
                else lo = mid + 1;
            }

            //경계 레벨과 그 아래 중 더 가까운 쪽
            foreach (int lv in new[] { lo - 1, lo })
            {
                if (lv < 1 || lv > maxLevel) continue;
                if (BulletLevelLoader.Instance.GetMinCountForLevel(lv) < 0) continue;

                float p = ComputePower(sos, lv);
                double err = System.Math.Abs(System.Math.Log10(System.Math.Max(1e-3, p)) - System.Math.Log10(target));
                if (err < bestErr)
                {
                    bestErr = err;
                    best = new Loadout { tier = kv.Key, level = lv, sos = sos, power = p };
                }
            }
        }
        return best;
    }

    // 게임과 동일한 전투력 계산:
    // RevolverSlots.CheckSlots(장착 6칸 CalculateDamage.Item1 합) 후 PlayerData.Power(× (1+보유효과))
    float ComputePower(List<BulletInfoSO> sos, int level)
    {
        List<BulletInfo> slots = new();
        float poss = 0f;

        foreach (BulletInfoSO so in sos)
        {
            BulletInfo info = new BulletInfo(so); //stats 비어 있음 = 세공 안 됨
            info.Level = level;
            slots.Add(info);
            poss += level * possScales[so.tier]; //보유 효과(유령은 장착 탄환만 보유)
        }
        while (slots.Count < 6) slots.Add(null); //빈 약실

        DamageModifier mod = calculator.CollectModifiers(slots);

        float revolverPower = 0f;
        for (int i = 0; i < 6; i++)
            revolverPower += calculator.CalculateDamage(slots[i], mod, i, poss).Item1;

        return revolverPower * (1f + poss); //PlayerData.Power 표기 기준
    }

    void ProcessNext()
    {
        if (index >= ghosts.Count)
        {
            Finish();
            return;
        }

        Ghost g = ghosts[index];
        PlayFabClientAPI.LoginWithCustomID(
            new LoginWithCustomIDRequest { CustomId = g.customId, CreateAccount = true },
            _ => SetDisplayName(g),
            e => Skip(g, "로그인 실패", e));
    }

    void SetDisplayName(Ghost g)
    {
        PlayFabClientAPI.UpdateUserTitleDisplayName(
            new UpdateUserTitleDisplayNameRequest { DisplayName = g.displayName },
            _ => SetInventory(g),
            e =>
            {
                // 닉네임 실패(중복/필터)해도 데이터는 저장하고 계속 진행
                Debug.LogWarning($"[GhostSeeder] {g.customId} 닉네임 실패: {e.GenerateErrorReport()}");
                SetInventory(g);
            });
    }

    // 세공 없는 탄환 인벤토리를 실제 저장 포맷 그대로 UserData에 저장
    void SetInventory(Ghost g)
    {
        string json = JsonUtility.ToJson(new BulletInventoryWrapper { bullets = g.bullets });

        PlayFabClientAPI.UpdateUserData(
            new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> { { BULLET_SAVE_KEY, json } }
            },
            _ => SetPower(g),
            e => Skip(g, "인벤토리 저장 실패", e));
    }

    void SetPower(Ghost g)
    {
        PlayFabClientAPI.UpdatePlayerStatistics(
            new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate { StatisticName = statisticName, Value = g.power },
                    new StatisticUpdate { StatisticName = pvpRankStatisticName, Value = g.pvpScore }, //PVP 점수(1~1000 분산)
                    new StatisticUpdate { StatisticName = stageStatisticName, Value = g.stage }       //스테이지(1~1000 분산)
                }
            },
            _ =>
            {
                Debug.Log($"[GhostSeeder] {index + 1}/{ghosts.Count} 완료: {g.displayName} 전투력 {g.power} / PVP {g.pvpScore}점 / {g.stage}층 ({g.loadoutDesc})");
                Advance();
            },
            e => Skip(g, "전투력 등록 실패", e));
    }

    void Skip(Ghost g, string reason, PlayFabError e)
    {
        failCount++;
        Debug.LogError($"[GhostSeeder] {g.customId} {reason}: {e.GenerateErrorReport()}");
        Advance();
    }

    void Advance()
    {
        index++;
        StartCoroutine(DelayNext());
    }

    IEnumerator DelayNext()
    {
        yield return new WaitForSeconds(delayBetweenUsers);
        ProcessNext();
    }

    void Finish()
    {
        uploading = false;
        Debug.Log($"[GhostSeeder] 등록 종료: 성공 {ghosts.Count - failCount} / 실패 {failCount}");

        // 세션이 마지막 유령 계정으로 남아있으므로 기기 계정으로 복귀
        if (login != null)
        {
            Debug.Log("[GhostSeeder] 기기 계정으로 재로그인");
            login.Login();
        }
        else
        {
            Debug.LogWarning("[GhostSeeder] login 참조가 비어 있음. 게임을 재시작해 기기 계정으로 돌아가세요");
        }
    }
}
