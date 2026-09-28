# Unity MCP 설치 — 2026-09-28

- 사용자 요청으로 MCP for Unity 10.2.0을 설치했다. Unity 패키지는 Git 태그 v10.2.0, Python 서버도 10.2.0으로 고정했다.
- 프로젝트: D:/Dev/PanningMaster, Unity 6000.0.67f1.
- 로컬 stdio 방식. Unity 브리지는 127.0.0.1에만 열리고 Codex가 필요한 서버 프로세스를 시작한다.
- uv 0.12.19를 사용자 범위로 설치했다. 관리자 권한/계정 로그인은 필요 없었다.
- Codex 설정의 unityMCP는 패키지 설정 도구가 등록했다. 실제 등록된 명령으로 MCP 초기화, 도구 목록, 프로젝트 정보, 에디터 상태, 열린 씬 목록, Console 읽기에 성공했다.
- 검증 때 오류 0건, 기존 PlayFab SingletonMonoBehaviour.cs:24의 CS0618 경고 1건. SDK는 수정하지 않았다.
- 미저장 RevolverFirstDrawPreview / RevolverEquippedPreview 씬을 저장하거나 폐기하지 않았다. UI 프리팹 수정도 하지 않았다.
- 기존 대화의 기본 도구 목록에는 아직 unityMCP가 나타나지 않았다. 사용자 스크린샷에서 플러그인 → MCP의 unityMCP 활성화는 확인했지만 상세 설정에 다시 시작 버튼은 없었다. 이전의 해당 버튼 안내는 잘못됐으므로 반복하지 않는다. Codex 앱을 완전히 종료 후 다시 열도록 안내했으며, 실제 재실행 완료 여부는 아직 확인되지 않았다. Unity는 켜둬도 된다. 다음 세션에서 도구 노출과 실제 호출 성공을 따로 확인한다.
- 재연결: Unity의 Window/MCP for Unity에서 stdio 브리지를 시작하거나 Tools/Panning UI/Connect Unity MCP 메뉴를 사용한다. 설치용 한 번 실행 요청은 처리 후 제거되며 자동 씬 저장 기능은 없다.
- 패키지 변경 전 파일 백업과 검증용 스크립트는 Library/UnityMCPSetup에 있다. Library는 임시 캐시이므로 재생성될 수 있다.
- MCP 사용 시 먼저 인스턴스 PanningMaster@98de0df9 및 프로젝트 경로를 확인한다. 기존 작업 범위/원격 검수 제한/커밋 금지 규칙은 그대로다.

상세 검증: unity-mcp-installation.json.

세션 인수인계는 프로젝트 루트 `NEXT_SESSION.md`와 `AGENTS.md`를 따른다. 1세션 1작업 완료 후 인수인계 문서와 다음 시작 프롬프트를 갱신한다. native 도구가 아직 없다면 기존 `Library/UnityMCPSetup/probe.py`로 실제 MCP 프로토콜 연결을 확인할 수 있다. Library는 재생성 가능한 임시 폴더이므로 파일 존재 여부부터 확인한다.
