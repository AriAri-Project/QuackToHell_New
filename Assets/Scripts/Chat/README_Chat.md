# Chat System

Unity Netcode 기반 MVP 패턴 채팅 시스템

- **ChatModel**: 네트워크 통신 및 데이터 관리
- **ChatTestView**: UI 및 ObjectPool 관리
- **ChatTestPresenter**: MVP 중간 계층

## 주요 설정

### ChatTestView

- `maxDisplayMessages`: 최대 표시 메시지 수 (기본 100)
- `autoScrollThreshold`: 자동스크롤 임계값 0~1 (0=아래, 1=위)

### ChatModel

- `maxMessages`: 저장할 최대 메시지 수 (기본 300)
- `maxMessageLength`: 메시지 최대 길이 (기본 500)
- `ClearAllMessages()`: 모든 채팅 완전 초기화

### ChatTestPresenter

- `loadExistingMessagesOnStart`: 씬 시작 시 이전 채팅내역 로드 여부
- `loadMessageCount`: 로드할 이전 메시지 개수 (기본 50)
- `showTimestamps`: 타임스탬프 표시 여부
- 메시지/이름 색상 설정 (본인/타인 구분)

## 추가 기능

사용법: 씬에 ChatTestPresenter + ChatTestView 배치
