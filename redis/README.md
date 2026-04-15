# Redis 학습 가이드

## Redis란?

**Redis**(Remote Dictionary Server)는 **인메모리 키-값 데이터 저장소**입니다. 데이터를 디스크가 아닌 **메모리에 저장**하여 MySQL 등 디스크 기반 DB보다 10~100배 빠른 읽기/쓰기 속도를 제공합니다.

### 게임 서버에서 Redis를 사용하는 이유

| 용도 | MySQL만 사용할 때 문제 | Redis로 해결 |
|:---|:---|:---|
| **인증 토큰** | 매 요청마다 DB 조회 → 느림 | 메모리에서 즉시 조회, TTL로 자동 만료 |
| **랭킹** | `ORDER BY` + `LIMIT` → 느린 쿼리 | SortedSet으로 O(logN) 정렬, 즉시 Top N 조회 |
| **분산 락** | 다중 서버 환경에서 동시 요청 제어 불가 | `SET NX`로 원자적 락 획득 |
| **속도 제한** | 별도 카운터 테이블 + 만료 관리 복잡 | 키 TTL로 자동 만료, Increment로 카운터 |
| **채팅 이력** | 채팅마다 INSERT → DB 부하 | List에 Push, 메모리에서 빠른 조회 |
| **세션 캐시** | 매 요청마다 유저 데이터 DB 조회 | Cache-Aside 패턴으로 메모리 캐시 |

> **핵심:** Redis는 MySQL을 **대체**하는 것이 아니라 **보완**합니다. 영구 저장은 MySQL, 빈번한 읽기/쓰기와 임시 데이터는 Redis를 사용합니다.

### Redis의 5가지 핵심 데이터 구조

| 데이터 구조 | Redis 명령 | C# (CloudStructures) | 게임 서버 활용 |
|:---|:---|:---|:---|
| **String** | `SET`, `GET`, `INCR` | `RedisString<T>` | 인증 토큰, 유저 세션, 카운터 |
| **List** | `LPUSH`, `LRANGE`, `LTRIM` | `RedisList<T>` | 채팅 이력, 활동 로그, 큐 |
| **Set** | `SADD`, `SREM`, `SMEMBERS` | `RedisSet<T>` | 좋아요, 친구 목록, 중복 방지 |
| **SortedSet** | `ZADD`, `ZRANGE`, `ZRANK` | `RedisSortedSet<T>` | 랭킹, 리더보드 |
| **Hash** | `HSET`, `HGET`, `HGETALL` | `RedisDictionary<TK,TV>` | 유저 프로필 필드별 관리 |

---

## 학습 자료 구조

```
redis/
├── README.md                          # 이 문서 (개요 및 시작 가이드)
├── docs/                              # CloudStructures 튜토리얼 (6장)
│   ├── index.md                       # 튜토리얼 목차 및 구조도
│   ├── 01_레디스_접속_설정_.md        # RedisConfig 설정 방법
│   ├── 02_레디스_연결_관리자_.md      # RedisConnection 싱글톤 관리
│   ├── 03_값_변환기_.md              # JSON 직렬화/역직렬화
│   ├── 04_레디스_데이터_구조_.md      # 5가지 데이터 구조 상세 설명
│   ├── 05_레디스_작업_결과_.md        # RedisResult<T> 안전한 결과 처리
│   ├── 06_연결_이벤트_처리기_.md      # 연결 실패/복구 이벤트 처리
│   └── challenge_problems.md          # 실습 과제 16개
│
└── RedisExampleServer/                # ASP.NET Core 예제 서버
    ├── Program.cs                     # 앱 초기화 + Redis 연결 설정
    ├── apiTest.http                   # API 테스트 파일 (18개 엔드포인트)
    ├── Services/
    │   ├── RedisService.cs            # Redis 연결 싱글톤 + 팩토리 메서드
    │   └── RedisKeyBuilder.cs         # Redis 키 네이밍 규칙 중앙 관리
    ├── Models/
    │   ├── ErrorCode.cs               # 에러코드 enum
    │   └── BaseResponse.cs            # API 공통 응답 DTO
    └── Controllers/
        ├── AuthController.cs          # String — 계정/로그인/유저데이터
        ├── ChatController.cs          # List — 채팅 메시지 저장/조회
        ├── LikeController.cs          # Set — 좋아요 토글/목록/수
        ├── RankingController.cs       # SortedSet — 랭킹 등록/조회
        ├── LockController.cs          # 분산 락 (SET NX + TTL)
        └── RateLimitController.cs     # 속도 제한 (TTL 활용 3패턴)
```

---

## 권장 학습 순서

```
1단계: 이론                   2단계: 예제 서버               3단계: 실습 과제
┌─────────────────┐          ┌─────────────────┐          ┌─────────────────┐
│ docs/ 튜토리얼  │    →     │ RedisExampleServer│    →     │ challenge_      │
│ 01~06장 순서대로 │          │ 코드 분석 + 실행  │          │ problems.md     │
│ 읽기            │          │ apiTest.http 테스트│          │ 16개 과제 구현   │
└─────────────────┘          └─────────────────┘          └─────────────────┘
```

### 예제 서버와 튜토리얼의 대응 관계

