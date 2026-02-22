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
        int level = DataManager.Instance.upgradeLevel;
        DrawData currentData = DrawLevelLoader.Instance.ReturnData(level);

        if (DataManager.Instance.UseTicket(multiple[index]))
        {
            for (int i = 0; i < multiple[index]; i++)
            {
                int tier = GetRandomTier(currentData);

                allBulletList.DrawBullet(tier);
            }
        }
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
    int GetRandomTier(DrawData data)
    {
        List<int> weights = data.weights;

        int totalWeight = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            totalWeight += weights[i];
        }

        if (totalWeight <= 0)
            return 0;

        int rand = Random.Range(0, totalWeight);


        int cumulative = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            cumulative += weights[i];

            if (rand < cumulative)
                return i;   // 리스트 index = tier
        }

        return 0; // 안전 fallback
    }
}
