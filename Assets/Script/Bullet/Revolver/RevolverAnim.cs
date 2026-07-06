using DG.Tweening;
using UnityEngine;

// 리볼버 실린더(Cylinder) 회전 연출.
// 발사: 한 발당 시계방향 60도. 장전: 반시계방향으로 돌며 항상 1번 약실(초기 위치)에 정렬되어 끝남.
public class RevolverAnim : MonoBehaviour
{
    [SerializeField] RectTransform cylinder;
    [SerializeField] RectTransform mark;
    [SerializeField] float stepAngle = 60f;      // 한 발당 회전 각도
    [SerializeField] float stepDuration = 0.08f; // 한 발 회전 애니메이션 시간
    [SerializeField] int reloadSpins = 3;        // 장전 시 회전 바퀴 수

    // 1번 약실 정렬 기준 각도(시작 시의 회전값)
    float cylinderHomeZ;
    float markHomeZ;

    void Awake()
    {
        if (cylinder != null) cylinderHomeZ = cylinder.localEulerAngles.z;
        if (mark != null) markHomeZ = mark.localEulerAngles.z;
    }

    // 탄환 1발 발사: 시계방향으로 stepAngle 만큼 (유니티 UI에서 시계방향 = Z 감소)
    // 작은 각도라 Blendable 로 연사 시 회전이 누적되어 자연스럽게 이어짐
    public void Fire()
    {
        FireOne(cylinder);
        FireOne(mark);
    }

    void FireOne(RectTransform t)
    {
        if (t == null) return;
        t.DOBlendableLocalRotateBy(new Vector3(0f, 0f, -stepAngle), stepDuration).SetEase(Ease.OutQuad);
    }

    // 장전: 장전시간 동안 반시계방향으로 reloadSpins 바퀴 돈 뒤 1번 약실(home)에 정확히 정렬
    public void PlayReload(float duration)
    {
        SpinToHome(cylinder, cylinderHomeZ, duration);
        SpinToHome(mark, markHomeZ, duration);
    }

    void SpinToHome(RectTransform t, float homeZ, float duration)
    {
        if (t == null) return;

        t.DOKill(); // 남은 발사 회전 정리

        float current = t.localEulerAngles.z;
        // 현재 위치에서 반시계(+Z)로 home 까지 남은 거리 + 추가 바퀴
        float toHome = Mod360(homeZ - current);
        float targetZ = current + toHome + 360f * reloadSpins;

        t.DOLocalRotate(new Vector3(0f, 0f, targetZ), duration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic);
    }

    // 진행 중인 회전(발사/장전)을 취소하고 즉시 1번 약실(home)로 정렬
    public void ResetToHome()
    {
        ResetOne(cylinder, cylinderHomeZ);
        ResetOne(mark, markHomeZ);
    }

    void ResetOne(RectTransform t, float homeZ)
    {
        if (t == null) return;
        t.DOKill();
        t.localRotation = Quaternion.Euler(0f, 0f, homeZ);
    }

    // 0 이상 360 미만으로 정규화
    static float Mod360(float v)
    {
        v %= 360f;
        return v < 0f ? v + 360f : v;
    }

    void OnDisable()
    {
        if (cylinder != null) cylinder.DOKill();
        if (mark != null) mark.DOKill();
    }
}
