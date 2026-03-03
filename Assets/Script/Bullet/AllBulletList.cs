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
    public (int, bool) DrawBullet(int tier)//id, 탄환 레벨업 여부 반환
    {
        int id = UnityEngine.Random.Range(0, 4) + tier * 4 + 1000;
        if (bulletInfos.ContainsKey(id))
        {
            int level = bulletInfos[id].Level;

            bulletInfos[id].IncreaseCount(1);
            bulletInfos[id].Level = BulletLevelLoader.Instance.GetLevelByBulletCount(bulletInfos[id].ReturnCount());

            onBulletAdded.Invoke(id);

            DataManager.Instance.possPower = CalculatePossPower();

            if (bulletInfos[id].Count == 1) inventory.AddBullet(id);

            if (level != bulletInfos[id].Level) return (id, true);
        }

        return (id, false);
    }
    public void LoadRefresh()
    {
        inventory.ActiveAll();
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
