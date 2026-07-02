using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using TMPro;

public interface ItemInteractive
{
    bool CheckItemCount(ItemType type);
    void AddItem(ItemType type, Vector3 pos);
    bool GetItem(ItemType type, Vector3 pos);
    float ReturnInterval();
}

public class Player : MonoBehaviour, ItemInteractive
{
    [SerializeField] private PlayerMaxText maxText;

    [Header("Stacks")]
    [SerializeField] private InventoryStack ironInventory;
    [SerializeField] private InventoryStack handCuffInventory;
    [SerializeField] private InventoryStack moneyInventory;

    [Header("Player Tools")]
    [SerializeField] private List<GameObject> tools;

    [Header("Inventory Settings")]
    [SerializeField] private int maxIronOreCount = 12;
    [SerializeField] private int maxHandcuffCount = 12;
    [SerializeField] private int maxMoneyCount = 9999;

    [Header("Stack Positions (Local)")]
    [Tooltip("철광석이 있거나, 철광석이 없고 돈만 있을 때 사용하는 1순위 위치")]
    [SerializeField] private Vector3 firstPosition;
    [Tooltip("철광석과 돈이 동시에 존재할 때 돈이 밀려나는 2순위 위치")]
    [SerializeField] private Vector3 secondPosition;

    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private float interactiveInterval = 0.05f;
    private int toolTier = 0;

    public UnityEvent onMiningStart = new();

    // 아이템 타입을 Key로 사용하는 딕셔너리 데이터베이스
    private Dictionary<ItemType, ItemData> itemInventory = new Dictionary<ItemType, ItemData>();

    // 외부 노출용 프로퍼티
    public int IronOreCount => itemInventory.ContainsKey(ItemType.Iron) ? itemInventory[ItemType.Iron].CurrentCount : 0;
    public int HandcuffCount => itemInventory.ContainsKey(ItemType.Handcuff) ? itemInventory[ItemType.Handcuff].CurrentCount : 0;
    public int MoneyCount => itemInventory.ContainsKey(ItemType.Money) ? itemInventory[ItemType.Money].CurrentCount : 0;
    public UnityEvent<int> OnHandcuffCountChanged;
    public UnityEvent<int> OnMoneyCountChanged;

    private void Awake()
    {
        // 딕셔너리 초기화 및 최대 수치 세팅
        itemInventory.Add(ItemType.Iron, new ItemData("철광석", maxIronOreCount, ironInventory));
        itemInventory.Add(ItemType.Handcuff, new ItemData("수갑", maxHandcuffCount, handCuffInventory));
        itemInventory.Add(ItemType.Money, new ItemData("돈", maxMoneyCount, moneyInventory));
    }

    private void Start()
    {
        // [추가] 철광석과 돈의 개수 변경 이벤트 구독
        itemInventory[ItemType.Iron].OnCountChanged += HandleInventoryChanged;
        itemInventory[ItemType.Money].OnCountChanged += HandleInventoryChanged;
        itemInventory[ItemType.Money].OnCountChanged += ChangeMoneyText;

        // 시작할 때 초기 위치 정렬
        RepositionStacks();
    }

    /// <summary>
    /// 철광석이나 돈의 개수가 바뀔 때마다 호출되는 이벤트 핸들러
    /// </summary>
    private void HandleInventoryChanged(int currentCount, int MaxCount)
    {
        RepositionStacks();
    }

    /// <summary>
    /// 핵심 로직: 철광석과 돈의 보유 상태를 체크하여 스택의 로컬 위치를 스위칭합니다.
    /// </summary>
    private void RepositionStacks()
    {
        bool hasIron = IronOreCount > 0;
        bool hasMoney = MoneyCount > 0;

        // 1. 철광석이 있다면 무조건 철광석이 1순위(firstPosition)
        if (hasIron)
        {
            ironInventory.transform.localPosition = firstPosition;

            // 철광석이 있는 상태에서 돈도 있다면 돈은 2순위(secondPosition)로 밀려남
            if (hasMoney)
            {
                moneyInventory.transform.localPosition = secondPosition;
            }
        }
        // 2. 철광석이 없는데 돈만 있다면 돈이 1순위(firstPosition)를 차지
        else if (hasMoney)
        {
            moneyInventory.transform.localPosition = firstPosition;
        }

        // 수갑(Handcuff)은 이 조건에서 제외이므로 기존 고정 위치를 유지합니다.
    }

