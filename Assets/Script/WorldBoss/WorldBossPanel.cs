using TMPro;
using UnityEngine;
using UnityEngine.UI;

// v45 월드보스 패널(프로토 renderEvent 포팅). WANTED 전단(얼굴·이름·속성 배지·남은 시간) +
// 요약 3칸(최고 데미지/내 순위/도전 횟수) + 도전 버튼. 랭킹은 전용 WorldBossRankBoard
// (내 행 맨 위 고정 + TOP 1~50 — 일반 RankBoard와 표시가 달라 별도 구현)를 같은 패널에 배치.
// 프리팹/씬 배치는 유저 몫 — 모든 참조는 널 허용.
public class WorldBossPanel : MonoBehaviour
{
    [Header("WANTED 전단")]
    [SerializeField] TextMeshProUGUI countdownText; // "5d 23h 14m"
    [SerializeField] TextMeshProUGUI faceText;      // 수배범 얼굴 이모지
    [SerializeField] Image faceImage;               // (선택) 스프라이트 얼굴 — 배정 시 이모지 대신 사용
    [SerializeField] Sprite[] faceSprites;          // (선택) 로테이션 인덱스(0~5) 순 얼굴 스프라이트
    [SerializeField] TextMeshProUGUI bossNameText;  // "홍염의 매드독" (속성색)
    [SerializeField] TextMeshProUGUI elemBadgeText; // "화염 속성 데미지 ×2" (속성색)
    [SerializeField] Image elemBadgeFrame;          // (선택) 배지 테두리 — 속성색으로 틴트

    [Header("요약")]
    [SerializeField] TextMeshProUGUI bestDamageText; // 최고 데미지 "2597M" / "—"
    [SerializeField] TextMeshProUGUI myRankText;     // 내 순위 — RankBoard.myRankText를 직접 연결해도 됨(그 경우 비워둘 것)
    [SerializeField] TextMeshProUGUI triesText;      // 도전 횟수 "3/3"

    [Header("도전")]
    [SerializeField] Button challengeButton;
    [SerializeField] TextMeshProUGUI challengeLabel; // "도전하기"/"오늘 도전 완료"/"동료 3명 보유 시 해금"

    [Header("랭킹 보드(선택)")]
    [SerializeField] WorldBossRankBoard rankBoard; // 월드보스 전용 보드(내 행 고정 + TOP 50) — 베스트 갱신 시 새로고침

    bool subscribed;
    float nextCountdownAt; // 카운트다운 1초 간격 갱신
    float nextWaitLogAt;   // 구독 대기 사유 로그 스로틀(3초)

    void Awake()
    {
        if (challengeButton != null) challengeButton.onClick.AddListener(OnChallengeClicked);
    }

    void OnDestroy()
    {
        if (!subscribed) return;
        if (WorldBossManager.Instance != null)
            WorldBossManager.Instance.onChanged.RemoveListener(Refresh);
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.onChanged.RemoveListener(Refresh);
    }

    void Update()
    {
        // 매니저/시트 준비 후 1회 구독(싱글톤 타이밍 안전망 — 기존 패널 문법)
        if (!subscribed) TrySubscribe();

        // 남은 시간은 1초 간격 갱신(주 경계 넘어가면 Refresh가 로테이션도 갱신)
        if (subscribed && Time.unscaledTime >= nextCountdownAt)
        {
            nextCountdownAt = Time.unscaledTime + 1f;
            var mgr = WorldBossManager.Instance;
            if (countdownText != null && mgr != null) countdownText.text = mgr.CountdownText();
            if (mgr != null && mgr.WeekStamp != mgr.WeekIndex()) Refresh(); // 주 롤오버 감지
        }
    }

    void TrySubscribe()
    {
        if (subscribed) return;

        // 구독 대기 사유 로그(3초 간격) — 이 단계에서 멈춰 있으면 Refresh 자체가 안 돌아 버튼이 계속 비활성
        if (WorldBossManager.Instance == null)
        {
            WaitLog("WorldBossManager 싱글톤 없음 — 씬에 WorldBossManager 오브젝트가 배치됐는지 확인");
            return;
        }
        if (WorldBossOutlawLoader.Instance == null || !WorldBossOutlawLoader.Instance.IsLoaded)
        {
            WaitLog("WorldBossOutlaw 시트 미로드 — 구글 시트에 WorldBossOutlaw 탭이 있는지/에러 로그 확인");
            return;
        }
        // CharacterManager(해금 판정 소스)가 준비되기 전에 굳히면 버튼이 비활성으로 고정됨 — 준비까지 대기
        if (CharacterManager.Instance == null || !CharacterManager.Instance.IsReady)
        {
            WaitLog("CharacterManager 미준비 — 초기화 대기(해금 판정 소스)");
            return;
        }

        WorldBossManager.Instance.onChanged.AddListener(Refresh);
        CharacterManager.Instance.onChanged.AddListener(Refresh); // 동료 획득/변동 → 해금 상태 즉시 갱신
        subscribed = true;
        Debug.Log("[WorldBoss] 패널 구독 완료 — Refresh 시작");

        Refresh();
    }

