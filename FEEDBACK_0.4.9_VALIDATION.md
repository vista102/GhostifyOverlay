# 0.4.9 검증

2026-10-13. 이미 집계한 시도를 무적·자동플레이 전환 시 차감하던 ValidateCurrentRun과 관련 상태를 제거했습니다. 다음 판 시작 시 집계 조건을 결정하며 Full 기록과 맵 세션 구분은 유지합니다.

- production 어템 집계 / inert Unity API: 28개 통과. 모드 전환·판 종료 후 전환에도 기존 횟수와 저장 기록을 유지하며 다음 무적 시작은 제외합니다.
- 실제 3.4.0 alpha SDK Release 빌드와 설치 EXE 제작 성공.
- 내장 ZIP·별도 ZIP·Release 파일 14개 지문 및 라이선스 원문 검사 통과.
- EXE의 실제 게임 SDK 읽기 전용 확인 통과. 실제 게임 설치 및 인게임 재현은 하지 않았습니다.

설치 파일: `dist/GhostifyOverlay-Setup-0.4.9.exe`.
SHA256: `9d35bdc997b9fcbfcb1bb774837e393c7a37db1f33fde58a66efd9e678b37113`.