    /// <summary>
    /// 아이템 획득 전에 손이 가득 찼는지 체크
    /// </summary>
    public bool CheckItemCount(ItemType type)
    {
        if (!itemInventory.ContainsKey(type)) return false;

        ItemData targetItem = itemInventory[type];
        return targetItem.CurrentCount < targetItem.MaxCount;
    }

    /// <summary>
    /// 아이템을 획득합니다. 성공 시 비주얼 스택을 켭니다.
    /// </summary>
    public void AddItem(ItemType type, Vector3 pos)
    {
        if (!itemInventory.ContainsKey(type)) return;

        ItemData targetItem = itemInventory[type];

        if (targetItem.Add(1))
        {
            //zero가 들어오는 경우는 광석 채굴시 혹은 애니메이션 재생 x
            if (pos != Vector3.zero) MovingItemPool.Instance.Spawn(type, pos, targetItem.InventoryStack.ReturnFrontPos());
        }
        else
        {
            //Debug.LogWarning($"{targetItem.Name} 보관량이 가득 찼습니다!");

            if (type == ItemType.Iron)//철만 max 텍스트 띄우기
            {
                maxText.gameObject.SetActive(true);
                maxText.Initialize(ironInventory.transform);
            }
        }

        if (type == ItemType.Handcuff) OnHandcuffCountChanged.Invoke(targetItem.CurrentCount);
        if (type == ItemType.Money) OnMoneyCountChanged.Invoke(targetItem.CurrentCount);
    }

    /// <summary>
    /// 아이템을 소모합니다. 성공 시 비주얼 스택을 끕니다.
    /// </summary>
    public bool GetItem(ItemType type, Vector3 pos)
    {
        if (!itemInventory.ContainsKey(type)) return false;

        ItemData targetItem = itemInventory[type];

        if (targetItem.Remove(1))
        {
            MovingItemPool.Instance.Spawn(type, targetItem.InventoryStack.ReturnFrontPos(), pos);
            //Debug.Log($"[Player] {targetItem.Name} 소모! 남은 개수: {targetItem.CurrentCount}");
            return true;
        }
        return false;
    }

    public float ReturnInterval()
    {
        return interactiveInterval;
    }

    public void UpgradeTool()
    {
        toolTier = Mathf.Min(toolTier + 1, 2);
        itemInventory[ItemType.Iron].UpdateMaxCount(5);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MiningArea"))
        {
            foreach (GameObject tool in tools)
            {
                tool.SetActive(false);
            }
            tools[toolTier].SetActive(true);

            onMiningStart.Invoke();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MiningArea"))
        {
            foreach (GameObject tool in tools)
            {
                tool.SetActive(false);
            }
        }
    }
    private void ChangeMoneyText(int val, int max)
    {
        moneyText.text = (val * 5).ToString();
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위한 이벤트 해제
        if (itemInventory.ContainsKey(ItemType.Iron))
            itemInventory[ItemType.Iron].OnCountChanged -= HandleInventoryChanged;

        if (itemInventory.ContainsKey(ItemType.Money))
            itemInventory[ItemType.Money].OnCountChanged -= HandleInventoryChanged;
    }
}
public class ItemData
{
    public string Name;
    public int CurrentCount;
    public int MaxCount;
    public InventoryStack InventoryStack;

    // 개수가 변경될 때마다 현재 개수를 전달하는 이벤트
    public event Action<int, int> OnCountChanged;

    public ItemData(string name, int maxCount, InventoryStack inventory)
    {
        Name = name;
        MaxCount = maxCount;
        InventoryStack = inventory;
        CurrentCount = 0;
    }
    public void UpdateMaxCount(int addedCount)
    {
        MaxCount += addedCount;
    }
    public bool Add(int amount)
    {
        if (CurrentCount >= MaxCount) return false;

        CurrentCount = Mathf.Clamp(CurrentCount + amount, 0, MaxCount);

        for (int i = 0; i < amount; i++)
        {
            InventoryStack.PushObject();
        }

        // [추가] 이벤트 호출
        OnCountChanged?.Invoke(CurrentCount, MaxCount);
        return true;
    }

    public bool Remove(int amount)
    {
        if (CurrentCount <= 0) return false;

        CurrentCount = Mathf.Max(0, CurrentCount - amount);
        InventoryStack.PopObject();

        // [추가] 이벤트 호출
        OnCountChanged?.Invoke(CurrentCount, MaxCount);
        return true;
    }
}