using System.Collections.Generic;

// 뽑기로 생성되는 무기 1자루. 등급이 왕, 레벨은 저축(프로토 v34c 가치 보존 원칙).
[System.Serializable]
public class WeaponData
{
    public int level;  // 등장 시점의 뽑기 레벨(=티어 외형)
    public int grade;  // 등급 인덱스(0=E ~ 12=MR)
    public long atk;   // 주스탯 공격력 = AtkBase × 1.15^(lv-1) × 등급배수 × 지터
    public List<WeaponSub> subs = new(); // 부옵 목록

    // 부옵 값 조회(해당 스탯이 없으면 0)
    public float GetSub(string sid)
    {
        foreach (var s in subs)
            if (s.sid == sid) return s.value;
        return 0f;
    }
}

// 무기 부옵 1줄 (스탯 키 + 값%)
[System.Serializable]
public class WeaponSub
{
    public string sid; // WeaponSubStat 시트의 Id
    public int value;  // = max(1, round(step × (1+등급) × (1+rand)))
}
