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

- `Run-ModManagerCompatibilityTests.ps1`: 실제 UMM 0.32.5에서 배포 DLL의 관리자·Harmony 참조, 진입점, 모든 UMM 멤버 참조를 해석합니다.
- `Run-ArchitectureTests.ps1`: 실제 서비스와 파일 저장 구현을 사용해 복구·원자 저장·동시 저장·시작 실패·종료 실패·지연 저장·재시도 계약을 검사합니다. Application 서비스를 Unity·Harmony·WinForms 없이 컴파일합니다.
- `Run-StandaloneAlphaTests.ps1`, `Run-PatchTests.ps1`: 실제 게임 SDK의 입력 서명, 독립형 설정창과 Harmony 대상을 검사합니다.
- `Run-LogicTests.ps1`, `Run-KeyViewerTests.ps1`, `Run-AlphaTests.ps1`: 저장 형식·배치·입력·누적·판정·문자열 표시 회귀를 검사합니다.
- `Run-RepositoryTests.ps1`: 소스 폴더, 프로젝트 경계, 문서 링크를 확인합니다.
- `Installer/Run-InstallerTests.ps1`, `Run-InstallerUiTests.ps1`, `Verify-Shipping.ps1`: 임시 게임 폴더의 설치·업데이트·실패 복구·사용자 파일 보존, 실제 WinForms UI, 내장 ZIP 무결성을 검사합니다.

설치 엔진 검사는 실제 사용자 게임 폴더에 쓰지 않습니다. 실제 게임이 실행 중이면 배포 EXE의 임시 폴더 설치 검사를 생략하며, 프로세스 감지를 대체한 엔진 검사는 계속 실행할 수 있습니다. 실제 인게임 플레이와 UAC 클릭은 별도 확인 항목입니다.

산출물은 `bin/Release/GhostifyOverlay/`, `dist/GhostifyOverlay-Setup-<버전>.exe`, `dist/GhostifyOverlay-<버전>.zip`, `dist/SHA256SUMS.txt`입니다.
