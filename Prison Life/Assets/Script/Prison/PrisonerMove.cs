using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PrisonerMove : MonoBehaviour
{
    [SerializeField] private Prison prison; 

    [Header("Movement Settings")]
    [SerializeField] private float durationToB = 2.0f; // A에서 B까지 이동 시간
    [SerializeField] private float durationToC = 1.5f; // B에서 C까지 이동 시간
    [SerializeField] private float durationToD = 2.0f; // B에서 D까지 이동 시간
    [SerializeField] private Ease moveEase = Ease.Linear; 
    
    [Header("Path Positions")]
    [SerializeField] private Vector3 posB;
    [SerializeField] private Vector3 posC;
    [SerializeField] private Vector3 posD; 

    private Sequence _moveSequence;
    private bool moveComplete = false;
    private bool isPrisonFullTriggered = false; // 감옥이 찼다는 신호를 받았는지 기억할 플래그

    void Awake()
    {
        prison = FindAnyObjectByType<Prison>();
        if (prison != null)
        {
            // 감옥이 가득 차면 이 플래그만 true로 바꿔둡니다.
            prison.onPrisonFull.AddListener(() => isPrisonFullTriggered = true);
        }
    }

    /// <summary>
    /// 외부에서 호출 시 죄수가 걷기 시작합니다. (무조건 B까지는 일단 직진)
    /// </summary>
    public void MoveAlongPath()
    {
        moveComplete = false; 

        if (_moveSequence != null && _moveSequence.IsActive())
        {
            _moveSequence.Kill();
        }

        _moveSequence = DOTween.Sequence();

        _moveSequence
            // 1. 무슨 일이 있어도 일단 A에서 B 지점까지는 무조건 이동합니다.
            .Append(transform.DOMove(posB, durationToB).SetEase(moveEase))
            
            // 2. [핵심] B 지점에 도착한 '바로 그 순간' 조건을 판별합니다.
            .AppendCallback(() => 
            {
                // B 지점에 도착했을 때 감옥이 가득 찬 상태라면?
                if (isPrisonFullTriggered)
                {
                    //Debug.Log($"[Path] B 지점 도착! 감옥이 꽉 찼으므로 D 지점으로 향합니다.");
                    // 다음 행선지를 D로 설정하여 시퀀스에 이어 붙입니다.
                    _moveSequence.Append(transform.DOMove(posD, durationToD).SetEase(moveEase));
                }
                // 감옥에 아직 자리가 있다면 원래 계획대로 C로 갑니다.
                else
                {
                    //Debug.Log($"[Path] B 지점 도착! 감옥에 여유가 있으므로 원래대로 C 지점으로 향합니다.");
                    _moveSequence.Append(transform.DOMove(posC, durationToC).SetEase(moveEase))
                                 .OnComplete(() => moveComplete = true);
                }
            });
    }

    void FixedUpdate()
    {
        if (transform.position.x < -3.8f && moveComplete) transform.position += new Vector3(0.2f, 0, 0); 
    }

    void OnTriggerStay(Collider other)
    {
        if (other.transform.CompareTag("Prisoner") && moveComplete)
        {
            Vector3 pushDirection = transform.position - other.transform.position;
            if (pushDirection.sqrMagnitude < 0.001f) pushDirection = Vector3.forward;

            float randomX = Random.Range(-0.25f, 0.25f);
            float randomZ = Random.Range(-0.25f, 0.25f);
            pushDirection += new Vector3(randomX, 0f, randomZ);

            transform.position += pushDirection.normalized * 0.1f;
        }
        
        if (other.transform.CompareTag("Wall") && moveComplete)
        {
            Vector3 pushDirection = transform.position - other.transform.position;
            pushDirection = new Vector3(pushDirection.x, 0, pushDirection.z);
            transform.position += pushDirection.normalized * 0.3f;
        }
    }

    private void OnDestroy()
    {
        if (_moveSequence != null && _moveSequence.IsActive())
        {
            _moveSequence.Kill();
        }
    }
}