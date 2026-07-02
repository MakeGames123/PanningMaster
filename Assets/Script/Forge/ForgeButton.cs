using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.EventSystems;
public class ForgeButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public UnityEvent onRerollStop = new();
    public UnityEvent onRerollStart = new();
    public UnityEvent onReroll = new();
    bool isHolding;
    float holdTime;
    float holdThreshold = 0.2f;

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        holdTime = 0f;
    }

    void Update()
    {
        if (!isHolding) return;

        holdTime += Time.unscaledDeltaTime;

        if (holdTime >= holdThreshold)
        {
            isHolding = false;
            onRerollStart.Invoke();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (holdTime < holdThreshold)
            onReroll.Invoke();
        else
            onRerollStop.Invoke();

        isHolding = false;
    }
}
