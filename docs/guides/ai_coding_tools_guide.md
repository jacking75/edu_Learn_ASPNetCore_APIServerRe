# AI 코딩 도구를 활용한 ASP.NET Core API 게임 서버 학습 가이드

이 문서는 Claude Code, OpenAI Codex(ChatGPT), Gemini CLI 등의 AI 코딩 도구를 활용하여 본 저장소의 학습 자료를 효과적으로 학습하고, API 게임 서버를 구현하는 방법을 안내한다.

---

## 1. 도구별 특성과 설치

### 1.1 Claude Code (Anthropic)

CLI 기반 에이전트. 프로젝트 디렉토리에서 직접 코드를 읽고, 편집하고, 빌드까지 수행한다.

```bash
# 설치
npm install -g @anthropic-ai/claude-code

# 프로젝트 디렉토리에서 실행
cd edu_Learn_ASPNetCore_APIServer-main
claude
```

**장점**: 파일 탐색/편집/빌드를 대화 안에서 바로 수행. CLAUDE.md를 읽어 프로젝트 맥락을 자동 파악.
**적합한 작업**: 코드 분석, 리팩토링, 새 기능 구현, 빌드 오류 수정, 프로젝트 전반 질문.

### 1.2 ChatGPT / OpenAI Codex

웹 또는 API 기반. 코드 조각을 붙여넣거나 설명을 요청하는 방식.

- **ChatGPT**: https://chat.openai.com (코드 해석, 설계 질문, 개념 설명)
- **GitHub Copilot**: VS Code/JetBrains 확장 (자동 완성, 인라인 코드 생성)
- **Codex CLI**: `npm install -g @openai/codex` (터미널 기반, 2025년 출시)

**장점**: 범용 질문, 개념 비교, 다양한 언어 지원, Copilot의 실시간 자동 완성.
**적합한 작업**: 개념 학습, 코드 리뷰, 알고리즘 설명, 테스트 코드 생성.

### 1.3 Gemini CLI (Google)

터미널 기반 AI 코딩 도구. 로컬 파일 접근과 명령 실행이 가능하다.

```bash
# 설치
npm install -g @anthropic-ai/claude-code  # Claude Code
npm install -g @google/gemini-cli         # Gemini CLI

# 프로젝트 디렉토리에서 실행
cd edu_Learn_ASPNetCore_APIServer-main
gemini
```

**장점**: Google 생태계 통합, 대용량 컨텍스트, 무료 사용 가능.
**적합한 작업**: 코드 분석, 문서 생성, 대규모 코드베이스 탐색.

### 1.4 도구 선택 가이드

| 상황 | 추천 도구 |
|------|----------|
| 프로젝트 코드 분석/수정 | Claude Code, Gemini CLI |
| 개념 학습/설계 질문 | ChatGPT, Claude Code |
| 코드 작성 중 자동완성 | GitHub Copilot |
| 빌드 오류 디버깅 | Claude Code, Gemini CLI |
| 코드 리뷰 | ChatGPT, Claude Code |
| DB 스키마/SQL 작성 | ChatGPT, Claude Code |

> **팁**: 도구 하나에 의존하지 말고 상황에 따라 병행 사용한다. 예를 들어 Claude Code로 코드를 수정하고, ChatGPT에서 설계 의견을 구하는 식으로 활용한다.

---

## 2. 학습 단계별 활용법

### 2.1 1단계: 프로젝트 구조 파악

저장소를 클론한 후 AI 도구로 전체 구조를 파악한다.

#### Claude Code / Gemini CLI에서

```
> 이 저장소의 전체 구조를 설명해줘. 어떤 프로젝트들이 있고 각각 무엇을 하는지 알려줘.
> codes/GameAPIServer_Template의 Program.cs를 분석해서 DI 등록, 미들웨어, 엔드포인트를 정리해줘.
> codes/GameAPIServer_Template과 codes/practice_MiniGameHeavenAPIServer의 구조 차이를 비교해줘.
```

#### ChatGPT에서

Program.cs 코드를 붙여넣고:
```
아래 ASP.NET Core Program.cs 코드를 분석해줘.
서비스 등록 순서, 미들웨어 파이프라인, 설정 로드 방식을 설명해줘.
[코드 붙여넣기]
```

#### 학습 포인트
- `codes/GameAPIServer_Template/README.md` — 템플릿 구조 파악
- `docs/references/APIServer_Directory.md` — 표준 디렉토리 컨벤션 이해
- `coding_rule.md` — 코딩 규칙 숙지

---

### 2.2 2단계: 핵심 개념 학습

AI 도구에 구체적인 질문을 던져 개념을 학습한다.

#### DI (의존성 주입)

