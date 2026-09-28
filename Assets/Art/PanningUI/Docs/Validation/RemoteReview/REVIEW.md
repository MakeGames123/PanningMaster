# 전체 UI 원격 검수 — 2026-09-28

사용자 후속 요청에 따라 Unity MCP로 실제 PanningMaster 6000.0.67f1에 연결해 확인했다. 준비된 팝업 24종과 홈 검수안 1종은 모두 프리팹/Preview 씬에 이미 적용돼 있었다. 미적용된 확정 화면은 없어서 UI를 중복 생성하거나 재조립하지 않았다.

## 실제 확인
- 별도 Preview Play 장면에서 1200×2601 Game View 25장 캡처 완료. `overview-1.jpg`~`overview-3.jpg`와 개별 PNG를 확인했다. 빈 프레임 없음.
- 25종: 씬의 프리팹 연결, Missing Script, 명시적 TMP 폰트와 글리프 검사 통과. `editor-checks.json`.
- 24종 팝업: 딤/기둥/닫기/본문 경계 검사 통과. 각 PNG 옆 JSON에 기록했다.
- 팝업 24종은 직전 A단계 기준 캡처와 픽셀 차이 0. `results.json`.
- 닫기/취소/기능 해금 닫기/우편 보상창 닫기/경고창 돌아가기 콜백 39건, 닉네임 빈 값·기존 값·변경 값·공백 저장 상태 4건, 합계 43건 통과. `interactions.json`. 격리한 인스턴스에서 UnityEvent를 호출한 검사이며 마우스 입력·서버 저장·실제 보상 지급 검증은 아니다.
- 홈의 HUD, 실린더 6칸, 퀘스트, 인벤토리, 뽑기 조작대, 내비 표시 확인. 인벤토리 세 번째 행이 부분적으로 보이는 것은 기록된 원본 Viewport 규격이다. 아이콘·무기 그림·전투 영역은 제공 전까지 빈 슬롯이다.
- 현재 Console 오류 0 / 경고 0. 검수 도구 추가 중 발생한 C# 조건식 타입 오류 1건은 수정 후 재컴파일/실제 캡처 통과했다.
- 기존 프리팹/씬/PNG/폰트, 개발자 코드·씬·Resources·TMP, Packages, ProjectSettings의 시작 시점 해시 1,126개와 종료 시점이 일치한다. PlayFab 접근/수정 및 commit/push 없음.

## 미결 사항의 범위
- 던전 클리어/결과는 기존 사용자 지시대로 수정 시안 대기. 제작 누락으로 간주하지 않았다.
- 홈은 home-skin-r01 검수안이며 디자인 확정이 아니다. 이번 검증은 기준 해상도만이며 홈은 기존 Match 0.5 유지다. SafeArea/Column 대응은 후속 적용 대상이다.
- 개발자 홈·전투 씬 삽입과 게임 로직 연결은 아직 안 했다. 이번 요청의 적용 확인은 PanningUI 프리팹/자체 Preview 기준이다.
- 팝업 해상도별/노치 검증은 `../../ScreenMatrix/RESULTS.md`의 기존 A단계 결과가 유효하다.

## 변경 파일
- `Editor/ScreenMatrixCapture.cs`: 전체 25종 실제 Game View 캡처 메뉴 추가, Home에 팝업 전용 검사를 적용하지 않도록 분기. `Tools/Panning UI/Capture All UI Review (Play Mode)`를 UI Preview 장면만 연 Play 상태에서 실행한다. 기존 씬 저장·폐기 없음. 백그라운드 실행과 Game View 설정은 종료 때 복구한다.
- `Docs/Validation/RemoteReview/`: 실제 캡처 25장, 검수 묶음 이미지 3장, 참조·동작·회귀 증거와 이 보고서/갤러리.
- `Docs/Validation/ui-batch-file-audit.json`: 최신 파일 검사 PASS.
- `Docs/ui-batch-manifest.json`, `Docs/UNITY_SESSION_OVERRIDES.md`, `Docs/Validation/ui-batch-delivery.md`, `README.md`, 루트 `NEXT_SESSION.md`: 검수 완료 기록.
- Player Settings/매니페스트 추가 변경 없음.

## 화면별 적용 현황
| 프리팹 | 자체 Preview 씬 | 적용·렌더·참조 |
|---|---|---|
| PopupCombatPower | CombatPowerPreview.unity | PASS |
| PopupConfirm | ConfirmPreview.unity | PASS |
| PopupDrawRates | DrawRatesPreview.unity | PASS |
| PopupFeatureUnlock | FeatureUnlockPreview.unity | PASS |
| PopupIdleRewards | IdleRewardsPreview.unity | PASS |
| PopupIdleRewardsEmpty | IdleRewardsEmptyPreview.unity | PASS |
| PopupIdleRewardsFull | IdleRewardsFullPreview.unity | PASS |
| PopupInsufficientCurrency | InsufficientCurrencyPreview.unity | PASS |
| PopupMailbox | MailboxPreview.unity | PASS |
| PopupMailboxReceived | MailboxReceivedPreview.unity | PASS |
| PopupNickname | NicknamePreview.unity | PASS |
| PopupProductDetail | ProductDetailPreview.unity | PASS |
| PopupRevolverAutoDraw | RevolverAutoDrawPreview.unity | PASS |
| PopupRevolverAutoFound | RevolverAutoFoundPreview.unity | PASS |
| PopupRevolverCompare | RevolverComparePreview.unity | PASS |
| PopupRevolverCompareDown | RevolverCompareDownPreview.unity | PASS |
| PopupRevolverCompareEqual | RevolverCompareEqualPreview.unity | PASS |
| PopupRevolverDiscardWarning | RevolverDiscardWarningPreview.unity | PASS |
| PopupRevolverEmpty | RevolverEmptyPreview.unity | PASS |
| PopupRevolverEquipped | RevolverEquippedPreview.unity | PASS |
| PopupRevolverEquipWarning | RevolverEquipWarningPreview.unity | PASS |
| PopupRevolverFirstDraw | RevolverFirstDrawPreview.unity | PASS |
| PopupRevolverNoCrates | RevolverNoCratesPreview.unity | PASS |
| PopupStatSources | StatSourcesPreview.unity | PASS |
| ScreenMainHome | MainHomePreview.unity | PASS |
