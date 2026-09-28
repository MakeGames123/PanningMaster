# UI 제작 및 T01-a 교체 결과 — 2026-09-28

## T01-a 닫기 X

- 원본 PNG SHA256 일치, 132×138 그대로. Image Simple, Sprite Single / Full Rect / 무압축 / 밉맵 없음 / PPU 100 / Border 0.
- 왼쪽 위 기준 Unity rect [534,2424,132,138], 화면 중심 x=600. Button targetGraphic 및 전체 Rect 클릭 영역 확인.
- 23개 Close 프리팹 Unity 저장 완료. 확인 팝업은 기존 X가 없어 동일 닫기 동작의 새 탭을 추가했다.
- 활성 코드/manifest의 구형 파일명 참조 0건, 프리팹/씬의 구형 Sprite GUID 참조 0건. 구형 PNG와 meta는 원래 해시 그대로 보관했다.
- 패키지 문서 3개는 원본과 바이트 단위 일치. 문서의 교체 이력 3곳과 asset-list의 보존 에셋 기록 1행에는 구형 이름이 남아 있다.
- Unity 메뉴 `Tools/Panning UI/Build All Close Tab Previews`로 X만 갱신했다. 개별 빌더의 글꼴 재생성/씬 교체를 피하고 기존 컴포넌트 참조를 보존한다.
- Preview 씬 23개를 Unity에서 재저장했다. 미저장 상태인 RevolverEquippedPreview와 RevolverFirstDrawPreview는 저장/폐기하지 않았다. 두 씬의 디스크 파일도 동일 프리팹을 연결하므로 X 변경을 상속한다.
- 현재 Unity Console 오류 0, 경고 0 (07:31 KST). 이번 컴파일/재저장/캡처에서 PlayFab TitleId·Android 모듈 오류는 발생하지 않았다. 이전 세션 로그의 이미 제거된 반사광 코드 오류 이력과 구분한다.

### 실제 Game View 캡처 (각 1200×2601)

- `Assets/Art/PanningUI/Docs/Validation/close-t01a-mail_game_1200.png`
- `Assets/Art/PanningUI/Docs/Validation/close-t01a-confirm_game_1200.png`
- `Assets/Art/PanningUI/Docs/Validation/close-t01a-off_game_1200.png`

## 함께 준비한 나머지 UI

아래 14개 프리팹과 Preview 씬이 준비되었다. Unity 컴파일 및 파일 참조/좌표 검사는 통과했다. 전체 화면의 시각적 판별과 게임 연결은 후속 단계다.

- `Prefabs/PopupRevolverEmpty.prefab` → `Scenes/RevolverEmptyPreview.unity`
- `Prefabs/PopupRevolverAutoDraw.prefab` → `Scenes/RevolverAutoDrawPreview.unity`
- `Prefabs/PopupRevolverAutoFound.prefab` → `Scenes/RevolverAutoFoundPreview.unity`
- `Prefabs/PopupRevolverNoCrates.prefab` → `Scenes/RevolverNoCratesPreview.unity`
- `Prefabs/PopupRevolverDiscardWarning.prefab` → `Scenes/RevolverDiscardWarningPreview.unity`
- `Prefabs/PopupRevolverEquipWarning.prefab` → `Scenes/RevolverEquipWarningPreview.unity`
- `Prefabs/PopupDrawRates.prefab` → `Scenes/DrawRatesPreview.unity`
- `Prefabs/PopupFeatureUnlock.prefab` → `Scenes/FeatureUnlockPreview.unity`
- `Prefabs/PopupNickname.prefab` → `Scenes/NicknamePreview.unity`
- `Prefabs/PopupMailboxReceived.prefab` → `Scenes/MailboxReceivedPreview.unity`
- `Prefabs/PopupIdleRewards.prefab` → `Scenes/IdleRewardsPreview.unity`
- `Prefabs/PopupIdleRewardsFull.prefab` → `Scenes/IdleRewardsFullPreview.unity`
- `Prefabs/PopupIdleRewardsEmpty.prefab` → `Scenes/IdleRewardsEmptyPreview.unity`
- `Prefabs/ScreenMainHome.prefab` → `Scenes/MainHomePreview.unity`

