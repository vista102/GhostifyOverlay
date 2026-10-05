# Ghostify Overlay 0.4.3 검증

2026-10-04. 판정 순서, 곡명 구분자, 색상 설정 통합과 레인 3 제거를 반영한 EXE를 제작했습니다. 실제 게임에는 설치하지 않았습니다.

2026-10-05 공개 배포 준비에서 README의 다운로드·AI 생성 코드 기록 안내와 현재 지원 배치를 정리하고 EXE/ZIP을 다시 묶었습니다. 모드 DLL과 설치 프로그램 소스는 변경하지 않았으며, 아래 881개 검사 결과는 그대로 적용됩니다. 재포장한 EXE의 14개 내장 파일·ZIP·Release SHA256 및 실제 alpha SDK의 읽기 전용 확인을 다시 통과했습니다. 아래 배포 해시는 이 최종 파일 기준입니다.

## 수정 및 확인 근거

- 세부판정: 앞선 코드는 PerfectPlus를 먼저 표시해 느림이 왼쪽으로 갔습니다. 설치된 게임의 scrPlanet.SwitchChosen 입력 경로는 입력 시각에서 타일 시각을 뺀 값을 GetHitMarginInSec에 전달합니다. 실제 게임 함수에서 음수 오차는 PerfectMinus(빠름), 양수 오차는 PerfectPlus(느림)를 반환하는 것을 확인했습니다. 표시를 빠름 / X+Auto / 느림으로 연결하고 결과창도 같은 시간 순서를 따릅니다.
- 순서 검증은 Lenient / Normal / Strict, BPM 90 / 180 / 600, pitch 0.5 / 1.5, timing scale 0.5 / 1 / 2의 54가지 조합에서 수행했습니다. 각각 실제 함수가 반환한 enum에 서로 다른 수치 11 / 47을 넣고 표시의 첫 값 / 끝 값을 대조했습니다. enum 이름만으로 순서를 추정하지 않습니다. 판정 계산과 원래 tracker는 변경하지 않습니다.
- 일반 누적: 왼쪽은 FailOverload만 집계하는 과부하, 오른쪽은 FailMiss를 집계하는 놓침입니다. 두 수치가 서로 다르고 Multipress / OverPress도 별도로 있는 fixture로 위치와 집계를 확인했습니다. 각 끝 열은 해당 native 판정 색을 사용합니다.
- 레인 3: 색상 필드·설정 행·30 너비 분기·세 번째 행 생성 경로를 제거했습니다. 레인 풀은 일반 2개 / 고스트 2개 그룹만 갖고, 모든 고스트 그룹은 두 일반 레인보다 뒤에 그립니다. 과거 RainColor3 JSON은 무시하며 레인 1·2 및 고스트 색은 유지합니다.
- 곡명: 게임 캡션과 메타데이터를 번갈아 쓰며 구분자가 달라지던 경로를 정리했습니다. 커스텀 곡은 항상 메타데이터로 `작곡가 - 곡명`을 조합합니다. 기본 곡은 원래 레벨 캡션과 작곡가 크레딧을 같은 형식으로 조합합니다. 한 항목만 있으면 불필요한 구분자를 넣지 않습니다. 중앙 상단 배치·긴 이름 줄바꿈·태그 제거는 유지합니다.
- 색상 기능 제거: JudgmentColors, DetailedPlusMinusColor, DetailedXColor와 독립 ComboColor / SongInfoColor / TimingScaleColor / FpsColor를 제거했습니다. 기존 파일의 해당 값은 더 이상 적용하거나 저장하지 않습니다. 누적판정은 기본 native 색, 세부판정은 고정 녹색 / 흰색을 사용합니다.
- 색상 통합: 글자 색은 좌우 글자·콤보 이름·곡 정보·Timing Scale에 적용합니다. 수치 색은 진행도·정확도·BPM·KPS·Pitch·FPS·음악/맵 시간·콤보 숫자에 적용합니다. 시도 횟수와 진행 막대는 각각 분리합니다. 기존 TextColor / ValueColor / AttemptsColor / ProgressBarColor, Full 시도 기록은 유지합니다.
- 색상 UI: HEX 오른쪽에 32×32 색상 네모를 두고 직접 클릭해 팔레트를 엽니다. HEX와 네모 사이 간격은 8입니다. 별도 ‘팔레트’ 글자 버튼을 없앴습니다. 실제 SettingsWindow.Colors.cs를 UI API 대체 환경에서 실행해 HEX 유효/무효 입력, 즉시 갱신, 팔레트 열기·교체·적용·취소, 저장 요청과 독립 항목 연결을 확인했습니다.

