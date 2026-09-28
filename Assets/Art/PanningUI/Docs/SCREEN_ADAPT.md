# 화면 대응 A단계

2026-09-28 · Unity 6000.0.67f1 · 네임스페이스 `PanningUI`

팝업 24개의 화면 기반을 이관했다. 기존 설계 400×867의 좌표·크기·폰트·PNG를 유지한다. `ScreenMainHome` 검수안과 개발자 홈/전투 씬은 B단계 대상으로 그대로 두었다.

## 계층

```text
Canvas — Screen Space Overlay, Scale With Screen Size, 1200×2601, Expand
├ FullBleed — 화면 전체 stretch, 안전영역 적용 안 함
│ ├ Dim90 — 기존 검정 230/255, 화면 전체 입력 차단
│ ├ PillarFill — Left / Right / LeftHairline / RightHairline
│ └ ReceivedLayerDim 또는 WarningLayerDim — 해당 3개 팝업만
└ SafeArea — SafeAreaFitter
  └ Column — ColumnLayout, 폭 1200, 중앙, 안전영역 높이 stretch
    ├ Top — 위 앵커, 높이 0인 장착 지점
    ├ Middle — 전체 높이 stretch
    │ └ PopupBody — 원래 좌표를 담는 1200×2601 기준 공간
    └ Bottom — 아래 앵커, 높이 0인 장착 지점
      └ Close — T01-a, (534,177), 132×138, pivot (0,1)
```

`PopupBody`는 기존 디자인 공간을 가운데 고정한다. 내부 패널의 고유 기준 위치까지 화면 정중앙으로 옮기는 재디자인은 하지 않았다. 긴 화면의 추가 높이는 위아래에 반씩 분배된다. 안전영역 높이가 2601보다 짧으면 부모의 y만 `max(0,(2601-safeHeight)/2)`만큼 올려 하단 받기 버튼과 닫기 탭의 원래 간격을 유지한다. iPhone 12 노치/홈 인디케이터 테스트에서 이 제한의 필요성을 확인했다. 기존 위젯의 설계 좌표는 바뀌지 않는다.

닫기 탭 윗선은 안전영역 하단에서 177 단위다. `2601-2424=177`로 기준 y2424를 아래 앵커로 환산했다. 개발자 내비의 **윗선**도 같은 Bottom 기준 177에 맞춘다. 팝업 Preview에는 개발자 내비를 추가하지 않았으므로 검증은 이 공유 기준선에 대한 수치 검증이다.

기둥 채움은 `#0A0908`, 경계선은 rgba(214,172,96,.55), 3단위 폭 두 개다. 모든 기둥 Image의 raycastTarget은 false다. Canvas 폭이 1200보다 넓을 때만 보인다. Column의 RectMask2D도 이때만 켜서 기존 방치 그림의 양옆 크롭을 유지한다. 기준 화면에서는 프레임버퍼가 이미 잘라 주므로 중복 마스크를 꺼 경계 픽셀 회귀를 피한다. ColumnLayout의 ICanvasRaycastFilter는 모든 비율에서 Column 밖 클릭을 차단한다.

우편 수령/판매 경고/장착 경고의 추가 딤은 FullBleed의 독립 Canvas(기본 sortingOrder+1), 해당 내용은 Column 내부 Canvas(+2)로 분리했다. FullBleedOverlay가 원래 overlay의 활성/닫기 상태와 딤을 동기화한다. 이 순서로 닫기 탭 위에 덮이던 기존 딤도 그대로 유지한다. Dim90과 추가 딤의 색·투명도는 기존과 같다.

## 런타임 컴포넌트 사용