| 컨트롤러 | Redis 구조 | 관련 튜토리얼 | 학습 포인트 |
|:---|:---|:---|:---|
| `AuthController` | `RedisString<T>` | 04장 | GET/SET, HasValue 패턴, TTL, 복합 객체 직렬화 |
| `ChatController` | `RedisList<T>` | 04장 | LeftPush, Range, Trim(크기 제한) |
| `LikeController` | `RedisSet<T>` | 04장 | Add/Remove, Contains, Members |
| `RankingController` | `RedisSortedSet<T>` | 04장 | Add(Score), RangeByRank, Rank |
| `LockController` | `RedisString` + `When.NotExists` | 04장 | 분산 락, NX 플래그, TTL 자동 해제 |
| `RateLimitController` | `RedisString` + TTL | 04장 | Increment, 카운터 TTL, 쿨다운 패턴 |

---

## Redis 설치 및 실행

### Docker (권장)

```bash
# Redis 서버 실행
docker run -d --name redis -p 6379:6379 redis:latest

# 정상 동작 확인
docker exec -it redis redis-cli ping
# 출력: PONG
```

### Windows (WSL2)

```bash
wsl
sudo apt update && sudo apt install redis-server
sudo service redis-server start
redis-cli ping
# 출력: PONG
```

Windows에서 WSL 없이 사용하려면 [Memurai](https://www.memurai.com/) (Redis 호환)를 설치할 수 있습니다.

### redis-cli 기본 명령어

설치 확인 및 학습에 유용한 기본 명령어입니다.

```bash
redis-cli                    # Redis CLI 접속

# String
SET mykey "hello"            # 키-값 저장
GET mykey                    # 값 조회 → "hello"
SET counter 0                # 카운터 초기화
INCR counter                 # 1 증가 → 1
SET token "abc" EX 60        # 60초 후 자동 만료

# List
LPUSH mylist "a" "b" "c"    # 왼쪽에 추가 → [c, b, a]
LRANGE mylist 0 -1           # 전체 조회

# Set
SADD myset "user1" "user2"  # 멤버 추가
SMEMBERS myset               # 전체 멤버 조회
SISMEMBER myset "user1"      # 멤버 여부 확인 → 1(있음)

# SortedSet
ZADD ranking 100 "player1"  # 점수와 함께 추가
ZREVRANGE ranking 0 9 WITHSCORES  # Top 10 (내림차순)
ZREVRANK ranking "player1"   # 순위 조회 (0-based)

# 키 관리
KEYS *                       # 모든 키 조회 (개발용, 운영 금지)
TTL mykey                    # 남은 만료 시간 확인 (-1: 만료 없음, -2: 키 없음)
DEL mykey                    # 키 삭제
FLUSHDB                      # 현재 DB의 모든 키 삭제
```

---

## 예제 서버 실행 및 테스트

### 사전 요구사항

- .NET 10.0 SDK
- Redis 서버 (`127.0.0.1:6379`에서 실행 중)

### 빌드 및 실행

```bash
cd redis/RedisExampleServer
dotnet build
dotnet run
# 서버 주소: http://localhost:11600
```

### API 테스트

VS Code의 [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) 확장 또는 JetBrains Rider에서 `apiTest.http` 파일을 열고 각 요청의 `Send Request`를 클릭합니다.

**권장 테스트 순서:**

1. **Auth** — 계정 생성 → 로그인 → 유저 데이터 저장/조회
2. **Chat** — 메시지 전송 2개 → 이력 조회
3. **Like** — 좋아요 토글 → 목록 → 수 → 취소
4. **Ranking** — 점수 5명 등록 → Top10 → 내 순위 → 주변 순위
5. **Lock** — 아이템 획득 → (3초 내) 동일 유저 재요청 → `AlreadyLocked` 확인
6. **RateLimit** — 닉네임 변경 4회(4번째 실패) → 일일 이벤트 2회(2번째 실패) → SMS 쿨다운

### CloudStructures 라이브러리

본 예제는 [CloudStructures](https://github.com/Cysharp/CloudStructures) 라이브러리를 사용합니다. CloudStructures는 `StackExchange.Redis`를 래핑하여 Redis의 각 데이터 구조를 C# 제네릭 클래스로 제공합니다.

```
CloudStructures 구조:
┌─────────────────────────────────────────┐
│            CloudStructures              │
│  RedisString<T>, RedisList<T>,          │
│  RedisSet<T>, RedisSortedSet<T> ...     │
├─────────────────────────────────────────┤
│          StackExchange.Redis            │
│  ConnectionMultiplexer, IDatabase ...   │
├─────────────────────────────────────────┤
│              Redis Server               │
└─────────────────────────────────────────┘
```

직접 `StackExchange.Redis`를 사용하면 `IDatabase.StringSetAsync(key, value)` 처럼 문자열 기반으로 작업하지만, CloudStructures를 사용하면 `RedisString<UserGameData>`처럼 **타입 안전하게** 작업할 수 있습니다.

## 기술 스택

- .NET 10.0
- CloudStructures 3.3.0 (Redis 클라이언트 추상화)
- StackExchange.Redis (CloudStructures 내부 사용)
