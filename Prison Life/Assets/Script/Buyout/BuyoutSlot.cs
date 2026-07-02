using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BuyoutSlot : MonoBehaviour
{
    [Header("Price Settings")]
    [SerializeField] protected int price = 5;// 가격

    [SerializeField] protected Player player;// 가격
    [SerializeField] protected BuyoutSlotUI ui;
    private int _money = 0;

    // 돈 프로퍼티
    public int Money
    {
        get => _money;
        private set
        {
            _money = value;
            ui.UpdateUI(price, _money);
            if (_money >= price) OnPurchaseComplete();
        }
    }
    void Awake()
    {
        ui.UpdateUI(price, 0);
    }
    public void AddMoney()
    {
        Money++;
    }
    public int GetRemainingPrice()
    {
        return price - Money;
    }
    protected virtual void OnPurchaseComplete()
    {
        gameObject.SetActive(false);
    }
}
