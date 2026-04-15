# 게임 API 서버 프로젝트 구조 가이드

## 왜 계층을 나누는가?

모든 코드를 컨트롤러 하나에 넣으면 처음에는 간단하지만, 기능이 늘어날수록 문제가 생긴다.

```csharp
// ❌ 컨트롤러에 모든 로직이 있는 경우
[HttpPost]
public async Task<LoginResponse> Login(LoginRequest request)
{
    // 1) DB에서 계정 조회 (SQL 쿼리)
    // 2) 비밀번호 검증 (해시 비교)
    // 3) Redis에 토큰 저장
    // 4) 마스터 데이터 확인
    // 5) 응답 생성
    // → 200줄짜리 메서드가 됨
}
```

**문제점:**
- 컨트롤러가 비대해짐 (수천 줄)
- 같은 로직을 다른 컨트롤러에서 재사용 불가
- DB를 바꾸면 컨트롤러를 전부 수정해야 함
- 테스트 작성 불가능 (DB가 있어야 테스트 가능)

---

## Controller → Service → Repository 패턴

본 저장소의 모든 프로젝트는 **3계층 구조**를 따른다.

```
요청 → Controller → Service → Repository → DB/Redis
                                                ↓
응답 ← Controller ← Service ← Repository ← 결과
```

| 계층 | 역할 | 알아야 하는 것 | 모르는 것 |
|:---|:---|:---|:---|
| **Controller** | 요청 수신, 응답 반환 | HTTP, DTO | SQL, Redis 명령 |
| **Service** | 비즈니스 로직 | 게임 규칙, 검증 | SQL, HTTP |
| **Repository** | DB/Redis 접근 | SQL, Redis 명령 | HTTP, 게임 규칙 |

### 각 계층의 책임

#### Controller — "무엇을 요청했는지" 처리

```csharp
// Controllers/LoginController.cs
[HttpPost]
public async Task<LoginResponse> Login(LoginRequest request)
{
    var response = new LoginResponse();

    // Service에 위임 (구현 세부사항을 모름)
    var (errorCode, uid, token) = await _authService.Login(request.UserID, request.Password);

    response.Result = errorCode;
    response.AuthToken = token;
    return response;
}
```

**규칙:** 컨트롤러에 비즈니스 로직을 넣지 않는다. Service 메서드를 호출하고 결과를 응답에 담을 뿐이다.

#### Service — "어떤 규칙으로 처리하는지" 구현

```csharp
// Services/AuthService.cs
public async Task<(ErrorCode, long, string)> Login(string userId, string password)
{
    // 1) DB에서 계정 조회 (Repository에 위임)
    var account = await _gameDb.GetAccountByUserId(userId);
    if (account == null) return (ErrorCode.AccountNotFound, 0, "");

    // 2) 비밀번호 검증 (비즈니스 로직)
    if (Security.HashPassword(password, account.Salt) != account.PasswordHash)
        return (ErrorCode.InvalidPassword, 0, "");

    // 3) 토큰 생성 및 Redis 저장 (Repository에 위임)
    var token = Security.CreateAuthToken();
    await _memoryDb.SetAuthToken(account.Uid, token);

    return (ErrorCode.None, account.Uid, token);
}
```

**규칙:** SQL 쿼리나 Redis 명령을 직접 실행하지 않는다. Repository를 통해 데이터에 접근한다.

#### Repository — "어떻게 저장/조회하는지" 구현

```csharp
// Repository/GameDb.cs
public async Task<Account?> GetAccountByUserId(string userId)
{
    using var connection = await GetConnection();
    return await connection.QueryFactory()
        .Query("account")
        .Where("user_id", userId)
        .FirstOrDefaultAsync<Account>();
}
```

**규칙:** 비즈니스 규칙을 판단하지 않는다. 데이터를 저장/조회/삭제하는 것만 담당한다.

---

## 실제 프로젝트 디렉토리 구조

```
GameAPIServer/
├── Program.cs                 # 앱 진입점 (DI 등록, 미들웨어 설정)
├── appsettings.json           # DB 접속 정보, 서버 포트 등 설정
├── ErrorCode.cs               # 에러코드 enum (클라이언트와 약속된 코드)
│
├── Controllers/               # [1계층] 요청 수신 → Service 호출 → 응답 반환
│   ├── LoginController.cs
│   ├── MailListController.cs
│   └── ...
│
├── Services/                  # [2계층] 비즈니스 로직 (게임 규칙, 검증)
│   ├── Interfaces/            #   인터페이스 (DI를 위해 분리)
│   │   ├── IAuthService.cs
│   │   └── ...
│   ├── AuthService.cs
│   └── ...
│
├── Repository/                # [3계층] DB/Redis 접근 (SQL 쿼리, Redis 명령)
│   ├── Interfaces/
│   │   ├── IGameDb.cs         #   MySQL 게임 DB
│   │   └── IMemoryDb.cs       #   Redis 캐시
│   ├── GameDb.cs
│   └── MemoryDb.cs
│
├── Models/                    # 데이터 모델
│   ├── DAO/                   #   DB 테이블과 1:1 매핑되는 모델
│   │   ├── Account.cs
│   │   └── ...
│   └── DTO/                   #   클라이언트 요청/응답 모델
│       ├── Login.cs           #   LoginRequest, LoginResponse
│       └── ...
│
├── Middleware/                 # 미들웨어 (요청 전처리)
│   ├── VersionCheck.cs
│   └── CheckUserAuth.cs
│
└── DB_Schema.md               # DB 테이블 스키마 문서
```

### DAO vs DTO

| 구분 | DAO (Data Access Object) | DTO (Data Transfer Object) |
|:---|:---|:---|
| **위치** | `Models/DAO/` | `Models/DTO/` |
| **용도** | DB 테이블의 행(row)을 C# 객체로 매핑 | 클라이언트와 주고받는 요청/응답 데이터 |
| **예시** | `Account { Uid, UserId, Salt, PwHash }` | `LoginRequest { UserID, Password }` |
| **규칙** | DB 컬럼과 1:1 대응, 비밀번호 해시 포함 가능 | 클라이언트에 노출해도 되는 데이터만 포함 |

---

## 왜 Interface를 사용하는가?

```
Services/
├── Interfaces/
│   └── IAuthService.cs    ← 인터페이스 (계약)
└── AuthService.cs          ← 구현체
```

**이유:** 테스트 시 실제 DB 없이 FakeGameDb로 교체할 수 있다.

```csharp
// 실제 운영
builder.Services.AddTransient<IGameDb, GameDb>();       // MySQL 사용

// 테스트/개발 (DB 없이)
builder.Services.AddTransient<IGameDb, FakeGameDb>();   // Mock 사용
```

> **참고 코드:** `codes/GameAPIServer_Template/Repository/FakeGameDb.cs`는 모든 메서드가 성공을 반환하는 Mock 구현이다. DB 없이도 서버를 실행할 수 있게 해준다.

---

## 참고

- 디렉토리 구조 예시: `docs/references/APIServer_Directory.md`
- DI 패턴 설명: `docs/patterns/why_di.md`
- ASP.NET Core 기초: `docs/guides/aspnetcore_basics.md`
