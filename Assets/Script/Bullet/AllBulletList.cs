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
    public UnityEvent<int> onBulletAdded;
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
            bulletInfos.Add(so.bulletId, new BulletInfo(bulletInfoSODic[so.bulletId]));
        }
    }
    public BulletInfo GetBullet(int id)
    {
        if(id == -1) return null;
        else return bulletInfos[id];
    }
    public void DrawBullet(int tier)
    {
        int id = UnityEngine.Random.Range(0, 4) + tier * 4;
        if (bulletInfos.ContainsKey(id))
        {
            bulletInfos[id].IncreaseCount(1);
            onBulletAdded.Invoke(id);

            if(bulletInfos[id].Count == 1) inventory.AddBullet(id);
        }
    }
}
