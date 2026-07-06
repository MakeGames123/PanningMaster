using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;
public class BattleResult : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI resultText;
    [SerializeField] TextMeshProUGUI rewardText;
    public void SetCondition(bool isWin, float gold)
    {
        resultText.enabled = true;
        rewardText.enabled = true;

        resultText.text = isWin ? "Win" : "Lose";
        
        string ticket = isWin ? "2" : "1";
        rewardText.text = $"골드:{gold} 티켓:{ticket}";

        Invoke(nameof(Disable), 1.5f);
    }

    //커스텀 결과 문구만 표시(던전 클리어/실패 등). 보상 텍스트는 숨김.
    public void ShowResult(string label)
    {
        resultText.enabled = true;
        rewardText.enabled = false;

        resultText.text = label;

        Invoke(nameof(Disable), 1.5f);
    }

    void Disable()
    {
        resultText.enabled = false;
        rewardText.enabled = false;
    }
}
