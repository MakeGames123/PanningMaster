using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Prison : MonoBehaviour
{
    [SerializeField] int maxPrisonerCount = 20;
    [SerializeField] TextMeshProUGUI prisonerCountText;
    List<Transform> prisoners = new();
    public UnityEvent onPrisonFull = new();
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Prisoner"))
        {
            if (!prisoners.Contains(other.transform))
            {
                prisoners.Add(other.transform);
                if(prisoners.Count >= maxPrisonerCount) onPrisonFull.Invoke();
                prisonerCountText.text = $"{prisoners.Count} / {maxPrisonerCount}";
            }
        }
    }
}
