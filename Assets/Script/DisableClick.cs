using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
public class DisableClick : MonoBehaviour, IPointerClickHandler
{
    public GameObject panel;
    public UnityEvent disable;
    public void OnPointerClick(PointerEventData eventData)
    {
        panel.SetActive(false);
        disable.Invoke();
    }
}
