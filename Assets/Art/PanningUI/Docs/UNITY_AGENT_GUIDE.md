# UNITY AGENT GUIDE — 팝업 UI r4 (확정 2026-09-27)

> **이 문서는 Unity 안에서 일하는 AI 에이전트용 입구입니다.** 이것부터 읽고, 숫자는 JSON에서 가져오세요.
> 대상 프로젝트: `D:\Dev\PanningMaster` (Unity 6000.0.67f1 · URP · TextMesh Pro). 우리 폴더 = `Assets/Art/PanningUI/`.
> 디자인 상태: **r4 겉모습 사용자 확정**(리뷰097 Q1, 2026-09-27) — 명일방주: 엔드필드 문법("//" 제목 표식 + 모서리 꺾쇠 칸, 무채 차콜) + 버튼 바이블 v1(금 C5 / 차콜 W7). 리볼버 전후 선택 팝업은 r3 확정 그대로.

## 0. 읽는 순서

1. 이 문서 (규칙·폰트·조립 방법)
2. `Docs/text-styles.json` — **글자 스타일 111종 + 장면별 글자 753줄의 위치·스타일**(폰트 정보의 단일 진실)
3. `Docs/unity-import.csv` — PNG마다 크기·9-slice 테두리
4. `Docs/layout-coordinates.json` — 장면별 요소 좌표(`finalScenes`)
5. `Docs/SPEC.md` — 상태 규칙·배경 설명(사람용 긴 문서. §9 = r4 문법)
6. `Docs/icon-list.csv` — 그릴 아이콘 ID 목록
7. `Docs/Reference/*.png` — 확정 시안(400 폭). **조립 결과는 이 그림과 나란히 비교해 검증**

## 1. 절대 규칙

| # | 규칙 | 이유 |
|---|---|---|
| 1 | **글자를 PNG에 굽지 않는다.** 모든 문구 = TextMeshPro 텍스트(로컬라이즈 대상). 예외는 등급 글자 E~MR(`GradeLetter/` PNG 13장)뿐 | 글로벌 로컬라이징 |
| 2 | **이모지 금지.** 시안의 점선 네모 = 사람이 그릴 아이콘 자리 → `Image` 오브젝트를 두고 이름을 아이콘 ID(`ico_gold` 등)로. 스프라이트가 없으면 비워 둠(투명 또는 회색 자리표시) | 아이콘은 전부 사용자가 직접 그림 |
| 3 | **TMP 기본 폰트(`TMP Settings` → `BMJUA_ttf SDF`)를 바꾸지 않는다.** 개발자 기존 UI 37곳이 쓴다. 우리 팝업은 TMP 컴포넌트마다 폰트 에셋을 직접 지정 | 기존 화면 보호 |
| 4 | `Assets/TextMesh Pro/`, `Assets/PlayFabSDK/`, 개발자 `Script/`·`Resources/`·씬은 사용자 지시 없이 수정하지 않는다. 새 것은 `Assets/Art/PanningUI/` 아래에 만든다 | 개발자 저장소 |
| 5 | **git commit / push 금지**(사용자 지시가 있을 때만) | 개발자 저장소 |
| 6 | 색·크기·간격은 JSON 값을 그대로 쓴다. 눈대중으로 바꾸지 말 것. 표와 시안이 다르면 **표가 우선** | 확정본 |

## 2. 화면 기준 — 모든 숫자의 단위

| 항목 | 값 |
|---|---|
| 설계 화면 | 400 × 867 (설계 px, 모바일 세로). JSON의 `css400`·`rect400`·`designPx` |
| Canvas Scaler | Scale With Screen Size, Reference **1200 × 2601**, Match **0.5** |
| 변환 | **Unity 값 = 설계 px × 3** (위치·크기·폰트 크기 모두). JSON의 `rectUnity1200`·`unityFontSize`는 이미 ×3 |
| 좌표 원점 | 화면 왼쪽 위. RectTransform에 넣을 땐 앵커 Top-Left, y는 부호 반대로 |
| PNG | 전부 @3x → Set Native Size 하면 설계 크기와 맞음(PPU 100, 위 Canvas 기준) |

현재 개발자 씬 Canvas는 Constant Pixel Size 800×600 기본값입니다. 우리 팝업은 **자기 Canvas(위 설정)를 가진 프리팹**으로 만드는 것을 권장합니다.

## 3. 폰트 — 4종이면 끝

### 3-1. 어떤 글자에 어떤 폰트 (역할 규칙)

