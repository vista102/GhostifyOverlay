# Ghostify Overlay 0.4.1 검증

2026-10-04. 피드백 8개, 고스트 레인 기본색, 과부하 항목 수정을 반영한 설치 EXE를 제작했습니다. 실제 게임에는 설치하지 않았습니다.

## 반영 사항

- 곡 정보: 작곡가 위 / 곡명 아래. 빈 곡 정보를 ScnGame 등 장면 이름으로 대체하지 않고 숨김. 일부 정보만 있으면 해당 정보만 표시.
- 시작 입력: Start_Rewind에서 이전 큐·키 눌림·레인·KPS 초기화. 시작할 때 누른 키는 실제 해제 후 다음 입력부터 정상 처리. 표시·포커스 복구에서도 시작 키 눌림을 되살리지 않음. 일반 Enter와 NumEnter의 복구 상태 구분.
- 입력 필터 호환: SkyHook HookCallback의 void 관찰 prefix에서 고스트 이벤트만 별도 큐로 전달. 일반 키는 기존 KeyUpdated 필터를 따름. 고스트만으로 입력 횟수·KPS·게임 허용 키를 늘리지 않음. 필터 앞/뒤 순서, 반복 입력, 해제, 포커스, native-code 바인딩, 관찰 패치 미사용 경로 검사.
- 고스트 깊이: 같은 레인의 모든 일반 막대 뒤에 배치. 생성·재사용 시 순서 유지.
- 폰트: 설정·통계·키뷰어 모두 Gmarket Sans Medium 사용, 한글 fallback·그림자 유지. 배포에 남아 있는 Light 파일은 런타임에서 로드하지 않음.
- Timing Scale: 1 → 100%, 0.75 → 75%. NaN·무한대·음수·변환 오버플로 숨김.
- 세부 누적: native PerfectPlus / XPerfect+Auto / PerfectMinus를 + / X / - 순서로 연결. native enum 순서는 Minus / X / Plus이므로 그대로 열거하지 않음. native 판정 이름과 부호는 설치된 게임의 resources.assets에서 확인.
- 고스트 기본색: #0EB4FCFF. 신규 설정·기본색 복원에 적용하며 저장된 사용자 지정 색 유지.
- 과부하: 설정 이름 Overload. FailOverload만 집계하며 Multipress·OverPress 제외. 기본색은 native ColourSchemeHitMargin.SelectByHitMargin(FailOverload) 사용. 사용자 지정 색 유지.

KeyboardChatterBlocker의 키 제한 prefix가 허용 목록 밖의 press를 KeyUpdated 전달 전에 거부하는 구조는 [모드 원본 Patch.cs](https://github.com/fangshenghan/KeyboardChatterBlocker/blob/master/KeyboardChatterBlocker/Patch.cs)에서 확인했습니다. 다른 모드의 필터·패치·허용 목록을 변경하지 않고 관찰 경로만 추가했습니다.

## 통과한 검사

| 검사 | 통과 수 |
|---|---:|
| Release DLL 키뷰어·저장 | 77 |
| Release DLL 로직·저장·프레젠테이션 | 80 |
| Release DLL alpha 판정·결과창 | 111 |
| Release DLL 키 연동·곡 정보·타이밍 | 105 |
| Release DLL 진행도·FPS·배치·세션·팔레트 | 157 |
| Release DLL 시작 키·부호·과부하 회귀 | 25 |
| 실제 SkyHook 메서드의 입력 차단 prefix 조합 | 26 |
| production 레인 코드의 Unity API 대체 검사 | 22 |
| production 폰트 코드의 TMP API 대체 검사 | 10 |
| 배치·그림자·입력 캡처 API 대체 검사 | 28 |
| 이펙트·저장·patch 실패 복구 | 28 |
| 게임 SDK Enter 매핑 | 4 |
| 설치 엔진: 임시 폴더 설치·업데이트·보존·복원 | 35 |
| 실제 설치 창: 제목·버튼·종료·자원 수명 | 38 |

위 실행 검사는 총 746개입니다. 추가로 EXE에 포함된 14개 파일과 ZIP·Release의 SHA256 일치, 배포 EXE의 실제 3.4.0 alpha SDK 읽기 전용 확인, 실행 중 설치 차단 및 한글 경로 fixture 무변경을 확인했습니다. 새 설치·업데이트 동작은 production InstallerEngine으로 프로젝트 내 임시 폴더에서 검사했습니다. 사용자 게임이 실행 중이어서 배포 EXE의 새 설치·업데이트 실행 검사는 생략했습니다.

Release 빌드·Git diff whitespace 검사를 통과했고 원본 DonQuixote 2,336개 파일의 해시 보존을 확인했습니다. 실제 설치 버전 0.4.0의 DLL·Info.json·SDK manifest와 UMM Params.xml은 검사 전후 동일합니다.

Harmony 타깃 metadata 26개, 일반 패치 생성·제거 24개, 수동 hook 3개를 확인했습니다. MenuBlockAsyncKeyboardPatch·NativeJudgmentColorPatch 2개는 standalone CLR의 Unity ECall 제한 때문에 offline 생성이 미검증입니다. 새 RawGhostInputPatch는 실제 SkyHook 메서드에서 차단 prefix와 함께 실행·제거까지 검사했습니다. SkyHook mapper 검사는 pwsh, 기존 게임 Harmony 검사는 .NET Framework에서 실행했습니다.

## 실제 게임에서 확인할 범위

이번 버전의 실제 Unity 렌더링과 KeyboardChatterBlocker 모드 자체를 함께 켠 플레이는 미검증입니다. 호환 검사는 원본의 이벤트 차단 동작을 실제 SkyHook 메서드에 적용한 시뮬레이션입니다. 설치 후 Space 시작·해제·재시작, 필터로 차단되는 고스트 입력, 겹친 레인 깊이, 빈 곡 정보, 폰트·백분율·부호 순서, 과부하 표시를 확인해야 합니다.

## 배포 파일

- EXE: dist/GhostifyOverlay-Setup-0.4.1.exe (5,137,920 bytes)
- EXE SHA256: F57D444F368C97D4692CE7F600DF7D15C0B224F917FF23924FD81CC3C6856299
- ZIP SHA256: 3124EAEA7232B4087419D30B76B9DA50267280440C3A241859763C4BA0B204D4
- 최종 검사 로그: Backups/Feedback-0.4.1 (로컬 전용)