- `SafeAreaFitter`: 전체 화면 Canvas 바로 아래의 RectTransform에 부착한다. width/height/safeArea/orientation을 매 프레임 비교하고 변경 시 정규화된 anchorMin/Max, offset 0을 적용한다. 0 크기 과도 프레임은 무시한다.
- `ScreenMetrics.Changed`: 모든 활성 SafeAreaFitter가 공유하는 static event 하나다. 값이 동일하면 발행하지 않는다. ScreenMetricsSnapshot은 값 형식이다. Poll 10,000회에 관리 힙 할당 0바이트, 안정 상태 이벤트 0회 확인.
- `ColumnLayout`: SafeArea 자식에 부착한다. 폭 1200, 가로 중앙, 세로 stretch를 유지한다. LateUpdate에서 실제 Canvas 크기를 확인하고 기둥·마스크·팝업 부모 위치를 갱신한다. 팝업 외 개발자 씬은 PopupBody 없이 사용 가능하다.
- `PillarFill`: FullBleed 아래에 부착하고 단색 Image 네 개를 Configure 또는 Inspector로 연결한다. ColumnLayout의 pillarFill에 연결한다. 이미지 파일은 필요 없다.
- `FullBleedOverlay`: 두 번째 모달 계층이 있는 3개 팝업의 보조 컴포넌트다. 딤 GameObject를 연결한다.

B단계의 전투 카메라는 다음과 같이 이벤트를 구독하고 해제한다. 화면 변화 알림 후 CanvasScaler 반영을 기다려 자신의 LateUpdate에서 카메라를 맞춘다. 초기 활성화 시에도 자신의 레이아웃 계산을 한 번 요청한다.

```csharp
void OnEnable() { ScreenMetrics.Changed += OnScreenChanged; cameraDirty = true; }
void OnDisable() { ScreenMetrics.Changed -= OnScreenChanged; }
void OnScreenChanged(ScreenMetricsSnapshot state) { cameraDirty = true; }
// LateUpdate: cameraDirty일 때 핵심 전투 사각형을 포함하도록 카메라를 계산.
```

이 이벤트는 활성 SafeAreaFitter가 있을 때 폴링된다. 팝업만 있다가 모두 닫힌 상황까지 카메라가 구독하려면, 홈의 상시 Canvas에 SafeAreaFitter를 두어야 한다. 이 단계에서 카메라 코드는 추가하지 않았다.

## 개발자 홈·전투에 적용하는 방법 — 아직 적용하지 않음

1. 해당 씬의 UI Canvas를 Expand 1200×2601로 맞춘다. 기존 전투 월드/카메라의 투영 설정은 별도로 보존한다.
2. 위 FullBleed와 SafeArea/Column/Top/Middle/Bottom을 만든다. 배경과 딤, 화면 전체 연출은 FullBleed로 옮긴다.
3. HUD를 Top, 조작대·내비를 Bottom으로 옮긴다. 내비 윗선은 안전영역 하단+177이다. 버튼/문구는 Column 아래에만 둔다.
4. 전투 틀은 Middle stretch, 캐릭터·적의 핵심 사각형은 B단계 카메라에서 별도로 보장한다. 두 열 배치나 배경 아트 확장은 이번 범위가 아니다.
5. 게임 내 실제 팝업 manager의 정렬값과 추가 모달 Canvas(+1/+2) 순서를 검토한다. 런타임에 부모 Canvas sortingOrder를 바꾸는 manager라면 두 추가 Canvas의 값도 함께 갱신한다.
6. 상시 SafeAreaFitter에서 ScreenMetrics를 공급하고 전투 카메라에서 구독한다. 노치, 분할창, 접기/펴기의 진행 상태·입력·카메라를 실제 기기에서 확인한다.

## Player Settings — 실제 조회한 변경 전후

설계서의 추정값 대신 Unity API와 ProjectSettings.asset에서 확인한 값이다. 실제 디스크 변경은 아래 두 필드뿐이다.

| 항목 | 변경 전 | 변경 후 |
|---|---|---|
| Default Orientation | AutoRotation (4) | Portrait (0) |
| Android Resizable Window / resizeableActivity | true | true 유지 |
| Android Render Outside Safe Area | true | true 유지 |
| Supported Aspect Ratio | Native (1) | Native (1) 유지 |
| Custom 최대 비율의 저장값 | 2.4 | 2.6 (Native에서는 비활성, Custom 전환 시 대비) |
| Android application entry | GameActivity | 유지 |
| Android appCategory | game (useAndroidAppCategory=1, category=3) | game 유지 |
| iOS Requires Full Screen | true | true 유지, 레이아웃은 이에 의존하지 않음 |
| 데스크톱 resizableWindow | false | false 유지 — Android 항목과 다름 |
| runInBackground | false | false 유지 — 검증 중 런타임 값만 일시 변경 |

