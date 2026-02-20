using UnityEngine;
using UnityEngine.UI;

public class DragSlotUI : MonoBehaviour
{
    [SerializeField] Image bulletImage;
    public void UpdateUI(Sprite image)
    {
        bulletImage.sprite = image;
    }
}
