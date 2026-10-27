# 코드 구조

0.4.11은 0.4.10의 기능·설정 형식·UMM 식별자를 유지하면서 모드와 설치기 코드를 역할별로 정리한 버전입니다.

```text
src/
  Mod/
    Bootstrap/             UMM 진입점과 서비스 조립
    Application/           생명주기, 저장 순서·예약, 데이터 조회 계약
    Domain/Configuration/  기존 설정·레이아웃 데이터와 정규화
    Domain/Presets/        키 배치 프리셋·이름·목록 데이터와 검증
    Features/
      Attempts/            어템 집계와 세션
      Overlay/             오버레이 표시·텍스트 캐시·배치
      KeyViewer/           입력, 배치, 레인, 표시, 설정
      LayoutEditing/       화면에서 위치·크기 편집
    Integration/
      Adofai/              게임 입력·판정·곡 정보 API 어댑터
      Unity/               Unity 객체 생성·정리
    Infrastructure/        JSON 저장, 복구, 글꼴, Harmony, 진단
    Presentation/          설정창, 팔레트, 공통 uGUI 컨트롤·테마
  Installer/
    Bootstrap/             실행 인수와 시작점
    Application/           설치 트랜잭션과 설치 서비스 계약
    Domain/                패키지·게임 상태·설치 결과
    Infrastructure/        안전한 파일 처리와 권한 상승 실행
    Presentation/          설치 창, 글꼴, 버튼
Tests/                     동작·회귀·서비스 검사
Installer/                 빌드·검증 스크립트, 이미지, manifest
docs/                      개발·계획·조사·검증 기록
```

## 책임과 의존성

`Main`은 UMM 콜백과 기존 기능 접근자를 연결합니다. `ModComposition`이 구현을 조립하고, `ModLifecycle`은 `IModRuntime`·`IModPatches`를 통해 시작·실패 복구·종료를 수행합니다. Unity 객체와 Harmony 처리는 각각 어댑터가 담당합니다. 종료 중 한 단계가 실패해도 패치는 정리합니다.

`SaveCoordinator`는 `ISaveParticipant` 목록의 실행 순서, 0.35초 지연 저장, 편집 중 저장 금지, 실패 후 2초 재시도를 담당합니다. 설정·레이아웃·키 설정·입력 횟수는 조립 단계에서 등록하므로 저장 대상이 늘어도 예약 로직을 수정하지 않습니다.

설정 저장소는 `ISettingsRepository<T>`를 사용합니다. `JsonSettingsRepository<T>`는 전달받은 역직렬화·기본값 정책으로 복구하고, `IJsonStorage`에 파일 작업을 맡깁니다. `AtomicJsonStorage`는 잠금·임시 파일·원자 교체·백업을 담당하며, `ReportingJsonStorage`는 저장 상태 표시만 추가합니다. 입력 횟수의 복구는 기존처럼 지연 저장합니다.

오버레이는 `IOverlayMetricsSource`와 `IOverlayMetadataSource`로 BPM·시간·곡 정보 공급자를 받습니다. 실제 게임 조회는 `AdofaiOverlayDataSource`에 있습니다. uGUI 구성, 텍스트 갱신, 캐시는 각각의 파일에서 관리합니다. `OverlayController`의 partial 파일은 같은 화면 컴포넌트의 구성·텍스트 표시를 나눈 것입니다.

설치 창은 `IInstallationService`·`IPackageAssets`·`IInstallationRunner`를 받습니다. 권한 상승과 자식 프로세스 실행은 `ElevatedInstallationRunner`, 경로 경계·심볼릭 링크 검사·안전한 파일 복사는 `InstallerFileSystem`에 있습니다. 설치·백업·실패 복구 순서는 설치 서비스가 담당합니다.

## SOLID 적용

| 원칙 | 적용 |
|---|---|
| 단일 책임 | 설정 모델, 저장·복구, 저장 예약, 생명주기, 화면, 게임 API, 설치 프로세스를 분리 |
| 개방·폐쇄 | 저장 참가자와 데이터 공급자를 추가해 기존 조정 로직을 확장 |
| 리스코프 치환 | 파일 저장소·실행 환경·패치 구현을 테스트 대역으로 교체해 동일한 계약 검증 |
| 인터페이스 분리 | 설정 저장, 패치, 런타임, 곡 정보, 수치, 패키지 자원, 권한 상승을 작은 계약으로 구분 |
| 의존성 역전 | Application 정책은 인터페이스에 의존하며 Unity·Harmony·WinForms 없이 검사 |

## 호환성 경계

기존 `DonQuixoteOverlay` 네임스페이스, UMM ID·진입점, 공개 설정 필드, JSON 스키마와 파일명은 유지합니다. 정적 `Main`, `SettingsStore`, `LayoutStore`, `KeyViewerStore`, `JsonFileStore`는 기존 게임 패치·UI·저장 호출의 호환 진입점입니다. 실제 저장과 실행 정책은 서비스로 위임합니다.

설정 DTO는 기존 Unity 색상과 JSON converter 특성을 유지합니다. `Domain` 폴더가 런타임과 완전히 독립된 별도 어셈블리라는 뜻은 아닙니다. Application 계약·정책은 런타임 UI와 분리되어 있습니다.

게임 안의 UI는 독립형 uGUI/TMP이며 Alt+D로 엽니다. 설치 UI는 Windows Forms입니다. 외부 라이브러리 고지와 글꼴 자산은 기존 배포 경로를 유지합니다.

## 0.5.0 설정 UX와 프리셋

설정창은 두 탭의 분류 탐색과 상세 스크롤을 `SettingsWindow.Navigation`에서 관리합니다. 키 배치, 레인, 색상, 입력과 프리셋의 uGUI 구성은 각 partial 파일로 나눕니다. 화면 편집의 복귀 콜백은 사용자 저장·취소 때만 실행하며 장면 변경·비활성화에서는 실행하지 않습니다.

`KeyPresetService`는 `ISettingsRepository<KeyPresetLibrary>`에 의존해 이름·복제·덮어쓰기·삭제·적용 전 백업·되돌리기를 관리합니다. 데이터와 배열을 복사하고 저장에 성공한 뒤 상태를 교체합니다. Unity, Harmony, UI에 의존하지 않습니다. `KeyPresetStore`가 기존 설정에서 선택한 손·발 배치만 추출하거나 반영하고, 별도 JSON 목록·파일 공유·이전 저장 복구를 연결합니다. 기존 설정 및 카운터 파일과 프리셋 파일은 분리합니다.
