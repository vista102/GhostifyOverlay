# 빌드와 검사

Windows .NET Framework 4.8, Visual Studio 2022 Build Tools, 로컬 얼불춤 SDK가 필요합니다. 지원 SDK의 식별 정보는 루트 `GameCompatibility.json`에 있습니다.

UMM 참조 기준은 **0.32.5**입니다. 빌드 스크립트는 다른 UMM 버전을 참조하면 중단합니다. 게임에 다른 버전이 설치돼 있다면 UMM 0.32.5의 `UnityModManager.dll`과 `0Harmony.dll`이 있는 폴더를 `Build.ps1 -ModManagerDir <폴더>` 또는 `Installer/Build-Setup.ps1 -ModManagerDir <폴더>`로 지정합니다. 게임의 관리자 파일을 교체할 필요는 없습니다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Build.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tests\Run-AllTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Installer\Build-Setup.ps1 -SkipModBuild
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tests\Run-AllTests.ps1 -InstallerOnly
```

모드 프로젝트는 `src/Mod/**/*.cs`, 설치기는 `Installer/Get-Sources.ps1`을 통해 `src/Installer/**/*.cs`만 컴파일합니다. 테스트 코드는 배포 어셈블리에 포함하지 않습니다.

주요 검사:

- `Run-ReplayApiTests.ps1`: 실제 선택적 TUFReplay 어댑터를 API 대역과 연결해 미설치·구형(v1) API 거부·v2 등록·모드 지연 로드, delegate 시그니처와 v2 등록 규칙, 게임 내/영상 리플레이 수명주기, `OnReplayState`의 시작 값·null·교체 전달과 세션 밖 값 무시, 캔버스 선택, 시계·typed 키/native 코드 전달과 실패·조기 취소 정리를 검사합니다. TUFReplay DLL이나 실행 중인 게임이 필요하지 않습니다.
- `Run-KeyViewerReplayTests.ps1`: production 입력·리플레이 코드를 inert 런타임에서 실행해 일반/고스트/native 키 전달, 같은 프레임의 눌림·해제, seek의 단독 해제, 64비트 기록 복사와 재생, 시작 값 도착 전 현재 값 표시와 값 교체(중복 가산 없음·짧은 배열 정리·null), 저장 격리, 실제 입력 차단과 종료 후 복구를 검사합니다. 실제 Unity 캡처 화면은 별도 확인 항목입니다.
- `Run-FeatureIntegrationTests.ps1`: 실제 입력 패치·곡 정보 가시성 어댑터·행간·설정 콜백을 통제된 런타임과 Harmony에 연결해 채터링·고스트 레인·외부 모드 공존을 검사합니다.
- `Run-ModManagerCompatibilityTests.ps1`: 실제 UMM 0.32.5에서 배포 DLL의 관리자·Harmony 참조, 진입점, 모든 UMM 멤버 참조를 해석합니다.
- `Run-FactoryDefaultsTests.ps1`: 승인된 설정 자료와 새 설치·초기화 값을 대조하고, 기존 설정·키 지정·레이아웃·어템·키 입력 횟수가 보존되는지 검사합니다. 자료에는 개인 누적 기록이 포함되지 않습니다.
- `Run-ArchitectureTests.ps1`: 실제 서비스와 파일 저장 구현을 사용해 복구·원자 저장·동시 저장·시작 실패·종료 실패·지연 저장·재시도 계약을 검사합니다. Application 서비스를 Unity·Harmony·WinForms 없이 컴파일합니다.
- `Run-StandaloneAlphaTests.ps1`, `Run-PatchTests.ps1`: 실제 게임 SDK의 입력 서명, 독립형 설정창과 Harmony 대상을 검사합니다.
- `Run-LogicTests.ps1`, `Run-KeyViewerTests.ps1`, `Run-AlphaTests.ps1`: 저장 형식·배치·입력·누적·판정·문자열 표시 회귀를 검사합니다.
- `Run-RepositoryTests.ps1`: 소스 폴더, 프로젝트 경계, 문서 링크를 확인합니다.
- `Installer/Run-InstallerTests.ps1`, `Run-InstallerUiTests.ps1`, `Verify-Shipping.ps1`: 임시 게임 폴더의 설치·업데이트·실패 복구·사용자 파일 보존, 실제 WinForms UI, 내장 ZIP 무결성을 검사합니다.

설치 엔진 검사는 실제 사용자 게임 폴더에 쓰지 않습니다. 실제 게임이 실행 중이면 배포 EXE의 임시 폴더 설치 검사를 생략하며, 프로세스 감지를 대체한 엔진 검사는 계속 실행할 수 있습니다. 실제 인게임 플레이와 UAC 클릭은 별도 확인 항목입니다.

산출물은 `bin/Release/GhostifyOverlay/`, `dist/GhostifyOverlay-Setup-<버전>.exe`, `dist/GhostifyOverlay-<버전>.zip`, `dist/SHA256SUMS.txt`입니다.
- `Run-KeyPresetTests.ps1`: 실제 프리셋 서비스·저장·어댑터에서 이름과 키 설정 왕복, 모든 배치 조합, 기존 설정 보존, 가져오기 검증, 저장 실패·복구·되돌리기를 검사합니다.
- `Run-OverlayGroupUiTests.ps1`: 실제 설정창 전체 빌더·콜백을 inert uGUI API에서 실행해 분류 탐색, 스크롤 기억, 프리셋 대화상자, 키 이름, 입력 연동, 편집 복귀를 검사합니다. 생성한 JSON 배치는 `Render-SettingsPreview.ps1 -InputDirectory <검사 폴더>`로 참고 이미지를 만들 수 있습니다. Unity 렌더링 캡처는 아닙니다.
- `Run-AttemptTrackerTests.ps1`: production 어템·플레이 데이터 해시·게임 API 어댑터에서 파일 이동/수정 구분, 주요 이벤트, 공식·구형 맵, 기존 경로 기록 이전, 캐시·로딩·오류 복구와 기존 집계 규칙을 검사합니다. 카운트다운 이후 실행 상태 캡처, 네 가지 시도 횟수·시작 지점·최고 진행도의 재생/교체/복구, 상태가 없는 옛 기록, 유한한 진행도, 리플레이·오프라인 렌더의 저장 차단도 검사합니다.

### macOS 관리 코드 검사

로컬 .NET SDK의 Roslyn, Mono의 .NET Framework 4.8 참조, 설치된 게임의 DLL만 사용해 production 전체 컴파일과 관리 코드 하네스를 실행할 수 있습니다. `Tests/Run-ManagedTests.py`는 산출물을 저장소의 `bin/managed-checks/`에 만들며 게임을 실행하거나 Mods 폴더에 설치하지 않습니다.

```sh
python3 Tests/Run-ManagedTests.py \
  --managed "$HOME/Library/Application Support/Steam/steamapps/common/A Dance of Fire and Ice/ADanceOfFireAndIce.app/Contents/Resources/Data/Managed" \
  --compiler "$HOME/.dotnet/sdk/10.0.203/Roslyn/bincore/csc.dll"
