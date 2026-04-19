# ASP.NET Core Web API 게임 서버 학습
[edu_Learn_ASPNetCore_APIServer](https://github.com/jacking75/edu_Learn_ASPNetCore_APIServer) 에 있는 문서와 코드를 AI에 입력하여 학습하기 위해 마크다운 문서로(혹은 .md 이지만 텍스트 파일) 만든 프로젝트이다. 여기에 있는 마크다운 문서로 AI를 사용하여 학습을 하면 효율적으로 할 수 있다.    
READE.md에 있는 외부 링크 문서는 직접 AI에 링크 그대로 입력하거나 혹은 마크다운 문서로 만들어서 사용하면 좋다.  
  
---  
    
ASP.NET Core로 게임 API 서버를 만드는 방법을 단계별로 학습하는 저장소.
학습 문서 + 예제 코드 + 실습 과제가 포함되어 있다.

## 기술 스택

| 영역 | 기술 |
|:---|:---|
| 언어/프레임워크 | C# / ASP.NET Core (.NET 10.0) |
| DB | MySQL (MySqlConnector + SqlKata) |
| 캐시 | Redis (CloudStructures) |
| 로깅 | ZLogger (구조화 JSON 로깅) |
| 모니터링 | Prometheus + Grafana |
| 서버 OS | Linux (Docker) |

> **참고**: 각 프로젝트의 `appsettings.json`에 포함된 DB 비밀번호는 **교육용 더미 값**이다. 운영 환경에서는 환경변수 또는 [User Secrets](https://learn.microsoft.com/ko-kr/aspnet/core/security/app-secrets)를 사용할 것.

---

## 학습 로드맵

아래 순서대로 진행한다. 각 단계에서 **읽기** → **코드 분석** → **실습** 순서로 학습한다.

### 0단계: 게임 서버가 뭔지 감 잡기

시작하기 전에 전체적인 그림을 파악한다.

| 자료 | 설명 |
|:---|:---|
| [인포그래픽](./docs/references/infographic.md) | 게임 서버 개발 전체 로드맵을 한 장으로 정리 |
| [Web 서비스의 서버 구성과 목적](https://docs.google.com/presentation/d/105NPfv7CPfgk0Iw_6vSB_oOavQZpes7-Wit5HuCm7oM/edit?usp=sharing) | 클라이언트-서버 구조, API 서버 역할 이해 |
| [서버 개발 기초 자료](https://sueshin.tistory.com/category/%EA%B0%9C%EC%9D%B8%EA%B3%B5%EB%B6%80/Web%20API%20%EA%B2%8C%EC%9E%84%20%EC%84%9C%EB%B2%84%20%EA%B3%B5%EB%B6%80) | 앞으로 배울 내용의 전체 흐름을 훑어보기 |

---

### 1단계: C# 기초

C#을 처음 접한다면 먼저 기본 문법을 익힌다.

| 자료 | 설명 |
|:---|:---|
| [(인프런 무료) C# 처음부터 배우기](https://inf.run/bfkW) | 동영상 강좌, 코딩 입문자용 |
| [C# 자료구조](http://www.csharpstudy.com/DS/array.aspx) | List, Dictionary 등 기본 자료구조 |
| [C# 비동기 프로그래밍 정리](https://docs.google.com/document/d/e/2PACX-1vRHRbQjeoJH9lXalTClFBuB-D41v9TaBTPc_TeUS-yKhPZTJa2dWjpv_Rib863b_disjspqymOjgKwq/pub) | async/await — API 서버에서 필수 |
| [코딩 규칙](./coding_rule.md) | 본 저장소에서 따르는 네이밍/스타일 규칙 |

---

### 2단계: ASP.NET Core 기본 — 읽기 + 실습

API 서버의 뼈대가 되는 3가지 핵심 개념을 익힌다.

#### 읽기

| 자료 | 핵심 개념 |
|:---|:---|
| [(유튜브) 1~6단계로 ASP.NET Core 기본 실습](https://youtu.be/YTDWXJG1SD8?si=PHz6XvNGy4yU-Sjj) | 프로젝트 생성부터 API 만들기까지 |
| [ASP.NET Core 기초 개념](./docs/guides/aspnetcore_basics.md) | **미들웨어, DI, 라우팅** — 본 저장소 문서 |
| [프로젝트 구조 가이드](./docs/guides/project_structure.md) | **Controller→Service→Repository** 패턴의 "왜" |
| [ErrorCode 설계 패턴](./docs/patterns/error_code_design.md) | ErrorCode enum 설계 원칙 |
| [DI 생명주기 설명](https://docs.google.com/document/d/e/2PACX-1vRFi_2Z6yMOWNwWfILDXGsbqYS3aJfiO6aO2u22Awy-pQ5XEEz0GpIOjehif47noYsR06jT6z_pD6Mr/pub) | AddTransient / AddScoped / AddSingleton 차이 |

#### 코드 분석

| 프로젝트 | 분석 포인트 |
|:---|:---|
| [`codes/GameAPIServer_Template/`](./codes/GameAPIServer_Template/) | `Program.cs`의 DI 등록과 미들웨어 체인 구조를 읽는다 |

#### 실습

- [VSCode 환경설정 가이드](./docs/guides/UsingVSCode.md)를 따라 개발 환경을 구성한다
- GameAPIServer_Template을 `dotnet run`으로 실행하고, `apiTest.http`로 API를 호출해 본다

---

### 3단계: MySQL + Redis — 데이터 저장

#### MySQL

| 자료 | 설명 |
|:---|:---|
| [MySQL + SqlKata 사용 가이드](./docs/guides/mysql_sqlkata.md) | 설정 → CRUD → 트랜잭션 단계별 가이드 |
| [DB 트랜잭션 가이드](./docs/guides/how_to_db_transaction.md) | 여러 DB 조작을 하나로 묶는 방법 |
| [SqlKata 라이브러리 소개](./docs/references/sqlkata.md) | SqlKata 상세 레퍼런스 |

#### Redis

| 자료 | 설명 |
|:---|:---|
| [(영상) Redis 야무지게 사용하기](https://www.youtube.com/watch?v=92NizoBL4uA) | Redis 개념 이해 (NHN 강의) |
| [Redis 학습 가이드](./redis/) | **README 정독 → docs/ 튜토리얼 6장 → RedisExampleServer 실습** |
| [Cache-Aside 패턴](./docs/patterns/Cache-Aside_pattern.md) | Redis를 캐시로 활용하는 패턴 |

#### 실습

```bash
# MySQL + Redis를 Docker로 실행 (최초 1회)
cd codes
docker compose up -d

# RedisExampleServer 실행 후 apiTest.http로 6가지 Redis 패턴 테스트
cd ../redis/RedisExampleServer
dotnet run
```

---

### 4단계: 로깅 — ZLogger

| 자료 | 설명 |
|:---|:---|
| [ZLogger 학습 가이드](./ZLogger/) | **README 정독 → SampleServer 실행 → apiTest.http 테스트** |

SampleServer를 실행하고 콘솔과 `logs/` 폴더에서 JSON 로그 출력을 확인한다.

---

### 5단계: 환경 설정

| 자료 | 설명 |
|:---|:---|
| [appsettings 환경별 설정](./docs/guides/appsettings_environment.md) | Development/Production 분리 방법 |
| [HttpClientFactory 사용법](./docs/guides/HttpClientFactory.md) | 서버 간 HTTP 통신 (Game↔Hive 서버) |

---

### 6단계: 프로젝트 분석 + 직접 구현

지금까지 배운 것을 종합하여 실제 프로젝트를 분석하고, 직접 만든다.

#### 분석할 프로젝트

| 프로젝트 | 분석 포인트 |
|:---|:---|
| [`codes/GameAPIServer_Template/`](./codes/GameAPIServer_Template/) | 단일 서버의 전체 구조 (미들웨어 → 컨트롤러 → 서비스 → DB) |
| [`codes/MultiAPIServer_Template/`](./codes/MultiAPIServer_Template/) | 멀티 서버 구성 (GameServer + HiveServer + MatchServer) |
| [`codes/practice_omok_game-1/`](./codes/practice_omok_game-1/) | 롱폴링 기반 실시간 게임, 시퀀스 다이어그램 |
| [`codes/practice_omok_game-2/`](./codes/practice_omok_game-2/) | 오목 게임 v2, 플로우 다이어그램 |

#### 직접 구현 도전

| 과제 | 설명 |
|:---|:---|
| [`codes/api_server_training_tany_farm/`](./codes/api_server_training_tany_farm/) | **과제 명세만 제공.** 서버를 직접 설계/구현한다 |

---

### 7단계 (선택): 모니터링

운영 환경에서 서버 상태를 파악하는 방법을 학습한다.

| 자료 | 설명 |
|:---|:---|
| [Prometheus + Grafana 가이드](./prometheus_grafana/) | Docker Compose로 모니터링 스택 구축 |
| [.NET Metrics API 가이드](./MetricsAPI/) | 커스텀 메트릭 정의 + 수집 |

---

## 디렉토리 구조

```
codes/                     # 예제 프로젝트 모음 (codes/README.md에 인덱스)
docs/                      # 학습 문서
  guides/                  #   실습 가이드 (기초 개념, DB, 환경설정 등)
  patterns/                #   설계 패턴 (Cache-Aside, DI, ErrorCode)
  references/              #   참고 자료 (SqlKata, Serilog, 디렉토리 구조)
redis/                     # Redis 학습 (CloudStructures 튜토리얼 + 예제 서버)
ZLogger/                   # ZLogger 로깅 학습
prometheus_grafana/        # Prometheus + Grafana 모니터링
MetricsAPI/                # .NET Metrics API 학습
```

---

## 참고 링크

- [AI 코딩 도구 활용 가이드](./docs/guides/ai_coding_tools_guide.md)
- [ASP.NET Core 팁 모음](./docs/references/aspnet_core_tips.md) (Polly 재시도, RateLimit, FluentValidation 등)
- [닷넷 빌드와 실행](./docs/references/dotnet_build.md)  