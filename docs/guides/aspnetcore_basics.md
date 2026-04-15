# ASP.NET Core 기초 개념

게임 API 서버를 이해하기 위해 알아야 할 ASP.NET Core의 3가지 핵심 개념을 설명한다.

---

## 1. 미들웨어 파이프라인

### 미들웨어란?

HTTP 요청이 컨트롤러에 도달하기 전에 **순서대로 통과하는 처리 단계**를 미들웨어라고 한다. 각 미들웨어는 요청을 검사하고, 다음 단계로 넘기거나 즉시 응답을 반환할 수 있다.

```
클라이언트 요청
    │
    ▼
┌──────────────────┐
│  VersionCheck    │  ← 앱 버전이 맞는지 확인. 틀리면 여기서 에러 응답.
└────────┬─────────┘
         │ (통과)
┌────────▼─────────┐
│  CheckUserAuth   │  ← 인증 토큰이 유효한지 확인. 틀리면 여기서 에러 응답.
└────────┬─────────┘
         │ (통과)
┌────────▼─────────┐
│   Controller     │  ← 비즈니스 로직 실행, 응답 생성
└────────┬─────────┘
         │
    ▼ 응답 반환
```

### Program.cs에서의 등록 순서

```csharp
// 미들웨어는 등록 순서대로 실행된다 (순서가 중요!)
app.UseMiddleware<VersionCheck>();                    // 1번째: 버전 체크
app.UseMiddleware<CheckUserAuthAndLoadUserData>();    // 2번째: 인증 + 유저 데이터 로드
app.UseRouting();                                     // 3번째: 라우팅
app.MapDefaultControllerRoute();                      // 4번째: 컨트롤러 매핑
```

> **참고 코드:** `codes/GameAPIServer_Template/Middleware/VersionCheck.cs`, `CheckUserAuth.cs`

### 미들웨어의 기본 구조

```csharp
public class VersionCheck
{
    readonly RequestDelegate _next;  // 다음 미들웨어를 호출하는 델리게이트

    public VersionCheck(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // 요청 검사 로직
        if (버전이_맞지_않으면)
        {
            context.Response.StatusCode = 426;
            return;  // 여기서 끊김 — 다음 미들웨어로 넘어가지 않음
        }

        await _next(context);  // 다음 미들웨어 호출
    }
}
```

### 게임 서버에서 자주 사용하는 미들웨어

| 미들웨어 | 역할 | 예시 |
|:---|:---|:---|
| **VersionCheck** | 클라이언트 앱 버전 확인 | 구버전 클라이언트 접속 차단 |
| **CheckUserAuth** | 인증 토큰 검증 | Redis에서 토큰 유효성 확인 |
| **RequestLock** | 동일 유저의 동시 요청 방지 | Redis 분산 락 |
| **UseHttpMetrics** | Prometheus 메트릭 수집 | 요청 수, 응답 시간 자동 기록 |

---

## 2. DI (의존성 주입)

### DI란?

클래스가 필요로 하는 객체(의존성)를 **직접 생성하지 않고, 외부에서 주입받는** 패턴이다.

```csharp
// ❌ DI 없이 (직접 생성) — 테스트/교체 어려움
public class LoginController
{
    readonly GameDb _db = new GameDb("connection-string");  // 직접 생성
}

// ✅ DI 사용 — 인터페이스로 받으므로 구현체 교체 가능
public class LoginController
{
    readonly IGameDb _db;

    public LoginController(IGameDb db)  // 생성자에서 주입받음
    {
        _db = db;
    }
}
```

### Program.cs에서의 DI 등록

```csharp
// Repository 계층 (DB 접근)
builder.Services.AddTransient<IGameDb, GameDb>();       // 요청마다 새 인스턴스
builder.Services.AddSingleton<IMemoryDb, MemoryDb>();   // 앱 전체에서 1개 공유
builder.Services.AddSingleton<IMasterDb, MasterDb>();   // 앱 전체에서 1개 공유

// Service 계층 (비즈니스 로직)
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IGameService, GameService>();
```

### 3가지 수명 주기

| 수명 | 메서드 | 언제 사용 | 게임 서버 예시 |
|:---|:---|:---|:---|
| **Transient** | `AddTransient` | 요청마다 새 인스턴스 | GameDb (DB 커넥션), Service 클래스 |
| **Singleton** | `AddSingleton` | 앱 전체에서 1개만 | Redis 연결(MemoryDb), 마스터 데이터 |
| **Scoped** | `AddScoped` | HTTP 요청 1개 동안 1개 | 트랜잭션이 필요한 DB 컨텍스트 |

> **핵심 규칙:** DB 연결처럼 상태가 있는 것은 `Transient`나 `Scoped`, Redis나 마스터 데이터처럼 공유해야 하는 것은 `Singleton`.

> **참고 코드:** `codes/GameAPIServer_Template/Program.cs`

---

## 3. 라우팅

### 라우팅이란?

HTTP 요청의 URL을 보고 **어떤 컨트롤러의 어떤 메서드를 실행할지** 결정하는 것이다.

### 어트리뷰트 라우팅

본 저장소의 모든 프로젝트는 어트리뷰트 라우팅을 사용한다.

```csharp
[ApiController]
[Route("[controller]")]          // URL: /Login
public class LoginController : ControllerBase
{
    [HttpPost]                    // POST /Login
    public async Task<LoginResponse> Login(LoginRequest request)
    {
        // ...
    }
}
```

| 어트리뷰트 | 의미 |
|:---|:---|
| `[Route("[controller]")]` | 클래스 이름에서 "Controller"를 뺀 것이 URL이 됨 (`LoginController` → `/Login`) |
| `[HttpPost]` | HTTP POST 메서드에 매핑 |
| `[HttpGet("detail")]` | GET /Login/detail 에 매핑 |
| `[FromBody]` | 요청 본문(JSON)을 파라미터로 바인딩 |

### 요청/응답 흐름

```
클라이언트:
  POST /Login
  Content-Type: application/json
  { "UserID": "test", "Password": "123" }

          ↓ ASP.NET Core가 자동으로:

  1) URL "/Login"  → LoginController 선택
  2) HTTP "POST"   → Login() 메서드 선택
  3) Body JSON     → LoginRequest 객체로 역직렬화
  4) 메서드 실행   → LoginResponse 객체 반환
  5) 응답 JSON     → { "Result": 0, "AuthToken": "abc..." }
```

---

## 다음 단계

이 기초 개념을 이해했다면:

1. `codes/GameAPIServer_Template/Program.cs`를 읽고 전체 구조를 파악한다
2. `docs/guides/project_structure.md`에서 Controller → Service → Repository 패턴을 학습한다
3. `docs/patterns/why_di.md`에서 DI를 더 깊이 이해한다
