using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DrawButton : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] Button button;
    [SerializeField] List<Image> multipleButtons;
    public AllBulletList allBulletList;
    List<int> multiple = new() { 1, 10, 50 };
    int multipleIndex = 0;
    int drawCount = 0;
    void Awake()
    {
        button.onClick.AddListener(DrawBullet);
        ChangeMultiple(0);
    }
    public void DrawBullet()
    {
        int level = DataManager.Instance.upgradeLevel;
        DrawData currentData = DrawPercentageLoader.Instance.ReturnData(level);

        if (DataManager.Instance.UseTicket(multiple[multipleIndex]))
        {
            for (int i = 0; i < multiple[multipleIndex]; i++)
            {
                int tier = GetRandomTier(currentData);

                allBulletList.DrawBullet(tier);
                drawCount++;
                if (drawCount >= DrawLevelUpLoader.Instance.GetRequiredXP(DataManager.Instance.upgradeLevel))
                {
                    DataManager.Instance.upgradeLevel++;
                }
                UpdateLevelText();
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
        this.multipleIndex = index;
    }
    void UpdateLevelText()
    {
        int currentCount = drawCount - DrawLevelUpLoader.Instance.GetRequiredXP(DataManager.Instance.upgradeLevel - 1);
        int req = DrawLevelUpLoader.Instance.GetRequiredXP(DataManager.Instance.upgradeLevel) - DrawLevelUpLoader.Instance.GetRequiredXP(DataManager.Instance.upgradeLevel - 1);
        levelText.text = $"Lv.{DataManager.Instance.upgradeLevel} {currentCount}/{req}";
    }
    int GetRandomTier(DrawData data)
    {
        List<float> weights = data.weights;

        float totalWeight = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            totalWeight += weights[i];
        }

        if (totalWeight <= 0)
            return 0;

        float rand = Random.Range(0f, totalWeight);


        float cumulative = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            cumulative += weights[i];

            if (rand < cumulative)
                return i;   // 리스트 index = tier
        }

        return 0; // 안전 fallback
    }
}
