# 게임 API 서버 보안 가이드

게임 서버는 재화, 아이템, 결제 등 금전적 가치가 있는 데이터를 다루기 때문에 보안이 특히 중요하다.
이 문서는 ASP.NET Core 게임 API 서버에서 반드시 적용해야 하는 보안 기법을 다룬다.

---

## 목차

1. [토큰 기반 인증 설계](#1-토큰-기반-인증-설계)
2. [입력값 검증](#2-입력값-검증)
3. [클라이언트 위조 요청 방어](#3-클라이언트-위조-요청-방어)
4. [API Rate Limiting](#4-api-rate-limiting)
5. [HTTPS 강제 전환](#5-https-강제-전환)
6. [SQL Injection 방지](#6-sql-injection-방지)
7. [민감 정보 관리](#7-민감-정보-관리)
8. [보안 체크리스트](#8-보안-체크리스트)

---

## 1. 토큰 기반 인증 설계

### 왜 토큰인가?

HTTP는 Stateless다. 매 요청마다 "이 요청이 누구에게서 왔는가"를 증명해야 한다.
세션 쿠키 방식은 서버가 상태를 들고 있어야 해서 수평 확장(Scale-out)이 어렵다.
**토큰 방식은 Redis에 저장하고 어느 서버에서도 검증할 수 있어서 멀티 서버 환경에 적합하다.**

### 이 프로젝트의 토큰 흐름

```
[클라이언트]
    │ 1. 로그인 요청 (ID + Password)
    ▼
[Hive Server]
    │ 2. 계정 검증 후 HiveToken 발급 (메모리 또는 Redis 저장)
    ▼
[클라이언트]
    │ 3. HiveToken으로 게임 서버에 로그인 요청
    ▼
[Game Server]
    │ 4. Hive Server에 토큰 검증 요청 (서버 간 HTTP 통신)
    │ 5. 검증 성공 → 게임 서버용 AuthToken 발급, Redis 저장
    ▼
[클라이언트]
    │ 6. 이후 모든 요청에 AuthToken 포함 (쿠키 or 헤더)
    ▼
[Game Server Middleware]
    │ 7. 매 요청마다 Redis에서 AuthToken 검증
```

### AuthToken 발급 및 저장

```csharp
// AuthService.cs
public async Task<(ErrorCode, string)> Login(Int64 playerId, string hiveToken)
{
    // 1. Hive 서버에 토큰 검증 요청
    var (errorCode, playerInfo) = await _hiveService.VerifyToken(playerId, hiveToken);
    if (errorCode != ErrorCode.None)
    {
        return (errorCode, null);
    }

    // 2. 게임 서버 AuthToken 생성
    //    GUID 사용: 예측 불가능하고 충돌 확률이 극히 낮다
    string authToken = Guid.NewGuid().ToString();

    // 3. Redis에 저장 (TTL = 세션 유효 시간)
    var sessionData = new UserSession
    {
        PlayerId = playerId,
        AuthToken = authToken,
        LoginTime = DateTime.UtcNow
    };

    var result = await _memoryDb.StoreUserSession(playerId, sessionData);
    if (result != ErrorCode.None)
    {
        return (ErrorCode.LoginFailStoreSessionError, null);
    }

    return (ErrorCode.None, authToken);
}
```

### 토큰 검증 미들웨어

```csharp
// Middleware/CheckUserAuth.cs
public class CheckUserAuth
{
    readonly RequestDelegate _next;
    readonly IMemoryDb _memoryDb;

    // 인증이 필요 없는 엔드포인트 목록
    static readonly HashSet<string> s_skipAuthPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/CreateAccount",
        "/Login",
    };

    public CheckUserAuth(RequestDelegate next, IMemoryDb memoryDb)
    {
        _next = next;
        _memoryDb = memoryDb;
    }

    public async Task Invoke(HttpContext context)
    {
        // 인증 불필요 경로는 건너뜀
        string path = context.Request.Path.Value ?? "";
        if (s_skipAuthPaths.Contains(path))
        {
            await _next(context);
            return;
        }

        // 쿠키에서 토큰 추출
        if (!context.Request.Cookies.TryGetValue("AuthToken", out string? authToken) ||
            !context.Request.Cookies.TryGetValue("PlayerId", out string? playerIdStr))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!Int64.TryParse(playerIdStr, out Int64 playerId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Redis에서 세션 검증
        var (errorCode, session) = await _memoryDb.GetUserSession(playerId);
        if (errorCode != ErrorCode.None || session == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // 토큰 일치 검증
        //   string.Equals 대신 CryptographicOperations.FixedTimeEquals를 쓰면
        //   타이밍 공격(Timing Attack)을 방어할 수 있다
        if (!string.Equals(session.AuthToken, authToken, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // 검증 성공 → 이후 컨트롤러에서 꺼내 쓸 수 있도록 Items에 저장
        context.Items["PlayerId"] = playerId;
        context.Items["UserSession"] = session;

        await _next(context);
    }
}
```

### 보안 강화 포인트

| 항목 | 나쁜 예 | 좋은 예 |
|:---|:---|:---|
| 토큰 생성 | `"token_" + userId` | `Guid.NewGuid().ToString()` |
| 토큰 비교 | `token1 == token2` | `CryptographicOperations.FixedTimeEquals(...)` |
| 토큰 전달 | URL 파라미터 `?token=...` | 쿠키(HttpOnly) 또는 Authorization 헤더 |
| 토큰 만료 | 만료 없음 | Redis TTL로 자동 만료 (예: 2시간) |

---

## 2. 입력값 검증

### 왜 서버에서도 검증해야 하는가?

클라이언트(앱, 게임 클라이언트)는 누구든지 수정할 수 있다.
Fiddler, Burp Suite 같은 도구로 패킷을 변조하면 클라이언트 검증은 완전히 무력화된다.
**서버는 클라이언트를 절대 신뢰하지 않는다.**

### FluentValidation 적용

FluentValidation은 요청 DTO 검증 로직을 선언적으로 작성할 수 있게 해준다.

```bash
dotnet add package FluentValidation.AspNetCore
```

```csharp
// DTOs/CreateAccount.cs
public class CreateAccountRequest
{
    public string UserID { get; set; } = "";
    public string Password { get; set; } = "";
    public string Nickname { get; set; } = "";
}

// Validators/CreateAccountValidator.cs
using FluentValidation;

public class CreateAccountValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.UserID)
            .NotEmpty().WithMessage("UserID는 필수입니다")
            .Length(4, 20).WithMessage("UserID는 4~20자여야 합니다")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("UserID는 영문, 숫자, 언더스코어만 허용됩니다");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password는 필수입니다")
            .MinimumLength(8).WithMessage("Password는 최소 8자 이상이어야 합니다")
            .Matches("[A-Z]").WithMessage("대문자를 포함해야 합니다")
            .Matches("[0-9]").WithMessage("숫자를 포함해야 합니다");

        RuleFor(x => x.Nickname)
            .NotEmpty()
            .Length(2, 12).WithMessage("닉네임은 2~12자여야 합니다")
            .Matches("^[가-힣a-zA-Z0-9]+$").WithMessage("닉네임에 특수문자는 허용되지 않습니다");
    }
}
```

```csharp
// Program.cs
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateAccountValidator>();
```

```csharp
// Controllers/CreateAccountController.cs
[ApiController]
[Route("[controller]")]
public class CreateAccountController : ControllerBase
{
    [HttpPost]
    public async Task<CreateAccountResponse> Post([FromBody] CreateAccountRequest request)
    {
        // FluentValidation이 자동으로 검증하고 400 Bad Request를 반환한다
        // 여기에 도달했다면 이미 검증 통과
        var errorCode = await _authService.CreateAccount(request.UserID, request.Password, request.Nickname);
        return new CreateAccountResponse { Result = errorCode };
    }
}
```

### 게임 서버 특화 검증 항목

```csharp
// 아이템 구매 요청 검증
public class BuyItemValidator : AbstractValidator<BuyItemRequest>
{
    public BuyItemValidator(IMasterDb masterDb)
    {
        // 수량은 반드시 양수
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("구매 수량은 1 이상이어야 합니다")
            .LessThanOrEqualTo(99).WithMessage("한 번에 99개 이상 구매할 수 없습니다");

        // 아이템 코드가 MasterDB에 실제로 존재하는지 검증
        RuleFor(x => x.ItemCode)
            .Must(itemCode => masterDb.GetShopItem(itemCode) != null)
            .WithMessage("존재하지 않는 아이템입니다");
    }
}
```

---

## 3. 클라이언트 위조 요청 방어

### 문제: 클라이언트는 서버를 속이려 한다

게임에서 발생하는 대표적인 위조 공격 유형이다.

| 공격 유형 | 내용 | 방어 방법 |
|:---|:---|:---|
| **파라미터 변조** | 아이템 가격을 0으로 변조해서 무료 구매 | 서버에서 MasterDB 가격으로 재계산 |
| **타인 데이터 접근** | 다른 유저의 uid로 요청 | 쿠키의 인증 토큰에서 uid 추출, 파라미터 uid는 무시 |
| **중복 요청** | 보상을 두 번 받기 위해 같은 요청 반복 | Redis 분산 Lock + 처리 결과 멱등성 보장 |
| **속도 위조** | 타이머를 조작해서 쿨타임 무시 | 서버 시간 기준으로 쿨타임 검증 |

### 타인 데이터 접근 방어

```csharp
// 나쁜 예: 요청 바디의 uid를 그대로 사용
[HttpPost]
public async Task<GetInventoryResponse> GetInventory([FromBody] GetInventoryRequest request)
{
    // request.Uid가 변조되면 다른 유저 데이터를 볼 수 있다 ← 위험!
    var items = await _gameDb.GetInventory(request.Uid);
    ...
}

// 좋은 예: 인증된 PlayerId를 미들웨어에서 꺼내 사용
[HttpPost]
public async Task<GetInventoryResponse> GetInventory()
{
    // 미들웨어가 검증하고 저장한 PlayerId만 사용
    Int64 playerId = (Int64)HttpContext.Items["PlayerId"]!;
    var items = await _gameDb.GetInventory(playerId);
    ...
}
```

### 서버 시간 기준 검증

```csharp
// 나쁜 예: 클라이언트가 보낸 시간을 신뢰
public async Task<ErrorCode> UseItem(Int64 playerId, int itemCode, DateTime clientTime)
{
    // clientTime은 변조 가능 ← 위험!
    if (clientTime < lastUseTime.AddSeconds(cooltime))
        return ErrorCode.ItemCoolTimeNotExpired;
    ...
}

// 좋은 예: 서버 시간으로 쿨타임 검증
public async Task<ErrorCode> UseItem(Int64 playerId, int itemCode)
{
    DateTime serverNow = DateTime.UtcNow; // 서버 시간 사용
    var lastUseTime = await _memoryDb.GetItemLastUseTime(playerId, itemCode);

    if (serverNow < lastUseTime.AddSeconds(GetCooltime(itemCode)))
        return ErrorCode.ItemCoolTimeNotExpired;

    await _memoryDb.SetItemLastUseTime(playerId, itemCode, serverNow);
    ...
}
```

### 결제/보상 금액은 반드시 서버에서 계산

```csharp
// 나쁜 예: 클라이언트가 보낸 가격을 사용
public async Task<ErrorCode> BuyItem(Int64 playerId, int itemCode, int clientPrice)
{
    // clientPrice를 변조하면 0원에 구매 가능 ← 절대 안 됨!
    await _gameDb.DeductMoney(playerId, clientPrice);
    ...
}

// 좋은 예: MasterDB에서 서버가 가격을 직접 조회
public async Task<ErrorCode> BuyItem(Int64 playerId, int itemCode)
{
    // 기획 데이터(MasterDB)에서 실제 가격 조회
    var shopItem = _masterDb.GetShopItem(itemCode);
    if (shopItem == null)
        return ErrorCode.ShopItemNotFound;

    // 서버가 계산한 가격으로 차감
    var errorCode = await _gameDb.DeductMoney(playerId, shopItem.Price);
    if (errorCode != ErrorCode.None)
        return ErrorCode.ShopInsufficientMoney;

    await _gameDb.AddItem(playerId, itemCode, 1);
    return ErrorCode.None;
}
```

---

## 4. API Rate Limiting

### 왜 필요한가?

- 특정 API(로그인, 회원가입)를 무한 반복 호출하는 브루트포스 공격을 막는다
- 서버 자원을 과도하게 소모하는 비정상 클라이언트를 차단한다

### ASP.NET Core 내장 Rate Limiter 사용 (.NET 7+)

```csharp
// Program.cs
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    // 로그인 API: IP 기준 1분에 10회
    options.AddFixedWindowLimiter("LoginPolicy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10;
        opt.QueueLimit = 0; // 초과 요청은 즉시 거부
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // 일반 API: IP 기준 1초에 30회
    options.AddSlidingWindowLimiter("DefaultPolicy", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(1);
        opt.SegmentsPerWindow = 2;
        opt.PermitLimit = 30;
        opt.QueueLimit = 0;
    });

    // 제한 초과 시 응답
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { Result = ErrorCode.TooManyRequests },
            cancellationToken);
    };
});

app.UseRateLimiter();
```

```csharp
// Controllers/LoginController.cs
[ApiController]
[Route("[controller]")]
[EnableRateLimiting("LoginPolicy")] // 컨트롤러 전체에 적용
public class LoginController : ControllerBase
{
    ...
}
```

---

## 5. HTTPS 강제 전환

운영 환경에서는 모든 트래픽을 HTTPS로 강제한다.

```csharp
// Program.cs
if (!app.Environment.IsDevelopment())
{
    // HTTP로 들어온 요청을 HTTPS로 리다이렉트
    app.UseHttpsRedirection();

    // HSTS: 브라우저에게 "이 사이트는 항상 HTTPS만 사용한다"고 알림
    app.UseHsts();
}
```

```json
// appsettings.Production.json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:80"
      },
      "Https": {
        "Url": "https://0.0.0.0:443",
        "Certificate": {
          "Path": "/certs/server.crt",
          "KeyPath": "/certs/server.key"
        }
      }
    }
  }
}
```

---

## 6. SQL Injection 방지

### SqlKata는 기본적으로 파라미터 바인딩을 사용한다

```csharp
// SqlKata 사용 시 자동으로 파라미터 바인딩이 된다 → SQL Injection 방어됨
var user = await _db.Query("Users")
    .Where("user_id", userId)        // userId가 변조되어도 안전
    .Where("password", hashedPw)
    .FirstOrDefaultAsync<UserInfo>();

// 위 코드가 실제로 생성하는 SQL:
// SELECT * FROM Users WHERE user_id = @p0 AND password = @p1
// @p0, @p1은 별도 파라미터로 전달 → SQL Injection 불가
```

### Raw SQL 사용 시 주의

```csharp
// 나쁜 예: 문자열 직접 연결 → SQL Injection 취약
string sql = $"SELECT * FROM Users WHERE user_id = '{userId}'";
// userId = "' OR '1'='1" 이면 모든 유저 데이터가 노출됨

// 좋은 예: SqlKata Raw + 파라미터 바인딩
var user = await _db.FromRaw(
    "SELECT * FROM Users WHERE user_id = ?",
    new object[] { userId }
).FirstOrDefaultAsync<UserInfo>();
```

---

## 7. 민감 정보 관리

### appsettings.json에 비밀번호를 넣지 않는다

```json
// 나쁜 예: appsettings.json (Git에 올라가면 비밀번호 노출)
{
  "ConnectionStrings": {
    "GameDb": "Server=db;Database=game;User=root;Password=MySecretPassword123;"
  }
}
```

```bash
# 좋은 예 1: 환경변수로 주입 (Docker, 서버 배포 환경)
export ConnectionStrings__GameDb="Server=db;Database=game;User=root;Password=MySecretPassword123;"
```

```bash
# 좋은 예 2: User Secrets (로컬 개발 환경)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:GameDb" "Server=localhost;Database=game;User=root;Password=dev_password;"
```

```csharp
// Program.cs - 환경변수가 appsettings.json보다 우선순위가 높다
// ASP.NET Core는 기본적으로 아래 순서로 설정을 읽는다:
// 1. appsettings.json
// 2. appsettings.{Environment}.json
// 3. User Secrets (Development 환경에서만)
// 4. 환경변수 ← 가장 높은 우선순위
// 별도 코드 없이 자동 처리됨
var app = builder.Build();
```

### 비밀번호 해싱

```csharp
// Security.cs - 단방향 해시 사용
public static class Security
{
    const string SaltKey = "YOUR_SALT_KEY"; // 실제로는 환경변수에서 읽는다

    // SHA256으로 해싱 (게임 서버에서 일반적으로 사용)
    public static string MakeHashPassword(string password)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(SaltKey + password);
        byte[] hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes);
    }
}

// 사용 예시
string hashedPw = Security.MakeHashPassword(request.Password);
var user = await _db.Query("Users")
    .Where("user_id", request.UserID)
    .Where("hashed_pw", hashedPw)
    .FirstOrDefaultAsync<UserInfo>();
```

> **참고**: 높은 보안이 요구되는 환경(결제 연동 등)에서는 SHA256보다 BCrypt나 Argon2 사용을 권장한다.

---

## 8. 보안 체크리스트

서버 배포 전 반드시 확인해야 할 항목이다.

### 인증/인가
- [ ] 모든 API 엔드포인트에 인증 미들웨어가 적용되어 있는가?
- [ ] 인증 불필요 엔드포인트 목록이 명시적으로 관리되는가?
- [ ] 토큰은 예측 불가능한 방식(GUID 등)으로 생성하는가?
- [ ] 토큰에 만료 시간(TTL)이 설정되어 있는가?
- [ ] 쿠키에 `HttpOnly` 플래그가 설정되어 있는가?

### 입력값 검증
- [ ] 모든 요청 DTO에 검증 로직이 적용되어 있는가?
- [ ] 수량, 가격 등 숫자 필드에 범위 검증이 있는가?
- [ ] 아이템 코드, 스테이지 ID 등이 MasterDB에 실제로 존재하는지 검증하는가?

### 게임 로직 보안
- [ ] 금액/가격 계산을 서버에서 MasterDB 기준으로 처리하는가?
- [ ] PlayerId를 요청 바디에서 받지 않고 인증 세션에서 추출하는가?
- [ ] 쿨타임, 시간 검증을 서버 시간(UTC) 기준으로 처리하는가?

### 인프라
- [ ] DB 비밀번호가 appsettings.json에 하드코딩되어 있지 않은가?
- [ ] 운영 환경에서 HTTPS가 강제 적용되는가?
- [ ] Rate Limiting이 로그인, 회원가입 등 민감 API에 적용되어 있는가?
- [ ] SQL Injection 위험이 있는 Raw SQL 사용 부분에 파라미터 바인딩이 적용되어 있는가?
