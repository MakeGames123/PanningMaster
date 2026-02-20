using TMPro;
using UnityEngine;

public class FieldUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI stageText;

    void Start()
    {
        DataManager.Instance.onStageChanged.AddListener((val)=>stageText.text = val.ToString());
    }
}