## 통과한 검사

| 검사 | 통과 수 |
|---|---:|
| Release DLL 키뷰어·저장 | 75 |
| Release DLL 로직·저장·프레젠테이션 | 80 |
| Release DLL alpha 판정·결과창 | 111 |
| Release DLL 키 연동·곡 정보·타이밍 | 105 |
| Release DLL 진행도·FPS·배치·세션·팔레트 | 139 |
| Release DLL 시작 키·발키 홀드·판정 집계 | 35 |
| Release DLL 태그·독립 위치·배치 전환·카운터 색 | 48 |
| 실제 SDK 빠름/느림 연결·색상 정리·기존 설정 복구 | 85 |
| production 색상 행 UI API 대체 실행 | 11 |
| 실제 SkyHook 차단 prefix 조합 | 26 |
| production 레인 코드 UI API 대체 실행 | 22 |
| production 폰트 TMP API 대체 실행 | 10 |
| production 배치·그림자·입력 캡처 API 대체 실행 | 29 |
| 이펙트·저장·patch 복구 | 28 |
| SDK Enter 매핑 | 4 |
| 설치 엔진의 임시 폴더 설치·업데이트·복원 | 35 |
| 실제 설치 창·버튼·종료·자원 수명 | 38 |

총 **881개** 실행 검사가 통과했습니다. 판정 기준의 확인 로그는 `Backups/Feedback-0.4.3/native-signs.log`, 새 검사는 `Tests/Run-FeedbackV5Tests.ps1`, 색상 UI 실행 검사는 `Tests/Run-ColorRowTests.ps1`입니다.

추가로 patch 대상 26개, 외부에서 생성 가능한 patch 24개, 수동 후크 3개를 확인하고 모두 정리했습니다. Unity 밖의 ECall 대상 2개는 구분했습니다. 빌드 및 Git whitespace 검사를 통과했고, 원본 DonQuixote 2,336개 파일의 해시를 보존했습니다.

EXE에 포함된 14개 파일, ZIP과 Release의 SHA256이 일치합니다. EXE의 읽기 전용 검증으로 설치된 3.4.0 alpha SDK를 확인했습니다. 최종 검사 시 게임이 실행 중이 아니어서 배포 EXE의 실행 중 설치 차단 fixture 검사는 생략했으며, 설치 엔진의 실행 중 차단 테스트는 통과했습니다. 실제 게임 설치는 생략했습니다. 설치된 DLL / Info.json / GameCompatibility.json 및 UMM Params.xml의 검사 전후 해시는 동일합니다.

실제 Unity 인게임 화면의 육안 확인과 물리 키로 플레이하는 검증은 하지 않았습니다. UI·레인 검사는 production 소스와 API 대체 환경, 판정 연결·설정 검사는 실제 모드 DLL 및 설치된 게임 SDK 함수로 검증했습니다.

## 배포 파일

- EXE: `dist/GhostifyOverlay-Setup-0.4.3.exe`, 5,143,040 bytes
- EXE SHA256: `93C75EFC3D6AFC6386ABA37B8F910B9017947DE226CFD141B486DD53A2D32F9E`
- ZIP SHA256: `58B9B5687CDC410ED9D3A7772108643EB04BCB95B7FDACE6664BB844382DE468`
- DLL SHA256: `5998A13A575126B63D88AF212C53F03F2C4B533F5425D2356A02205AE1FBD9F4`
