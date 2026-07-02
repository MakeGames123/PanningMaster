using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ItemMoving : MonoBehaviour
{
    [SerializeField] private ItemType type;     
    [Header("Movement Settings")]
    [SerializeField] private float moveDuration = 0.8f;     
    [SerializeField] private Ease moveEase = Ease.OutCubic;   

    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rotateSpeedAndAxis = new Vector3(0f, 360f, 360f); 
    [SerializeField] private int rotationLoops = 2;          

    private Sequence _moveSequence;

    /// <summary>
    /// 아이템이 날아갈 준비를 합니다.
    /// </summary>
    /// <param name="poolKey">풀 매니저에 등록된 아이템 식별자 ("Gold", "Iron" 등)</param>
    public void FlyToTarget(Vector3 posA, Vector3 posB)
    {
        if (_moveSequence != null && _moveSequence.IsActive())
        {
            _moveSequence.Kill();
        }

        transform.position = posA;
        transform.localRotation = Quaternion.identity; 

        _moveSequence = DOTween.Sequence();

        _moveSequence
            .Append(transform.DOMove(posB, moveDuration).SetEase(moveEase))
            .Join(transform.DORotate(rotateSpeedAndAxis * rotationLoops, moveDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear))
            // [핵심 연계] 목적지에 도착하면 풀 매니저에게 반납 요청
            .OnComplete(() =>
            {
                MovingItemPool.Instance.Despawn(type, this);
            });
    }
    public void SimpleFlyToTarget(Vector3 posA, Vector3 posB)
    {
        if (_moveSequence != null && _moveSequence.IsActive())
        {
            _moveSequence.Kill();
        }

        transform.position = posA;
        transform.localRotation = Quaternion.identity; 

        _moveSequence = DOTween.Sequence();

        _moveSequence
            .Append(transform.DOMove(posB, moveDuration).SetEase(moveEase))
            // [핵심 연계] 목적지에 도착하면 풀 매니저에게 반납 요청
            .OnComplete(() =>
            {
                MovingItemPool.Instance.Despawn(type, this);
            });
    }

    private void OnDestroy()
    {
        if (_moveSequence != null && _moveSequence.IsActive())
        {
            _moveSequence.Kill();
        }
    }
}