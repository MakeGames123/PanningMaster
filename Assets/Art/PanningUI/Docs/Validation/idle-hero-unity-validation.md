# 방치 보상 3종 — Unity 실제 렌더 검증

2026-09-28. **기술 검증 PASS, 배치 수정 불필요.** 사용자 디자인 승인과 게임 연결 검증은 별도다.

## 검증 결과

| 항목 | 크럼블 기준 | Unity 3종 공통 |
|---|---:|---:|
| 실제 그림 높이 / 화면 폭 | 724 / 1080 = 67.037% | 804.444 / 1200 = 67.037% |
| 제목 띠 상단 겹침 / 화면 폭 | 32 / 1080 = 2.963% | 35.556 / 1200 = 2.963% |
| 원본 그림 가로세로 비율 | 서로 다른 그림 | 1.5 유지, 비균등 늘림 없음 |
| 좌우 여백 | 화면 가장자리까지 그림 | 여백 없음, alpha128 경계 좌우 각각 약 59.261px 화면 밖 |
| 그림과 제목 순서 | 제목 띠 위로 그림 겹침 | Panel → Header → RewardHero → 내용/TMP |

- 기본 / 최대 누적 / 빈 보상 3장 모두 직접 확인했다. 제목과 본문이 그림에 가려지지 않는다. 양쪽 주변 그림의 일부가 잘리는 것은 현재 확대 기준에 따른 대칭 크롭이며, 기준 Rect 그대로다.
- 계산은 **Unity가 실제로 배치한 screen-space Rect와 변경하지 않은 원본 PNG의 alpha≥128 경계**를 이용했다. PNG 전체 높이 또는 약한 외곽 광택까지의 높이가 아니다. 크럼블 경계는 기존 수동 측정이다. 서로 다른 그림의 픽셀 일치나 똑같은 시각적 무게를 주장하지 않는다.
- JSON / manifest / 실제 프리팹 Rect가 일치했다. RewardHero와 패널·문구·버튼·X 모두 수정하지 않았다.

## 실제 캡처와 비교

- [기본 1200×2601](idle-hero-off_game_1200.png)
- [최대 누적 1200×2601](idle-hero-off-cap_game_1200.png)
- [빈 보상 1200×2601](idle-hero-off-empty_game_1200.png)
- [전체 화면 나란히 비교](idle-hero-unity-comparison.png)
- [제목 띠를 맞춘 부분 비교](idle-hero-unity-header-comparison.png)
- [수치·원본 해시](idle-hero-unity-validation.json), [MCP 조회 근거](idle-hero-unity-evidence.json), [보존 검사](idle-hero-preservation.json)

Unity 6000.0.67f1의 **Game View targetTexture**를 1200×2601로 읽었다. HTML 렌더나 별도 카메라 합성으로 대체하지 않았다. 비교 이미지만 Pillow로 폭을 동일하게 축소/정렬했다. 원본 캡처는 덧그리지 않았다.

## 연결·격리·보존

1. Native Unity MCP 도구 노출 확인. 최초 instances=0이며 Unity 프로세스도 없었다. 설치를 반복하지 않고 기존 Unity를 해당 프로젝트로 실행하고 기존 `UnityMcpConnectionSetup` 요청 경로로 브리지를 연결했다.
2. `PanningMaster@98de0df9`, `D:/Dev/PanningMaster`, 에디터 준비·비 Play·비 컴파일 상태를 실제 호출로 확인했다.
3. 시작 시 열린 RevolverFirstDrawPreview / RevolverEquippedPreview / CloseTabValidationPreview는 모두 dirty=false였다. 이전 인수인계의 dirty=true 두 씬 기록은 이번 현재 상태가 아니다. 이번 세션에서 사용자 씬을 저장·폐기하거나 복구 선택을 하지 않았다. 이전 세션 종료 전 미저장 내용의 행방은 이번 조회만으로 확인할 수 없다.
4. 기존 씬의 Canvas/Camera가 Display 7을 사용하고 있어, **Display 8의 추가 임시 씬**에 대상 프리팹을 하나씩 인스턴스화했다. C#은 MCP로 메모리에서 실행했으며 소스 파일을 추가하지 않았다. 임시 씬도 저장하지 않았다.
5. 기존 `PanningMailBuilder.PrepareCaptureFor`와 `Capture`를 재사용했다. 초기 빈 프레임은 실패로 취급해 같은 출력 경로에서 교체했다. Canvas 갱신과 Game View `RepaintImmediately` / `RenderView` 후 최종 이미지의 크기·비어 있지 않음·실제 내용을 확인했다. `RenderView` 인자는 렌더용 좌표이며 마우스 이벤트를 보내지 않았다.
6. 완료 후 이번 임시 씬만 닫고 기존 active scene, Game View display/size를 복원했다. 기존 3개 씬의 dirty 상태와 Canvas/Camera enabled/display가 시작과 일치했다.
7. 전후 Console 오류·경고 0건. `audit_ui_batch.py` PASS, 기존 `Library/UIBatch-baseline.json` 기준 보호 파일 변경 0건. 시작 시 별도로 기록한 1,867개 파일 SHA 비교에서도 기존 파일 변경 0건을 확인했다.

원본 PNG·프리팹·씬·JSON/manifest·기존 게임 코드·TMP 기본 폰트·패키지를 수정하지 않았다. 검증 이미지/문서와 `NEXT_SESSION.md`만 남겼다. 마우스·키보드 원격 조작, git commit/push 없음. 홈 등 다른 작업에 착수하지 않았다.
