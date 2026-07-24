using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

// 캐릭터(총잡이) 보유 로스터 + 모집 레벨 관리. 뽑기 메커니즘은 CharacterRecruiter가 담당.
// 모집 레벨 = 누적 모집 수 파생(RecruitLevel) — 레벨업하면 상위 등급이 풀에 데뷔(ChracterGrade.DebutRecruitLv).
public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    const string StarterId = "rusty"; // 스타터(결정론)

    // 보유 캐릭터 상태 — id·스프라이트는 SO가 소유
    [System.Serializable]
    public class CharacterState
    {
        public CharacterInfoSO infoSO;
        public int cards; // 🃏 중복 모집 카드

        public string Id => infoSO != null ? infoSO.characterId : null;
    }

    [SerializeField] List<CharacterInfoSO> characterInfoSOs = new(); // 인스펙터 등록(AllBulletList 문법)
    readonly Dictionary<string, CharacterInfoSO> infoSODic = new();

    readonly Dictionary<string, CharacterState> roster = new();

    public const int PartySize = 3;
    readonly string[] party = new string[PartySize]; // 장착 슬롯(프로토 G.party — null = 빈 슬롯)

    public int RecruitCount { get; private set; } // 누적 모집 수

    public UnityEvent onChanged = new(); // 상태 변화 → UI 갱신

    bool initialized;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // SO 딕셔너리 구성(AllBulletList 문법)
        foreach (var so in characterInfoSOs)
        {
            if (so == null || string.IsNullOrEmpty(so.characterId)) continue;
            infoSODic[so.characterId] = so;
        }
    }

    public CharacterInfoSO GetInfoSO(string id)
    {
        if (infoSODic.TryGetValue(id, out var so)) return so;
        Debug.LogWarning($"[캐릭터] CharacterInfoSO 미등록: {id}");
        return null;
    }

    public Sprite GetSprite(string id)
    {
        var so = GetInfoSO(id);
        return so != null ? so.characterSprite : null;
    }

    void Update()
    {
        // 시트가 준비되면 스타터 지급(싱글톤 타이밍 안전망)
        if (!initialized) TryInit();
    }

    void TryInit()
    {
        if (initialized) return;
        if (RecruitLevelLoader.Instance == null || !RecruitLevelLoader.Instance.IsLoaded) return;
        if (CharacterGradeLoader.Instance == null || !CharacterGradeLoader.Instance.IsLoaded) return;
        if (CharacterRosterLoader.Instance == null || !CharacterRosterLoader.Instance.IsLoaded) return;

        // 스타터 결정론 지급 + 1번 슬롯 자동 배치(프로토 party[0]='rusty')
        if (!roster.ContainsKey(StarterId))
            roster[StarterId] = new CharacterState { infoSO = GetInfoSO(StarterId) };
        if (party[0] == null) party[0] = StarterId;

        initialized = true;
        onChanged.Invoke();
    }

    public bool IsReady => initialized;

    public int RecruitLevel => RecruitLevelLoader.Instance != null
        ? RecruitLevelLoader.Instance.GetRecruitLevel(RecruitCount) : 1;

    // 프로토 chLvCost: Lv→Lv+1 요구 🃏 카드 수 = ceil(lv/2). 성장 복원 시에도 이 공식 사용
    public static int LevelUpCost(int lv) => (lv + 1) / 2;

    public CharacterState GetState(string id) => roster.TryGetValue(id, out var st) ? st : null;
    public bool IsOwned(string id) => roster.ContainsKey(id);

    // ── 장착 파티(프로토 chAssign) ──

    public string GetPartyMember(int slot)
        => slot >= 0 && slot < PartySize ? party[slot] : null;

    public int PartySlotOf(string id) => System.Array.IndexOf(party, id);
    public bool IsEquipped(string id) => PartySlotOf(id) >= 0;

    // 장착 — 이미 다른 슬롯에 있으면 자리 스왑(프로토 chAssign 스왑 규칙)
    public bool EquipParty(int slot, string id)
    {
        if (slot < 0 || slot >= PartySize || !IsOwned(id)) return false;

        int prev = PartySlotOf(id);
        if (prev == slot) return true;
        if (prev >= 0) party[prev] = party[slot]; // 스왑
        party[slot] = id;

        if (QuestEventManager.Instance != null) QuestEventManager.Instance.AddEvent("chSet");
        onChanged.Invoke();
        return true;
    }

    public void UnequipParty(int slot)
    {
        if (slot < 0 || slot >= PartySize || party[slot] == null) return;
        party[slot] = null;

        if (QuestEventManager.Instance != null) QuestEventManager.Instance.AddEvent("chSet");
        onChanged.Invoke();
    }

    // ── CharacterRecruiter가 호출하는 상태 변이 ──

    public void AddRecruitCount() => RecruitCount++;

    // 획득: 미보유 = 로스터 등록 / 중복 = 🃏 카드 +1. 반환 = 신규 여부.
    public bool Acquire(string id)
    {
        bool isNew = !roster.ContainsKey(id);
        if (isNew) roster[id] = new CharacterState { infoSO = GetInfoSO(id) };
        else roster[id].cards++;

        onChanged.Invoke();
        return isNew;
    }
}
