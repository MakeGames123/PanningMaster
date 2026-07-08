using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PVP 순위표 한 줄(순위/얼굴/이름/점수). TOP3 슬롯과 내 주변 리스트 행 양쪽에서 쓴다.
// 내 행이면 배경을 강조색으로 바꾸고 이름 뒤에 (나)를 붙인다.
public class RankEntryUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI rankText;  //"196"
    [SerializeField] Image faceImage;           //얼굴(선택)
    [SerializeField] TextMeshProUGUI nameText;  //"도박꾼 로사"
    [SerializeField] TextMeshProUGUI scoreText; //"1047점"
    [SerializeField] Image background;          //내 행 강조용(선택)
    [SerializeField] Color myColor = new(1f, 0.84f, 0f, 0.25f);

    Color normalColor;
    bool colorCached;

    public void Set(int rank, string playerName, int score, bool isMe, Sprite face, string scoreSuffix = "점")
    {
        if (!colorCached && background != null)
        {
            normalColor = background.color;
            colorCached = true;
        }

        if (rankText != null) rankText.text = rank.ToString();
        if (nameText != null) nameText.text = isMe ? $"{playerName} (나)" : playerName;
        if (scoreText != null) scoreText.text = $"{score}{scoreSuffix}";

        if (faceImage != null)
        {
            faceImage.enabled = face != null;
            if (face != null) faceImage.sprite = face;
        }

        if (background != null) background.color = isMe ? myColor : normalColor;
    }
}
