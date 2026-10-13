# 0.5.0 UMM 설정 이식 검증

2026-10-13. 0.4.9의 어템 수정과 기존 uGUI 설정 화면을 그대로 사용합니다. 0.4.9 EXE는 별도 파일로 보존했습니다.

## 구성

- `ModEntry.OnGUI`로 UMM 설정 영역을 확보하고 기존 Canvas를 전용 카메라의 RenderTexture로 표시합니다. 그룹·토글·팔레트·키 등록·배치 편집 콘텐츠는 기존 uGUI 코드를 재사용합니다.
- Alt+D는 UMM의 Ghostify 설정 항목을 선택합니다. 설정 아이콘에서도 같은 화면을 표시합니다. 화면 배치는 960×720 비율을 유지하며 UMM의 바깥 스크롤과 기존 내부 스크롤을 구분합니다.
- UMM 영역과 scroll clip의 물리 좌표를 텍스처 좌표로 변환하는 BaseInput을 사용합니다. 원래 EventSystem은 임시 비활성화 후 복구합니다. 전용 raycaster/camera 우선순위는 UMM의 차단 Canvas와 게임 UI보다 앞섭니다.
- 이미지 밖에서 시작한 클릭은 UI로 보내지 않고, 내부에서 시작한 드래그는 외부 해제도 전달합니다. mouse-up 누락도 해제합니다. 새 팔레트 객체는 동일한 카메라 레이어에 포함합니다.
- 팔레트·키 등록에서 소비한 Esc는 UMM의 닫기까지 전달하지 않습니다. X·배치 편집은 UMM을 닫고 기존 화면 편집으로 전환합니다.
- UMM 탭 변경·창 닫기·장면 변경·모드 해제에 입력과 카메라·텍스처를 정리합니다. 동일 크기의 텍스처를 재사용하고 크기 변경 시 이전 자원을 해제합니다. 최대 텍스처는 1920×1440입니다.
- IMGUI는 UMM 내부에 이미지를 배치하는 연결 부분에만 사용합니다. 설정 컨트롤은 uGUI/TMP입니다. UMM 자체 DLL은 변경하지 않습니다.

## 검사

| 검사 | 결과 |
|---|---|
| 실제 SDK Release 빌드 | 성공 |
| production UMM host / input / window partial, inert Unity API | 38개 통과: 크기 변경, clip, 클릭·드래그·스크롤, 카메라 픽셀 좌표, 입력 복구, Esc patch, 자원 해제, 할당 실패 |
| 실제 DLL UMM SDK 및 좌표 변환 | 15개 통과 |
| production 어템 집계 | 28개 통과, 모드 전환 시 이미 집계한 횟수 보존 |
| 그룹 설정·JSON 복구 | 172개 통과 |
| 키뷰어·저장 | 75개 통과 |
| production 그룹 UI·배치 편집 | 50개 통과 |
| 기존 로직·배치 저장 | 80개 통과, 원본 파일 2336개 지문 유지 |
| 색상 입력·팔레트 연결 | 11개 통과 |
| 키 입력 캡처·그림자·키 배치 | 32개 통과 |
| Harmony 대상 | 20개 해석. 17개 생성, 3개 Unity ECall은 게임 밖 생성 검증 불가 |
| 수동 입력 훅 | 3개 생성·소유 패치 제거 확인 |
| 설치 엔진 / 임시 폴더 | 55개 통과 |
| 이번 배포 EXE / 임시 한글 경로 게임 폴더 | 새 설치·업데이트·이전 폴더 이전·오류 처리 통과, 사용자 파일 8개 보존 |
| 내장 ZIP·독립 ZIP·Release | 파일 14개 SHA256 일치, 라이선스 원문 검사 통과 |
| 실제 게임 SDK | 읽기 전용으로 3.4.0 alpha 확인 |

실제 게임 설치와 GitHub 업로드는 수행하지 않았습니다. 실제 Unity 렌더링, 인게임 UI 시각·입력 확인은 아직 하지 않았으며 위 UI 실행 검사는 inert API에서 production 소스를 실행한 결과입니다. 3개 ECall 대상에는 UMM ToggleWindow의 Esc 보호 패치가 포함됩니다.

## 배포 지문

| 파일 | SHA256 |
|---|---|
| GhostifyOverlay-Setup-0.4.9.exe | `9d35bdc997b9fcbfcb1bb774837e393c7a37db1f33fde58a66efd9e678b37113` |
| GhostifyOverlay-Setup-0.5.0.exe | `6c083361785b42cacaa3c507997fc0d8a1b9f5a84885843b7268a6a47eeaaffa` |
| GhostifyOverlay-0.5.0.zip | `efa0c61f1eea6e387d6defad0e2bac14f190477ea6b7082d70ee882b3c3644de` |
