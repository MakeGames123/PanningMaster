using UnityEngine;
using DG.Tweening; // DOTween 추가

public class ArrowHover : MonoBehaviour
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverRange = 0.3f;    // 위아래로 움직일 최대 반경 (미터 단위)
    [SerializeField] private float duration = 1.5f;      // 위에서 아래로(혹은 반대로) 왕복하는 데 걸리는 시간
    [SerializeField] private Ease hoverEase = Ease.InOutQuad; // 부드러운 가감속을 위한 이징 (Quad나 Sine 추천)

    private Tween _hoverTween;

    private void Start()
    {
        StartHover();
    }

    public void StartHover()
    {
        // 1. 기존에 돌고 있는 트윈이 있다면 안전하게 킬(Kill)
        if (_hoverTween != null && _hoverTween.IsActive())
        {
            _hoverTween.Kill();
        }

        // 2. 현재 Y축 위치를 기준으로 위아래 둥둥 연출 세팅
        // DOMoveY를 사용해 현재 위치에서 hoverRange만큼 더한 위치로 이동시킵니다.
        _hoverTween = transform.DOMoveY(transform.position.y + hoverRange, duration)
            .SetEase(hoverEase)
            .SetLoops(-1, LoopType.Yoyo); // -1은 무한 루프, Yoyo는 갔다가 다시 돌아오는 왕복 모드
    }

    private void OnDestroy()
    {
        // 오브젝트가 파괴되거나 풀에 반납될 때 메모리 누수 방지
        if (_hoverTween != null && _hoverTween.IsActive())
        {
            _hoverTween.Kill();
        }
    }
}