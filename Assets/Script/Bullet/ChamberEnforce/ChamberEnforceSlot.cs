using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 약실(챔버) 1칸의 강화 슬롯. 슬롯마다 강화 수치/확률이 별개로 관리된다.
// 클릭하면 컨트롤러(ChamberEnforcePanel)에 자신을 알려 하단 UI가 이 슬롯을 대상으로 바뀐다.
public class ChamberEnforceSlot : MonoBehaviour
{
    [SerializeField] Button button;                  // 클릭용 버튼
    [SerializeField] GameObject clickRing;           // 선택 시 활성화되는 테두리 링
    [SerializeField] Image background;
    [SerializeField] Image bulletImage;
    [SerializeField] TextMeshProUGUI enforceAmount;  // "+0"
    [SerializeField] TextMeshProUGUI slotNum;        // "1"

    public int SlotIndex { get; private set; }       // 0부터
    public int Level { get; private set; }           // 강화 수치(+N)
    public float BaseRate { get; private set; }      // 현재 기본 성공 확률(%)
    public float BonusRate { get; private set; }     // 실패로 누적된 보너스 확률(%)

    // 클릭 시 컨트롤러가 구독
    public System.Action<ChamberEnforceSlot> OnClicked;

    // 유효 성공 확률(기본 + 실패 누적), 0~100 클램프
    public float SuccessRate => Mathf.Clamp(BaseRate + BonusRate, 0f, 100f);

    public void Init(int index, float baseRate)
    {
        SlotIndex = index;
        BaseRate = baseRate;
        Level = 0;
        BonusRate = 0f;

        if (button != null) button.onClick.AddListener(() => OnClicked?.Invoke(this));

        SetSelected(false);
        RefreshDisplay();
    }

    // 선택 시 테두리 링 활성화 / 해제
    public void SetSelected(bool on)
    {
        if (clickRing != null) clickRing.SetActive(on);
    }

    // 강화 시도. 성공 여부 반환.
    // 성공: 레벨 +1, 기본 확률 -basePenalty, 누적 보너스 초기화
    // 실패: 레벨 유지, 다음 확률 +bonusPerFail
    public bool TryEnhance(float bonusPerFail, float basePenaltyOnSuccess)
    {
        bool success = Random.Range(0f, 100f) < SuccessRate;

        if (success)
        {
            Level++;
            BaseRate = Mathf.Max(0f, BaseRate - basePenaltyOnSuccess);
            BonusRate = 0f;
        }
        else
        {
            BonusRate += bonusPerFail; // 실패해도 레벨은 유지
        }

        RefreshDisplay();
        return success;
    }

    void RefreshDisplay()
    {
        if (slotNum != null) slotNum.text = (SlotIndex + 1).ToString();
        if (enforceAmount != null) enforceAmount.text = $"+{Level}";
    }
}