| 화면 | 굵은 글자(제목·수치·버튼·강조) | 보통 글자(본문·줄 제목·표·설명) |
|---|---|---|
| 팝업 9종 (`Sprites/Popups/Common`) — 재화 부족·확인·상품 상세·우편함·전투력 상세·던전 결과·확률표·기능 해금·닉네임 | **Pretendard-SemiBold SDF** | **Pretendard-Medium SDF** |
| 방치 보상 (`Sprites/Popups/IdleReward`) | Pretendard-SemiBold SDF | Pretendard-Medium SDF |
| 리볼버 전후 선택 (`Sprites/Popups/RevolverCompare`) | **GmarketSans-Bold SDF** | **GmarketSans-Medium SDF** |
| 등급 글자 E~MR | 폰트 아님 — `GradeLetter/ui_rev_grade_NN_<등급>.png` | |

- 굵기는 폰트 파일로 고른다(TMP Bold 스타일 버튼 ✗). 기울임은 두 곳만 TMP Italic(`_I`로 끝나는 스타일 ID).
- 폰트 파일: `Fonts/` (라이선스 전부 SIL OFL 1.1 — 게임 임베드·상업 사용 가능, 파일 단독 판매만 금지. `Fonts/LICENSE_*.txt`, 해시 `Fonts/SHA256.txt`).

| 파일 | 만들 TMP Font Asset 이름 |
|---|---|
| `Fonts/Pretendard-SemiBold.otf` | `Pretendard-SemiBold SDF` |
| `Fonts/Pretendard-Medium.otf` | `Pretendard-Medium SDF` |
| `Fonts/GmarketSans-Bold.ttf` | `GmarketSans-Bold SDF` |
| `Fonts/GmarketSans-Medium.ttf` | `GmarketSans-Medium SDF` |
| `Fonts/Fallback/PretendardJP-SemiBold.otf` · `-Medium.otf` | `PretendardJP-SemiBold SDF` · `PretendardJP-Medium SDF` (일본어 폴백) |

### 3-2. TMP Font Asset 만들기 (에디터 작업)

`Window > TextMeshPro > Font Asset Creator` 대신, 프로젝트 창에서 폰트 파일 우클릭 **Create > TextMeshPro > Font Asset > SDF** 로 만든 뒤 Inspector에서:

| 설정 | 값 |
|---|---|
| Atlas Population Mode | **Dynamic** (한글 11,172자를 미리 굽지 않음) |
| Sampling Point Size | 90 |
| Padding | 9 |
| Atlas Width × Height | 2048 × 2048 |
| Render Mode | SDFAA |
| Multi Atlas Textures | On |
| Include Font Data | On (동적 아틀라스에 필요) |
| Clear Dynamic Data On Build | On |

저장 위치 `Assets/Art/PanningUI/Fonts/SDF/`. 머티리얼 프리셋도 같은 폴더.

### 3-3. 폴백 (Font Asset Inspector → Fallback Font Assets)

| 기본 | 폴백 순서 |
|---|---|
| Pretendard-SemiBold SDF | PretendardJP-SemiBold SDF → (현지화 때) Noto Sans SC → TC → Thai |
| Pretendard-Medium SDF | PretendardJP-Medium SDF → (현지화 때) Noto Sans SC → TC → Thai |
| GmarketSans-Bold SDF | **Pretendard-SemiBold SDF** (Gmarket에 없는 ✓ › ‹ 기호, 그리고 그 뒤 체인) |
| GmarketSans-Medium SDF | Pretendard-Medium SDF |

Noto Sans SC/TC/Thai 파일은 아직 없음(출시 현지화 때 Google Fonts에서 받아 static Medium/SemiBold로 추가).

### 3-4. 글리프 검사 결과 (2026-09-27, fontTools로 확인)

- 네 폰트 모두 한글 11,172자 전부 있음. 시안의 글자는 전부 그려짐. **없는 기호 3개만 아이콘으로**:

| 기호 | 쓰는 곳 | 처리 |
|---|---|---|
| ✕ U+2715 | 리볼버 "✕ 닫기"·"✕ 자동 멈춤" | 아이콘 `ico_close_s` (글자는 "닫기"만) |
| ▾ U+25BE | 전투력 상세 접힘 줄 | 아이콘 `ico_fold` |
| ➤ U+27A4 | 방치 보상 "방치 중 돌파" 화살표 | 아이콘 `ico_arrow_r` |
| ✓ › ‹ | Gmarket에 없음 | Pretendard 폴백이 그림(조치 불필요). 우편 "받음" 체크는 `ico_check` 아이콘 |

### 3-5. 머티리얼 프리셋 5개 (Font Asset의 기본 머티리얼을 복제해서)

