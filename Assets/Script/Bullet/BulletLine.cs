using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BulletLine : MonoBehaviour
{
    [SerializeField] RectTransform rect;

    public void AdjustLine(Vector2 posA, Vector2 posB)
    {
        Vector2 direction = posB - posA;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 중앙 위치
        rect.anchoredPosition = posA + direction * 0.5f;

        // 회전
        rect.rotation = Quaternion.Euler(0f, 0f, angle);

        // 길이 조절 (x축 기준으로 길이)
        Vector2 size = rect.sizeDelta;
        size.x = distance;
        rect.sizeDelta = size;

        StartCoroutine(DelayReturn());
    }

    private IEnumerator DelayReturn()
    {
        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }
}
