# PanningUI — 개발자 시작점

Unity **6000.0.67f1** · `Assets/Art/PanningUI` · 2026-09-28

**먼저 [개발자 인수인계](Docs/DEVELOPER_HANDOFF.md)를 읽으세요.** Unity 메뉴 **Tools → Panning UI → 00 Open UI Review**에서 한 화면씩 확인할 수 있습니다. Preview는 Display 7, 프리팹 기본은 Display 1입니다. 저장되지 않은 씬은 보존합니다.

- **팝업24종**: 공통6, 전투력/출처2, 우편2, 방치3, 리볼버11. 자체 프리팹·Preview·폰트·머티리얼·화면 대응 기반 완료.
- **홈은 부분 적용본**: 제공된 채팅 요약·퀘스트·재화 프레임·최적 편성만 적용. 나머지 홈 요소는 임시이며 새 시안 예정. 제공 슬롯 PNG·장착중 배지는 사용하지 않음.
- **기본 내비 가운데는 빈 칸**: 7칸 중 3번을 비우고 메뉴는 0/1/2/4/5/6. X는 팝업에만 표시.
- 던전 시안은 추후 사용자 요청 때 진행.
- 게임/서버 로직 및 개발자 홈·전투 씬 연결은 미적용. 구체적인 연결 순서는 인수인계 문서에 있음.

## 보기
- [최신 PNG 갤러리](Docs/Delivery/index.html)
- [전체 목록 PNG](Docs/Delivery/Overview_PNG/00_전체_25종.png)
- [기준 PNG25장](Docs/Delivery/PNG_1200/)
- [파일/에셋 목록](Docs/Delivery/catalog.json)
- [화면 대응 구조·사용법·Player Settings 변경](Docs/SCREEN_ADAPT.md)
- [사용자 후속 정정](Docs/UNITY_SESSION_OVERRIDES.md)

## 제작·재생성
`Prefabs`는 재사용 UI, `Scenes`는 자체 검수용입니다. Preview 씬을 게임 Build Settings에 추가하지 않습니다. Runtime 컴포넌트는 개발자 씬에서도 재사용 가능합니다. 전체 빌더는 필요 없이, 바꾸는 화면의 메뉴만 사용하세요.

- `Build Adaptive Home Preview`: 홈의 Expand/안전영역/기둥/상하 앵커, 가운데 빈 내비, 제공 에셋 네 종류를 멱등 적용.
- `Build Screen Adapt Previews`: 기존 팝업 구조 이관.
- `Editor/Tools~/build_home.py`, `ui_batch.py`: 동일 구조로 파일 생성. 기존 프리팹을 자동 덮어쓰지 않음.
- `python Assets/Art/PanningUI/Editor/Tools~/audit_ui_batch.py`: 파일 참조/좌표/폰트/스프라이트 검사.
- `ScreenMatrixCapture.StartHomeMatrix()` / `StartExport(true)`: UI 전용 Preview만 열린 Play 상태에서 실제 Game View 캡처. 고해상도 ZIP 생성은 `Editor/Tools~/export_delivery.py`.

기존 `SPEC.md`, `UNITY_AGENT_GUIDE.md`, `Reference`는 원본 사양 이력입니다. Match 0.5, 이전 반사광/X/홈 설명은 최신 `UNITY_SESSION_OVERRIDES.md`, `SCREEN_ADAPT.md`, 인수인계 문서가 우선합니다. 원본 PNG나 TMP 기본 BMJUA를 변경하지 않습니다.
