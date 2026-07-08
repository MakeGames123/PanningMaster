using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PVP 결투 전체화면 연출. (HTML 프로토타입 pvp-ov의 1차 이식)
// 흐름: 양쪽 입장 슬라이드 -> DRAW! -> 양쪽 6발 속사(상대는 살짝 늦게, 궤적 상하 편차)
//       -> 1초 뒤 승패(전투력 무관 50%, 추후 전투력 기반으로 교체 예정)
//       -> 패자가 빙글빙글 돌며 화면 밖으로 날아감 -> 결과 표시 -> 터치로 복귀.
// 연출 중에는 닫을 수 없고 결과 화면에서만 닫힌다.
public class PvpDuel : MonoBehaviour
{
    enum Phase { Idle, Cine, Fire, Fall, Result }

    [Header("오버레이")]
    [SerializeField] RectTransform overlayRoot; //전체화면 루트. 평소엔 화면 밖(9999,0), 결투 중엔 (0,0). 레이캐스트 막는 배경 Image 권장
    [SerializeField] Button closeButton;        //오버레이 전체를 덮는 투명 버튼(결과 화면에서만 동작)

    [Header("결투사")]
    [SerializeField] RectTransform myFighter;  //왼쪽
    [SerializeField] RectTransform foeFighter; //오른쪽
    [SerializeField] Image foeFaceImage;       //상대 얼굴(선택)
    [SerializeField] TextMeshProUGUI myTagText;  //"나 123"
    [SerializeField] TextMeshProUGUI foeTagText; //"무법자 잭 456"

    [Header("연출")]
    [SerializeField] TextMeshProUGUI countText;   //"DRAW!"
    [SerializeField] GameObject bulletLinePrefab; //기존 BulletLine 재사용(0.2초 뒤 자동 파괴)
    [SerializeField] RectTransform tracerParent;  //궤적 생성 부모(비우면 오버레이 루트)
    [SerializeField] float tracerYJitter = 36f;   //궤적 상하 편차(±)
    [SerializeField] float entryOffsetX = 400f;   //입장 슬라이드 시작 거리
    [SerializeField] Vector2 flyOffset = new(500f, 700f); //패자가 날아가는 이동량(x는 바깥 방향)

    [Header("결과")]
    [SerializeField] GameObject resultRoot;
    [SerializeField] TextMeshProUGUI resultBigText; //승리!/패배...
    [SerializeField] TextMeshProUGUI resultSubText;
    [SerializeField] Color winColor = new(1f, 0.84f, 0f);   //금색
    [SerializeField] Color loseColor = new(0.96f, 0.26f, 0.21f); //붉은색

    [Header("타이밍(초)")]
    [SerializeField] float entryDuration = 0.5f;  //입장 슬라이드
    [SerializeField] float drawDelay = 1f;        //입장 후 DRAW!까지
    [SerializeField] float shotInterval = 0.14f;  //연사 간격
    [SerializeField] float foeShotOffset = 0.06f; //상대 발사 지연
    [SerializeField] int shotCount = 6;           //발사 수
    [SerializeField] float outcomeDelay = 1f;     //마지막 발사 후 승패까지
    [SerializeField] float flyDuration = 0.65f;   //패자가 날아가는 시간

    public System.Action onClosed; //결과 화면이 닫힐 때(PvpPanel이 다음 상대 매칭)

    PvpFoe foe;
    Phase phase = Phase.Idle;
    Vector2 myHome, foeHome;
    Vector3 myScale, foeScale;
    Vector2 hiddenPos = new(9999f, 0f); //비활성 시 대기 좌표(NavButtons 방식)

    void Awake()
    {
        if (myFighter != null) { myHome = myFighter.anchoredPosition; myScale = myFighter.localScale; }
        if (foeFighter != null) { foeHome = foeFighter.anchoredPosition; foeScale = foeFighter.localScale; }
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (overlayRoot != null) overlayRoot.anchoredPosition = hiddenPos; //화면 밖 대기
    }

    // PvpPanel이 결투 시작 시 호출
    public void Begin(PvpFoe foe, float myPower, Sprite foeFace)
    {
        if (phase != Phase.Idle) return;
        this.foe = foe;

        ResetFighters();

        if (foeFaceImage != null)
        {
            foeFaceImage.enabled = foeFace != null;
            if (foeFace != null) foeFaceImage.sprite = foeFace;
        }
        if (myTagText != null) myTagText.text = $"나 {myPower:F0}";
        if (foeTagText != null) foeTagText.text = $"{foe.name} {foe.power:F0}";
        if (countText != null) countText.alpha = 0f;
        if (resultRoot != null) resultRoot.SetActive(false);

        if (overlayRoot != null) overlayRoot.anchoredPosition = Vector2.zero; //화면 안으로

        StopAllCoroutines();
        StartCoroutine(DuelRoutine());
    }

