using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using System.Net.Sockets;
using System.Linq;
using Unity.Android.Gradle.Manifest;

public class AllBulletList : MonoBehaviour
{
    public static AllBulletList Instance { get; private set; }

    public List<BulletInfoSO> bulletInfoSOs = new();
    public Dictionary<int, BulletInfoSO> bulletInfoSODic = new();
    public Dictionary<int, BulletInfo> bulletInfos = new();
    public UnityEvent<int> onBulletChanged;
    public Inventory inventory;
    public SheetService table;
    List<float> posses;

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

        table = FindFirstObjectByType<SheetService>();

        table.OnAllTablesLoaded.AddListener(LoadData);
    }
    public void LoadData()
    {
        posses = TierDataLoader.Instance.ReturnColumn(t => t.possScale);
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
            info.Level = BulletLevelLoader.Instance.GetLevelByBulletCount(info.Count);
            onBulletChanged.Invoke(info.infoSO.bulletId);
        }
    }
    //뽑기외에 탄환 정보 업데이트
    public void UpdateBullet(BulletInfo info)
    {
        BulletInfo bulletInfo = bulletInfos[info.infoSO.bulletId];

        bulletInfo.Count = info.Count;

        bulletInfo.Level = BulletLevelLoader.Instance.GetLevelByBulletCount(bulletInfo.Count);

        onBulletChanged.Invoke(info.infoSO.bulletId);
    }
    //뽑기에서 적용
    public void UpdateBullet(DrawInfo drawInfo)
    {
        BulletInfo bulletInfo = bulletInfos[drawInfo.Id];

        bulletInfo.Count = drawInfo.Count;

        bulletInfo.Level = BulletLevelLoader.Instance.GetLevelByBulletCount(bulletInfo.Count);

        DataManager.Instance.UpdatePossPower(CalculatePossPower());

        onBulletChanged.Invoke(drawInfo.Id);
    }
    private float CalculatePossPower()
    {
        float power = 0;

        foreach (BulletInfo info in bulletInfos.Values)
        {
            power += info.Level * posses[info.infoSO.tier];
        }

        return power;
    }

}
