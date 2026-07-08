using UnityEngine;

// Player가 발사할 수 있는 표적. (연구소 던전처럼 적이 여러 마리인 필드에서 사용)
public interface IFireTarget
{
    Vector3 AimPosition { get; }  //조준점(월드 좌표)
    bool Attacked(float damage);  //피격 처리. 죽었으면 true
}
