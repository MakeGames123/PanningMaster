using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NavButtons : MonoBehaviour
{
    [SerializeField] RectTransform bulletPanel;
    [SerializeField] RectTransform dungeonPanel;
    [SerializeField] RectTransform shopPanel;
    [SerializeField] RectTransform upgradePanel;
    List<RectTransform> allPanels = new();
    [SerializeField] Button bulletButton;
    [SerializeField] Button dungeonButton;
    [SerializeField] Button shopButton;
    [SerializeField] Button upgradeButton;
    List<Button> allButtons = new();
    Vector2 disablePos = new Vector2(-9999, -9999);
    void Awake()
    {
        allPanels.Add(bulletPanel);
        allPanels.Add(dungeonPanel);
        //allPanels.Add(shopPanel);
        allPanels.Add(upgradePanel);

        allButtons.Add(bulletButton);
        allButtons.Add(dungeonButton);
        //allButtons.Add(shopButton);
        allButtons.Add(upgradeButton);

        bulletButton.onClick.AddListener(() => EnablePanel(bulletPanel));
        dungeonButton.onClick.AddListener(() => EnablePanel(dungeonPanel));
        //shopButton.onClick.AddListener(() => EnablePanel(shopPanel));
        upgradeButton.onClick.AddListener(() => EnablePanel(upgradePanel));
    }
    bool _initialized;

    void OnEnable()
    {
        TryInitialize();
    }

    void Start()
    {
        TryInitialize();
    }

    void TryInitialize()
    {
        if (_initialized) return;
        if (bulletButton == null || bulletPanel == null) return;
        _initialized = true;
        EnablePanel(bulletPanel);
    }
    public void EnablePanel(RectTransform target)
    {
        foreach (RectTransform panel in allPanels)
        {
            if(target == panel) continue;

            panel.anchoredPosition = disablePos;
        }

        target.anchoredPosition = Vector2.zero;
    }
}
