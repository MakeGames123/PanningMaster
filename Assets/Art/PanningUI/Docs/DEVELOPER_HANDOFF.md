# 개발자 인수인계 — PanningUI (2026-09-28)

## 먼저 여는 곳

1. `codex/panning-ui-screen-adapt` 브랜치를 받아 Unity **6000.0.67f1**로 프로젝트를 엽니다. 개발자 작업 브랜치에 병합하기 전에 변경 목록을 검토해 주세요.
2. **Tools → Panning UI → 00 Open UI Review**를 엽니다. 목록의 25개 화면 중 하나를 선택하고 **Open selected preview**를 누릅니다.
3. Game View는 자동으로 **Display 7, 1200×2601**이 됩니다. 일반 Game Display 1에서는 Preview가 보이지 않는 것이 정상입니다.
4. 다른 저장된 PanningUI Preview는 닫아 동시에 여러 화면이 겹치지 않게 했습니다. 미저장 씬은 저장/폐기하지 않고 카메라·Canvas 표시만 잠시 숨깁니다. 창을 닫으면 숨겼던 상태가 복구됩니다. 개발자 씬은 닫거나 저장하지 않습니다.
5. Unity를 열기 전에는 `Docs/Delivery/index.html`, `Docs/Delivery/Overview_PNG/00_전체_25종.png` 또는 `Docs/Delivery/PNG_1200/`를 보시면 됩니다.

## 완료된 범위와 미확정 범위

- 팝업 프리팹 **24종**과 자체 Preview 씬, TMP 폰트/머티리얼/스프라이트를 조립했습니다. 씬은 검수용이며 게임의 씬 전환 흐름에 추가할 필요가 없습니다.
- 홈 `ScreenMainHome`은 **부분 적용 검수본**입니다. 사용자 제공 **채팅 요약(배경+말풍선), 퀘스트, 재화 프레임, 최적 편성** 네 종류만 실제 제공 에셋을 연결했습니다.
- 홈의 슬롯/실린더/전투/프로필 등 나머지 요소는 임시 표시입니다. 사용자는 새 시안을 만들 예정이라고 했습니다. `Sprites/Inventory/ui_slot_*.png`와 `ui_badge_equipped_ko.png`는 **사용하지 않기로 했으며 현재 홈에서 참조하지 않습니다**. 파일은 이력 보존용입니다.
- 하단 내비는 7칸 중 **가운데(3번)를 비워 두는 것이 기본**입니다. 메뉴 6개는 0/1/2/4/5/6번 칸입니다. 중앙에는 Image/Button/클릭 영역이 없고, **X는 팝업이 열릴 때 팝업 소유로 표시**합니다.
- 던전 클리어/결과는 사용자가 나중에 요청할 때까지 보류입니다.
- 아이콘·무기 그림은 아직 빈 슬롯인 곳이 있습니다. 임의 대체 그림이나 이모지를 추가하지 않았습니다.
- 로컬 닫기·취소·보조 창 닫기와 닉네임 입력/저장 활성 상태만 연결했습니다. 서버 저장, 재화 차감, 보상 지급, 실제 뽑기/장착/스탯 계산은 **연결하지 않았습니다**.

## 개발자 작업 순서

1. **프리팹을 게임 UI에 연결**: 자체 Preview 씬을 제품 씬으로 바꾸지 말고, 필요한 `Prefabs/Popup*.prefab`를 기존 UI 관리자가 생성/표시하게 연결합니다. 홈 전체 프리팹은 미확정 부분이 있으므로 제품 홈을 통째로 교체하지 마세요. 확정된 네 컴포넌트와 아래 대응 구조부터 적용할 수 있습니다.
2. **표시 Display 변경**: 납품 프리팹 root Canvas의 기본값은 **Display 1(index 0), sortingOrder 100**입니다. 자체 Preview 씬에서만 Display 7(index 6)로 오버라이드합니다. 제품에서는 프리팹을 사용하고, Preview 씬에서 복사한 인스턴스라면 `targetDisplay=0`인지 확인하세요. 실제 UI manager의 `sortingOrder` 규칙도 적용하세요. 프리팹 내부의 기준 숫자와 스케일은 유지합니다.
3. **데이터/액션 바인딩**: 각 TMP 문구에 실제 데이터를 넣고, Button에 기존 게임 액션을 연결하세요. 현재 닫기 버튼은 루트 `SetActive(false)`입니다. 풀링/스택 관리자가 있으면 닫기 계약을 맞추세요. 액션 연결에 `RemoveAllListeners()`를 쓰더라도 Inspector의 persistent callback은 제거되지 않으므로, 두 번 닫기/중복 호출이 생기지 않게 확인하세요. 보상 지급과 구매는 클릭 콜백만으로 처리하지 말고 기존 서버/게임 서비스 흐름을 사용하세요.
4. **중첩 모달 정렬**: 우편 수령 및 리볼버 경고는 FullBleed의 보조 딤 Canvas(+1)와 Column 내부 내용 Canvas(+2)가 있습니다. manager가 root sortingOrder를 런타임에 바꾸면 이 두 정렬값도 함께 조정하세요. `FullBleedOverlay`가 딤의 활성 상태를 동기화합니다.
5. **홈/전투 기반 이관**: `Canvas(Expand 1200×2601) → FullBleed + SafeArea/Column(폭1200)/Top·Middle·Bottom`. HUD=Top, 조작대·내비=Bottom, 남는 높이=Middle입니다. 개발자 `Assets/Scenes`와 전투 카메라는 수정하지 않았습니다. 카메라의 핵심 전투 사각형 보장은 별도 적용이 필요합니다. `ScreenMetrics.Changed`를 구독하고 LateUpdate에서 반영할 수 있습니다.
6. **안전영역/내비 정렬**: 내비 윗선과 팝업 X 윗선은 안전영역 하단에서 177 Canvas 단위입니다(기준 y2424). 배경·딤은 FullBleed, 버튼·문구는 Column 안에 둡니다. 홈의 상시 SafeAreaFitter를 통해 팝업이 닫혀도 화면 변화 이벤트가 유지됩니다.
7. **실기기 QA**: Android 접기/펴기·분할창에서 Activity가 재시작하지 않는지, iPad 가변 창, 입력/진행 상태·터치 크기·키보드/닉네임 정책을 확인하세요. 현재 결과는 Unity Game View/Device Simulator 검증이며 APK/IPA 실행 검증은 아닙니다.

