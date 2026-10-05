# AI 생성 코드 기록

Ghostify Overlay는 Codex를 사용해 개발했습니다. AI가 생성·수정한 코드의 줄 단위 기록은 [git-ai](https://github.com/git-ai-project/git-ai)의 Git Notes 형식으로 `refs/notes/ai`에 저장하고 GitHub에 함께 올립니다. GitHub 기본 소스 화면의 줄마다 자동 배지가 붙는 방식은 아니며, 아래 명령으로 생성 코드의 위치를 확인할 수 있습니다.

## 0.4.3 기록 범위

git-ai 1.7.5를 0.4.3 공개 배포 준비 중에 도입했습니다. 이전 커밋 `6ed1c69` 이후 Codex가 만든 미커밋 소스·검사·문서 변경분은 `codex` / `not-recorded-retrospective`로 사후 기록했습니다. 원래 편집 시점의 정확한 모델과 도구 호출 기록은 남아 있지 않아 재구성하지 않았습니다. 이전 커밋과 변경하지 않은 원본 코드를 AI 생성 코드로 소급 표시하지 않습니다.

공개 배포를 위한 문서 수정은 별도 Codex checkpoint로 기록합니다. 원본 키뷰어의 출처와 BSD-3-Clause 라이선스는 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md), 폰트의 라이선스는 `Assets`의 OFL 문서를 따릅니다. 이 기록은 원본 코드·글꼴·이미지의 저작권 표시를 대체하지 않습니다.

## 확인 방법

git-ai를 설치한 뒤 저장소에서 실행합니다. 일반 clone은 Git Notes를 자동으로 가져오지 않으므로 먼저 notes ref를 가져옵니다.

```sh
git fetch origin refs/notes/ai:refs/notes/ai
git-ai stats HEAD
git-ai blame NativeJudgments.cs
git log -1 --show-notes=ai
```

`git-ai diff HEAD`로 이번 커밋의 추가·삭제 줄과 생성 코드 표시를 함께 볼 수 있습니다. 표시가 없는 과거 줄은 기록이 없는 것이며, 사람이 작성했다는 판정이 아닙니다.

## 기록 설정

배포 기록은 프롬프트 저장을 로컬로 제한하고, 대화 스트리밍·대화 검색·로그 업로드·텔레메트리를 끈 상태에서 생성했습니다. 대화 원문과 개인 설정·게임 DLL·로컬 검사 로그는 공개 저장소에 포함하지 않습니다. 공개되는 Git Notes에는 코드 줄의 작성 도구, 사후 기록 여부 및 해당 checkpoint 식별자가 들어 있습니다.