주의: Unity의 Android.maxAspectRatio setter는 모드를 Custom으로 바꾸므로 최대값 변경 후 Native를 다시 지정했다. 최종 디스크 diff는 defaultScreenOrientation 4→0, androidMaxAspectRatio 2.4→2.6만 있다. 자동 회전 허용 플래그는 보존됐고 Portrait 모드에서는 사용하지 않는다.

### 매니페스트 확인

Unity 6의 [PlayerSettings.Android.appCategory](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PlayerSettings.Android-appCategory.html)를 사용하므로 Custom Main Manifest는 추가하지 않았다. 현재 appCategory는 이미 game이다.

설치된 `Unity.Android.Gradle.dll`의 AndroidManifestUtils.AndroidConfigChanges를 읽고, `AndroidPlayerBuildProgram.exe`의 GenerateManifests.PatchLibraryManifest 구현을 확인했다. 기본 목록에 orientation, screenLayout, screenSize, smallestScreenSize가 있고, 실제 target SDK >23이면 density를 추가한다. 프로젝트 target은 Automatic이며 설치 SDK는 34/35/36이어서 요구한 다섯 플래그가 생성 경로에 포함된다. 근거는 `ScreenMatrix/android-manifest-evidence.json`이다. APK/Gradle export를 새로 빌드하거나 Android 실기기에서 activity 재시작 여부까지 실행한 결과는 아니다.

개발자 공유: 이번 저장소 설정 변경은 세로 고정과 비활성 Custom 최대 비율 2.6뿐이다. Android의 Native/Resizable/Render Outside Safe Area/game 카테고리는 이미 올바르게 설정돼 있어 유지했다. 커스텀 매니페스트 파일은 없으며 Unity 기본 GameActivity 생성기를 사용한다. 개발자가 이후 매니페스트나 빌드 설정을 바꾸면 최종 merged manifest의 appCategory=game 및 다섯 configChanges를 재확인해야 한다. iOS 전체 화면 강제에 의존하지 않고 SafeArea/Column을 홈·전투에도 적용하는 작업은 B단계다.

## 빌더와 검증

- 기존 11개 Build … Preview 메뉴를 재실행했다. 각 C# 빌더는 저장 직전에 ScreenAdaptBuilder.Apply를 호출하므로 원래 숫자를 유지한 채 새 계층으로 저장한다.
- `Build Screen Adapt Previews`는 나머지 Python 제작 팝업도 멱등 이관하고 자체 Preview 씬만 재저장한다. `Build All Close Tab Previews`도 Bottom 환산을 이해한다. 미저장 씬은 보존/거부하며 임의 폐기하지 않는다.
- Python `Editor/Tools~/ui_batch.py`는 이관된 PopupRevolverCompare의 구조와 컴포넌트 연결을 템플릿으로 쓴다. Home은 이전 구조를 유지한다. 보조 모달 구조는 PopupMailboxReceived에서 읽는다. 신규 생성 probe 2개에서 로컬 참조 누락 0을 확인 후 probe는 삭제했다.
- Docs/ui-batch-manifest.json의 원본 rect 숫자는 그대로다. layoutParent와 screenAdapt 메타데이터로 부모와 해석을 명시했다.
- UIBatchPreviewWindow의 Canvas 검사는 팝업에 Expand를 요구한다. 아직 이관하지 않은 Home만 기존 Match 규칙을 유지한다.
- File audit: `python Assets/Art/PanningUI/Editor/Tools~/audit_ui_batch.py`. 이번 보호 기준은 Library/ScreenAdapt/start-hashes.json이며, 검증 결과는 Docs/Validation/ui-batch-file-audit.json이다.
- 캡처 유틸리티: UI 전용 Preview만 열린 Play 모드에서 `PanningUI.Editor.ScreenMatrixCapture.Start(false)`를 실행한다. Display 8을 임시로 사용하고 원래 Game View 크기/Display·active scene을 복구한다. 기존 화면을 숨기거나 게임 씬을 저장하지 않는다. Start(true)는 변경 전, Start(true,true)는 변경 후 기준 24개 캡처다. 캡처는 실제 Game View 렌더 텍스처를 읽으며 빈 프레임을 거부한다.

