using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;

public class AllBulletList : MonoBehaviour
{
    public static AllBulletList Instance { get; private set; }

    public List<BulletInfoSO> bulletInfoSOs = new();
    public Dictionary<int, BulletInfoSO> bulletInfoSODic = new();
    public Dictionary<int, BulletInfo> bulletInfos = new();
    public Dictionary<BulletInfoSO, int> bulletCount = new();
    public Action<BulletInfoSO, int> onBulletAdded;
    public Inventory inventory;

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach(BulletInfoSO so in bulletInfoSOs)
        {
            bulletInfoSODic.Add(so.bulletId, so);
        }
    }

    public void DrawBullet()
    {
        int id = UnityEngine.Random.Range(0, 4);

        if (bulletInfos.ContainsKey(id))
        {
            bulletInfos[id].IncreaseCount(1);
        }
        else
        {
            bulletInfos.Add(id, new BulletInfo(bulletInfoSODic[id]));
            inventory.AddBullet(bulletInfos[id]);
        }
    }
}
