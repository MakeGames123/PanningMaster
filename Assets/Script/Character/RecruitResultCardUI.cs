using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 모집 결과 카드 1장(세리머니 전용 — x1은 큰 카드 1장, x10은 미니 카드 10장이 같은 컴포넌트).
// 등급색 테두리 + 스프라이트 + 등급명 + 이름 + 중복/신규 표시. 아이콘은 UI 이미지 몫(텍스트 삽입 안 함).
public class RecruitResultCardUI : MonoBehaviour
{
    [SerializeField] Image frame;               // 등급색 테두리
    [SerializeField] Image icon;                // 캐릭터 스프라이트
    [SerializeField] TextMeshProUGUI gradeText; // 등급명(등급색)
    [SerializeField] TextMeshProUGUI nameText;  // 이름(선택 연결 — x1 큰 카드는 생략 가능)
    [SerializeField] TextMeshProUGUI dupText;   // "+1"(중복 카드) / "신규!"

    public void Set(CharacterRosterData c, bool isNew)
    {
        var grade = CharacterGradeLoader.Instance.Get(c.grade);
        var gradeColor = CharacterCardUI.ParseColor(grade != null ? grade.colorHex : null, Color.white);

        if (frame != null) frame.color = gradeColor;

        if (icon != null)
        {
            var sprite = CharacterManager.Instance != null ? CharacterManager.Instance.GetSprite(c.id) : null;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        if (gradeText != null) { gradeText.text = grade != null ? grade.nameKo : ""; gradeText.color = gradeColor; }
        if (nameText != null) nameText.text = c.nameKo;
        if (dupText != null) dupText.text = isNew ? "신규!" : "+1";
    }
}
