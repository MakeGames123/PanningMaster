using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 던전 상세 팝업(프로토 dgOpenEntry 포팅). 배너를 누르면 열리고 ◀▶로 던전 간 순환.
// 상세 정보 행·보유 열쇠·도전 버튼을 표시하고, 입장(열쇠 소모)도 여기서 처리한다.
public class DungeonDetailPopup : MonoBehaviour
{
    [SerializeField] GameObject root; //팝업 루트(켜고 끔)

    [Header("헤더")]
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI subText;
    [SerializeField] TextMeshProUGUI iconText; //대형 아이콘(임시 이모지 — 스프라이트 전환 시 교체)

    [Header("본문")]
    [SerializeField] TextMeshProUGUI stageText; //"4단계 도전" / "30초 타임어택" — 등반은 단계 선택 인덱스 겸용
    [SerializeField] Button stageLeftButton;    //◀ 이전 단계(등반 전용 — 다른 던전은 자동 숨김)
    [SerializeField] Button stageRightButton;   //▶ 다음 단계(최대 = 최고 기록+1)
    [SerializeField] GameObject[] rowRoots;        //정보 행(위에서부터 채움, 남는 행은 숨김)
    [SerializeField] TextMeshProUGUI[] rowLabels;  //행 왼쪽 라벨
    [SerializeField] TextMeshProUGUI[] rowValues;  //행 오른쪽 값
    [SerializeField] TextMeshProUGUI noteText;     //하단 안내문
    [SerializeField] TextMeshProUGUI keyText;      //"🔑 보유 3/4"

    [Header("버튼")]
    [SerializeField] Button leftButton;   //◀ 이전 던전
    [SerializeField] Button rightButton;  //▶ 다음 던전
    [SerializeField] Button closeButton;
    [SerializeField] Button enterButton;
    [SerializeField] TextMeshProUGUI enterText; //"도전 (🔑1)" / "열쇠 없음"

    [Header("연결")]
    [SerializeField] NavButtons navButtons; //입장 시 전투 화면(bulletPanel)으로 전환

    readonly List<DungeonBannerUI> banners = new(); //◀▶ 순환 순서 = 목록 순서
    int index = -1;
    int selectedStage; //등반 도전 단계(1~최고 기록+1). 0 = 미선택 → 최신 단계로 초기화
    bool subscribed;

    public bool IsOpen => root != null && root.activeSelf;

    //DungeonPanel이 배너 목록을 주입(순환 순서)
    public void SetBanners(List<DungeonBannerUI> list)
    {
        banners.Clear();
        banners.AddRange(list);
    }

    void Awake()
    {
        if (leftButton != null) leftButton.onClick.AddListener(() => Step(-1));
        if (rightButton != null) rightButton.onClick.AddListener(() => Step(1));
        if (stageLeftButton != null) stageLeftButton.onClick.AddListener(() => StepStage(-1));
        if (stageRightButton != null) stageRightButton.onClick.AddListener(() => StepStage(1));
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (enterButton != null) enterButton.onClick.AddListener(Enter);
        if (root != null) root.SetActive(false);
    }

    void Start() => TrySubscribe();

    void TrySubscribe()
    {
        if (subscribed || DataManager.Instance == null) return;
        DataManager.Instance.onKeyChanged.AddListener(OnKeyChanged); //열쇠 충전/소모 실시간 반영
        subscribed = true;
    }

    void OnDestroy()
    {
        if (subscribed && DataManager.Instance != null)
            DataManager.Instance.onKeyChanged.RemoveListener(OnKeyChanged);
    }

    void OnKeyChanged() { if (IsOpen) Refresh(); }

    public void Open(DungeonBannerUI banner)
    {
        int i = banners.IndexOf(banner);
        if (i < 0) return;
        index = i;
        selectedStage = 0; //던전이 바뀌면 최신 단계(최고 기록+1)로 초기화
        TrySubscribe();
        if (root != null) root.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
    }

