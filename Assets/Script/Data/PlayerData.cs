using UnityEngine;

// 플레이어 전투 스탯 데이터. 싱글톤(PlayerData.Instance)으로 접근.
// 리볼버 장착/해제 시 RevolverSlots가 계산한 값을 여기에 반영하고, 계산기는 여기서 값을 읽는다.
public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private float possPower; //보유 효과(전체 공격력 % 증가)
    public float PossPower
    {
        get { return possPower; }
        set { possPower = value; }
    }

    private float power;
    public float Power
    {
        get { return power * (PossPower + 1); }
        set { power = value; }
    }

    //추가 스탯 (ShootSpeed / ReloadSpeed 는 백분율: 값 1 = 속도 1% 상승)
    public float CriticalChance; //치명타 확률
    public float CriticalDamage; //치명타 피해
    public float GoldAcq;        //골드 획득량
    public float ShootSpeed;     //발사 속도(%)
    public float ReloadSpeed;    //장전 속도(%)
    public float Damage;         //데미지
    public float FinalDamage;    //최종 데미지
    public float TypeDamage;     //속성 데미지

    [Header("기본 속도")]
    public float baseReloadTime = 2.5f; //기본 장전 시간(초)
    public float baseAttackSpeed = 0.1f; //기본 공격 간격(초)

    //ReloadSpeed/ShootSpeed(%)를 반영한 실제 값. 속도가 x% 빠르면 걸리는 시간은 /(1 + x/100)
    public float ReloadTime => baseReloadTime / (1f + ReloadSpeed / 100f);
    public float AttackSpeed => baseAttackSpeed / (1f + ShootSpeed / 100f);
}