```

SDK 버전에 맞게 `--compiler`를 지정하고, 필요하면 `--framework`, `--dotnet`, `--mono`도 지정합니다. 게임 DLL은 읽기 전용 참조로 쓰고, 실행 시에는 Unity의 BCL 대신 Mono의 BCL을 사용합니다. 이 검사는 Windows PowerShell 전체 빌드·패키징이나 실제 게임에서의 시각적 결과를 대신하지 않습니다.

### 실행 상태 캡처 시점

`Start_Rewind` postfix가 `AttemptTracker.PrepareRun`을 호출하고, 다음 프레임 `OverlayRuntime.Update`에서 `BeginRun`이 시도 횟수를 증가시킵니다. `scrConductor.StartMusicCo`는 yield 이후 카운트다운·체크포인트로 전환하므로 TUFReplay의 `CaptureRunState`는 정상적으로 증가한 뒤 화면에 표시하던 값을 읽습니다. 캡처는 메모리 값과 36개 키 슬롯만 복사하며, 지연된 레벨 해시 계산이나 저장을 강제로 실행하지 않습니다. 레벨 식별 대기나 집계 제외 상태라면 그때 표시 중인 값 그대로 캡처합니다.

리플레이에서 `OnReplayBegin(bool)`은 모드 전환만 합니다. 저장된 값은 바로 다음 `OnReplayState(시작 값, 0)`으로 받고, 그 전에는 저장된 상태가 없는 기록처럼 현재 값을 표시합니다. 시작 값이 없거나 읽을 수 없는 기록에는 null이 오며 ReplayApi v1의 null과 같이 현재 값을 표시합니다. 타임라인을 이동하면 TUFReplay가 기록을 처음부터 다시 적용하므로 시작 값이 `OnReplayState`로 다시 옵니다. 이때 이전 값을 교체하며 카운터에 더하지 않고, 이동한 지점 전의 키는 다시 재생되지 않습니다. Ghostify는 플레이 중 이벤트(`AddEvent`)를 기록하지 않습니다.

상태 DTO는 `Application/Replays/ReplayRunState.cs`에 있으며 public 필드만 저장합니다. 필드 이름 변경이나 숫자 타입 축소에는 이전 모양을 유지한 타입과 provider의 `UpgradeRunState`가 필요하며, `OnReplayState`의 값 타입은 `CaptureRunState`의 반환 타입과 같아야 합니다. TUFReplay DLL 참조·SDK 복사·NuGet 패키지는 추가하지 않습니다.
