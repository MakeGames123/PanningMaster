using System.Collections.Generic;
using UnityEngine;

public class MovingItemPool : MonoBehaviour
{
    // 어디서나 쉽게 접근할 수 있도록 싱글톤 세팅
    public static MovingItemPool Instance { get; private set; }

    [System.Serializable]
    public struct Pool
    {
        public ItemType itemKey;          // 아이템을 식별할 이름 (예: "Gold", "Iron", "Gem")
        public GameObject prefab;       // 생성할 프리팹
        public int initialSize;         // 게임 시작 시 미리 생성해 둘 개수
    }

    [Header("Pool Configurations")]
    [SerializeField] private List<Pool> pools;

    // 각 아이템 종류별로 대기 상태의 오브젝트들을 보관할 딕셔너리 큐
    private Dictionary<ItemType, Queue<ItemMoving>> poolDictionary = new Dictionary<ItemType, Queue<ItemMoving>>();
    // 프리팹 참조용 딕셔너리 (풀이 모자라 새로 생성해야 할 때 사용)
    private Dictionary<ItemType, GameObject> prefabDictionary = new Dictionary<ItemType, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializePools();
    }

    /// <summary>
    /// 게임 시작 시 설정된 프리팹들을 미리 생성하여 비활성화 상태로 풀에 채워둡니다.
    /// </summary>
    private void InitializePools()
    {
        foreach (var pool in pools)
        {
            if (poolDictionary.ContainsKey(pool.itemKey)) continue;

            Queue<ItemMoving> objectPool = new Queue<ItemMoving>();
            prefabDictionary[pool.itemKey] = pool.prefab;

            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = Instantiate(pool.prefab, transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj.GetComponent<ItemMoving>());
            }

            poolDictionary.Add(pool.itemKey, objectPool);
        }
    }

    /// <summary>
    /// 풀에서 아이템을 하나 꺼내어 활성화합니다. (모자라면 자동 추가 생성)
    /// </summary>
    public ItemMoving Spawn(ItemType itemKey, Vector3 posA, Vector3 posB)
    {
        if (!poolDictionary.ContainsKey(itemKey))
        {
            Debug.LogWarning($"[Pool] {itemKey} 라는 이름의 풀이 존재하지 않습니다.");
            return null;
        }

        ItemMoving objToSpawn;

        // 풀에 남아있는 오브젝트가 있다면 꺼내 쓰고, 없다면 새로 확장 생성
        if (poolDictionary[itemKey].Count > 0)
        {
            objToSpawn = poolDictionary[itemKey].Dequeue();
        }
        else
        {
            objToSpawn = Instantiate(prefabDictionary[itemKey], transform).GetComponent<ItemMoving>();
        }

        objToSpawn.gameObject.SetActive(true);
        objToSpawn.FlyToTarget(posA, posB);

        return objToSpawn;
    }

    /// <summary>
    /// 풀에서 아이템을 하나 꺼내어 활성화합니다. (모자라면 자동 추가 생성) 광부 전용
    /// </summary>
    public ItemMoving SimpleSpawn(ItemType itemKey, Vector3 posA, Vector3 posB)
    {
        if (!poolDictionary.ContainsKey(itemKey))
        {
            Debug.LogWarning($"[Pool] {itemKey} 라는 이름의 풀이 존재하지 않습니다.");
            return null;
        }

        ItemMoving objToSpawn;

        // 풀에 남아있는 오브젝트가 있다면 꺼내 쓰고, 없다면 새로 확장 생성
        if (poolDictionary[itemKey].Count > 0)
        {
            objToSpawn = poolDictionary[itemKey].Dequeue();
        }
        else
        {
            objToSpawn = Instantiate(prefabDictionary[itemKey], transform).GetComponent<ItemMoving>();
        }

        objToSpawn.gameObject.SetActive(true);
        objToSpawn.FlyToTarget(posA, posB);

        return objToSpawn;
    }

    /// <summary>
    /// 사용이 끝난 아이템을 다시 풀로 안전하게 반납합니다.
    /// </summary>
    public void Despawn(ItemType itemKey, ItemMoving obj)
    {
        if (!poolDictionary.ContainsKey(itemKey))
        {
            Destroy(obj);
            return;
        }

        obj.gameObject.SetActive(false);
        poolDictionary[itemKey].Enqueue(obj);
    }
}