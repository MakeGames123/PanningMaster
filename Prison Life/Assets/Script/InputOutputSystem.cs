using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InputOutputSystem : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] protected string targetTag = "Player"; // 감지할 기본 태그 (필요 시 여러 태그 처리도 가능)
    [SerializeField] protected InventoryStack stack; // 감지할 기본 태그 (필요 시 여러 태그 처리도 가능)

    // [변경] 들어온 대상(Key)과 그 대상의 독립적인 코루틴(Value)을 매핑하여 다중 관리
    protected Dictionary<ItemInteractive, Coroutine> activeRoutines = new Dictionary<ItemInteractive, Coroutine>();

    // 트리거 범위 안으로 무언가 들어왔을 때
    private void OnTriggerEnter(Collider other)
    {
        // 들어온 오브젝트의 태그 검사 (상황에 따라 여러 타겟을 검사하도록 변경 가능)
        if (other.CompareTag(targetTag) || other.CompareTag("DeliveryMan")) 
        {
            // 인터페이스 성격의 컴포넌트 추출 (Player 혹은 DeliveryMan 등이 상속받은 스크립트)
            if (other.TryGetComponent<ItemInteractive>(out var interactiveTarget))
            {
                // 이미 해당 대상의 코루틴이 돌고 있다면 중복 실행 방지
                if (activeRoutines.ContainsKey(interactiveTarget))
                {
                    return; 
                }

                // 해당 대상만을 위한 전용 프로세스 코루틴 시작 후 딕셔너리에 등록
                Coroutine targetRoutine = StartCoroutine(ProcessRoutine(interactiveTarget));
                activeRoutines.Add(interactiveTarget, targetRoutine);
            }
        }
    }

    // 트리거 범위 밖으로 나갔을 때
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag) || other.CompareTag("DeliveryMan"))
        {
            if (other.TryGetComponent<ItemInteractive>(out var interactiveTarget))
            {
                // 나간 대상의 전용 코루틴을 찾아서 안전하게 종료
                if (activeRoutines.TryGetValue(interactiveTarget, out Coroutine targetRoutine))
                {
                    if (targetRoutine != null)
                    {
                        StopCoroutine(targetRoutine);
                    }
                    activeRoutines.Remove(interactiveTarget);
                }
            }
        }
    }

    // [변경] 이제 코루틴이 묻지도 따지지도 않고 도는 게 아니라, "특정 대상"을 품고 돕니다.
    private IEnumerator ProcessRoutine(ItemInteractive target)
    {
        // 성능 최적화: 매 루프마다 가비지가 생성되지 않도록 캐싱
        WaitForSeconds wait = new WaitForSeconds(target.ReturnInterval());

        while (true)
        {
            // 대상이 파괴되거나 비활성화되는 등 유실되었는지 안전 점검
            if (target == null)
            {
                // activeRoutines에서 null 키가 생성될 수 있으므로 아래 OnDisable 등에서 함께 안전하게 비워줍니다.
                yield break;
            }

            // [변경] 자식 클래스에서 "누구를 대상으로" 행동할지 명확하게 전달
            RoutineBehaviour(target);
            
            yield return wait;
        }
    }

    // [변경] 어떤 대상을 상호작용 타겟으로 삼을지 매개변수로 넘겨받도록 추상 메서드 변경
    protected abstract void RoutineBehaviour(ItemInteractive target);

    // 오브젝트가 비활성화되거나 파괴될 때 남아있는 모든 다중 코루틴을 한 번에 청소
    private void OnDisable()
    {
        foreach (var pair in activeRoutines)
        {
            if (pair.Value != null)
            {
                StopCoroutine(pair.Value);
            }
        }
        activeRoutines.Clear();
    }
}