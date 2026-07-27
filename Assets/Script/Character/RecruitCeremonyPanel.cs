using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 모집 결과창(세리머니) — 모집하면 열리고, 창 안의 [모집 x1]/[모집 x10]으로 연속 모집.
// x1 = 큰 카드 1장(singleGroup), x10 = 미니 카드 10장 그리드(multiGroup).
// 하단 정보 = 모집 Lv · 보증까지 남은 횟수 · 모집서 보유량.
public class RecruitCeremonyPanel : MonoBehaviour
{
    [SerializeField] GameObject view;                 // 결과창 루트(꺼진 채 시작 — 이 스크립트는 항상 켜진 부모에)
    [SerializeField] TextMeshProUGUI titleCountText;  // 제목 옆 "x10"(선택 연결 — x1이면 빈 문자열)

    [SerializeField] GameObject singleGroup;          // x1 결과 영역
    [SerializeField] RecruitResultCardUI singleCard;  // 큰 카드

    [SerializeField] GameObject multiGroup;           // x10 결과 영역(그리드)
    [SerializeField] RecruitResultCardUI[] multiCards = new RecruitResultCardUI[10];

    [SerializeField] TextMeshProUGUI infoText;        // "모집 Lv.N · {등급} 보증까지 M회 · 모집서 K"
    [SerializeField] Button recruit1Button;
    [SerializeField] Button recruit10Button;
    [SerializeField] Button closeButton;

    void Awake()
    {
        if (recruit1Button != null) recruit1Button.onClick.AddListener(() => RecruitAndShow(1));
        if (recruit10Button != null) recruit10Button.onClick.AddListener(() => RecruitAndShow(10));
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (view != null) view.SetActive(false);
    }

    // 모집 실행 + 결과 표시. 모집서 부족이면 아무것도 뽑지 않고 정보만 갱신
    public void RecruitAndShow(int count)
    {
        var recruiter = CharacterRecruiter.Instance;
        if (recruiter == null) return;

        var results = recruiter.TryRecruitMany(count);
        if (results == null)
        {
            Debug.Log("[모집] 모집서가 부족합니다");
            if (view != null && view.activeSelf) RefreshInfo();
            return;
        }

        Show(results);
    }

    void Show(List<CharacterRecruiter.RecruitResult> results)
    {
        if (view != null) view.SetActive(true);

        bool multi = results.Count > 1;
        if (singleGroup != null) singleGroup.SetActive(!multi);
        if (multiGroup != null) multiGroup.SetActive(multi);
        if (titleCountText != null) titleCountText.text = multi ? "x" + results.Count : "";

        if (!multi)
        {
            if (singleCard != null) singleCard.Set(results[0].character, results[0].isNew);
        }
        else
        {
            for (int i = 0; i < multiCards.Length; i++)
            {
                if (multiCards[i] == null) continue;

                bool has = i < results.Count;
                multiCards[i].gameObject.SetActive(has);
                if (has) multiCards[i].Set(results[i].character, results[i].isNew);
            }
        }

        RefreshInfo();
    }

    void RefreshInfo()
    {
        var mgr = CharacterManager.Instance;
        var recruiter = CharacterRecruiter.Instance;
        if (infoText == null || mgr == null || recruiter == null) return;

        var pityGrade = CharacterGradeLoader.Instance.Get(recruiter.PityTargetGrade());
        int scroll = DataManager.Instance != null ? DataManager.Instance.scroll : 0;

        infoText.text = $"모집 Lv.{mgr.RecruitLevel} · {(pityGrade != null ? pityGrade.nameKo : "")} 보증까지 {recruiter.PityRemaining}회 · 모집서 {scroll}";
    }

    public void Close()
    {
        if (view != null) view.SetActive(false);
    }
}