```
> docs/patterns/why_di.md를 읽고, 이 문서에서 설명하는 DI 패턴을
  GameAPIServer_Template의 Program.cs에서 어떻게 적용했는지 예시와 함께 설명해줘.
```

#### 미들웨어

```
> codes/GameAPIServer_Template/Middleware/ 안의 파일들을 읽고,
  요청이 들어올 때 VersionCheck → CheckUserAuthAndLoadUserData → Controller 순서로
  처리되는 흐름을 단계별로 설명해줘.
```

#### Redis Cache-Aside 패턴

```
> docs/patterns/Cache-Aside_pattern.md를 읽고,
  codes/api_server_training_dungeon_farming에서 Redis를 실제로 어떻게 사용하는지 코드로 보여줘.
```

#### DB 트랜잭션

```
> docs/guides/how_to_db_transaction.md를 읽고,
  SqlKata에서 트랜잭션을 처리하는 코드 패턴을 설명해줘.
```

---

### 2.3 3단계: 참고 프로젝트 분석

완성된 프로젝트를 AI와 함께 깊이 분석한다.

#### 추천 분석 순서

1. **GameAPIServer_Template** — 기본 구조
2. **practice_MiniGameHeavenAPIServer** — 가장 완성도 높은 프로젝트
3. **api_server_training_dungeon_farming** — Redis + 마스터 데이터 고급 패턴
4. **practice_omok_game-1 / omok_game-2** — 롱폴링 실시간 게임

#### 효과적인 분석 프롬프트

```
> codes/practice_MiniGameHeavenAPIServer/APIServer/Controllers/ 안의 컨트롤러들을 분류해줘.
  인증, 유저, 아이템, 우편, 출석, 친구, 미니게임 카테고리별로 정리하고
  각 컨트롤러가 어떤 서비스를 호출하는지 매핑해줘.

> codes/practice_omok_game-1/SequenceDiagram/ 의 다이어그램을 읽고,
  롱폴링으로 실시간 게임을 구현하는 방식을 설명해줘.
  일반적인 WebSocket 방식과 비교하면 장단점이 뭐야?

> codes/api_server_training_dungeon_farming의 ErrorCode.cs를 분석해서
  에러 코드 체계(범위별 분류, 네이밍 규칙)를 설명해줘.
```

---

### 2.4 4단계: 직접 API 게임 서버 만들기

#### 프로젝트 생성

```bash
# CLI에서 새 프로젝트 생성
dotnet new webapi -n MyGameServer
cd MyGameServer
```

AI 코딩 도구(Claude Code 또는 Gemini CLI)를 프로젝트 디렉토리에서 실행:

```
> GameAPIServer_Template의 구조를 참고해서 이 프로젝트에 다음을 세팅해줘:
  1. Controllers, Services/Interfaces, Repository/Interfaces, Middleware, DTOs, Models 디렉토리 생성
  2. ErrorCode.cs 생성 (UInt16 enum, 범위별 카테고리)
  3. appsettings.json에 DbConfig (Redis, GameDb, MasterDb) 섹션 추가
  4. Program.cs에 DI 등록 구조 세팅
```

#### 기능 구현 (단계적으로)

AI에게 한 번에 모든 것을 요구하지 말고, **한 기능씩** 구현한다.

```
> 1단계: CreateAccount API를 만들어줘.
  - POST /CreateAccount
  - 요청: { "UserID": string, "Password": string }
  - 비밀번호는 SHA256 + salt로 해싱
  - AccountDb에 저장
  - GameAPIServer_Template의 AuthService 패턴을 참고해줘.

> 2단계: Login API를 만들어줘.
  - POST /Login
  - 비밀번호 검증 후 Redis에 인증 토큰 저장
  - 응답에 uid, token 포함
  - MemoryDb의 토큰 관리 패턴을 참고해줘.

> 3단계: 인증 미들웨어를 만들어줘.
  - 헤더에서 uid, token을 읽어 Redis에서 검증
  - Login, CreateAccount는 건너뜀
  - 요청 중복 방지를 위한 락 처리 포함
  - GameAPIServer_Template/Middleware/CheckUserAuthAndLoadUserData.cs를 참고해줘.
```

#### Redis 실습

`redis_training/README.md`의 챌린지를 AI와 함께 풀어본다.

```
> redis_training/README.md를 읽고, 1번 과제(로그인 시 인증키 저장)를 구현해줘.
  CloudStructures 라이브러리를 사용하고, API 엔드포인트로 만들어줘.

> 5번 과제(Rate Limiting: 2분에 3번만 요청 가능)를 Redis로 구현해줘.
  Sorted Set을 사용하는 방식과 String + TTL을 사용하는 방식 두 가지를 비교해줘.
```

