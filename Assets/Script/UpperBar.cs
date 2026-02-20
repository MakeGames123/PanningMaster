using TMPro;
using UnityEngine;

public class UpperBar : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI ticketText;
    [SerializeField] TextMeshProUGUI goldText;
    [SerializeField] TextMeshProUGUI powerText;

    void Start()
    {
        DataManager.Instance.onTicketChanged.AddListener((val)=>ticketText.text = "티켓:" + val.ToString());
        DataManager.Instance.onGoldChanged.AddListener((val)=>goldText.text = "골드:" + val.ToString());
        DataManager.Instance.onPowerChanged.AddListener((val)=>powerText.text = $"{val:F0}");
    }
}