    //◀▶ = 던전 간 순환(프로토 dgEntryNav — 목록 끝에서 반대편으로 감김)
    void Step(int dir)
    {
        if (banners.Count == 0) return;
        index = (index + dir + banners.Count) % banners.Count;
        selectedStage = 0; //던전이 바뀌면 최신 단계로 초기화
        Refresh();
    }

    //등반 도전 단계 선택(1 ~ 최고 기록+1)
    void StepStage(int dir)
    {
        if (index < 0 || index >= banners.Count || DataManager.Instance == null) return;
        DungeonBannerUI b = banners[index];
        if (b.Kind != DungeonKind.Laboratory && b.Kind != DungeonKind.Armory) return;

        int max = DataManager.Instance.GetClimbBest(b.DungeonId) + 1;
        int cur = selectedStage > 0 ? selectedStage : max;
        selectedStage = Mathf.Clamp(cur + dir, 1, max);
        Refresh();
    }

    void Refresh()
    {
        if (index < 0 || index >= banners.Count) return;
        DataManager dm = DataManager.Instance;
        if (dm == null) return;

        DungeonBannerUI b = banners[index];
        DungeonData d = b.SheetData;
        DungeonConfigLoader cfg = DungeonConfigLoader.Instance;

        if (nameText != null) nameText.text = b.DisplayName;
        if (subText != null) subText.text = d != null ? d.subKo : "";
        if (iconText != null) iconText.text = d != null ? d.emoji : "⚔️";

        List<(string label, string value)> rows = new();
        string note;

        //단계 선택 화살표는 등반 전용 — 기본 숨김, 등반 케이스에서 다시 켠다
        if (stageLeftButton != null) stageLeftButton.gameObject.SetActive(false);
        if (stageRightButton != null) stageRightButton.gameObject.SetActive(false);

        switch (b.Kind)
        {
            case DungeonKind.Laboratory:
            case DungeonKind.Armory:
            {
                int best = dm.GetClimbBest(b.DungeonId);
                int max = best + 1; //선택 가능 최대 = 최고 기록+1
                if (selectedStage < 1 || selectedStage > max) selectedStage = max; //미선택/범위 밖 → 최신 단계
                int stage = selectedStage;
                SetStage($"{stage}단계", "도전");

                //단계 선택 화살표: 1단계면 왼쪽, 최신 단계면 오른쪽 숨김(구 DungeonEntry 층 선택 문법)
                if (stageLeftButton != null) stageLeftButton.gameObject.SetActive(stage > 1);
                if (stageRightButton != null) stageRightButton.gameObject.SetActive(stage < max);

                if (d != null)
                {
                    AddRewardRow(rows, d.reward1Type, d.reward1Base + d.reward1PerStage * stage);
                    AddRewardRow(rows, d.reward2Type, d.reward2Base + d.reward2PerStage * stage);
                }
                rows.Add(("👹 몬스터 체력", NumberFormatLoader.Abbrev(ClimbDungeonField.ClimbHpFor(stage))));
                rows.Add(("🏅 최고 기록", best > 0 ? $"{best}단계" : "기록 없음"));
                note = "한 탄창(6발) 안에 처치하면 클리어";
                break;
            }
            case DungeonKind.Gold:
            {
                int sec = cfg != null ? cfg.GetInt("DurationSec", 30) : 30;
                long bestGold = dm.bestGoldMined;
                SetStage($"{sec}초", "타임어택");
                rows.Add(("💥 부순 만큼", "🪙 골드"));
                rows.Add(("🏅 최고 기록", bestGold > 0 ? $"🪙 {NumberFormatLoader.Abbrev(bestGold)}" : "기록 없음"));
                note = "시간이 끝나면 캔 만큼 정산 — 실패 없음";
                break;
            }
            case DungeonKind.Train: //프로토 v39 tk 팝업
            {
                int sec = cfg != null ? cfg.GetInt("DurationSec", 30) : 30;
                int every = cfg != null ? cfg.GetInt("TkGoldenEvery", 4) : 4;
                int bonus = cfg != null ? cfg.GetInt("TkGoldenBonus", 12) : 12;
                int best = dm.bestTrainCarts;
                SetStage($"{sec}초", "열차 습격");
                rows.Add(("🚃 부순 화물칸만큼", "🎫 티켓"));
                rows.Add(($"✨ {every}칸마다", $"황금 화물칸 (+🎫{bonus})"));
                rows.Add(("🏅 최고 기록", best > 0 ? $"{best}칸" : "기록 없음"));
                note = "시간 안에 화물칸을 최대한 부수자 — 실패 없음";
                break;
            }
            default: //Normal — 층 진행형(수치는 DungeonField 인스펙터 기본값 기준)
            {
                int floor = dm.clearedDungeonFloor + 1;
                SetStage($"{floor}층", "도전");
                rows.Add(("⏱ 제한시간", "15초"));
                rows.Add(("👹 몬스터 체력", "현재 스테이지 ×10"));
                note = "제한시간 안에 처치하면 다음 층 해금";
                break;
            }
        }

        FillRows(rows);
        if (noteText != null) noteText.text = note;

        int own = dm.GetKey(b.Kind); //열쇠는 던전마다 개별 종류
        if (keyText != null) keyText.text = $"{b.KeyEmoji} 보유 {own}/{dm.GetMaxKey(b.Kind)}";
        if (enterButton != null) enterButton.interactable = own > 0;
        if (enterText != null) enterText.text = own > 0 ? $"도전 ({b.KeyEmoji}1)" : "열쇠 없음";
    }

