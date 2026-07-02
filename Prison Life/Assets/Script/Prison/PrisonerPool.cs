using System.Collections.Generic;
using UnityEngine;

public class PrisonerPool : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private GameObject prefab;    // 풀링할 단 하나의 프리팹
    [SerializeField] private int initSize = 30;    // 게임 시작 시 미리 생성해 둘 초기 개수

    // 풀링된 오브젝트들을 담아둘 단 하나의 큐
    private Queue<GameObject> poolQueue = new Queue<GameObject>();

    private void Awake()
    {
        InitializePool();
    }

    /// <summary>
    /// 게임 시작 시 설정된 크기만큼 오브젝트를 미리 생성(Prewarm)합니다.
    /// </summary>
    private void InitializePool()
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[{name}] Prefab이 할당되지 않았습니다.");
            return;
        }

        for (int i = 0; i < initSize; i++)
        {
            CreateNewObject();
        }
    }

    /// <summary>
    /// 오브젝트를 새로 생성하여 풀(Queue)에 비활성화 상태로 집어넣습니다.
    /// </summary>
    private void CreateNewObject()
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        poolQueue.Enqueue(obj);
    }

    /// <summary>
    /// [핵심] 풀에서 오브젝트를 하나 꺼내어 원하는 위치와 회전값으로 배치하고 활성화합니다.
    /// </summary>
    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        if (poolQueue.Count == 0)
        {
            // [자동 확장] 혹시 생성 속도가 너무 빨라 풀이 바닥났다면 실시간으로 하나 더 생성
            CreateNewObject();
        }

        // 큐의 맨 앞에서 비활성화 상태인 오브젝트를 꺼냅니다.
        GameObject obj = poolQueue.Dequeue();

        // 오브젝트 배치 및 활성화
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        return obj;
    }

    /// <summary>
    /// 사용이 끝난 오브젝트를 다시 풀로 안전하게 반납합니다.
    /// </summary>
    public void Despawn(GameObject obj)
    {
        // 오브젝트를 끄고 다시 큐의 맨 뒤로 넣어 재활용 대기 상태로 만듭니다.
        obj.SetActive(false);
        poolQueue.Enqueue(obj);
    }
}