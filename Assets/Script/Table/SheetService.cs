using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

// POCO 시트 로더가 구현하는 인터페이스. SheetService가 Url을 fetch해 Parse에 CSV를 넘긴다.
public interface ISheetLoader
{
    string Url { get; }
    void Parse(string csv);
}

// 모든 시트 로더(POCO)의 로딩을 한 곳에서 구동하는 서비스.
// 로더들을 생성(각 생성자가 자신의 static Instance 설정)하고, URL을 병렬로 받아 Parse에 넘긴 뒤
// 전부 끝나면 OnAllTablesLoaded 를 발화한다. (기존 TableLoaderManager 대체)
public class SheetService : MonoBehaviour
{
    public static SheetService Instance { get; private set; }

    public UnityEvent OnAllTablesLoaded = new();
    public bool IsAllLoaded { get; private set; }

    readonly List<ISheetLoader> loaders = new();
    int loadedCount;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // POCO 로더 생성 — 각 생성자가 자신의 static Instance 를 설정한다.
        loaders.Add(new TierDataLoader());
        loaders.Add(new BulletGachaProbLoader());
        loaders.Add(new BulletLevelLoader());
        loaders.Add(new BulletDrawLevelLoader());
        loaders.Add(new GameConfigLoader());
        loaders.Add(new GrowthStatLoader());
        loaders.Add(new CraftConditionLoader());
        loaders.Add(new LabNodeLoader());
        loaders.Add(new LabEdgeLoader());
        loaders.Add(new QuestMainLoader());
        loaders.Add(new MonsterHpTableLoader());
        loaders.Add(new StatTableLoader());
        loaders.Add(new StatTypeLoader());
        loaders.Add(new StatWeightLoader());
        loaders.Add(new NumberFormatLoader());
        loaders.Add(new TutorialStepLoader());
    }

    void Start()
    {
        foreach (var loader in loaders)
            StartCoroutine(Load(loader));
    }

    IEnumerator Load(ISheetLoader loader)
    {
        using UnityWebRequest req = UnityWebRequest.Get(loader.Url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[SheetService] {loader.GetType().Name} 로드 실패: {req.error}");
        }
        else
        {
            try { loader.Parse(req.downloadHandler.text); }
            catch (System.Exception e) { Debug.LogError($"[SheetService] {loader.GetType().Name} 파싱 실패: {e}"); }
        }

        loadedCount++;
        if (loadedCount >= loaders.Count)
        {
            IsAllLoaded = true;
            Debug.Log($"[SheetService] 전체 {loaders.Count}개 시트 로드 완료");
            OnAllTablesLoaded.Invoke();
        }
    }
}