## 결과와 한계

`ScreenMatrix/`에 4장면×11해상도 44장 + 기준 4장, `Baseline1200/`에 변경 전 팝업 24장, `Reference1200/`에 변경 후 24장과 픽셀 비교 결과가 있다. `matrix-overview.jpg`는 44장 요약이다. iPhone 12(노치/홈 인디케이터)와 Pixel 5(펀치홀)의 Device Simulator 캡처도 추가했다. 게임 프레임만 읽었으므로 기기 외형/노치 그림을 합성하지 않았다.

각 해상도 판정 표와 상세 수치는 ScreenMatrix/RESULTS.md 및 JSON을 참조한다. 모든 버튼의 Column 계층과 클릭 제한, 실제 TMP glyph 경계, 균일 배율, 닫기 탭 기준선, 전폭 딤, 기둥 표시 여부를 검사했다. 기준 1200×2601은 24개 전부 픽셀 차이 0이다. 기존에 승인된 방치 그림의 양옆 크롭은 회귀나 새 잘림으로 세지 않았다.

동일 인스턴스를 유지한 iPhone12→Pixel5→904×2316→2448×1848→1200×2601 전환에서 버튼 비활성 상태가 유지됐다. 실제 Android 프로세스 수명·iPad 창 관리·기기 DPI별 물리적 터치 크기는 기기 QA 대상이다. 16:9의 닫기 탭은 설계 폭 기준 정규화 배율 약 82.01%, 44×46→36.09×37.73이지만, 이것을 모든 기기의 실측 pt로 주장하지 않는다.

PNG/Sprites와 개발자 씬·게임 코드·PlayFab SDK는 수정하지 않았다. 기존 빌더 재실행에 따른 폰트 4개/머티리얼 2개의 Unity 직렬화 갱신은 파일 목록에 별도 표시했고 24개 기준 렌더 회귀는 없다. 커밋/푸시 없음.


## 후속: 자체 홈 대응 및 사용자 에셋 정정 (2026-09-28)

`ScreenMainHome` 자체 검수본도 Expand 1200×2601로 이관했다. Top/TopContent와 Bottom/BottomContent는 각각 위/아래 고정 1200×2601 좌표 공간이다. HUD 끝 y324와 하단 조작부 시작 y663 사이를 Middle로 두어 추가 세로를 흡수한다. 배경은 FullBleed이다. 개발자 홈/전투 씬과 카메라는 이관하지 않았다.

순수 계층 이관은 이전 홈 캡처와 픽셀 차이0이었다. 이후 사용자 요청으로 네 가지 제공 에셋과 가운데 빈 7칸 내비를 적용하여 홈 외형은 의도적으로 변경됐다. 팝업24종은 여전히 픽셀 차이0이다. 홈은 전체 완성 디자인이 아니라 네 가지 에셋의 부분 적용 검수본이다. `DEVELOPER_HANDOFF.md`의 최신 범위와 `home-provided-assets.json`의 출처를 참고한다.

`HomeScreenAdaptBuilder` / `HomeNavigationBuilder` / `HomeProvidedAssetsBuilder`가 멱등 이관한다. 파일 전용 Python 생성기도 같은 규칙을 반영한다. 홈도 Canvas 검사는 Expand이다. 자체 Preview의 내비 상단은 Bottom+177이며 중앙은 비상호작용 빈 Rect다. HomeScreenAudit가 안전영역·기둥·균일 배율·내비·중앙 빈칸·제공 에셋 사용과 제외된 슬롯 PNG 미사용을 검사한다.

최신 홈 해상도 증거는 `ScreenMatrix/Home/`, 최신 사용자용 화면 PNG는 `Delivery/`다. 고해상도/ZIP과 이전 캡처 이미지들은 로컬 제공본이며 Git에는 최신 기준 PNG와 목록 PNG를 포함한다.
