# 최종 검증 — 2026-09-28

- 팝업24종 + 홈 부분 적용1종: 실제 Unity Game View 1200×2601 및 2400×5202 PNG 각25장. 빈 프레임 거부 후 이미지 확인.
- 홈: 기준+요청11해상도12케이스, Device Simulator iPhone12/Pixel5 추가2케이스, 총14 PASS. 안전영역/글리프·버튼 경계/균일 배율/기둥/내비 상단177/중앙 빈칸/제공 에셋4종/사용하지 않는 슬롯·장착중 PNG 미참조 확인.
- 팝업24종은 직전 기준과 픽셀차이0. 홈은 순수 화면 대응 이관에서 픽셀차이0 확인 후, 사용자 정정으로 네 가지 제공 에셋과 빈 중앙 내비를 의도적으로 변경했다.
- 제공 PNG는 카카오톡 원본과 SHA256 일치. 기존 Sprite 파일 전체도 작업 전 해시 보존.
- 파일 audit PASS, 홈 생성기 probe234위젯 참조누락0. 25프리팹 씬 연결/글리프/폰트/스크립트 검증 및 이전 로컬 콜백43건 PASS 기록 유지.
- UI Review 메뉴 실제 목록25개 확인. 확인→홈 순서로 전환하여 clean Preview 자동 정리 검증. 최종 열린 씬은 MainHomePreview 하나이며 dirty=false. 사용자 미저장 씬 저장/폐기 없음.
- 현재 Unity 컴파일 정상, Console 오류0/경고0. Play 종료, 임시 Simulator 씬 제거, runInBackground 런타임 설정 복구.
- 개발자 코드/씬/Resources/TMP/Packages/ProjectSettings 등 이 턴 시작 기준 보호파일747개 변경0. 기존 A단계 ProjectSettings 두 필드만 UI 전달 커밋에 포함.
- 과거 probe 씬2개는 `Library/HomeAdapt/ArchivedProbes`로 보존 후 Assets에서 정리. 오래된 CloseTabValidationPreview는 로컬 캡처 도구용으로 Git 제외.
- UI MCP 개인 연결 스크립트/Packages 변경은 전달 제외. 개발자 사용에 MCP는 필요 없다.

한계: 게임/서버 서비스 연결, 실제 APK/IPA·기기 Activity 재시작·물리 터치 크기는 개발자 단계다. 홈 전체 디자인은 미완성이며 네 가지 제공 에셋만 적용했다. 던전은 보류.