- 홈은 이전 home-skin-r01 제안 기반이며 확정 홈 시안이 아니다. 전투 영역/아이콘/무기 그림 슬롯은 비워 두었다.
- 닉네임은 TMP_InputField와 로컬 글자 수/저장 버튼 상태까지만 구현했다. 서버 저장·재화 차감은 연결하지 않았다.
- 빛바램·반사광은 제거 상태 유지, 하락 수치 빨강/총 전투력 동률 중립 0 유지. 던전 클리어/결과는 수정 시안 대기.
- `Editor/UIBatchPreviewWindow.cs`의 Batch Preview 메뉴에서 이후 하나씩 열기/참조 검사/캡처가 가능하다. 현재 캡처 검수 씬에는 방치 보상이 표시되어 있다.
- 재생성 스크립트는 기존 프리팹을 자동 덮어쓰지 않는다. 신규 생성 순서는 `build_remaining_ui.py` → `build_home.py` → `finish_nickname.py`다. `audit_ui_batch.py`로 다시 검사할 수 있다.
- 보존 기준 파일 검사 통과: 기존 Sprite/Font/TMP Settings/원본 SampleScene 등. Unity가 자동 계산한 TMP ScaleRatio 두 파일은 기준 해시와 정확히 일치하는 원본 바이트로 복구했다.
- 커밋/푸시 없음.

## X 교체 변경 파일 목록

아래 목록은 X 교체 및 Unity 재저장 작업의 파일이다. 함께 새로 생성한 14개 화면은 위 목록과 ui-batch-manifest.json에서 확인한다.

- `Assets/Art/PanningUI/Sprites/Navigation/ui_nav_close_t01a.png`
- `Assets/Art/PanningUI/Sprites/Navigation/ui_nav_close_t01a.png.meta`
- `Assets/Art/PanningUI/Prefabs/PopupCombatPower.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupDrawRates.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupIdleRewards.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupIdleRewardsEmpty.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupIdleRewardsFull.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupInsufficientCurrency.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupMailbox.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupMailboxReceived.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupNickname.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupProductDetail.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupRevolverAutoDraw.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupRevolverAutoFound.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupRevolverCompare.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupRevolverCompareDown.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupRevolverCompareEqual.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupRevolverDiscardWarning.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupRevolverEmpty.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupRevolverEquipped.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupRevolverEquipWarning.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupRevolverFirstDraw.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupRevolverNoCrates.prefab`
- `Assets/Art/PanningUI/Prefabs/PopupStatSources.prefab`
- `Assets/Art/PanningUI/Editor/PanningBuyBuilder.cs`
- `Assets/Art/PanningUI/Editor/PanningMailBuilder.cs`
- `Assets/Art/PanningUI/Editor/PanningNeedBuilder.cs`
- `Assets/Art/PanningUI/Editor/PanningPowerBuilder.cs`
- `Assets/Art/PanningUI/Editor/PanningPowerSourceBuilder.cs`
- `Assets/Art/PanningUI/Editor/RevolverCompareBuilder.cs`
- `Assets/Art/PanningUI/Editor/RevolverCompareDownBuilder.cs`
- `Assets/Art/PanningUI/Editor/RevolverCompareEqualBuilder.cs`
- `Assets/Art/PanningUI/Editor/RevolverEquippedBuilder.cs`
- `Assets/Art/PanningUI/Editor/RevolverFirstDrawBuilder.cs`
- `Assets/Art/PanningUI/Editor/Tools~/ui_batch.py`
- `Assets/Art/PanningUI/Docs/ui-batch-manifest.json`
- `Assets/Art/PanningUI/Docs/SPEC.md`
- `Assets/Art/PanningUI/Docs/UNITY_AGENT_GUIDE.md`
- `Assets/Art/PanningUI/Docs/unity-import.csv`
- `Assets/Art/PanningUI/Docs/asset-list.csv`
- `Assets/Art/PanningUI/Prefabs/PopupConfirm.prefab`
- `Assets/Art/PanningUI/Editor/PanningConfirmBuilder.cs`
- `Assets/Art/PanningUI/Editor/CloseTabBuilder.cs`
- `Assets/Art/PanningUI/Editor/CloseTabBuilder.cs.meta`
- `Assets/Art/PanningUI/Editor/Tools~/build_revolver_first.py`
- `Assets/Art/PanningUI/Editor/Tools~/build_revolver_equipped.py`
- `Assets/Art/PanningUI/Scenes/CloseTabValidationPreview.unity`
- `Assets/Art/PanningUI/Scenes/CloseTabValidationPreview.unity.meta`
- `Assets/Art/PanningUI/Scenes/CombatPowerPreview.unity`
- `Assets/Art/PanningUI/Scenes/ConfirmPreview.unity`
- `Assets/Art/PanningUI/Scenes/DrawRatesPreview.unity`
- `Assets/Art/PanningUI/Scenes/FeatureUnlockPreview.unity`
- `Assets/Art/PanningUI/Scenes/IdleRewardsEmptyPreview.unity`
- `Assets/Art/PanningUI/Scenes/IdleRewardsFullPreview.unity`
- `Assets/Art/PanningUI/Scenes/IdleRewardsPreview.unity`
- `Assets/Art/PanningUI/Scenes/InsufficientCurrencyPreview.unity`
- `Assets/Art/PanningUI/Scenes/MailboxPreview.unity`
- `Assets/Art/PanningUI/Scenes/MailboxReceivedPreview.unity`
- `Assets/Art/PanningUI/Scenes/MainHomePreview.unity`
- `Assets/Art/PanningUI/Scenes/NicknamePreview.unity`
- `Assets/Art/PanningUI/Scenes/ProductDetailPreview.unity`
- `Assets/Art/PanningUI/Scenes/RevolverAutoDrawPreview.unity`
- `Assets/Art/PanningUI/Scenes/RevolverAutoFoundPreview.unity`
- `Assets/Art/PanningUI/Scenes/RevolverCompareDownPreview.unity`
- `Assets/Art/PanningUI/Scenes/RevolverCompareEqualPreview.unity`
- `Assets/Art/PanningUI/Scenes/RevolverComparePreview.unity`
- `Assets/Art/PanningUI/Scenes/RevolverDiscardWarningPreview.unity`
- `Assets/Art/PanningUI/Scenes/RevolverEmptyPreview.unity`
- `Assets/Art/PanningUI/Scenes/RevolverEquipWarningPreview.unity`
- `Assets/Art/PanningUI/Scenes/RevolverNoCratesPreview.unity`
- `Assets/Art/PanningUI/Scenes/StatSourcesPreview.unity`
- `Assets/Art/PanningUI/Docs/Validation/close-t01a-mail_game_1200.png`
- `Assets/Art/PanningUI/Docs/Validation/close-t01a-confirm_game_1200.png`
- `Assets/Art/PanningUI/Docs/Validation/close-t01a-off_game_1200.png`

