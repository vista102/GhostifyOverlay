# Ghostify Overlay 0.4.4 검증

2026-10-06. 출처 설명을 간결하게 정리하고 배포 고지 검사를 추가했습니다. 모드·설치 프로그램의 실행 코드는 변경하지 않았습니다.

## 출처와 배포 고지

- JipperResourcePack의 [기준 커밋 LICENSE](https://github.com/Jongye0l/JipperResourcePack/blob/ac6d6410571ee7d585aedce7f5085de34f615af2/LICENSE)를 직접 받아 동봉된 BSD-3-Clause 전문과 대조했습니다. 저작권자, 세 조건, 면책 전문을 유지합니다. 작성자 이름은 출처 표기에만 사용합니다.
- Gmarket Sans Light / Medium 파일은 [공식 배포](https://corp.gmarket.com/fonts/)의 TTF ZIP과 SHA256이 같습니다. 동봉된 공급자 OFL 전문을 유지합니다.
- Noto Sans KR 고지는 [Google Fonts 기준 커밋 OFL](https://github.com/google/fonts/blob/4efc2774c63917927efe769ca845def6bd6debae/ofl/notosanskr/OFL.txt)과 같습니다. 변환 내역·원본 지문은 `Assets/NotoSansKR-source.json`에 유지합니다.
- `THIRD-PARTY-NOTICES.md`를 구성 요소·사용 범위·라이선스 파일의 짧은 목록으로 정리했습니다. 세 라이선스 원문은 수정하지 않았으며 소스, EXE 내장 ZIP, 독립 ZIP 및 임시 설치 폴더에 모두 포함됩니다.
- `Installer/Verify-Notices.ps1`은 세 전문의 지문을 확인합니다. 패키징과 배포 검사에서 소스 및 Release 폴더에 적용합니다. 줄바꿈 차이는 허용하며 내용 누락·변경은 거부합니다.

## 이번에 실행한 검사

| 검사 | 결과 |
|---|---|
| Release 빌드 및 EXE 제작 | 성공 |
| 고지 검사 | 원본·CRLF 허용, 파일 누락·저작권자 변경·BSD 잘림·폰트 고지 변경 거부: 6개 통과 |
| EXE 내장 파일 / ZIP / Release SHA256 | 14개 모두 일치, 내장 ZIP과 독립 ZIP 동일 |
| 설치된 3.4.0 alpha SDK 읽기 전용 확인 | 성공 |
| InstallerEngine | 35개 통과 |
| 배포 EXE 자체 | 임시 한글 경로 새 설치·업데이트/백업·잘못된 경로 거부 통과 |
| Windows Forms 설치 UI | 38개 통과, 반복 종료·폰트 자원 해제·버튼 상태 포함 |
| production 글꼴 제공자 / API 대체 환경 | 10개 통과 |

실제 게임 폴더에는 설치하지 않았습니다. 인게임 화면과 UAC 사용자 클릭은 이번 자동 검사에 포함하지 않습니다. 모드 DLL SHA256은 0.4.3과 같으며, 기존 실행 기능의 검증 기록은 [0.4.3 검증](FEEDBACK_0.4.3_VALIDATION.md)에 있습니다. git-ai 기록은 원본 코드·글꼴의 저작권 고지를 대체하지 않습니다.

## 배포 지문

| 파일 | SHA256 |
|---|---|
| GhostifyOverlay-Setup-0.4.4.exe | `6e052cf158836c3de710f8cb7c51509904c584f357c04934312366c36bc41c9f` |
| GhostifyOverlay-0.4.4.zip | `68f4ffdf54bb83dc9a91fd307f39583ea829154fdca96af6f5d367f623b4f0de` |
| GhostifyOverlay.dll | `5998a13a575126b63d88af212c53f03f2c4b533f5425d2356a02205ae1fbd9f4` |
