using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawButton : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] List<Image> multipleButtons;
    public AllBulletList allBulletList;
    List<int> multiple = new() { 1, 10, 50 };
    int index = 0;
    void Awake()
    {
        button.onClick.AddListener(DrawBullet);
        ChangeMultiple(0);
    }
    public void DrawBullet()
    {
        if (DataManager.Instance.UseTicket(multiple[index])) for (int i = 0; i < multiple[index]; i++) allBulletList.DrawBullet();
    }
    public void ChangeMultiple(int index)
    {
        foreach (Image button in multipleButtons)
        {
            button.color = Color.white;
        }

        multipleButtons[index].color = Color.yellow;
        this.index = index;
    }
}