    IEnumerator DuelRoutine()
    {
        // 입장: 양쪽에서 슬라이드 인 (프로토타입의 서부극 2컷 오프닝 간소화 버전)
        phase = Phase.Cine;
        myFighter.anchoredPosition = myHome + new Vector2(-entryOffsetX, 0f);
        foeFighter.anchoredPosition = foeHome + new Vector2(entryOffsetX, 0f);
        myFighter.DOAnchorPos(myHome, entryDuration).SetEase(Ease.OutCubic);
        foeFighter.DOAnchorPos(foeHome, entryDuration).SetEase(Ease.OutCubic);
        yield return new WaitForSeconds(entryDuration + drawDelay);

        // DRAW!
        PopCount("DRAW!");

        // 양쪽 6발 속사. 상대는 매 발마다 살짝 늦게 쏜다.
        phase = Phase.Fire;
        for (int i = 0; i < shotCount; i++)
        {
            FireOne(myFighter, foeFighter);
            yield return new WaitForSeconds(foeShotOffset);
            FireOne(foeFighter, myFighter);
            yield return new WaitForSeconds(Mathf.Max(0f, shotInterval - foeShotOffset));
        }

        yield return new WaitForSeconds(outcomeDelay);

        // 승패: 전투력 무관 50% (추후 전투력 기반으로 교체 예정)
        phase = Phase.Fall;
        bool win = Random.value < 0.5f;

        RectTransform loser = win ? foeFighter : myFighter;
        float dir = win ? 1f : -1f; //패자는 자기 진영 바깥쪽으로 날아감
        loser.DOKill();
        loser.DOAnchorPos(loser.anchoredPosition + new Vector2(flyOffset.x * dir, flyOffset.y), flyDuration).SetEase(Ease.OutQuad);
        loser.DOLocalRotate(new Vector3(0f, 0f, -720f * dir), flyDuration, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad);
        loser.DOScale(loser.localScale * 0.4f, flyDuration).SetEase(Ease.InQuad);

        yield return new WaitForSeconds(flyDuration);

        // 결과 표시. 이제부터 터치로 닫을 수 있다.
        phase = Phase.Result;
        if (resultBigText != null)
        {
            resultBigText.text = win ? "승리!" : "패배...";
            resultBigText.color = win ? winColor : loseColor;
        }
        if (resultSubText != null)
            resultSubText.text = win ? $"{foe.name}을(를) 쓰러뜨렸다!" : $"{foe.name}에게 당했다...";
        if (resultRoot != null) resultRoot.SetActive(true);
    }

    // 발사 1발: 반동 + 상대 쪽으로 궤적(상하 편차)
    void FireOne(RectTransform from, RectTransform to)
    {
        float kickDir = from == myFighter ? -1f : 1f; //자기 진영 뒤쪽으로 반동
        from.DOComplete();
        from.DOPunchAnchorPos(new Vector2(10f * kickDir, 3f), 0.13f, 1, 0f);

        if (bulletLinePrefab == null) return;

        RectTransform parent = tracerParent != null ? tracerParent : overlayRoot;
        if (parent == null) return;

        Vector2 a = parent.InverseTransformPoint(from.position);
        Vector2 b = parent.InverseTransformPoint(to.position);
        b.y += Random.Range(-tracerYJitter, tracerYJitter);

        GameObject line = Instantiate(bulletLinePrefab, parent);
        line.GetComponent<BulletLine>().AdjustLine(a, b);
    }

    // DRAW! 텍스트 팝: 커졌다가 자리잡고 잠시 뒤 사라짐
    void PopCount(string text)
    {
        if (countText == null) return;

        countText.text = text;
        countText.alpha = 1f;
        RectTransform rect = countText.rectTransform;
        rect.DOKill();
        rect.localScale = Vector3.one * 2f;
        Sequence seq = DOTween.Sequence();
        seq.Append(rect.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
        seq.AppendInterval(0.4f);
        seq.Append(countText.DOFade(0f, 0.15f));
    }

    // 오버레이를 덮는 투명 버튼이 호출. 결과 화면에서만 닫힌다.
    void Close()
    {
        if (phase != Phase.Result) return;

        StopAllCoroutines();
        phase = Phase.Idle;
        if (overlayRoot != null) overlayRoot.anchoredPosition = hiddenPos; //화면 밖으로
        onClosed?.Invoke();
    }

    // 결투사 위치/회전/크기 원상 복구
    void ResetFighters()
    {
        if (myFighter != null)
        {
            myFighter.DOKill();
            myFighter.anchoredPosition = myHome;
            myFighter.localRotation = Quaternion.identity;
            myFighter.localScale = myScale;
        }
        if (foeFighter != null)
        {
            foeFighter.DOKill();
            foeFighter.anchoredPosition = foeHome;
            foeFighter.localRotation = Quaternion.identity;
            foeFighter.localScale = foeScale;
        }
    }

    void OnDisable()
    {
        if (myFighter != null) myFighter.DOKill();
        if (foeFighter != null) foeFighter.DOKill();
        if (countText != null) countText.rectTransform.DOKill();
    }
}
