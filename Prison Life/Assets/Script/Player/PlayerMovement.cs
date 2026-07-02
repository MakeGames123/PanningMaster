using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private DynamicJoystick joystick;
    [SerializeField] private CharacterController controller;
    [SerializeField] private float maxSpeed = 5f;

    private void Update()
    {
        // 조이스틱의 방향 벡터 가져오기 (Vector2 -> Vector3 변환)
        Vector3 direction = new Vector3(joystick.InputDirection.x, 0f, joystick.InputDirection.y);

        // 카메라 시점 보정 Y축 기준 45도 오른쪽 회전
        direction = Quaternion.Euler(0f, 45f, 0f) * direction;

        if (direction.magnitude > 0f)
        {
            // 땡긴 크기(joystick.InputMagnitude)에 비례해서 속도가 달라짐 (0.0 ~ 1.0 범위)
            float currentSpeed = maxSpeed * joystick.InputMagnitude;

            // 이동 처리
            Vector3 moveVelocity = direction * currentSpeed;
            controller.Move(moveVelocity * Time.deltaTime);

            // 이동 방향으로 캐릭터 회전
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }
}