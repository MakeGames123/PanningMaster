using System;
using System.Collections.Generic;
using UnityEngine;

// 튜토리얼 전용 이벤트 버스. 퀘스트의 QuestEventManager와 완전히 분리된 독립 카운터.
// 게임 행동 발생 시 AddEvent로 누적하고, TutorialManager가 OnEventChanged를 구독해 await 스텝을 진행한다.
// 씬에 미리 두지 않아도 최초 접근 시 자동 생성되는 지연 싱글톤(발행/구독 어느 쪽이 먼저든 같은 인스턴스).
public class TutorialEventManager : MonoBehaviour
{
    static TutorialEventManager instance;
    public static TutorialEventManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<TutorialEventManager>();
                if (instance == null)
                {
                    var go = new GameObject(nameof(TutorialEventManager));
                    instance = go.AddComponent<TutorialEventManager>();
                }
            }
            return instance;
        }
    }

    readonly Dictionary<string, long> values = new();

    // (key, 바뀐 후 값)
    public event Action<string, long> OnEventChanged;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 누적 증가. AddEvent("draw", 10)
    public void AddEvent(string key, long amount = 1)
    {
        if (string.IsNullOrEmpty(key)) return;

        values.TryGetValue(key, out long cur);
        long next = cur + amount;
        values[key] = next;
        OnEventChanged?.Invoke(key, next);
    }

    public long GetValue(string key)
        => (!string.IsNullOrEmpty(key) && values.TryGetValue(key, out long v)) ? v : 0;
}