---

### 2.5 5단계: 코드 리뷰와 개선

작성한 코드를 AI에게 리뷰 요청한다.

```
> 내가 작성한 LoginController.cs를 리뷰해줘.
  coding_rule.md의 코딩 규칙을 기준으로 네이밍, 구조, 보안 문제를 체크해줘.

> 내 GameDb 클래스의 SqlKata 쿼리를 보고 SQL 인젝션 위험이 있는지,
  트랜잭션 처리가 올바른지 확인해줘.

> 현재 내 서비스 코드에서 예외 처리가 부족한 부분을 찾아줘.
  ErrorCode enum과 연계해서 적절한 에러 코드를 반환하도록 개선해줘.
```

---

## 3. 실전 워크플로우

### 3.1 학습 세션 예시 (2시간)

```
[0:00 - 0:20] 개념 학습
  → AI에게 오늘 학습할 주제(예: Redis Cache-Aside) 질문
  → docs/patterns/ 문서를 함께 읽으며 이해

[0:20 - 0:50] 참고 코드 분석
  → AI와 함께 관련 프로젝트 코드를 분석
  → "이 부분은 왜 이렇게 구현했어?" 식의 질문

[0:50 - 1:40] 직접 구현
  → AI에게 구현 방향을 설명하고, 코드 생성 요청
  → 생성된 코드를 이해한 후 수정/개선
  → dotnet build로 빌드 확인

[1:40 - 2:00] 복습 및 정리
  → AI에게 오늘 구현한 코드 리뷰 요청
  → 개선점 반영, 커밋
```

### 3.2 기능 구현 워크플로우

```
1. 기획    → AI에게 "우편 시스템을 만들려는데 DB 스키마를 어떻게 설계하면 좋을까?"
2. 설계    → AI에게 시퀀스 다이어그램 생성 요청 (Mermaid 문법)
3. 스키마  → AI에게 SQL CREATE TABLE 문 생성 요청
4. 코드    → Repository → Service → Controller 순서로 한 레이어씩 구현
5. 테스트  → AI에게 apiTest.http 테스트 케이스 생성 요청
6. 리뷰    → AI에게 전체 코드 리뷰 요청
```

### 3.3 디버깅 워크플로우

```
# 빌드 오류 시
> dotnet build 결과 아래 오류가 발생했어. 원인과 해결 방법을 알려줘.
  [오류 메시지 붙여넣기]

# 런타임 오류 시
> API 호출 시 500 에러가 발생해. 로그는 다음과 같아:
  [로그 붙여넣기]
  원인을 분석하고 수정 코드를 제안해줘.

# 성능 문제 시 (CLI 도구)
> 이 컨트롤러의 응답이 느린데, DB 쿼리와 Redis 호출을 분석해서
  병목 지점을 찾아줘.
```

---

## 4. 효과적인 프롬프트 작성법

### 4.1 좋은 프롬프트의 구조

```
[맥락] + [구체적 요청] + [참고 자료] + [제약 조건]
```

#### 나쁜 예
```
우편 기능 만들어줘.
```

#### 좋은 예
```
우편 수령 API를 만들어줘.
- POST /MailReceive, 요청 본문에 MailSeq (long) 포함
- 우편의 아이템을 유저 인벤토리에 추가하고, 우편 상태를 "수령 완료"로 변경
- 트랜잭션으로 두 작업을 묶어야 해
- codes/GameAPIServer_Template/Services/MailService.cs의 패턴을 참고해줘
- ErrorCode는 기존 ErrorCode.cs의 8000번대를 사용해줘
```

### 4.2 단계별 요청

AI에게 한 번에 모든 것을 요청하면 품질이 떨어진다. 레이어별로 나눠서 요청한다.

```
1차: "우편 수령의 DB 스키마와 Repository 메서드를 만들어줘"
2차: "Repository를 사용하는 MailService.ReceiveMail 메서드를 만들어줘"
3차: "MailReceiveController를 만들어줘"
4차: "apiTest.http에 테스트 요청을 추가해줘"
```

### 4.3 AI 응답을 그대로 쓰지 않기

AI가 생성한 코드를 반드시 이해한 후 사용한다.

- **이해 확인**: "이 코드에서 `SetAsync`의 `When.NotExists` 옵션은 무슨 역할이야?"
- **대안 비교**: "이 구현 말고 다른 방법은 없어? 장단점을 비교해줘"
- **코딩 규칙 준수**: coding_rule.md 기준으로 네이밍/스타일 직접 수정

---

## 5. 도구별 CLAUDE.md / 설정 활용

### 5.1 Claude Code — CLAUDE.md

