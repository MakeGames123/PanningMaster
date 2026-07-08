using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 결투장(PVP) 탭 패널. 내 전투력 VS 랜덤 상대 카드 + 다른 상대 찾기 / 결투 시작.
// 결투가 끝나면 다음 상대를 자동으로 매칭한다. (HTML 프로토타입 tab-pvp의 1차 이식)
public class PvpPanel : MonoBehaviour
{
    static readonly string[] FoeNames =
    {
        "무법자 잭", "빠른손 케이트", "황야의 콜트", "저격수 로사", "도박꾼 에이스", "검은모자 카일",
        "쌍권총 맥스", "은탄환 실버", "현상금 사냥꾼 밥", "붉은노을 진", "방랑자 하울", "턱수염 더건"
    };

    [Header("내 쪽")]
    [SerializeField] TextMeshProUGUI myPowerText;

    [Header("상대 쪽")]
    [SerializeField] TextMeshProUGUI foeNameText;
    [SerializeField] TextMeshProUGUI foePowerText;
    [SerializeField] Image foeFaceImage;
    [SerializeField] Sprite[] foeFaces; //상대 얼굴 후보(비워도 동작, 그 경우 이미지 숨김)

    [Header("상대 전투력 = 내 전투력 * (min~max)")]
    [SerializeField] float foePowerMin = 0.6f;
    [SerializeField] float foePowerMax = 1.5f;

    [Header("버튼/결투 연출")]
    [SerializeField] Button rerollButton; //다른 상대 찾기
    [SerializeField] Button startButton;  //결투 시작
    [SerializeField] PvpDuel duel;

    PvpFoe foe;
    bool subscribed;

    float MyPower => PlayerData.Instance != null ? PlayerData.Instance.Power : 0f;

    void Awake()
    {
        if (rerollButton != null) rerollButton.onClick.AddListener(Reroll);
        if (startButton != null) startButton.onClick.AddListener(StartDuel);
        if (duel != null) duel.onClosed += OnDuelClosed;
    }

    void OnEnable()
    {
        TrySubscribe();
        if (foe == null) RollFoe();
        Refresh();
    }

    void Start()
    {
        // NavButtons는 패널을 비활성화하지 않고 화면 밖으로 옮기므로 이벤트로도 갱신
        TrySubscribe();
        if (foe == null) RollFoe();
        Refresh();
    }

    void TrySubscribe()
    {
        if (subscribed || DataManager.Instance == null) return;
        DataManager.Instance.onPowerChanged.AddListener(OnPowerChanged);
        subscribed = true;
    }

    void OnDestroy()
    {
        if (subscribed && DataManager.Instance != null)
            DataManager.Instance.onPowerChanged.RemoveListener(OnPowerChanged);
        if (duel != null) duel.onClosed -= OnDuelClosed;
    }

    void OnPowerChanged(double _) => Refresh();

    // 랜덤 상대 매칭: 이름/얼굴 랜덤, 전투력은 내 전투력 기준 배율(최소 10)
    void RollFoe()
    {
        foe = new PvpFoe
        {
            name = FoeNames[Random.Range(0, FoeNames.Length)],
            faceIndex = foeFaces != null && foeFaces.Length > 0 ? Random.Range(0, foeFaces.Length) : -1,
            power = Mathf.Max(10f, MyPower * Random.Range(foePowerMin, foePowerMax))
        };
    }

    void Refresh()
    {
        if (myPowerText != null) myPowerText.text = $"{MyPower:F0}";

        if (foe == null) return;
        if (foeNameText != null) foeNameText.text = foe.name;
        if (foePowerText != null) foePowerText.text = $"{foe.power:F0}";
        if (foeFaceImage != null)
        {
            bool hasFace = foe.faceIndex >= 0;
            foeFaceImage.enabled = hasFace;
            if (hasFace) foeFaceImage.sprite = foeFaces[foe.faceIndex];
        }
    }

    void Reroll()
    {
        RollFoe();
        Refresh();
    }

    void StartDuel()
    {
        if (duel == null || foe == null) return;

        Sprite face = foe.faceIndex >= 0 ? foeFaces[foe.faceIndex] : null;
        duel.Begin(foe, MyPower, face);
    }

    // 결투 종료 -> 다음 상대 자동 매칭
    void OnDuelClosed()
    {
        RollFoe();
        Refresh();
    }
}