글자 색은 **TMP Vertex Color**로 넣고 머티리얼 Face는 흰색 그대로 둡니다(아래 Fx 2개만 예외). 오프셋은 설계 px — Underlay Offset을 조절해 `Docs/Reference/` 시안과 같아 보이게 맞추세요.

| 프리셋 | 켜는 것 | 값 (설계 px) | 쓰는 곳 |
|---|---|---|---|
| `MAT_Shadow1` | Underlay | 검정 α1, 아래 1px, softness 0 | 제목 띠 글자·차콜 버튼 글자·꺾쇠 수치 일부 |
| `MAT_BtnGoldText` | Underlay | `#fff5d2` α0.5, 아래 1px | 금 버튼(p) 글자 |
| `MAT_NumShadow` | Underlay | 검정 α0.35, 아래 2~3px, 살짝 흐림 | 큰 수치(던전 단계·스테이지) |
| `MAT_UnlockFx` | Face `#ffd87a` + Underlay + Glow | 갈색 `#8a4a10` 아래 4px · 금빛 `#ffc850` α0.4 반경 22px · Italic | 기능 해금 큰 이름 |
| `MAT_ClearFx` | Face `#ffd87a` + Underlay + Glow | 갈색 `#8a4a10` 아래 5px · 금빛 `#ffc850` α0.55 반경 26px · Italic | 던전 "클리어!" 연출(키 `ui.dungeon.clear`) |

### 3-6. 스타일 찾는 법

`Docs/text-styles.json`:
- `styles[]` — 스타일 111종: `id` · `fontAsset` · `designPx` · **`unityFontSize`** · `color`(hex, `@0.5` = 알파) · `characterSpacing`(TMP 자간, 1/100 em) · `italic` · `material` · `examples` · `scenes`
- `sceneTexts.<장면>[]` — 그 장면의 글자 줄 전부: `text`(예시 문구) · `style`(위 id) · `align` · `rect400` · **`rectUnity1200`**
- 스타일 ID 읽는 법: `pop_SB_15_titleGold_sh` = 팝업 / Pretendard **S**emi**B**old / 설계 15px / 색 이름 titleGold / 그림자 머티리얼. `M` = Medium, `GB`·`GM` = Gmarket Bold·Medium, `_I` = 기울임, `9p5` = 9.5px.

장면 키: `pop-need` 재화 부족 · `pop-confirm` 확인 · `pop-buy` 상품 상세 · `pop-mail`/`pop-mailgot` 우편함(받기 전/후) · `pop-power`/`pop-power2` 전투력 상세(1층/출처) · `pop-dgclearfx` 클리어 연출 · `pop-dgclear` 던전 보상 · `pop-forgeinfo` 확률표 · `pop-unlock` 기능 해금 · `pop-nick` 닉네임 · `off`/`off-cap`/`off-empty` 방치 보상 · `rev-*` 리볼버 13상태.

**자주 쓰는 스타일 (팝업 9종 기준)**

| 역할 | 스타일 ID | 폰트 · Unity 크기 · 색 |
|---|---|---|
| 팝업 제목(제목 띠, "//" 뒤) | `pop_SB_15_titleGold_sh` | Pretendard-SemiBold · 45 · `#f0cf82` · 자간 6 · Shadow1 |
| 큰 제목(상품·확률표) | `pop_SB_17_titleGold_sh` | SemiBold · 51 · `#f0cf82` |
| 줄 제목 | `pop_M_12_text` | Medium · 36 · `#f3e8d6` |
| 보상·재화 수치 | `pop_SB_10p5_num` | SemiBold · 31.5 · `#f0dcb0` |
| 흐린 라벨 | `pop_SB_10p5_dim` · `pop_SB_9_dim` | SemiBold · 31.5 / 27 · `#b8a58a` |
| 강조 금 수치 | `pop_SB_11_accentGold` 등 `_accentGold` | SemiBold · `#ffd87a` |
| 금 버튼(큰) | `pop_SB_17_btnP_sh` | SemiBold · 51 · `#2a1a08` · BtnGoldText |
| 금 버튼(줄 안 작은) | `pop_SB_11_btnP_sh` | SemiBold · 33 · `#2a1a08` · BtnGoldText |
| 확인창 버튼 | `pop_SB_14_btnP_sh` / `pop_SB_14_btnS_sh` / `pop_SB_14_off_sh` | 42 · 금 `#2a1a08` / 차콜 `#e6d9c3` / 꺼짐 `#7d6f5e` |
| 확인창 질문 | `pop_SB_14_text` | SemiBold · 42 · `#f3e8d6` |