검증 상세: `close-tab-file-audit.json`, `close-tab-editor-validation.txt`, `ui-batch-file-audit.json`.

## 후속 수정: 방치 보상 그림 비율

`PopupIdleRewards`, `PopupIdleRewardsFull`, `PopupIdleRewardsEmpty`의 RewardHero Rect 및 형제 순서만 변경했다. 크럼블 원본 촬영본의 화면 폭 대비 그림 높이와 패널 겹침을 측정해 약 24.3% 확대했다. 원본 PNG, 문구·패널·버튼·X·다른 프리팹은 그대로다. 프리팹을 연결한 Preview 씬은 변경을 상속한다.

- 배치 비교: `idle-hero-proportion-preview.png` / `.html` (HTML 렌더, Unity 캡처 아님).
- 수치/범위 검증: `idle-hero-proportion-audit.json`.
- 최신 배치 기준: `../idle-hero-layout.json`.
- 파일 검사 통과. 이번 수정에서는 원격 조작을 하지 않았다. 앞서 만든 `close-t01a-off_game_1200.png`는 수정 전 캡처다.


## 전체 UI 원격 검수 완료 (2026-09-28)

사용자 후속 요청으로 팝업24종 + 홈 검수안의 Unity 실제 캡처25장, 참조검사, 로컬 동작43건을 확인했다. 모두 자체 프리팹/Preview에 적용돼 있으며 미적용 확정 화면 없음. 홈은 아직 검수안이고 기준 해상도 검증만 완료했으며 Match0.5를 유지한다. 던전 수정 시안 대기와 개발자 씬/게임 연결 미완료 상태는 유지한다. 상세: [RemoteReview/REVIEW.md](RemoteReview/REVIEW.md).


## 최신 전달

화면 대응/홈 제공 에셋4종/가운데 빈 내비/전체 PNG 묶음은 `../DEVELOPER_HANDOFF.md`, `FINAL_VALIDATION.md`, `../Delivery/`를 참조한다. 홈의 나머지 구성은 임시이며 슬롯5색과 장착중 PNG는 사용자 요청으로 미사용이다. 새 시안이 나오기 전까지 완성 홈으로 간주하지 않는다. 최신 사용자는 작업 완료 후 원격 푸시를 승인했다.
