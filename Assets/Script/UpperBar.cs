using TMPro;
using UnityEngine;

public class UpperBar : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI ticketText;
    [SerializeField] TextMeshProUGUI goldText;
    [SerializeField] TextMeshProUGUI powerText;
    [SerializeField] TextMeshProUGUI stageText;

    void Start()
    {
        DataManager.Instance.Ticket.onValueChanged += (val)=>ticketText.text = "티켓:" + val.ToString();
        DataManager.Instance.Gold.onValueChanged += (val)=>goldText.text = "골드:" + val.ToString();
        DataManager.Instance.onPowerChanged.AddListener((val)=>powerText.text = $"{val:F0}");
        DataManager.Instance.onStageChanged.AddListener((val)=>stageText.text = val.ToString());
    }
}
