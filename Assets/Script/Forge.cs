using UnityEngine;
using UnityEngine.UI;

public class Forge : MonoBehaviour
{
    [SerializeField] Button button;
    public AllBulletList allBulletList;
    void Awake()
    {
        button.onClick.AddListener(ForgeBullet);
    }
    public void ForgeBullet()
    {
        if(DataManager.Instance.UseTicket()) allBulletList.DrawBullet();
    }
}
