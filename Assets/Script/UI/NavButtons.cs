using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NavButtons : MonoBehaviour
{
    [SerializeField] RectTransform bulletPanel;
    [SerializeField] RectTransform dungeonPanel;
    [SerializeField] RectTransform shopPanel;
    [SerializeField] RectTransform rankPanel;
    [SerializeField] RectTransform upgradePanel;
    [SerializeField] RectTransform pvpPanel;
    List<RectTransform> allPanels = new();
    [SerializeField] Button bulletButton;
    [SerializeField] Button dungeonButton;
    [SerializeField] Button shopButton;
    [SerializeField] Button rankButton;
    [SerializeField] Button upgradeButton;
    [SerializeField] Button pvpButton;
    List<Button> allButtons = new();
    Vector2 disablePos = new Vector2(-9999, -9999);
    void Awake()
    {
        allPanels.Add(bulletPanel);
        allPanels.Add(dungeonPanel);
        //allPanels.Add(shopPanel);
        allPanels.Add(rankPanel);
        allPanels.Add(upgradePanel);
        if (pvpPanel != null) allPanels.Add(pvpPanel); //아직 씬에 연결 안 됐으면 무시

        allButtons.Add(bulletButton);
        allButtons.Add(dungeonButton);
        //allButtons.Add(shopButton);
        allButtons.Add(rankButton);
        allButtons.Add(upgradeButton);
        if (pvpButton != null) allButtons.Add(pvpButton);

        bulletButton.onClick.AddListener(() => EnablePanel(bulletPanel));
        dungeonButton.onClick.AddListener(() => EnablePanel(dungeonPanel));
        //shopButton.onClick.AddListener(() => EnablePanel(shopPanel));
        rankButton.onClick.AddListener(() => EnablePanel(rankPanel));
        upgradeButton.onClick.AddListener(() => EnablePanel(upgradePanel));
        if (pvpButton != null && pvpPanel != null) pvpButton.onClick.AddListener(() => EnablePanel(pvpPanel));
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