리볼버 팝업 스타일은 `rev_*`(SPEC §5 표와 같음), 방치 보상은 `idle_*`.

> 시안 잔여 정리: 시안 HTML에서 팝업·방치 안 4곳이 다른 폰트로 찍혔습니다(멈추기·방치 받기·2배 받기 = Gmarket, 전투력 출처 "1번/위력" = v48 픽셀 폰트). **Unity에서는 Pretendard-SemiBold로 통일** — JSON `note`에 표시.

## 4. 스프라이트 조립

- PNG 설정은 `Editor/PanningUIImporter.cs`가 처음 임포트 때 자동(Sprite · Full Rect · 무압축 · 밉맵 없음 · PPU 100 · 9-slice는 `unity-import.csv`의 `Sliced` 행). 다시 적용 = 그 PNG의 `.meta` 삭제 후 Reimport.
- `Image Type`: CSV에서 `Sliced`인 것은 **Sliced**, 나머지는 Simple + Set Native Size.
- 팝업 9종 부품 이름 → 역할: SPEC §9-2. 팝업별 위→아래 구성: SPEC §4-2. 좌표: `layout-coordinates.json` `finalScenes.<장면>`.
- 목록 줄 구분선은 PNG 없음 → 1px(Unity 3) 단색 `Image`, 색 `rgba(214,172,96,.16)`.
- 레이어: 게임 화면 → 검정 딤 90%(α230) → 팝업 → 하단 내비 + 닫기 탭 X(`Sprites/Navigation/ui_nav_close_t01a`, rect {178,808,44,46} — SPEC §10, 옛 `ui_nav_close_b1_red` 대체). 클리어 연출만 딤 60%(α150).
- 버튼 역할: 금 = 주 행동(받기·구매·진행), 차콜 = 보조(취소·돌아가기·멈추기), 꺼짐 = `_off`. SPEC §8.

## 5. 아이콘 자리

- `Docs/icon-list.csv`의 ID로 `Image` 오브젝트 이름을 짓고 권장 크기(설계 px × 3)로 둔다. 스프라이트는 비워 둔다(사용자가 그려 `Sprites/Icons/`에 넣을 예정).
- 게임 코드의 이모지 문자열은 아이콘 ID로 바꾼다(구현 단계). 이모지 폰트·TMP Sprite Asset 이모지 사용 금지.

## 6. 완료 검증 (에이전트 체크리스트)

1. 콘솔 오류 0 (기존 PlayFab TitleId·Android 모듈 경고는 우리 작업과 무관 — 보고만)
2. 만든 팝업을 Game 뷰 1200×2601(또는 9:19.5)로 캡처 → `Docs/Reference/<같은 장면>_400.png`와 **나란히 한 장**으로 저장해 사용자에게 보여 준다
3. 글자는 전부 TMP(글자 PNG 0) · 이모지 0 · 폰트는 3-1 표대로 · TMP 기본 폰트 그대로
4. git 상태는 건드리지 않음(커밋 없음)

## 부록 A. 스타일 전체 표 (text-styles.json `styles`와 같은 내용)

