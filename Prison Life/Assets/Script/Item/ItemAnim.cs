using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // DOTween 사용을 위해 추가

public class ItemAnim : MonoBehaviour
{
    [Header("Scale Animation Settings")]
    [SerializeField] private float maxScaleMultiplier = 1.3f; // 커질 때의 배율 (원래 크기의 1.3배)
    [SerializeField] private float duration = 0.15f;          // 커졌다가 커진 상태를 유지하는 구간의 시간
    [SerializeField] private Ease scaleEase = Ease.OutQuad;    // 부드러운 스케일 이징

    private Vector3 _originalScale;
    private Tween _scaleTween;

    private void Awake()
    {
        // 1. 게임 시작 시 오브젝트의 원본 스케일을 미리 기억해 둡니다.
        _originalScale = transform.localScale;
    }

    /// <summary>
    /// [방식 1] 정석적인 Yoyo 방식: 지정한 크기만큼 깔끔하게 커졌다가 돌아옵니다.
    /// </summary>
    public void PlayScalePop()
    {
        // 기존에 돌고 있던 스케일 트윈이 있다면 꼬이지 않게 종료하고 크기 리셋
        if (_scaleTween != null && _scaleTween.IsActive())
        {
            _scaleTween.Kill();
        }
        transform.localScale = _originalScale;

        // 목표 스케일 계산 (원본 크기 * 배율)
        Vector3 targetScale = _originalScale * maxScaleMultiplier;

        // 트윈 실행: 목표 크기까지 커졌다가(Loops = 2, Yoyo) 제자리로 복귀
        _scaleTween = transform.DOScale(targetScale, duration)
            .SetEase(scaleEase)
            .SetLoops(2, LoopType.Yoyo); 
    }
    private void OnDestroy()
    {
        if (_scaleTween != null && _scaleTween.IsActive())
        {
            _scaleTween.Kill();
        }
    }
}