    void WaitLog(string reason)
    {
        if (Time.unscaledTime < nextWaitLogAt) return;
        nextWaitLogAt = Time.unscaledTime + 3f;
        Debug.Log($"[WorldBoss] 패널 대기 중: {reason}");
    }

    void OnChallengeClicked()
    {
        var mgr = WorldBossManager.Instance;
        if (mgr == null) return;

        if (mgr.TryBeginChallenge()) Refresh(); // 횟수 차감 반영(전투 필드로 전환됨)
    }

    void Refresh()
    {
        var mgr = WorldBossManager.Instance;
        if (mgr == null) return;

        var outlaw = mgr.CurrentOutlaw();
        var el = outlaw != null && ElementLoader.Instance != null ? ElementLoader.Instance.Get(outlaw.element) : null;
        Color elColor = el != null && ColorUtility.TryParseHtmlString(el.colorHex, out var c) ? c : Color.white;

        // ── WANTED 전단 ──
        if (countdownText != null) countdownText.text = mgr.CountdownText();

        bool useSprite = faceImage != null && outlaw != null
            && faceSprites != null && outlaw.rotationIndex >= 0 && outlaw.rotationIndex < faceSprites.Length
            && faceSprites[outlaw.rotationIndex] != null;
        if (faceImage != null)
        {
            faceImage.enabled = useSprite;
            if (useSprite) faceImage.sprite = faceSprites[outlaw.rotationIndex];
        }
        if (faceText != null)
        {
            faceText.gameObject.SetActive(!useSprite);
            faceText.text = outlaw != null ? outlaw.face : "";
        }

        if (bossNameText != null)
        {
            bossNameText.text = outlaw != null ? outlaw.nameKo : "";
            bossNameText.color = elColor;
        }
        if (elemBadgeText != null)
        {
            elemBadgeText.text = el != null ? $"{el.nameKo} 속성 데미지 x{mgr.CurrentElemMul():0.#}" : "";
            elemBadgeText.color = elColor;
        }
        if (elemBadgeFrame != null) elemBadgeFrame.color = new Color(elColor.r, elColor.g, elColor.b, 0.4f);

        // ── 요약 3칸 ──
        if (bestDamageText != null)
            bestDamageText.text = mgr.BestDamage > 0 ? NumberFormatLoader.Abbrev(mgr.BestDamage) : "—";
        if (myRankText != null && mgr.BestDamage <= 0)
            myRankText.text = "—"; // 기록 생기면 RankBoard가 실순위로 덮어씀
        if (triesText != null)
            triesText.text = $"{mgr.TriesLeft()}/{mgr.FreeTriesPerDay}";

        // ── 도전 버튼 ──
        bool unlocked = mgr.IsUnlocked;
        bool canGo = unlocked && mgr.TriesLeft() > 0 && !mgr.BattleActive;
        if (challengeButton != null) challengeButton.interactable = canGo;

        // 비활성 사유 진단 로그(원인 파악용 — 확인 후 삭제 예정)
        if (!canGo)
        {
            var cm = CharacterManager.Instance;
            string why =
                cm == null ? "CharacterManager 싱글톤 없음"
                : !cm.IsReady ? "CharacterManager 미초기화(IsReady=false)"
                : !unlocked ? $"해금 조건 미달 — 보유 동료 {cm.OwnedCount}/{mgr.UnlockCharCount}명"
                : mgr.TriesLeft() <= 0 ? $"오늘 도전 횟수 소진({mgr.FreeTriesPerDay}회)"
                : mgr.BattleActive ? "이미 월드보스 전투 진행 중(BattleActive)"
                : "알 수 없음";
            Debug.Log($"[WorldBoss] 도전 버튼 비활성: {why} · outlaw={(outlaw != null ? outlaw.nameKo : "null")} · FieldManager={(FieldManager.Instance != null ? "OK" : "없음")}");
        }
        if (challengeLabel != null)
            challengeLabel.text = !unlocked ? $"동료 {mgr.UnlockCharCount}명 보유 시 해금"
                : mgr.TriesLeft() > 0 ? "도전하기" : "오늘 도전 완료";

        // 베스트가 갱신됐을 수 있으니 랭킹 보드 새로고침
        if (rankBoard != null) rankBoard.Refresh();
    }
}
