using System.Collections;
using TMPro;
using UnityEngine;

public class PowerFloatingText : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI powerText;
    [SerializeField] float floatDuration = 1f;    // 플로팅 지속 시간
    [SerializeField] float floatDistance = 40f;   // 위로 떠오르는 거리(px)
    [SerializeField] Color upColor = Color.green;  // 전투력 상승
    [SerializeField] Color downColor = Color.red;  // 전투력 하락

    double lastPower;      // 마지막 전투력 캐시
    bool initialized;      // 첫 값 수신 여부
    Vector2 basePos;       // 플로팅 시작 위치
    Coroutine routine;

    void Awake()
    {
        if (powerText != null)
        {
            basePos = powerText.rectTransform.anchoredPosition;
            SetAlpha(0f);
        }
    }

    void Start()
    {
        DataManager.Instance.onPowerChanged.AddListener(Floating);
        lastPower = PlayerData.Instance.Power;
    }

    void OnDestroy()
    {
        if (DataManager.Instance != null)
            DataManager.Instance.onPowerChanged.RemoveListener(Floating);

    }

    public void Floating(double power)
    {
        if (!initialized) //캐싱값이 없으면 현재 전투력을 받아와 기준으로 사용
        {
            initialized = true;
        }

        double delta = power - lastPower;
        lastPower = power;

        if (delta < 1) return; //변화 없으면 연출 없음

        bool isUp = delta > 0;
        powerText.color = isUp ? upColor : downColor;
        powerText.text = $"{(isUp ? "+" : "-")}{System.Math.Abs(delta):F0}";

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FloatRoutine());
    }

    IEnumerator FloatRoutine()
    {
        float t = 0f;
        while (t < floatDuration)
        {
            t += Time.deltaTime;
            float p = t / floatDuration;
            powerText.rectTransform.anchoredPosition = basePos + Vector2.up * (floatDistance * p);
            SetAlpha(1f - p); //위로 떠오르며 서서히 사라짐
            yield return null;
        }

        powerText.rectTransform.anchoredPosition = basePos;
        SetAlpha(0f);
        routine = null;
    }

    void SetAlpha(float a)
    {
        if (powerText == null) return;
        Color c = powerText.color;
        c.a = a;
        powerText.color = c;
    }
}
