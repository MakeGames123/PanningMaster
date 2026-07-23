using TMPro;
using UnityEngine;

public enum UITextValueType
{
    Ticket,
    Gold,
    Power,
    Stage,
    Crate
}

[RequireComponent(typeof(TextMeshProUGUI))]
public class UIText : MonoBehaviour
{
    [SerializeField] UITextValueType type;
    [SerializeField] TextMeshProUGUI text; // 비워두면 같은 오브젝트에서 자동으로 가져옴

    void Awake()
    {
        if (text == null) text = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        var dm = DataManager.Instance;
        if (dm == null) return;

        switch (type)
        {
            case UITextValueType.Ticket:
                dm.Ticket.onValueChanged += OnTicket;
                OnTicket(dm.Ticket.GetValue());
                break;
            case UITextValueType.Gold:
                dm.Gold.onValueChanged += OnGold;
                OnGold(dm.Gold.GetValue());
                break;
            case UITextValueType.Power:
                dm.onPowerChanged.AddListener(OnPower);
                OnPower(PlayerData.Instance != null ? PlayerData.Instance.Power : 0);
                break;
            case UITextValueType.Stage:
                dm.onStageChanged.AddListener(OnStage);
                OnStage(dm.stage);
                break;
            case UITextValueType.Crate:
                dm.onCrateChanged.AddListener(OnCrate);
                OnCrate(dm.crate);
                break;
        }
    }

    void OnDestroy()
    {
        var dm = DataManager.Instance;
        if (dm == null) return;

        switch (type)
        {
            case UITextValueType.Ticket: dm.Ticket.onValueChanged -= OnTicket; break;
            case UITextValueType.Gold: dm.Gold.onValueChanged -= OnGold; break;
            case UITextValueType.Power: dm.onPowerChanged.RemoveListener(OnPower); break;
            case UITextValueType.Stage: dm.onStageChanged.RemoveListener(OnStage); break;
            case UITextValueType.Crate: dm.onCrateChanged.RemoveListener(OnCrate); break;
        }
    }

    void OnTicket(long v) => text.text = "티켓:" + NumberFormatLoader.Abbrev(v);
    void OnGold(long v) => text.text = "골드:" + NumberFormatLoader.Abbrev(v);
    void OnPower(double v) => text.text = NumberFormatLoader.Abbrev(v);
    void OnStage(int v) => text.text = v.ToString(); // 스테이지는 카운터라 축약 없이 원값
    void OnCrate(int v) => text.text = "🧰" + NumberFormatLoader.Abbrev(v);
}
