using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PanningUI
{
    // Local editing feedback only. The host owns name policy, persistence and server calls.
    public sealed class NicknameDraftView : MonoBehaviour
    {
        [SerializeField] TMP_InputField input;
        [SerializeField] TMP_Text count;
        [SerializeField] Button save;
        [SerializeField] TMP_Text saveLabel;
        [SerializeField] TMP_Text costLabel;
        [SerializeField] Sprite enabledSprite;
        [SerializeField] Sprite disabledSprite;
        [SerializeField] Material enabledMaterial;
        [SerializeField] Material disabledMaterial;
        string original;

        public string Draft => input == null ? string.Empty : input.text.Trim();

        void OnEnable()
        {
            if (input == null) return;
            original = input.text.Trim();
            input.onValueChanged.AddListener(Refresh);
            Refresh(input.text);
        }

        void OnDisable()
        {
            if (input != null) input.onValueChanged.RemoveListener(Refresh);
        }

        void Refresh(string value)
        {
            string trimmed = value.Trim();
            int length = new StringInfo(trimmed).LengthInTextElements;
            bool canSave = length > 0 && length <= 10 && trimmed != original;
            count.text = length.ToString(CultureInfo.InvariantCulture);
            save.interactable = canSave;
            if (save.targetGraphic is Image image)
                image.sprite = canSave ? enabledSprite : disabledSprite;
            saveLabel.color = canSave ? new Color32(42,26,8,255) : new Color32(125,111,94,255);
            costLabel.color = canSave ? new Color32(90,58,16,255) : new Color32(95,84,70,255);
            saveLabel.fontSharedMaterial = canSave ? enabledMaterial : disabledMaterial;
        }
    }
}
