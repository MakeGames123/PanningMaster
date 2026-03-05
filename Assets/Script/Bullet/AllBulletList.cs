using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using System.Net.Sockets;
using System.Linq;

public class AllBulletList : MonoBehaviour
{
    public static AllBulletList Instance { get; private set; }

    public List<BulletInfoSO> bulletInfoSOs = new();
    public Dictionary<int, BulletInfoSO> bulletInfoSODic = new();
    public Dictionary<int, BulletInfo> bulletInfos = new();
    public UnityEvent<int> onBulletChanged;
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

        foreach (BulletInfoSO so in bulletInfoSOs)
        {
            bulletInfoSODic.Add(so.bulletId, so);
            bulletInfos.Add(so.bulletId, new BulletInfo(bulletInfoSODic[so.bulletId]));
        }
    }
    public BulletInfo GetBullet(int id)
    {
        if (id == -1) return null;
        else return bulletInfos[id];
    }
    public void LoadRefresh()
    {
        foreach (BulletInfo info in bulletInfos.Values)
        {
            onBulletChanged.Invoke(info.infoSO.bulletId);
        }
    }
    public void AddBullet(DrawInfo drawInfo)
    {
        BulletInfo bulletInfo = bulletInfos[drawInfo.Id];

        bulletInfo.Level = drawInfo.Level;
        bulletInfo.Count = drawInfo.Count;

        onBulletChanged.Invoke(drawInfo.Id);
    }
    private float CalculatePossPower()
    {
        float power = 0;
        List<float> posses = TierDataLoader.Instance.ReturnColumn(t => t.possScale);

        foreach (BulletInfo info in bulletInfos.Values)
        {
            power += info.Level * posses[info.infoSO.tier];
        }

        return power;
    }
}
