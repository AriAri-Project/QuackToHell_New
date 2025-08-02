\# Team Development Guide for Gemini Code Assist



아래는 우리 팀의 개발 규칙, PR 규칙, 브랜치 규칙, 주석 규칙, 깃 커밋 컨벤션, 네이밍 및 코드 컨벤션이다.

앞으로 이 규칙에 맞게 코드 리뷰, 커밋 메시지 작성, 네이밍, 주석 작성 등을 수행한다.



\## \[개발 및 PR 규칙]



\- \*\*보이스 카우트 규칙\*\*: 코드를 볼 때마다 조금씩 개선한다. 작업 중 눈에 띄는 부분은 본인 코드가 아니어도 리팩토링 한다.



\### PR 규칙:

\- PR은 작게 작게 나눠서 보낸다.

\- PR 작성자는 PR 메시지에 해당 코드가 무엇을 하는지, 어떤 구조인지 설명을 반드시 작성한다.

\- PR 메시지는 구조 중심으로 작성하여 코드 줄 단위가 아닌 구조 단위로 리뷰할 수 있게 한다.



\### 브랜치 규칙:

\- master 브랜치 1개, feature 브랜치 여러 개 운영

\- 브랜치 네이밍 규칙은 따로 없음

\- PR은 작게 나눠서 보내되 리뷰는 주 1회(회의 시간에) 진행



\### 주석 규칙:

\- 함수에는 주의점이나 TODO가 있을 때만 `///` 형태로 주석 작성

\- 기타 주석은 자유롭게 작성



\## \[깃 커밋 컨벤션]



\- 메시지 타입은 간소화하여 다음 4가지만 사용:

&nbsp; - `feat` : 기능 추가

&nbsp; - `fix` : 버그 수정

&nbsp; - `docs` : 문서 변경

&nbsp; - `chore` : 빌드, 설정, 코드 개선 등 기타 변경

&nbsp; -  'test' : 테스트 코드



\## \[네이밍 규칙]



\- 싱글톤 클래스명은 `Manager` 접미사 사용

\- 한 씬 내에서만 국한되는 클래스는 `Controller` 접미사 사용

\- 태그 및 레이어 명: PascalCase

\- 하이어라키 오브젝트: PascalCase (예: UI\_SpeechBubble, Mat\_Red)

\- 폴더 내 파일명: PascalCase + 폴더명 접두사 (예: Sprite\_Player, Anim\_PlayerIdle)



\## \[코드 컨벤션]



\- private/protected 필드: `\_camelCase` (예: \_myVariable)

\- public 프로퍼티: `PascalCase` (예: MyVariable)



\## \[설계법]



\- 책임주도설계 (Responsibility-Driven Design)

\- MVP(Model-View-Presenter) 모델 사용

\- 위 설계 원칙을 어길 경우 코드 리뷰 필요