    void SetStage(string big, string small)
    {
        if (stageText != null) stageText.text = $"{big} {small}";
    }

    //등반 클리어 보상 행: 지급량 = floor(Base + PerStage×단계)
    static void AddRewardRow(List<(string, string)> rows, string type, float amount)
    {
        if (string.IsNullOrEmpty(type)) return;
        string emoji = type switch
        {
            "Crate" => "🧰",
            "Hourglass" => "⏳",
            "Ticket" => "🎫",
            "Gold" => "🪙",
            "Gem" => "💎",
            _ => "",
        };
        rows.Add(($"{emoji} 클리어 보상", $"+{NumberFormatLoader.Abbrev(Mathf.FloorToInt(amount))}"));
    }

    void FillRows(List<(string label, string value)> rows)
    {
        if (rowRoots == null) return;
        for (int i = 0; i < rowRoots.Length; i++)
        {
            bool used = i < rows.Count;
            if (rowRoots[i] != null) rowRoots[i].SetActive(used);
            if (!used) continue;
            if (rowLabels != null && i < rowLabels.Length && rowLabels[i] != null) rowLabels[i].text = rows[i].label;
            if (rowValues != null && i < rowValues.Length && rowValues[i] != null) rowValues[i].text = rows[i].value;
        }
    }

    void Enter()
    {
        if (index < 0 || index >= banners.Count || FieldManager.Instance == null) return;
        DungeonBannerUI b = banners[index];

        if (!DataManager.Instance.UseKey(b.Kind, 1)) { Refresh(); return; } //열쇠 부족(던전별 개별 열쇠)

        Close();

        switch (b.Kind)
        {
            case DungeonKind.Laboratory: FieldManager.Instance.EnterLabDungeon(selectedStage); break;
            case DungeonKind.Armory: FieldManager.Instance.EnterArmoryDungeon(selectedStage); break;
            case DungeonKind.Gold: FieldManager.Instance.EnterGoldDungeon(); break;
            case DungeonKind.Train: FieldManager.Instance.EnterTrainDungeon(); break;
            default: FieldManager.Instance.EnterDungeon(DataManager.Instance.clearedDungeonFloor + 1); break;
        }

        if (navButtons != null) navButtons.Navigate("bullet"); //전투가 보이는 탄환 화면으로 전환
    }
}