## 설정 변경 — 확인 후 병합

| 항목 | 기존 저장값 | 이번 저장값 |
|---|---|---|
| Default Orientation | Auto Rotation(4) | Portrait(0) |
| 비활성 Custom 최대 화면 비율 | 2.4 | 2.6 |
| Android Supported Aspect Ratio | Native | 유지 |
| Android Resizable / Render Outside Safe Area | 켜짐 | 유지 |
| Android appCategory | game | 유지 |
| iOS Requires Full Screen | 켜짐 | 유지(이 설정에 레이아웃을 의존하지 않음) |

추가 Custom Android Manifest는 없습니다. Unity 기본 GameActivity 생성 경로에서 screenSize/smallestScreenSize/screenLayout/orientation과 targetSDK>23의 density를 확인했습니다. 빌드 시 최종 merged manifest에도 해당 configChanges와 appCategory=game이 있는지 확인해 주세요. 자세한 근거는 `SCREEN_ADAPT.md`입니다.

## 파일 지도

| 경로 | 용도 |
|---|---|
| `Prefabs/` | 실제 재사용 UI 25개(팝업24 + 부분 홈1) |
| `Scenes/*Preview.unity` | 화면별 격리 검수. Build Settings 등록 불필요 |
| `Runtime/` | SafeArea/Column/Pillar/화면 이벤트, 자체 도형과 닉네임 로컬 피드백 |
| `Sprites/`, `Fonts/`, `Materials/` | UI가 참조하는 에셋. `.meta`와 함께 유지 |
| `Editor/UIBatchPreviewWindow.cs` | 개발자용 단일 검수 입구 |
| `Editor/*Builder.cs` | 필요한 화면만 명시적으로 재생성하는 메뉴. 전체 빌더 실행은 검수에 필요 없음 |
| `Editor/Tools~/` | 원본 좌표 기반 파일 생성/검사/PNG 묶음 Python 도구(Unity에서 컴파일하지 않음) |
| `Docs/SCREEN_ADAPT.md` | 구조/컴포넌트/설정/개발자 씬 이관 방법 |
| `Docs/Delivery/` | 최신 검수 PNG/목록/카탈로그 |
| `Docs/Validation/`, `Docs/ScreenMatrix/` | 검증 기록. 과거 캡처 바이너리는 로컬 이력, 최신 화면은 Delivery 기준 |

PNG 고해상도(2400×5202)와 ZIP은 로컬 제공본입니다. 중복 바이너리가 커지는 것을 막기 위해 Git에는 기준 PNG(1200×2601)와 목록 PNG를 포함합니다. 필요하면 UI 전용 Preview에서 Play 후 `ScreenMatrixCapture.StartExport(true)`, 완료 후 `python Assets/Art/PanningUI/Editor/Tools~/export_delivery.py`로 재생성할 수 있습니다. Python Pillow가 필요합니다.

## 의존성과 변경 범위

- Unity 6000.0.67f1 / 프로젝트 기존 uGUI·TMP·URP 환경. UI 사용에 Unity MCP나 Python 설치는 필요하지 않습니다.
- 로컬 MCP 설치용 Packages 변경과 개인 연결 스크립트는 이 커밋에서 제외합니다. PlayFab SDK·기본 TMP·개발자 코드/씬은 변경/포함하지 않습니다.
- `ProjectSettings/ProjectSettings.asset`의 위 두 필드와 `Assets/Art/PanningUI`만 제품 변경입니다. Unity `.meta` GUID를 보존합니다.
- 기존 로컬 `SampleScene`, DOTween, ShaderGraph, Packages 및 다른 사용자 변경은 이번 UI 커밋에 섞지 않았습니다.

## 개발자에게 전달할 메시지

PanningUI의 팝업24종과 화면 대응 기반을 별도 브랜치에 올렸습니다. `Tools/Panning UI/00 Open UI Review`에서 한 화면씩 보실 수 있고, `Docs/Delivery`에 최신 PNG가 있습니다. 홈은 채팅 요약·퀘스트·재화 프레임·최적 편성만 적용한 부분 검수본이며 나머지 홈 디자인과 던전은 아직 확정 대상이 아닙니다. 기본 내비 중앙은 비워뒀고 X는 팝업에만 있습니다. 제품 씬/게임 동작 연결은 아직 안 했으므로, 프리팹 기본 Display 1을 유지하고 기존 UI manager·데이터·서버 액션을 연결하고 홈/전투의 SafeArea 및 카메라 대응을 적용해 주세요. 세로 고정과 최대 비율 저장값2.6 변경도 함께 검토 부탁드립니다.