| 스타일 ID | 폰트 에셋 | 설계px → Unity | 색 | 자간 | 머티리얼 | 예시 |
|---|---|---|---|---|---|---|
| `pop_M_12_text` | Pretendard-Medium | 12 → **36** | `#f3e8d6` | 1.7 | — | 주간 현상수배 랭킹 보상 (12위) / 출석 7일차 보상 / 업데이트 기념 선물 |
| `pop_M_9p5_dim` | Pretendard-Medium | 9.5 → **28.5** | `#b8a58a` | 2.1 | — | 교환 후 되돌릴 수 없습니다 |
| `pop_M_9p5_text` | Pretendard-Medium | 9.5 → **28.5** | `#f3e8d6` | 2.1 | — | — / 스탯 / 성장 |
| `pop_M_9_dim` | Pretendard-Medium | 9 → **27** | `#b8a58a` | 2.2 | — | [E] 엠버 — 주스탯·부옵션은 위 표 장비 열에 스탯별 반영 / 화염 탄환 최종 데미지 +40% ( |
| `pop_SB_64_accentGold_I` | Pretendard-SemiBold | 64 → **192** | `#ffd87a` | 2 | MAT_ClearFx | 클리어! |
| `pop_SB_46_accentGold_I` | Pretendard-SemiBold | 46 → **138** | `#ffd87a` | 2 | MAT_UnlockFx | 던전 |
| `pop_SB_40_accentGold_sh` | Pretendard-SemiBold | 40 → **120** | `#ffd87a` | 0.5 | MAT_NumShadow | 37 |
| `pop_SB_34_text` | Pretendard-SemiBold | 34 → **102** | `#f3e8d6` | 0.6 | — | / 5 |
| `pop_SB_34_down` | Pretendard-SemiBold | 34 → **102** | `#ff6b5b` | 0.6 | — | 0 |
| `pop_SB_34_accentGold` | Pretendard-SemiBold | 34 → **102** | `#ffd87a` | 0.6 | — | 0 |
| `pop_SB_26_accentGold` | Pretendard-SemiBold | 26 → **78** | `#ffd87a` | 12 | — | 보상 획득! |
| `pop_SB_24_num` | Pretendard-SemiBold | 24 → **72** | `#f0dcb0` | 0.8 | — | 120 |
| `pop_SB_22_accentGold_sh` | Pretendard-SemiBold | 22 → **66** | `#ffd87a` | 4.1 | MAT_Shadow1 | ‹ |
| `pop_SB_22_accentGold` | Pretendard-SemiBold | 22 → **66** | `#ffd87a` | 0.9 | — | 37 |
| `pop_SB_17_btnP_sh` | Pretendard-SemiBold | 17 → **51** | `#2a1a08` | 1 | MAT_BtnGoldText | 300 구매 / 모두 받기 / 다음 단계 (38) |
| `pop_SB_17_off_sh` | Pretendard-SemiBold | 17 → **51** | `#7d6f5e` | 1 | MAT_Shadow1 | 모두 받음 |
| `pop_SB_17_titleGold_sh` | Pretendard-SemiBold | 17 → **51** | `#f0cf82` | 5.3 | MAT_Shadow1 | 열쇠 꾸러미 / 뽑기 확률표 |
| `pop_SB_15_titleGold_sh` | Pretendard-SemiBold | 15 → **45** | `#f0cf82` | 6 | MAT_Shadow1 | 우편함 / 무기 상자 부족 / 전투력 상세 |
| `pop_SB_14_btnP_sh` | Pretendard-SemiBold | 14 → **42** | `#2a1a08` | 1 | MAT_BtnGoldText | 바로 받기 |
| `pop_SB_14_off_sh` | Pretendard-SemiBold | 14 → **42** | `#7d6f5e` | 1 | MAT_Shadow1 | 저장 |
| `pop_SB_14_btnS_sh` | Pretendard-SemiBold | 14 → **42** | `#e6d9c3` | 1 | MAT_Shadow1 | 취소 |
| `pop_SB_14_text` | Pretendard-SemiBold | 14 → **42** | `#f3e8d6` | 1.4 | — | 무기 상자 10개를 바로 받을까요? |
| `pop_SB_14_accentGold` | Pretendard-SemiBold | 14 → **42** | `#ffd87a` | 1.4 | — | 5 / 96.85% / 2.74% |
| `pop_SB_13_tierRare` | Pretendard-SemiBold | 13 → **39** | `#2196f3` | 1.5 | — | 레어 |
| `pop_SB_13_tierMagic` | Pretendard-SemiBold | 13 → **39** | `#4caf50` | 1.5 | — | 매직 |
| `pop_SB_13_tierNormal` | Pretendard-SemiBold | 13 → **39** | `#9e9e9e` | 1.5 | — | 노멀 |
| `pop_SB_13_dim_sh` | Pretendard-SemiBold | 13 → **39** | `#b8a58a` | 1.5 | MAT_NumShadow | 단계 돌파 |
| `pop_SB_13_text` | Pretendard-SemiBold | 13 → **39** | `#f3e8d6` | 1.5 | — | 닉네임 변경 / 단계 돌파 |
| `pop_SB_13_accentGold` | Pretendard-SemiBold | 13 → **39** | `#ffd87a` | 1.5 | — | +3 / ▾ / › |
| `pop_SB_12_titleGold` | Pretendard-SemiBold | 12 → **36** | `#f0cf82` | 1.7 | — | 새로운 기능 오픈! / 노다지 광맥 |
| `pop_SB_12_text` | Pretendard-SemiBold | 12 → **36** | `#f3e8d6` | 1.7 | — | 매일 열쇠로 보상을 챙길 수 있는 던전이 열렸네! |
| `pop_SB_12_white_sh` | Pretendard-SemiBold | 12 → **36** | `#ffffff` | 7.5 | MAT_Shadow1 | ? |
| `pop_SB_11_btnP_sh` | Pretendard-SemiBold | 11 → **33** | `#2a1a08` | 1 | MAT_BtnGoldText | 바로 가기 / 받기 |
| `pop_SB_11_dim` | Pretendard-SemiBold | 11 → **33** | `#b8a58a` | 1.8 | — | › |
| `pop_SB_11_gold2` | Pretendard-SemiBold | 11 → **33** | `#e3c47a` | 1.8 | — | 최고 기록 37단계! |
| `pop_SB_11_text` | Pretendard-SemiBold | 11 → **33** | `#f3e8d6` | 1.8 | — | 획득 (지급 완료) / 주 1회 한정 / 캐릭터별 전투력 |
| `pop_SB_11_accentGold` | Pretendard-SemiBold | 11 → **33** | `#ffd87a` | 1.8 | — | + / 360M / 6 |
| `pop_SB_10p5_up` | Pretendard-SemiBold | 10.5 → **31.5** | `#6ee05a` | 1.9 | — | ✓ 받음 |
| `pop_SB_10p5_dim` | Pretendard-SemiBold | 10.5 → **31.5** | `#b8a58a` | 1.9 | — | 어디서 얻나요 / 루비 보유 1350 → / 보유 4200 → |
| `pop_SB_10p5_num` | Pretendard-SemiBold | 10.5 → **31.5** | `#f0dcb0` | 1.9 | — | 240M / 50 / 30 |
| `pop_SB_10p5_text` | Pretendard-SemiBold | 10.5 → **31.5** | `#f3e8d6` | 1.9 | — | 스탯 출처 |
| `pop_SB_10p5_accentGold` | Pretendard-SemiBold | 10.5 → **31.5** | `#ffd87a` | 1.9 | — | +0% / 0% / 1230 |
| `pop_SB_10_off2` | Pretendard-SemiBold | 10 → **30** | `#5f5446` | 1.4 | — | 무료 |
| `pop_SB_10_dim` | Pretendard-SemiBold | 10 → **30** | `#b8a58a` | 2 | — | 보유 / 필요 / 전투력 / /10 |
| `pop_SB_10_btnS_sh` | Pretendard-SemiBold | 10 → **30** | `#e6d9c3` | 1 | MAT_Shadow1 | 멈추기 ※시안에서는 GmkB — Unity에서는 이 표대로 통일 |
| `pop_SB_10_text` | Pretendard-SemiBold | 10 → **30** | `#f3e8d6` | 2 | — | 공격력 / 속성 데미지 / 크리티컬 확률 |
| `pop_SB_10_fire` | Pretendard-SemiBold | 10 → **30** | `#ff4136` | 2 | — | 1번 / 위력 ×1.1 ※시안에서는 Galmuri11 — Unity에서는 이 표대로 통일 |
| `pop_SB_10_accentGold` | Pretendard-SemiBold | 10 → **30** | `#ffd87a` | 2 | — | 3 / 0 |
| `pop_SB_9p5_dim` | Pretendard-SemiBold | 9.5 → **28.5** | `#b8a58a` | 2.1 | — | = / 핫딜·결제 페이백으로 모은 보석 — 여기서만 쓰입니다 / (보유 보너스 0.0% 포함) |
| `pop_SB_9p5_accentGold` | Pretendard-SemiBold | 9.5 → **28.5** | `#ffd87a` | 2.1 | — | +0% / 0% / +2% |
| `pop_SB_9_dim` | Pretendard-SemiBold | 9 → **27** | `#b8a58a` | 2.2 | — | 던전 · 단계 등반 / 보상 / 줄을 누르면 출처 |
| `pop_SB_9_accentGold` | Pretendard-SemiBold | 9 → **27** | `#ffd87a` | 2.2 | — | 구매 |
| `pop_SB_9_accentGold_sh` | Pretendard-SemiBold | 9 → **27** | `#ffd87a` | 10 | MAT_Shadow1 | Lv.1 |
| `pop_SB_8p5_dim` | Pretendard-SemiBold | 8.5 → **25.5** | `#b8a58a` | 2.4 | — | — / 골드 던전 / 무기고 |
| `pop_SB_8p5_accentGold` | Pretendard-SemiBold | 8.5 → **25.5** | `#ffd87a` | 2.4 | — | 96.85% / 2.74% / 0.41% |
| `idle_M_9p5_dim` | Pretendard-Medium | 9.5 → **28.5** | `#b8a58a` | 2.1 | — | 지나온 관문 1곳 — 전리품도 챙겨뒀네 |
| `idle_SB_28_accentGold_sh` | Pretendard-SemiBold | 28 → **84** | `#ffd87a` | 0.7 | MAT_NumShadow | 1200 |
| `idle_SB_26_accentGold_sh` | Pretendard-SemiBold | 26 → **78** | `#ffd87a` | 0.8 | MAT_NumShadow | 1200 |
| `idle_SB_18_btnP_sh` | Pretendard-SemiBold | 18 → **54** | `#2a1a08` | 1 | MAT_BtnGoldText | 받기 ※시안에서는 GmkB — Unity에서는 이 표대로 통일 |
| `idle_SB_16_dim` | Pretendard-SemiBold | 16 → **48** | `#b8a58a` | 1.2 | — | 1182 |
| `idle_SB_15_titleGold_sh` | Pretendard-SemiBold | 15 → **45** | `#f0cf82` | 6 | MAT_Shadow1 | 돌아왔군, 총잡이! |
| `idle_SB_14_text` | Pretendard-SemiBold | 14 → **42** | `#f3e8d6` | 1.4 | — | ➤ |
| `idle_SB_13_num` | Pretendard-SemiBold | 13 → **39** | `#f0dcb0` | -2 | — | 480M / 40 / 15 |
| `idle_SB_13_text` | Pretendard-SemiBold | 13 → **39** | `#f3e8d6` | 1.5 | — | 1324K / 24 / 3 |
| `idle_SB_12_text_I` | Pretendard-SemiBold | 12 → **36** | `#f3e8d6` | 1.7 | — | 현재 획득 중인 보상 |
| `idle_SB_11_up` | Pretendard-SemiBold | 11 → **33** | `#6ee05a` | 1.8 | — | +18 스테이지 돌파! |
| `idle_SB_11_dim` | Pretendard-SemiBold | 11 → **33** | `#b8a58a` | 1.8 | — | 방치 시간 / / 12시간 / 최대 누적 시간 : |
| `idle_SB_11_text` | Pretendard-SemiBold | 11 → **33** | `#f3e8d6` | 1.8 | — | 1시간 당 |
| `idle_SB_11_accentGold` | Pretendard-SemiBold | 11 → **33** | `#ffd87a` | 1.8 | — | 8시간 0분 / 12시간 0분 / 12시간 |
| `idle_SB_10p5_up_I` | Pretendard-SemiBold | 10.5 → **31.5** | `#6ee05a` | 1.9 | — | +2% |
| `idle_SB_10p5_text` | Pretendard-SemiBold | 10.5 → **31.5** | `#f3e8d6` | 1.9 | — | 현재 스테이지 |
| `idle_SB_10p5_text_I` | Pretendard-SemiBold | 10.5 → **31.5** | `#f3e8d6` | 1.9 | — | 스테이지 1201 도달 시 / 보상 |
| `idle_SB_10_dim` | Pretendard-SemiBold | 10 → **30** | `#b8a58a` | 10 | — | — 방치 중 돌파 — |
| `idle_SB_10_white_I` | Pretendard-SemiBold | 10 → **30** | `#ffffff` | 2 | — | MAX |
| `idle_SB_9p5_btnAsub` | Pretendard-SemiBold | 9.5 → **28.5** | `#c9a560` | 1.4 | — | 2배 받기 / 즉시수령 0/4 ※시안에서는 GmkB — Unity에서는 이 표대로 통일 |
| `idle_SB_9_dim` | Pretendard-SemiBold | 9 → **27** | `#b8a58a` | 2.2 | — | 골드 / 티켓 / 무기 상자 |
| `rev_GB_18_lime` | GmarketSans-Bold | 18 → **54** | `#c6f24a` | 0 | — | ▲25K / ▲0 / ▲12K |
| `rev_GB_18_text` | GmarketSans-Bold | 18 → **54** | `#f1e6d4` | 0 | — | 0 |
| `rev_GB_18_down` | GmarketSans-Bold | 18 → **54** | `#ff6a5a` | 0 | — | ▼25K |
| `rev_GB_16_btnP_sh` | GmarketSans-Bold | 16 → **48** | `#2a1a08` | 2 | MAT_BtnGoldText | 뽑기 / 장착하고 계속 |
| `rev_GB_16_btnS_sh` | GmarketSans-Bold | 16 → **48** | `#e6d9c3` | 2 | MAT_Shadow1 | 접어두기 |
| `rev_GB_16_accentGold_sh` | GmarketSans-Bold | 16 → **48** | `#ffd87a` | 2 | MAT_Shadow1 | 상자 얻기 |
| `rev_GB_14_btnP_sh` | GmarketSans-Bold | 14 → **42** | `#2a1a08` | 0 | MAT_BtnGoldText | 그래도 판매 / 그래도 장착 |
| `rev_GB_14_btnS_sh` | GmarketSans-Bold | 14 → **42** | `#e6d9c3` | 0 | MAT_Shadow1 | 돌아가기 |
| `rev_GB_13_text` | GmarketSans-Bold | 13 → **39** | `#f1e6d4` | 1.5 | — | 128 |
| `rev_GB_13_down` | GmarketSans-Bold | 13 → **39** | `#ff6a5a` | 1.5 | — | 0 |
| `rev_GB_13_yellow` | GmarketSans-Bold | 13 → **39** | `#ffd23a` | 1.5 | — | 12 |
| `rev_GB_12_white` | GmarketSans-Bold | 12 → **36** | `#ffffff` | 0 | — | 파이어브랜드 / 블리자드 / 리볼버 없음 |
| `rev_GB_11_dark` | GmarketSans-Bold | 11 → **33** | `#1a130e` | 1.8 | — | ! |
| `rev_GB_10_ice` | GmarketSans-Bold | 10 → **30** | `#39cccc` | 0 | — | 얼음 |
| `rev_GB_10_lime` | GmarketSans-Bold | 10 → **30** | `#c6f24a` | 0 | — | +2624% / +9% / +24% |
| `rev_GB_10_text` | GmarketSans-Bold | 10 → **30** | `#f1e6d4` | 0 | — | +1312% / +7% / +28% |
| `rev_GB_10_fire` | GmarketSans-Bold | 10 → **30** | `#ff4136` | 0 | — | 화염 |
| `rev_GB_10_white` | GmarketSans-Bold | 10 → **30** | `#ffffff` | 0 | — | Lv.15 |
| `rev_GB_9p5_btnPsub` | GmarketSans-Bold | 9.5 → **28.5** | `#1f5a0e` | 2.1 | — | 1개 사용 / ▲ 전투력 상승 / 자동 뽑기 재개 |
| `rev_GB_9p5_tagNewText` | GmarketSans-Bold | 9.5 → **28.5** | `#2a1a06` | 0 | — | NEW |
| `rev_GB_9p5_dim` | GmarketSans-Bold | 9.5 → **28.5** | `#9d8f7e` | 2.1 | — | ✕ 닫기 |
| `rev_GB_9p5_dim_98` | GmarketSans-Bold | 9.5 → **28.5** | `#a8987f` | 2.1 | — | 백그라운드에서 계속 / 획득처 보기 |
| `rev_GB_9p5_lime` | GmarketSans-Bold | 9.5 → **28.5** | `#c6f24a` | 0 | — | 장착 효과 |
| `rev_GB_9p5_text` | GmarketSans-Bold | 9.5 → **28.5** | `#f1e6d4` | 0 | — | 장착 중 |
| `rev_GB_9p5_stop` | GmarketSans-Bold | 9.5 → **28.5** | `#ff7a6a` | 2.1 | — | ✕ 자동 멈춤 |
| `rev_GB_8_up` | GmarketSans-Bold | 8 → **24** | `#53e05a` | 0 | — | ▲ / 변경 |
| `rev_GB_8_down` | GmarketSans-Bold | 8 → **24** | `#ff6a5a` | 0 | — | ▼ |
| `rev_GM_12_good` | GmarketSans-Medium | 12 → **36** | `#7fd18a` | 1.7 | — | 더 강합니다 |
| `rev_GM_12_text` | GmarketSans-Medium | 12 → **36** | `#f1e6d4` | 1.7 | — | 전투력 차이 25K — 정말 이대로 선택하시겠습니까? / 버리려는 새 리볼버가 / 새 리볼버가 |
| `rev_GM_12_bad` | GmarketSans-Medium | 12 → **36** | `#ff8a80` | 1.7 | — | 더 약합니다 |
| `rev_GM_9p8_pill` | GmarketSans-Medium | 9.8 → **29.4** | `#e6dac6` | -1 | — | = 최적 편성 기준 전투력 · 선택하면 자동으로 재편성됩니다 |
| `rev_GM_9p5_dim2` | GmarketSans-Medium | 9.5 → **28.5** | `#d8cbb6` | 0 | — | 속성 |
| `rev_GM_9p5_text` | GmarketSans-Medium | 9.5 → **28.5** | `#f1e6d4` | 0 | — | 공격력 / 크리티컬 확률 / 크리티컬 데미지 |
| `rev_GM_9_dim` | GmarketSans-Medium | 9 → **27** | `#9d8f7e` | 0 | — | 전투력 변화 |
| `rev_GM_8p5_dim` | GmarketSans-Medium | 8.5 → **25.5** | `#9d8f7e` | 2.4 | — | 보유 / 회 |
