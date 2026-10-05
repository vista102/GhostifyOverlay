# Ghostify Overlay 최신 알파 대응 계획

조사일: 2026-10-03 (한국 시간). 기준 배포물: 0.2.10. 설치된 3.4.0 alpha의 DLL·Steam manifest·실행 로그를 직접 확인해 초안을 수정했다.

## 0.3.0 구현 결과 (2026-10-03)

확정한 알파 대응 범위를 구현했다. 자체 초정확·판정 크기/숨기기·키 제한 모듈과 설정 페이지를 제거하고, 내장 HitMargin과 플레이어 1 트래커의 확정 값을 읽는다. 세부 프리셋 OFF/ON에 HUD 표시가 연동되며 기록은 게임이 계속 보관한다. 일반 Perfect 및 세부 X 누적에 Auto를 한 번 포함하고 공식 XAccuracy/XScore는 변경하지 않는다. 콤보는 일괄 추가와 체크포인트 복원을 지원하며 로비는 시도 기록에서 제외한다.

돈키호테 세부 색과 + / X / - 순서를 유지한다. 게임 색 프리셋을 우선하며 폰트·머티리얼·크기·숨김·미터·파티클은 변경하지 않는다. 모드 해제 시 직접 적용한 RGB만 복원하고 게임의 fade alpha 및 타 모드 색 변경은 보존한다. 원본 이펙트 코드, uGUI/TMP, 두 언어 폰트와 키뷰어 구조를 유지했다. 설정 schema 3은 제거한 필드를 무시하며 배치·바인딩·기록·사용자 지정 색을 보존하고 이전 기본 팔레트만 #FFC939/화이트로 바꾼다.

Release 오류·경고 0. Alpha 검증 111, Logic 106, KeyViewer 67, UserAdjustments 26, Font 8, EnterMap 4, UiFix 28, Effect 69, EffectIntegration 28, Installer 33, Installer UI 36개 검사를 통과했다. 패치 22개 대상 메타데이터가 존재하고 Unity 외부 적용 20개 성공, Unity ECall로 외부 적용이 불가능한 2개 및 수동 입력 훅 3개를 구분했다. 실제 alpha 게임에서 전체 등록이 성공하고 Ghostify 0.3.0이 초록불로 표시되며 설정창·X 닫기·OFF/ON이 작동함을 확인했다. 원본 2,336개 파일 해시도 보존했다.

실제 게임 실행에서 TogetherBootstrap의 OnToggle NullReference 오류는 별도로 발생했다. Ghostify 로그에는 활성화/재활성화 오류가 없다. 장시간 맵 플레이, 판정 경계별 샘플, 협동 모드, 모든 표시 프리셋의 실제 HUD 전환·결과창·해상도·성능·타 모드 조합은 아직 수동 출시 검증 대상이다. 아래 표는 그 전체 검증 범위를 보존하며, 자동 검사와 초기 런타임 검사 완료를 전체 플레이 검증 완료로 간주하지 않는다.

## 조사 결과와 대상 버전

현재 설치본과 개발 대상은 다음과 같이 확정했다.

| 항목 | 확인값 |
|---|---|
| 대상 | 사용자가 설치한 3.4.0 alpha |
| Steam 선택/장착 브랜치 | `alpha` / `alpha` |
| 설치 build ID | `25590222` — Steam manifest의 alpha 빌드와 동일 |
| 실행 로그 리비전/커밋 | `r150` / `32e5b8a` |
| 실행 로그 빌드 날짜 | `2026/09/28 11:18 PM` |
| Unity | `6000.3.21f1 (c02631ffc030)` |
| Unity Mod Manager | `0.33.0.0` |
| Assembly-CSharp SHA256 | `6BA2CFB8D260962D3C8A3E4409F60E8E98293CA9B2D2ECA4875096BD02D40230` |
| UnityPlayer SHA256 | `35F489B6A7753D536CE2E7B1BD1F729AFB8B4E70B43CAAA01AF03D1C20F64D6C` |

설치 경로는 `C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice`다. 초기 조사 때의 public build `24397494`, `r148`, Unity `6000.3.10f1`은 비교 기준으로 보관한다. Unity 6 전환은 이미 끝난 상태이며, 이번에는 6.3 패치 버전 갱신과 게임 판정 API 변경에 대응한다. DLL의 파일 버전 `0.4.3.0`은 게임 표시 버전으로 사용하지 않는다.

공식 v3.4.0 변경 문서는 기능 설계의 근거다. 실제 API는 설치된 alpha `25590222`를 기준으로 확정했고, 이후 alpha 업데이트로 빌드가 달라지면 다시 비교한다. 게임 업데이트/재설치 단계는 이번 계획에서 완료 처리한다.

공식 출처:

- [게임 변경 내역 목록](https://7thbeat.notion.site/game-changelogs)
- [v3.4.0 변경 내역](https://7thbeat.notion.site/ADOFAI-v3-4-0-3d890365a1618083b9fbd66cdeed896f)
- [비교 기준인 3.3 계열 변경 내역](https://7thbeat.notion.site/ADOFAI-v3-3-0-v3-3-1-3a990365a16180a08ef1d1df153f1e10)
- [공식 alpha 참여 방법](https://7thbeat.notion.site/How-to-access-the-beta-or-alpha-version-113ec51423a94dd7a0be85c6d9713935)

public의 관련 타입 23개 기록은 `Research/GameApi-public-24397494.json`, alpha의 확장된 관련 타입 44개 기록은 `Research/GameApi-alpha-25590222.json`이다. 실제 메서드·필드·속성·enum 값과 DLL 해시를 저장했다. 기존 23개 타입 및 소스의 Harmony 패치 클래스 28개/서로 다른 대상 메서드 27개를 비교한 결과는 `Research/ApiDiff-public-24397494-to-alpha-25590222.json`이다. 게임 함수나 생성자를 실행하지 않고 메타데이터와 관련 메서드 본문을 읽었으며, 이번 조사에서 모드 런타임 소스/설치된 모드/사용자 설정은 변경하지 않았다.

## 설치본에서 추가로 확정한 변경

1. **현재 모드의 로딩 실패 원인 확인:** `Player.log`에 `XPerfectCalculatePatch`의 `Undefined target method`가 기록되어 있다. `scrMisc.GetHitMargin`은 alpha에서 없어졌고 `GetHitMarginInDeg`/`GetHitMarginInSec`로 나뉘었다. 필수 patch 그룹 실패로 `OnToggle(true)`가 중단된다. 기존 패치를 새 계산 함수에 옮기지 않고 자체 초정확 모듈과 함께 제거한다.
2. **내장 판정 enum 확인:** `HitMargin`은 12개에서 16개로 바뀌었다. 기존 `Perfect`는 삭제되고 `PerfectMinus=3`, `XPerfect=4`, `PerfectPlus=5`가 추가됐다. `Auto=12`, `Midspin=14`, `FailedFloor=15`이며 다른 판정의 인덱스도 이동했다. 현재 `HitMargin.Perfect` 참조는 새 SDK로 빌드할 때 수정이 필요하고, 기존 숫자 인덱스는 잘못된 누적 값을 읽는다.
3. **별도 세부 판정 기록은 불필요:** `scrMarginTracker.hitMarginsCount`, `GetHits(HitMargin)`, `GetHits(HitMargin[])`가 새 enum별 누적을 제공한다. `AddHit(HitMargin)`와 새 `AddHits(HitMargin,int)`는 공통 `AddHitInternal`을 거친다. 누적은 트래커를 읽고, 한 함수의 postfix에만 의존해 별도 이력을 유지하지 않는다.
4. **판정 집계 정책을 구체화:** Ghostify 일반 Perfect 누적은 `PerfectMinus + XPerfect + PerfectPlus + Auto`, 세부 표시는 `PerfectPlus / (XPerfect + Auto) / PerfectMinus`로 기존 자동플레이 포함 정책을 이어 간다. `Midspin`은 세부 정확으로 더하지 않는다. 게임의 `PerfectHitMargins`는 위 세 판정 외에 `Auto`, `Midspin`, `Multipress`도 포함하므로 HUD/콤보에 그대로 합산하지 않는다. 콤보의 중립 이벤트와 실제 실패는 별도 매핑한다.
5. **공식 점수 공급원 확인:** 트래커에 `xScore`, `maxXScore`, `percentXAcc`, `maxPossibleXAcc`가 있다. `maxXScore`는 `scrLevelMaker.PlayerHitFloors.Count`에 내장 XPerfect 점수를 곱한다. 공식 값은 이 필드/속성에서 읽는다. `RegisterDeadTiles`는 이제 `FailedFloor`를 `AddHits`로 기록하므로 실패 뒤 남은 타일을 일반 입력 실패와 중복 집계하지 않도록 검증한다.
6. **내장 결과창 중복과 순서 확인:** `DetailedResults.GenerateResults(scrMarginTracker)`의 공개 서명은 유지되지만, 내부에서 초정확·±정확과 `showXScore` 옵션을 이미 처리한다. ± 표시를 함께 켠 경로의 순서는 `- / X / +`다. Ghostify HUD는 요청한 `+ / X / -`를 유지하고, 결과창도 순서를 유지하려면 기존 내장 행의 표시만 조정한다. 이전처럼 추가 행을 삽입하지 않는다.
7. **텍스트/미터 구조 확인:** `ShowHitText`에는 `Nullable<int>` 인수가, `scrHitTextMesh.Show`에는 문자열 인수가 추가됐다. `CalculateTickColor`는 삭제됐다. 미터 필드는 GameObject/Image 배열에서 Image/`ADOFAI.ErrorMeterTick` 배열로 변경됐다. 제거 예정인 크기·숨김·자체 미터 패치를 새 구조에 포팅하지 않는다.
8. **게임 표시 설정 확인:** `Persistence`에 `hitMarginPerfectText`, `hitMarginPerfectTick`, `hitMarginText`, `hitMarginColor`, `hideHitMargin`, `hitMarginTextScale`, `showXScore`가 있다. Ghostify의 크기·숨김 설정을 이 속성에 복제하거나 예전 값으로 덮어쓰지 않는다.
9. **입력 변경 범위 확인:** `AsyncInputManager`의 프레임/곡/오프셋 타임스탬프와 `scrController.ProcessKeyInputs`가 `UInt64`에서 `Int64`로 바뀌었다. 현재 키뷰어의 이벤트 시계는 이미 `long`이고 `SkyHookEvent.GetTimeInTicks()`도 `Int64`이므로 일괄 형변환 수정은 필요 없다. `ToggleHook(bool)`, `isActive`, `_instance`와 내부 SkyHook 리스너는 존재하며, 기존 관찰 훅이 게임 타이밍·훅 소유권을 방해하지 않는지 실제 게임에서 검증한다.
10. **이펙트 API 범위 확인:** 기존 이펙트 patch 대상의 서명과 `scrVfxPlus.filterToComp` 딕셔너리 형식은 동일하다. 메타데이터가 같다는 이유로 동작까지 검증됐다고 취급하지 않고, 기존 7개 설정과 복원 회귀 검사를 진행한다.

`XPerfectModule`에 통계와 시도 횟수 초기화도 섞여 있다. `Record`의 `OverlayController.NotifyHit`, `Reset`의 `OverlayController.ResetRun`, `XPerfectResetPatch`의 `AttemptTracker.BeginRun`을 필요한 통계 코드로 옮긴 뒤 파일을 제거해야 한다. 파일만 삭제하면 콤보·시도 횟수 갱신이 끊긴다.

메타데이터 조사에서 기존 Harmony 대상 27개 중 2개가 삭제됐고 2개의 서명이 변경됐다. 나머지 23개는 서명이 동일하다. 새 대상에 대한 Harmony 적용/해제 및 인게임 동작은 개발 단계의 검증으로 남아 있다. 실행 로그에는 `TogetherBootstrap`의 별도 예외도 있으므로 모드 단독 검증과 동시 사용 검증의 결과를 구분한다.

## 내장 초정확 사용과 기존 모듈 제거

사용자 의견을 반영해 최신 alpha에서는 **기존 `XPerfectModule`을 제거한다.** 자체 판정 경계 계산, 전역 pending 판정, 자체 XPerfect 미터와 게임 결과에 세부 판정을 추가하는 코드는 유지하지 않는다. 판정 결과·기준·점수의 공급원은 게임 내장 기능으로 통일한다. 알파 API를 읽지 못할 때 예전 계산으로 대체하지 않고 해당 표시의 호환 상태를 안내한다.

모듈에 함께 들어 있던 Ghostify 고유 표시 요구는 별도로 보존한다. 자동플레이 포함 누적, `+ / X / -` 표시 순서와 돈키호테 색상은 게임의 확정 판정에 연결하는 표시 코드로 옮긴다. 내장 설정으로 충족되는 텍스트·파티클·미터 기능은 게임 설정을 사용한다. 같은 기능의 독립 토글이나 두 번째 판정·미터를 추가하지 않는다.

추가 사용자 요청에 따라 **Ghostify의 판정 텍스트 크기 조절과 판정 텍스트 숨기기 기능도 제거한다.** 크기 슬라이더와 Perfect/XPerfect/±Perfect/전체 숨김 토글, 관련 설정 필드·검증·패치를 정리한다. 게임 내장 설정을 사용하며 Ghostify UI에 같은 조절기를 다시 만들지 않는다. 기존 JSON의 해당 값은 무시하고 게임의 현재 크기·숨김 설정에 덮어쓰지 않는다. 오버레이 누적 표시의 켜기/끄기와 오버레이·키뷰어의 위치/크기 편집은 별개 기능으로 유지한다.

**키 제한 기능도 제거한다.** Ghostify의 키 제한 토글, 허용 키 목록·등록·삭제, 키뷰어의 손/발 키를 허용 목록에 복사하는 버튼과 설명, `KeyLimiterEnabled`/`AllowedKeys` 설정 및 게임 입력 필터를 정리한다. 비워진 입력 페이지와 `InputSettings` 객체도 제거한다. 이전 JSON의 키 제한 값은 무시하며 게임 자체 입력 설정을 변경하지 않는다. 키뷰어의 손/발/고스트 키 등록·별칭·입력 관찰과 Enter/숫자패드 Enter 구분은 유지한다. 설정 창·배치 편집 중 게임 입력이 함께 실행되지 않게 하는 임시 UI 입력 차단은 키 제한과 분리해 유지하고, 창을 닫으면 해제되는지 검증한다.

추가 요청에 따라 **게임에서 세부 판정 표시를 끄면 Ghostify의 `+ / X / -` 누적 텍스트도 자동으로 숨긴다.** 표시 기준은 `Persistence.hitMarginPerfectText`다. `Default`는 세부 누적 숨김, 초정확 또는 ±정확을 표시하는 나머지 5개 프리셋은 세부 누적 표시로 처리한다. 파티클 유무는 누적 표시 여부에 영향을 주지 않는다. 게임 설정 변경을 재시작 없이 반영하고, 다시 켜면 트래커의 최신 누적 값을 표시한다. 일반 판정 누적·정확도·콤보 및 게임 내부 집계는 유지하며 숨김 때문에 기록을 초기화하지 않는다. Ghostify의 기존 누적 표시 설정을 껐을 때에는 게임 세부 판정이 켜져 있어도 숨긴다. 별도 세부 판정 토글은 만들지 않는다.

## 확인된 변경과 수정 우선순위

아래 게임 변경은 공식 문서와 설치된 alpha의 API/메서드 본문으로 확인했다. 실제 게임에서의 동작·다른 모드와의 공존은 아직 검증하지 않았다.

| 우선순위 | 게임 변경 / 현재 코드 | 필요한 수정 |
|---|---|---|
| P0 | 게임 내장 XPerfect 추가. 공식 기준은 약 ±16.6666ms 또는 ±12.5도. 기존 패치 대상 `GetHitMargin`은 삭제되어 로딩 실패 중이다. | 기존 모듈과 자체 판정 패치를 먼저 제거한다. 새 계산 함수에 같은 패치를 옮기지 않으며 게임이 제공한 판정을 사용한다. |
| P0 | 공식 표시는 빠름 `-Perfect`, 늦음 `Perfect+`. 현재 `ClassifySignedDelta`는 음수를 PlusPerfect로 분류한다. | 기존 분류 함수를 제거하고 내장 세부 종류를 그대로 사용한다. 사용자가 지정한 누적·결과 표시 순서는 `+ / X / -`를 유지한다. |
| P0 | XAccuracy에서 자동플레이 타일과 미드스핀 제외. | XAccuracy는 게임 값을 그대로 사용한다. 이전에 요청한 자동플레이 포함 일반·세부 누적은 계속 같은 포함 기준을 적용한다. 공식 정확도와 화면 누적의 분모를 혼동하지 않는다. |
| P0 | XScore 도입. 트래커에 `xScore`/`maxXScore`가 있고 내장 결과도 이를 표시한다. | 공식 정확도·점수를 그대로 읽고 결과 행을 보존한다. 별도 XScore 계산기나 중복 결과 행을 만들지 않는다. |
| P0 | 기존 `HitMargin.Perfect` 삭제와 enum 인덱스 이동. 새 `AddHits`/`FailedFloor` 처리 추가. | 일반·세부 누적은 이름 기반으로 트래커에서 읽는다. 콤보/시도 초기화를 모듈 밖으로 옮기고 일괄 판정·체크포인트·실패 후 남은 타일을 검증한다. |
| P0 | 입력 관련 수정: 비동기 판정 정확도와 알려진 키의 이름 표시 개선. | 입력 훅·타임스탬프·키 이름 변환을 다시 확인한다. 게임 비동기 입력을 방해하지 않고 Enter/숫자패드 Enter를 구분한다. |
| P1 | 사용자 요청으로 Ghostify 키 제한 기능 폐기. 현재 `InputController.Process`는 허용 키 필터와 메뉴 입력 차단을 함께 처리한다. | 허용 키 필터·설정·UI를 제거한다. 공유 경로에서는 UI가 열렸을 때만 입력을 차단하도록 분리하고, 일반 플레이에서는 Ghostify 허용 목록으로 입력을 거르지 않는다. |
| P1 | 내장 판정 미터의 XPerfect 표시·외곽선 옵션 추가. `CalculateTickColor` 삭제와 tick 구조 변경. | 기존 `XPerfectMeterZoneController`와 미터 색/구간 패치를 제거하고 내장 미터를 사용한다. 기존 GameObject/Image 배열 접근도 남기지 않는다. |
| P1 | 판정 프리셋, 색, 숨김, 파티클, 크기 등의 내장 설정 추가. | 기존 `StyleText`의 판정 이름 덮어쓰기와 Ghostify 숨김 토글/패치를 제거한다. 내장 설정을 사용하고, 게임 설정으로 충족되지 않는 요청 색상 등만 별도 표시 코드로 옮긴다. |
| P1 | 게임 세부 판정 표시와 Ghostify 세부 누적 표시 연동 요청. | `hitMarginPerfectText`를 기준으로 세부 누적을 자동 표시/숨김 처리한다. 파티클 옵션과 분리하고, 숨기는 동안에도 게임 집계를 유지한다. |
| P1 | 내장 판정 크기 0.50~4.00배. 모드는 연속 슬라이더 0.4~1.2배, 기본 0.72배. | Ghostify 크기 슬라이더·배율 설정·크기 변경 코드를 제거한다. 게임 내장 크기를 그대로 사용하고 예전 0.72배를 게임 설정에 강제로 이전하지 않는다. |
| P1 | 현재 필수 patch 그룹에 입력·판정·결과·미터가 모두 묶여 있다. | UI 시작, 입력, 판정 통계, 텍스트, 미터를 기능별로 검사한다. 선택 기능 하나의 API 변경이 모드 전체 로딩 실패로 번지지 않게 한다. 필수 판정 데이터가 없으면 상태를 명시하고 잘못된 수치를 표시하지 않는다. |
| P1 | 설치기는 게임 파일 존재와 UMM 버전을 검사하지만 검증된 게임 빌드 정보는 없다. | 검증한 alpha 빌드·Unity·참조 DLL 정보를 배포 메타데이터와 설치 화면에 기록한다. 다른 빌드일 때 호환 상태를 정확히 안내한다. |
| P2 | 프리룸 복귀 수정, 장식 Crop pivot offset 추가. | 프리룸·재시작·체크포인트·레인 위치를 회귀 검사한다. 직접 참조하지 않는 장식 기능은 기능 추가보다 영향 여부 확인을 우선한다. |

3.3 계열의 Steamworks 재작성과 전체 화면 수정은 비교 기준에도 포함된 변경이다. Ghostify는 Steamworks API를 직접 참조하지 않는다. 설치 경로 탐색과 전체 화면/uGUI 표시 검증 대상으로 관리하며 이번 alpha에서 새로 발생한 변경으로 분류하지 않는다.

## 개발 순서

### 1. 설치본·SDK 확인 (대상 및 메타데이터 비교 완료)

1. 현재 0.2.10 소스/배포물과 사용자 설정 8개·UMM 설정의 복구 기준을 보관한다.
2. **완료:** 설치된 alpha build `25590222`, r150, Unity와 게임 DLL 해시를 기록했다. alpha를 다시 설치하는 단계는 생략한다.
3. **완료:** alpha SDK 44개 타입을 별도 JSON에 저장하고 public 기준 23개 타입과 비교했다. 게임 DLL은 로컬 참조로만 사용하고 배포물에 포함하지 않는다.
4. **완료:** 기존 Harmony 대상 27개/패치 클래스 28개의 메타데이터를 비교했고, 비동기 수동 훅의 대상과 SkyHook 이벤트 시계가 존재함을 확인했다. 실제 Harmony 적용 검사는 수정 후 수행한다.
5. **완료:** 표시 프리셋과 별개인 내장 `HitMargin` 및 트래커 누적 경로를 확인했다. **남은 검증:** 초정확 표시를 끈 상태, 자동 타일/미드스핀/협동 모드, 모드 없는 게임 시작과 Ghostify 단독 로딩을 실제 게임에서 확인한다.

확정 기준: **개발 대상은 alpha `25590222`이며 판정·집계·표시·입력 API 비교 기록을 확보했다.** 인게임 검증까지 완료된 상태는 아니다. 다음 alpha 업데이트 때에는 이 기록과 새 빌드를 비교한다.

API 기록 예시:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Research\Capture-GameApi.ps1 -OutputFile '최신-alpha-API-기록의-절대경로.json'
```

### 2. 판정·통계 연결 교체 (P0)

1. `XPerfectModule.cs`와 자체 판정 계산/전역 pending 판정/미터 클래스 및 관련 패치 등록을 제거한다. `Main`, 오버레이, 설정 창과 테스트의 참조도 함께 정리한다. 필요한 누적 표시 코드는 게임의 확정 판정 데이터를 읽는 연결로 옮긴다.
2. 누적 표시는 `scrMarginTracker.GetHits`/`hitMarginsCount`를 직접 읽도록 한다. 단일 `AddHit` postfix와 자체 이력은 없애고 `AddHits`, 리셋, 체크포인트 복원도 게임 기록을 참조한다. 콤보에 필요한 이벤트 연결만 별도로 유지하며 중복 통지를 막는다.
3. `HitMargin.Perfect`와 고정 인덱스를 판정 이름에 따른 명시적 매핑으로 바꾼다. 일반 Perfect와 세부 누적의 Auto 포함은 위 정책을 따른다. `Midspin`/`Multipress`/`FailedFloor`는 일반 정확 입력과 구분한다. 기존 표시의 플레이어 1 범위를 유지하고, `firstMarginTracker`는 트래커 배열과 길이가 유효할 때만 사용한다.
4. 자동 타일 포함 누적과 공식 XAccuracy/XScore를 각각 해당 기준으로 표시한다. 전체 자동플레이와 자동플레이 타일 이벤트를 구분해 검증한다.
5. `DetailedResults.GenerateResults`에 세부 판정 행을 추가하던 코드를 제거한다. 내장 ± 결과의 `- / X / +`를 요청한 `+ / X / -`로 표시할 때에는 기존 행만 조정하고 내장 XScore·최대값·Auto 표기·언어별 프리셋을 보존한다. 판정 숨김/표시 프리셋이 다른 경우에도 중복 행이 생기지 않게 한다.
6. 기존 모듈의 `NotifyHit`, `ResetRun`, `AttemptTracker.BeginRun` 연결을 통계 코드로 옮겨 콤보·시도 횟수를 유지한다. 체크포인트/연습 시작/재시작/씬 전환에서는 트래커의 복원된 수치를 읽는다. 첫 판정 전·분모 0·NaN/Infinity는 초기 정확도 표시에서 처리하며 정상 보너스 정확도를 잘라내지 않는다.

완료 기준: **동일 판정에서 게임과 Ghostify의 세부 종류/공식 점수가 일치하고, 누적 포함 정책·플레이어 범위·복원이 검증됨.**

### 3. 표시·입력·이펙트 연결 정리 (P1)

1. 기존 `StyleText`와 자체 미터를 제거한 상태에서 내장 텍스트/미터를 검증한다. 게임의 ms·축약·Early/Late 프리셋을 그대로 사용한다.
2. Ghostify 누적의 돈키호테 판정색과 `+ / X / -` 순서를 유지한다. `_xPerfectAboveMeter`의 표시 조건에서 제거되는 `EnableXPerfect`를 게임 프리셋 기준으로 바꾼다. 세부 표시 여부는 `HitMarginHelper.IsShowXPerfect(preset, false) || HitMarginHelper.IsShowSignedPerfects(preset)`로 판별하고, 기존 오버레이 누적 표시 조건과 함께 적용한다. `false` 인수로 파티클 없는 초정확 프리셋도 포함한다. 프레임 갱신·전체 HUD 표시·모드 재활성화 등 모든 표시 경로에 같은 조건을 적용한다. 표시 OFF는 게임 통계를 변경하지 않고, ON으로 바뀌면 최신 트래커 값을 갱신한다. 떠오르는 판정 텍스트의 요청 색상을 내장 설정으로 표현할 수 있는지 확인하고, 부족한 부분만 표시 변경으로 처리한다. 별도 초정확 표시 모드를 추가하지 않는다.
3. 판정 폰트/머티리얼은 최신 게임의 네이티브 자산을 사용한다. 판정 크기·숨김은 게임 설정에 맡기고 Ghostify는 변경하지 않는다. 관련 슬라이더/토글, `TextSizeScale`, `HidePerfect`, `HideXPerfect`, `HidePlusMinusPerfect`, `HideAll` 및 `JudgmentVisibilityPatch`를 제거한다. 별도 색상 변경이 필요하면 모드 해제 시 모드가 소유한 변경만 복원하고, 중간에 바뀐 게임 설정을 오래된 캡처값으로 되돌리지 않는다.
4. `InputController`의 `FilterKeys`, 허용 목록 캐시/서명, 키 제한 전용 정규화와 오류 상태를 제거한다. `LegacyInputPatch`/`AsyncInputPatch`와 `Main`의 초기화 참조는 UI 입력 차단에 필요한 부분만 분리해 정리한다. `SettingsWindow`의 허용 키 등록 경로·복사 버튼과 빈 입력 페이지도 제거하되, 키뷰어 등록의 공용 캡처 경로는 보존한다. `AsyncInputHook`, 키뷰어 키 변환, `KeyCaptureUiLease`를 alpha 입력 경로에 맞춰 조정한다. 키뷰어 관찰용 입력이 게임의 훅 시작/종료와 판정 타이밍을 바꾸지 않는지 확인한다.
5. 이펙트 patch 대상 서명과 필터 딕셔너리 형식은 기존과 동일하므로 일괄 재작성하지 않는다. 기존 7개 설정 구성, 필터 예외, 카메라 상태 복원과 Move Track 제한을 실제 게임에서 회귀 검사한다. 새 판정 파티클 설정과의 중복 여부도 확인한다.
6. `PatchRegistry`, 시작/종료 및 오류 표시를 기능별 호환 상태에 맞춘다. API가 없는 선택 기능의 실패와 모드 핵심의 실패를 구분해서 안내한다.

완료 기준: **새 게임 설정과 Ghostify가 충돌하지 않고, 입력/이펙트 연결과 해제·복원이 실제 게임에서 확인됨.**

### 4. 설정·배포 정리

- 게임 UI는 **uGUI/TMP**로 구현한다. 최신 사용자 요청에 따라 전체 UI의 기본 테마는 **#FFC939와 화이트(#FFFFFF)**로 변경한다. 버튼·토글·탭·슬라이더·강조·테두리 등 공통 색상과 키뷰어 기본 테마에 적용한다. Gmarket Sans(부족한 글리프는 Noto Sans KR), 캡슐 토글과 X 닫기를 사용한다.
- 키뷰어 기본 키·레인 색상도 #FFC939/#FFFFFF 테마에 맞춘다. 사용자 지정 색상의 저장 기능은 유지하고 저장한 배치·크기·카운트를 보존한다. 손 16키의 기준 50×50, 간격 4 및 레인 폭을 회귀 검사한다.
- 새 설정 필드는 이전 JSON에 없을 때만 기본값을 채운다. 자체 초정확·판정 크기·숨김·키 제한의 설정 필드와 독립 토글은 제거한다. `JudgmentSettings`가 더 이상 사용되지 않으면 설정 객체와 판정 설정 페이지도 정리하며, 키 제한 전용 `InputSettings`와 빈 입력 페이지도 제거한다. 이전 JSON에 남은 제거된 필드는 로드 시 무시하고 다시 저장할 때 정리하며, 게임 내장 설정을 덮어쓰지 않는다. 다른 사용자 설정은 보존한다.
- 새 모드 버전과 검증한 alpha 빌드를 README/배포 메타데이터에 함께 기록한다. UMM ID와 저장 경로의 호환을 유지한다.
- 설치기에도 검증 게임 빌드 정보와 호환 표시를 추가한다. EXE의 새 설치/업데이트/백업/복원, 찾기·취소·종료 회귀 검사를 다시 수행한다.

## 검증표와 출시 조건

| 영역 | 필수 검사 |
|---|---|
| 판정 경계 | 공식 기준 부근의 조기/정확/지연, 저·고 BPM, pitch/speed/marginScale, CW/CCW. Ghostify가 읽은 세부 종류가 게임의 확정 판정과 일치하는지 검사하며 별도 경계를 계산하지 않는다. |
| 집계 | 일반 타일/미드스핀/자동 타일, 전체 자동플레이, 같은 프레임 여러 입력, 빠른 연타, 내장 초정확 표시 ON/OFF, 플레이어 1/로컬 협동. `AddHits`의 일괄 실패/`FailedFloor`, enum 기반 매핑, 콤보·시도 횟수 연결 누락과 중복 검사. |
| 정확도·점수 | 첫 판정 전/유효 타일 0개, XAccuracy 분모, XScore/최대값, 일반 Accuracy의 정상 100% 초과 보너스, 결과와 오버레이 일치. |
| 복원 | 재시작, 체크포인트, 중간 시작, 연습 모드, 프리룸, 씬 전환, 모드 OFF/ON, 게임 설정 변경 후 모드 해제. |
| 표현 | 기본/축약/ms/± 부호/점수 손실/Early-Late 프리셋, 색/파티클, 미터 외곽선, 폰트·그림자, 결과 중복 없음. 게임 세부 판정 프리셋 6개를 순환하며 세부 누적이 즉시 표시/숨김되는지, 파티클 OFF에서도 표시되는지, 숨긴 동안의 기록이 재표시에 반영되는지 검사. 일반 판정 누적 표시 OFF, HUD 재표시·씬 전환·모드 OFF/ON에서도 동일한 연동 조건을 확인. 게임 판정 크기·숨김 설정이 바뀌지 않고 Ghostify에 중복 조절기가 없는지 검사. |
| 입력·키뷰어 | 비동기 ON/OFF 전환, Enter/숫자패드 Enter, 알려진 키 이름, 손/발/고스트 키 등록·별칭, 등록 Esc 취소, 포커스 상실, 레인·KPS·Total. 이전 `KeyLimiterEnabled=true`/허용 목록이 남은 JSON으로도 Ghostify 입력 필터가 작동하지 않는지 검사. 설정 창·배치 편집 중 UI 입력 차단과 닫은 뒤 복귀를 확인. |
| 이펙트 | 7개 설정/예외 필터/Move Track 범위, 이벤트 예외와 카메라/필터 복원, 판정 파티클 설정과 공존. |
| 시작·설치 | `GetHitMargin`/`CalculateTickColor`의 폐기 패치가 남지 않아 UMM 정상 로딩되는지 검사. 새 Harmony 연결·해제, Ghostify 단독/다른 모드와 동시 사용, 게임 실행 중 설치 차단, 설정 8개 보존, 실패 복원, EXE 반복 종료·자원 해제. |
| UI | 전체 화면/창 모드, 여러 해상도·배율, 저장 위치·50×50 키와 레인 폭, UI 조작과 게임/키뷰어 입력의 경계. |

출시 조건은 대상 alpha SDK 빌드 성공, 필요한 API/패치 연결 검사, 기존 회귀 검사 및 위 표의 실제 게임 검증 통과다. Unity 밖에서 메타데이터/패치 생성만 확인한 결과를 인게임 동작 검증으로 간주하지 않는다. 실제 게임 검증을 끝낸 빌드만 설치 EXE와 GitHub 릴리스로 배포한다.

## 주요 수정 파일

- `XPerfectModule.cs`: 파일과 자체 판정/텍스트/결과/미터 패치를 제거한다. 필요한 게임 판정 읽기와 Ghostify 누적 표시 연결은 통계·표시 코드로 옮긴다.
- `OverlayController.cs`, `OverlayPresentation.cs`: 새 enum 기반 누적·트래커 읽기, 플레이어 범위·XAccuracy/XScore 표시와 캐시, 콤보·시도 초기화 연결 유지.
- `InputController.cs`, `KeyViewerContents/AsyncInputHook.cs`, `KeyCaptureUiLease.cs`: 키 제한 필터 제거, UI 입력 차단 분리, alpha 입력 관찰·키뷰어 등록과 복원.
- `Settings.cs`, `SettingsWindow.cs`: 자체 초정확·판정 텍스트 크기·숨김·키 제한 옵션과 빈 페이지 제거, 이전 JSON의 폐기 필드 처리 및 다른 사용자 설정 보존.
- `PatchRegistry.cs`, `Main.cs`, `RuntimeStatus.cs`: 기능별 호환 검사·시작/해제·오류 안내.
- `GameplayPatches.cs`, `EffectRestoration.cs`, `EffectLifetime.cs`: 동일한 이펙트 API의 연결·복원 회귀 검사와 실제 검증에서 드러난 수정.
- `GhostifyOverlay.csproj`, `Build.ps1`, `Installer/*`, `Tests/*`: 제거한 모듈의 참조·기존 계산 테스트 정리, 최신 alpha 참조, 호환 기록, 내장 판정 연결·설치·회귀 검증.

## 0.3.1 추가 피드백 구현 및 검증 (2026-10-03)

사용자가 키뷰어 → 게임 설정 자동 반영과 Gmarket Sans 교체를 확정했다. 아래 9개 항목을 구현해 로컬 검증용 빌드를 설치했다.

| 피드백 | 구현 |
|---|---|
| 고스트 레인 색 | 별도 GhostRainColor 저장/입력/초기화, 활성 레인·재사용 레인에 즉시 적용. |
| 자동 플레이 문구 위치 | 원래 문구 탐색·이동·복원 코드를 제거해 게임 배치를 사용. |
| 곡명·작곡가 | 진행 정보 아래 작은 일반 텍스트. 커스텀 LevelData 및 공식 WorldData 메타데이터 사용, 빈 행 제거·긴 제목 말줄임. |
| 판정 타이밍 범위 | BPM 아래 네이티브 GetAdjustedTimeBoundaries를 읽어 ± ms 표시. 현재 속도·피치·난이도·다음 타일 marginScale 반영. |
| 누적 숫자 간격/끼임 | 내용에 따른 셀 폭과 8-unit 간격, 최소 강제 전체 폭 제거, 세로 여유와 Overflow 적용. |
| Gmarket Sans | 한·영 UI/통계/키뷰어/설치기에 공식 Medium 사용. 게임 판정 폰트 유지. |
| Combo 구성 | 위 숫자, 아래 Combo. |
| 특정 키만 입력으로 간주 | 활성 손·발 키를 중복/None 없이 네이티브 Unity·비동기 허용 목록에 자동 반영. 고스트 키 제외, 최초 이전 목록 백업. 자체 입력 필터는 없음. |
| 플레이 중에만 표시 | 게임 States의 Countdown/Checkpoint/PlayerControl에서 표시, Space 전 Start와 로비·실패·완료에서 숨김. 설정·배치 편집은 미리보기 표시. |

곡 정보·판정 범위는 독립 토글 및 레이아웃 8·9번으로 조절한다. 기존 위치·배율·키/별칭·색·누적 횟수·시도 기록은 보존했다. Windows 네이티브 비동기 허용 목록에서는 두 Enter가 VK_RETURN을 공유한다. 키뷰어 슬롯과 Unity 프레임 허용 목록에서는 두 Enter를 구분한다.

자동 검사 603개 통과: Logic 80, Alpha 111, KeyViewer 67, UserAdjustments 26, Font 8, EnterMap 4, UI 28, Effect 69, EffectIntegration 28, Installer 33, InstallerUI 36, Feedback 113. 실제 alpha DLL의 판정 helper와 확정 함수가 72개 조합에서 일치함을 확인했다. 원본 돈키호테 2,336개 파일의 해시는 유지됐다.

실제 게임 검증: Ghostify 0.3.1 UMM Active/초록, Gmarket Sans uGUI 설정창, 로비 숨김, 손 키 10개의 게임 설정 허용 목록 반영을 확인했다. 게임 비동기 설정과 키뷰어 표시 옵션은 검증 전 값으로 복원했고 기존 저장 값 72개와 누적 파일 해시를 비교했다. 로그에 Ghostify 예외는 없으며 별도 TogetherBootstrap의 기존 OnToggle NullReference는 남아 있다.

이번 UI 자동화에서는 실제 레벨/에디터 진입을 완료하지 못했다. 새 곡·판정 범위의 실제 맵 렌더링, Space 시작/실패/완료 화면 전환, 긴 플레이 및 해상도별 배치 검증은 남아 있다. 로컬 빌드 설치 완료와 전체 출시 조건 완료를 구분하며, 이번 빌드를 GitHub 정식 릴리스로 게시하지 않았다.
