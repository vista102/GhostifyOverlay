# Alpha 26044349 호환성

2026-10-19. 설치된 Steam alpha와 로컬 0.4.9 소스 커밋 `a94ba35`를 기준으로 분석했습니다. 게임 설치와 사용자 설정을 수정하지 않고 DLL·메타데이터·기존 로그를 읽었습니다.

## 대상과 증거

| 항목 | 이전 기준 | 이번 설치본 |
|---|---|---|
| Steam alpha build | 25590222 | 26044349 |
| 게임 / revision | 3.4.0 / r150 | 3.4.0 / r150 |
| 실행 로그 commit | 32e5b8a | c59229a |
| Unity | 6000.3.21f1 | 6000.3.25f1 |
| 게임 DLL SHA256 | `6BA2CFB8D260962D3C8A3E4409F60E8E98293CA9B2D2ECA4875096BD02D40230` | `F692210F9A342F7EED4BE40AAD463286AB40BF046599959823942EA02F4CA3FF` |
| 이번 게임 DLL MVID | — | `c0baa18f-e115-4186-bf86-1b2288496232` |

Steam appmanifest의 alpha build, UnityPlayer 버전, 기존 Player.log의 시작 정보로 설치본을 식별했습니다. REA의 실행 없는 정적 검사에서 관리형 Unity Mono/CIL DLL과 완전한 메타데이터를 확인했습니다.

- DLL 식별: REA `ev_f374f3b9cd928b7ccd443d454f4576fdfb5da1cd2b320da31e21afd16fc1def4` (요약 `ev_37d118c4a981115c91206b8ab2472c2c91a83d1a5853a892377ed01c4fa2b4eb`).
- 선택 타입 멤버: REA `ev_87dea5ba646232486f4467de33cc0cb7e6230d9ad0dfd2e8e411da271e8022a3`.
- 입력 메서드 서명 페이지: REA `ev_3cdaf429e2c1c58e4ec2bed6c28ace0e32eac2d48fa19e8255cedc546c59ba81`. `RDInput.GetStateKeys`의 이번 빌드 토큰은 `0x06001431`, 반환형은 `void`, 인수는 목록과 ButtonState입니다. 토큰은 위 SHA256/MVID의 빌드 안에서만 유효합니다.

## 확인한 변경과 수정

관련 타입 44개의 기존 API 기록과 새 설치본을 비교했습니다. `HitMargin`의 16개 판정과 판정·누적 통계 관련 캡처 서명은 유지됩니다. 주요 변경은 입력 목록의 재사용 버퍼 전환입니다.

| 항목 | 이전 | 새 alpha |
|---|---|---|
| RDInput.GetStateKeys | `List<AnyKeyCode> GetStateKeys(ButtonState)` | `void GetStateKeys(List<AnyKeyCode>, ButtonState)` |
| RDInput.GetMainPressKeys / GetMainHeldKeys | 목록 반환 | 전달받은 목록 채움 |
| AsyncKeyboard.GetSpecialInput | 목록 반환 | 전달받은 목록 채움 |
| AsyncKeyboard | — | GetUpKeys 및 재사용 버퍼 추가 |

기존 Player.log에 `Cannot get result from void method ... GetStateKeys(...)`가 기록되어 있습니다. 필수 입력 보호 패치가 반환값 `__result`를 요구해 로딩을 중단한 상황과 새 서명이 일치합니다. 새 메서드의 IL에서는 인수 0에 `Clear`와 `AddRange`를 호출하는 것을 확인했습니다. 패치를 정확한 새 서명에 지정하고 반환값 대신 인수 0의 목록을 비우도록 변경했습니다. 설정창·배치 편집 밖의 입력과 자동플레이 예외는 기존 동작을 유지합니다.

다른 캡처 변경에는 `scrController.listBPM` 제거, `RDC.force4PlayerCoop`의 속성→필드 전환, hold 입력 메서드와 내부 콜백 변경이 있습니다. 이번 모드에는 해당 멤버를 직접 사용하는 코드가 없습니다. 캡처에 없는 SkyHookManager를 API 삭제로 잘못 판정하지 않도록 비교 도구도 수정했습니다. 실제 Harmony 검사에서는 고스트 입력 대상과 수동 훅 3개를 모두 해석·생성했습니다.

## 0.4.9 기준 복원

`Main.cs`, `PatchRegistry.cs`, 프로젝트 구성을 0.4.9와 동일하게 복원했습니다. `SettingsWindow.cs`는 0.4.9와 휠 감도 80 설정만 다릅니다. 어템 로직은 0.4.9 그대로이며, 설정창은 Alt+D로 여는 독립형 uGUI입니다. UMM 렌더 텍스처·입력 어댑터·Escape 패치를 제거했습니다. 앞서 요청한 사각 창 모서리와 새 설치기 그림·아이콘은 유지했습니다.

이 비교는 선택한 타입의 메타데이터 및 정적 IL 검사입니다. 게임 전체의 변경 사항을 모두 설명하거나 실제 Unity 실행을 관측한 결과는 아닙니다. 테스트 범위와 남은 인게임 확인 항목은 [검증 기록](../FEEDBACK_0.4.10_VALIDATION.md)에 정리합니다.