프로젝트 루트의 `CLAUDE.md`에 프로젝트 정보를 작성하면 Claude Code가 자동으로 읽어 맥락을 파악한다. 본 저장소에는 이미 작성되어 있다.

자신의 프로젝트에서도 CLAUDE.md를 작성하면 효과적이다:

```markdown
# MyGameServer
## 기술 스택
- .NET 8.0 / ASP.NET Core Web API
- MySQL + SqlKata, Redis + CloudStructures, ZLogger
## 코딩 규칙
- coding_rule.md 준수
- 디렉토리: Controllers, Services, Repository, Middleware, DTOs, Models
## 현재 작업
- 우편 시스템 구현 중
```

### 5.2 Gemini CLI — GEMINI.md

Gemini CLI도 프로젝트 루트의 `GEMINI.md`를 참조한다. CLAUDE.md와 유사한 형식으로 작성한다.

### 5.3 GitHub Copilot — .github/copilot-instructions.md

Copilot Custom Instructions를 설정하면 자동 완성 품질이 향상된다:

```markdown
- C# / ASP.NET Core Web API 프로젝트
- dotnet/runtime 코딩 스타일 (PascalCase 클래스/메서드, _camelCase 필드)
- SqlKata 쿼리 빌더 사용 (EF Core 미사용)
- CloudStructures Redis 라이브러리 사용
- ZLogger 구조화 로깅
```

---

## 6. 주의사항

### 6.1 AI 코드의 한계

- **보안**: AI가 생성한 SQL 쿼리에 인젝션 취약점이 있을 수 있다. SqlKata의 파라미터 바인딩을 반드시 사용한다.
- **패키지 버전**: AI가 오래된 API를 제안할 수 있다. 본 저장소의 패키지 버전(ZLogger 2.4.1, CloudStructures 3.3.0 등)을 기준으로 확인한다.
- **할루시네이션**: 존재하지 않는 메서드나 클래스를 제안할 수 있다. `dotnet build`로 반드시 빌드 확인한다.
- **비즈니스 로직**: AI는 게임 기획 의도를 모른다. 기획 요구사항은 직접 판단한다.

### 6.2 학습 효과를 높이려면

- **복사-붙여넣기 금지**: AI가 생성한 코드를 그대로 복사하지 말고, 이해한 후 직접 타이핑한다.
- **"왜?"를 물어라**: 코드가 동작하는 것에 만족하지 말고 "왜 이렇게 구현했는지" 질문한다.
- **다른 도구와 비교**: 같은 질문을 Claude Code와 ChatGPT에 각각 던져보고 답변을 비교한다.
- **오류를 직접 해결해보기**: 빌드 오류가 나면 바로 AI에게 물어보지 말고, 먼저 5분간 직접 해결을 시도한다.

---

## 7. 학습 경로별 추천 프롬프트 모음

### 초급: ASP.NET Core 입문

```
> ASP.NET Core의 미들웨어 파이프라인이 뭔지 쉽게 설명해줘. 
  HTTP 요청이 들어왔을 때 어떤 순서로 처리되는지 그림으로 보여줘.

> DI(의존성 주입)에서 AddTransient, AddScoped, AddSingleton의 차이를 
  게임 서버 예시로 설명해줘. 각각 언제 쓰는 게 적절해?

> codes/GameAPIServer_Template/Program.cs를 한 줄씩 설명해줘.
  각 서비스 등록이 왜 Transient인지 Singleton인지 이유도 알려줘.
```

### 중급: DB + Redis 연동

```
> SqlKata로 MySQL INSERT, SELECT, UPDATE, DELETE 쿼리를 작성하는 
  기본 패턴을 보여줘. 파라미터 바인딩도 포함해서.

> CloudStructures로 Redis String, Hash, SortedSet을 다루는 기본 코드를 보여줘.
  각 자료형은 게임 서버에서 어떤 용도로 쓰여?

> redis_training/README.md의 11번 과제(랭킹 시스템)를 구현해줘.
  Top 10 조회, 내 순위 조회, 내 순위 ±2명 조회를 각각 API로 만들어줘.
```

### 고급: 실시간 게임 + 성능

```
> 롱폴링으로 턴제 게임(오목)을 구현하는 원리를 설명해줘.
  codes/practice_omok_game-1의 구현을 분석해서 알려줘.

> 게임 서버에서 동시 요청을 처리할 때 Redis 락을 어떻게 구현해?
  분산 락의 원리와 codes/GameAPIServer_Template의 구현을 비교해줘.

> 내 API 서버의 부하 테스트를 위한 시나리오를 설계해줘.
  로그인 → 게임 데이터 로드 → 아이템 사용 → 우편 확인 순서로.
```
