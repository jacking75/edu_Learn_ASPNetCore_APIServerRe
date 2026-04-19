# 게임 API 서버 핵심 패턴

ASP.NET Core 게임 API 서버에서 반복적으로 사용되는 핵심 설계 패턴 4가지를 다룬다.
이 패턴들은 일반 웹 서버와 달리 게임 서버만의 특수한 요구사항에서 비롯된다.

---

## 목차

1. [유저 요청 Lock 패턴](#1-유저-요청-lock-패턴)
2. [MasterDB 로딩 패턴](#2-masterdb-로딩-패턴)
3. [롱폴링 패턴](#3-롱폴링-패턴)
4. [세션 관리 패턴](#4-세션-관리-패턴)

---

## 1. 유저 요청 Lock 패턴

### 왜 필요한가?

게임 클라이언트는 네트워크 지연, 버그, 혹은 의도적인 공격으로 인해 같은 요청을 동시에 여러 번 보낼 수 있다.

**구체적인 문제 상황:**

```
[클라이언트] ──(보상 받기)──▶ [서버A] 
[클라이언트] ──(보상 받기)──▶ [서버B]  ← 동시에 두 서버에 요청이 도달

서버A: 보상 지급 완료를 DB에 쓰기 전에 서버B도 "아직 미수령"으로 읽음
→ 보상이 두 번 지급됨 (중복 보상 버그)
```

이 문제는 단일 서버에서도 발생하고, 멀티 서버 환경에서는 더 빈번하다.
**Redis의 원자적(Atomic) 연산을 사용한 분산 Lock으로 해결한다.**

### Redis SET NX (Not eXist) 원리

```
SET key value NX EX 30
```
- `NX`: 키가 없을 때만 설정 성공 → 원자적으로 Lock 획득
- `EX 30`: 30초 후 자동 만료 → 서버 크래시 시에도 Lock이 영구 잠기지 않음

### 구현

```csharp
// Repository/MemoryDbKeyMaker.cs
public static class MemoryDbKeyMaker
{
    // 유저별 Lock 키
    public static string MakeUserLockKey(Int64 playerId)
        => $"ULock_{playerId}";
}

// Repository/MemoryDb.cs
public class MemoryDb : IMemoryDb
{
    readonly RedisConnection _redisConn;

    // Lock 획득 시도
    public async Task<bool> LockUserRequest(Int64 playerId)
    {
        string key = MemoryDbKeyMaker.MakeUserLockKey(playerId);

        // SET NX: 키가 없을 때만 성공 (원자적 연산)
        var result = await _redisConn.GetRedisString<string>(key)
            .SetAsync("1", expiry: TimeSpan.FromSeconds(5), when: When.NotExists);

        return result; // true = Lock 획득 성공, false = 이미 다른 요청이 처리 중
    }

    // Lock 해제
    public async Task UnlockUserRequest(Int64 playerId)
    {
        string key = MemoryDbKeyMaker.MakeUserLockKey(playerId);
        await _redisConn.GetRedisString<string>(key).DeleteAsync();
    }
}
```

### 미들웨어로 자동 적용

모든 API에 개별적으로 Lock 코드를 넣는 것은 실수가 생기기 쉽다.
미들웨어에서 인증 후 자동으로 Lock을 걸고 응답 후 자동으로 해제한다.

```csharp
// Middleware/CheckUserAuth.cs (유저 인증 + Lock 통합)
public class CheckUserAuth
{
    readonly RequestDelegate _next;
    readonly IMemoryDb _memoryDb;
    readonly ILogger<CheckUserAuth> _logger;

    static readonly HashSet<string> s_skipAuthPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/CreateAccount",
        "/Login",
    };

    public async Task Invoke(HttpContext context)
    {
        string path = context.Request.Path.Value ?? "";
        if (s_skipAuthPaths.Contains(path))
        {
            await _next(context);
            return;
        }

        // [1] 인증 검증
        if (!TryGetAuthInfo(context, out Int64 playerId, out string authToken))
        {
            context.Response.StatusCode = 401;
            return;
        }

        var (errorCode, session) = await _memoryDb.GetUserSession(playerId);
        if (errorCode != ErrorCode.None || session?.AuthToken != authToken)
        {
            context.Response.StatusCode = 401;
            return;
        }

        // [2] 유저 Lock 획득
        bool lockAcquired = await _memoryDb.LockUserRequest(playerId);
        if (!lockAcquired)
        {
            // 이미 처리 중인 요청이 있음
            await context.Response.WriteAsJsonAsync(
                new { Result = ErrorCode.AuthTokenAlreadyInUse });
            return;
        }

        try
        {
            context.Items["PlayerId"] = playerId;
            await _next(context); // 실제 컨트롤러 처리
        }
        finally
        {
            // [3] Lock 해제 (예외 발생 여부와 관계없이 반드시 해제)
            await _memoryDb.UnlockUserRequest(playerId);
        }
    }
}
```

### 서버 크래시 시 자동 복구

```
[서버 크래시 상황]
1. 유저가 요청을 보냄
2. 서버가 Lock 획득 (Redis에 키 생성, TTL=5초)
3. 처리 중 서버 크래시 발생
4. Lock 해제 코드가 실행되지 않음
5. ← TTL이 지나면 Redis가 자동으로 키를 삭제
6. 5초 후 유저는 다시 요청 가능
```

TTL을 너무 길게 설정하면 크래시 후 복구 시간이 길어지고, 너무 짧게 설정하면 정상적인 느린 요청도 Lock이 만료될 수 있다.
**API 최대 처리 시간의 2~3배를 TTL로 설정하는 것이 적절하다.**

---

## 2. MasterDB 로딩 패턴

### MasterDB란?

기획자가 작성하는 **게임 밸런스 데이터**다. 아이템 정보, 스테이지 정보, 보상 테이블, 레벨업 경험치 등이 여기에 해당한다.

| 구분 | GameDB | MasterDB |
|:---|:---|:---|
| 내용 | 유저 데이터 (레벨, 아이템, 재화 등) | 기획 데이터 (아이템 정보, 가격, 밸런스) |
| 변경 주기 | 실시간으로 변경됨 | 게임 업데이트 시에만 변경됨 |
| 저장소 | MySQL (영구 저장) | MySQL (읽기만 함) |
| 접근 방법 | 매 요청마다 DB 조회 | 서버 시작 시 메모리에 전체 로드 |

### 왜 메모리에 올리는가?

```
[MasterDB를 매번 DB에서 조회하는 경우]
요청 1: 아이템 구매 → DB에서 아이템 가격 조회 → 처리
요청 2: 아이템 구매 → DB에서 아이템 가격 조회 → 처리
요청 3: 아이템 구매 → DB에서 아이템 가격 조회 → 처리
→ 아이템 가격은 변하지 않는 데이터인데 DB를 계속 조회하는 것은 낭비

[MasterDB를 메모리에 올려두는 경우]
서버 시작 시: DB에서 아이템 전체 데이터 1회 조회 → Dictionary에 저장
요청 1: Dictionary에서 O(1) 조회 → 처리
요청 2: Dictionary에서 O(1) 조회 → 처리
→ DB 부하 0, 응답 속도 대폭 향상
```

### 구현

```csharp
// Models/MasterData.cs - 기획 데이터 모델
public class MasterItem
{
    public int ItemCode { get; set; }
    public string ItemName { get; set; } = "";
    public int ItemType { get; set; }    // 1: 소비, 2: 장비, 3: 재화
    public int MaxStack { get; set; }    // 최대 보유 수량
    public int SellPrice { get; set; }  // 판매 가격
}

public class MasterShopItem
{
    public int ShopItemId { get; set; }
    public int ItemCode { get; set; }
    public int Price { get; set; }
    public int Quantity { get; set; }
}

public class MasterAttendanceReward
{
    public int AttendanceDay { get; set; }  // 출석 일수
    public int RewardItemCode { get; set; }
    public int RewardQuantity { get; set; }
}
```

```csharp
// Repository/MasterDb.cs
public class MasterDb : IMasterDb
{
    // Dictionary에 미리 올려두면 O(1) 조회
    Dictionary<int, MasterItem> _items = new();
    Dictionary<int, MasterShopItem> _shopItems = new();
    Dictionary<int, MasterAttendanceReward> _attendanceRewards = new();

    readonly ILogger<MasterDb> _logger;
    readonly IConfiguration _config;

    public MasterDb(ILogger<MasterDb> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    // 서버 시작 시 1회 호출
    public async Task<bool> LoadAllMasterDataAsync()
    {
        try
        {
            string connStr = _config.GetConnectionString("MasterDb")!;
            using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            var db = new QueryFactory(conn, new MySqlCompiler());

            // 아이템 데이터 로드
            var items = await db.Query("master_item").GetAsync<MasterItem>();
            _items = items.ToDictionary(x => x.ItemCode);

            // 상점 데이터 로드
            var shopItems = await db.Query("master_shop").GetAsync<MasterShopItem>();
            _shopItems = shopItems.ToDictionary(x => x.ShopItemId);

            // 출석 보상 데이터 로드
            var rewards = await db.Query("master_attendance_reward").GetAsync<MasterAttendanceReward>();
            _attendanceRewards = rewards.ToDictionary(x => x.AttendanceDay);

            _logger.ZLogInformation(
                $"MasterDB 로드 완료: Items={_items.Count}, Shop={_shopItems.Count}, Attendance={_attendanceRewards.Count}");

            return true;
        }
        catch (Exception e)
        {
            _logger.ZLogCritical(e, $"MasterDB 로드 실패");
            return false;
        }
    }

    // 조회 메서드 (O(1))
    public MasterItem? GetItem(int itemCode)
        => _items.GetValueOrDefault(itemCode);

    public MasterShopItem? GetShopItem(int shopItemId)
        => _shopItems.GetValueOrDefault(shopItemId);

    public MasterAttendanceReward? GetAttendanceReward(int day)
        => _attendanceRewards.GetValueOrDefault(day);

    // 전체 목록이 필요한 경우 Values로 반환
    public IEnumerable<MasterShopItem> GetAllShopItems()
        => _shopItems.Values;
}
```

```csharp
// Program.cs - 서버 시작 시 MasterDB 로드
var app = builder.Build();

// 서버가 요청을 받기 전에 MasterDB를 메모리에 올린다
var masterDb = app.Services.GetRequiredService<IMasterDb>();
bool loaded = await masterDb.LoadAllMasterDataAsync();
if (!loaded)
{
    // MasterDB 로드 실패 시 서버 시작을 중단한다
    // 기획 데이터 없이 서버가 동작하면 더 큰 문제가 생긴다
    throw new Exception("MasterDB 로드 실패. 서버를 시작할 수 없습니다.");
}

app.Run();
```

```csharp
// DI 등록 시 Singleton으로 등록 (서버 전체에서 하나의 인스턴스 공유)
builder.Services.AddSingleton<IMasterDb, MasterDb>();
// Transient이나 Scoped로 등록하면 요청마다 새 인스턴스가 생성되어 메모리에서 다시 로드됨 ← 잘못된 사용
```

### 데이터 버전 관리

MasterDB는 게임 업데이트 시 변경된다.
클라이언트가 구버전 기획 데이터를 가지고 있으면 서버와 불일치가 발생한다.

```csharp
// 버전 체크 미들웨어
public class VersionCheck
{
    readonly RequestDelegate _next;
    readonly IConfiguration _config;

    public async Task Invoke(HttpContext context)
    {
        // 클라이언트가 보낸 데이터 버전
        if (!context.Request.Headers.TryGetValue("DataVersion", out var clientVersionStr))
        {
            await context.Response.WriteAsJsonAsync(new { Result = ErrorCode.MismatchDataVersion });
            return;
        }

        int clientVersion = int.Parse(clientVersionStr!);
        int serverVersion = _config.GetValue<int>("DataVersion");

        if (clientVersion != serverVersion)
        {
            // 클라이언트에게 강제 업데이트 신호를 보낸다
            await context.Response.WriteAsJsonAsync(new { Result = ErrorCode.MismatchDataVersion });
            return;
        }

        await _next(context);
    }
}
```

---

## 3. 롱폴링 패턴

### 왜 롱폴링인가?

실시간 게임 상태(매칭 완료, 상대방의 돌 두기 등)를 클라이언트에 전달하는 방법은 여러 가지다.

| 방법 | 장점 | 단점 | 적합한 경우 |
|:---|:---|:---|:---|
| **짧은 폴링** | 구현 단순 | 불필요한 요청 폭발 | 변경이 드문 데이터 |
| **롱폴링** | HTTP만으로 준실시간 구현 | 서버 연결 점유 | 변경 빈도가 낮고 WebSocket이 불가한 경우 |
| **WebSocket** | 진짜 실시간, 양방향 | 구현 복잡, 연결 관리 필요 | 실시간 전투, 채팅 |
| **SSE** | 서버→클라이언트 단방향 실시간 | 단방향만 가능 | 알림, 공지 |

**롱폴링**: 클라이언트가 요청을 보내면 서버는 변화가 생길 때까지 응답을 보류하다가, 변화가 생기거나 타임아웃이 되면 응답한다.

### 오목 게임 롱폴링 예시

```
[매칭 완료 확인 롱폴링]

클라이언트 ──(매칭 시작 요청)──▶ 매칭 서버
클라이언트 ──(매칭 확인 요청)──▶ 게임 서버

게임 서버: "아직 매칭 안 됨, 잠깐 기다려봐..."
  ↓ (0.5초 대기)
게임 서버: "아직 안 됨..."
  ↓ (0.5초 대기) × 최대 N회
게임 서버: "매칭 됐어! 룸 ID = ABC123" ──▶ 클라이언트
```

### 구현

```csharp
// Controllers/MatchCheckController.cs
[ApiController]
[Route("[controller]")]
public class MatchCheckController : ControllerBase
{
    readonly IMemoryDb _memoryDb;
    readonly ILogger<MatchCheckController> _logger;

    // 롱폴링 설정
    const int MaxRetryCount = 12;           // 최대 재시도 횟수
    const int RetryIntervalMs = 500;        // 재시도 간격 (밀리초)
    // 총 대기 시간 = 12 * 500ms = 6초

    [HttpPost]
    public async Task<MatchCheckResponse> Post()
    {
        Int64 playerId = (Int64)HttpContext.Items["PlayerId"]!;

        for (int i = 0; i < MaxRetryCount; i++)
        {
            // Redis에서 매칭 결과 확인
            var (errorCode, matchResult) = await _memoryDb.GetMatchResult(playerId);

            if (errorCode == ErrorCode.None && matchResult != null)
            {
                // 매칭 완료 → 즉시 응답
                _logger.ZLogInformation($"[MatchCheck] 매칭 완료, PlayerId={playerId}, RoomId={matchResult.RoomId}");
                return new MatchCheckResponse
                {
                    Result = ErrorCode.None,
                    RoomId = matchResult.RoomId
                };
            }

            // 아직 매칭 안 됨 → 잠깐 대기 후 재시도
            await Task.Delay(RetryIntervalMs);
        }

        // 타임아웃: 매칭 실패로 응답
        _logger.ZLogWarning($"[MatchCheck] 매칭 타임아웃, PlayerId={playerId}");
        return new MatchCheckResponse { Result = ErrorCode.MatchCheckNotMatchedYet };
    }
}
```

```csharp
// Controllers/OmokPeekController.cs - 게임 상태 확인 롱폴링
[HttpPost]
public async Task<OmokPeekResponse> Post()
{
    Int64 playerId = (Int64)HttpContext.Items["PlayerId"]!;

    for (int i = 0; i < MaxRetryCount; i++)
    {
        var (errorCode, gameState) = await _memoryDb.GetGameState(playerId);

        if (errorCode != ErrorCode.None)
        {
            return new OmokPeekResponse { Result = errorCode };
        }

        // 상대방이 돌을 뒀는지 확인 (내 차례가 됐는가?)
        if (gameState.IsMyTurn(playerId))
        {
            return new OmokPeekResponse
            {
                Result = ErrorCode.None,
                GameState = gameState,
                IsMyTurn = true
            };
        }

        await Task.Delay(RetryIntervalMs);
    }

    // 타임아웃: 아직 상대방 차례
    return new OmokPeekResponse
    {
        Result = ErrorCode.None,
        IsMyTurn = false
    };
}
```

### 클라이언트 사이드 롱폴링

```
클라이언트 흐름:

while (게임 중) {
    response = 서버에 Peek 요청 (최대 6초 대기)

    if (response.IsMyTurn == true) {
        // 내 차례 UI 표시
        돌 두기 UI 활성화
    } else {
        // 아직 상대방 차례
        // 즉시 다시 요청 (서버가 이미 6초 기다렸으므로)
    }
}
```

### 롱폴링의 한계와 주의사항

| 주의사항 | 설명 |
|:---|:---|
| **서버 연결 점유** | 롱폴링 중 HTTP 연결이 유지된다. 동접 1000명이면 1000개 연결이 동시에 대기 중 |
| **타임아웃 설정** | 클라이언트의 HTTP 타임아웃보다 서버 대기 시간이 짧아야 한다 |
| **유저 Lock과 충돌** | 롱폴링 중에도 Lock이 걸려 있으면 다른 요청(돌 두기)이 막힌다. Peek API는 Lock에서 제외하거나 별도 처리 필요 |
| **실시간성 한계** | 0.5초 간격 폴링이면 평균 0.25초의 지연이 발생한다 |

---

## 4. 세션 관리 패턴

### 세션의 역할

HTTP는 Stateless다. "이 요청이 로그인한 유저 A에게서 왔다"는 것을 증명하기 위해 세션이 필요하다.
이 프로젝트에서는 **Redis를 세션 저장소**로 사용한다.

```
로그인 성공 후:
  Redis: SET "Session_{playerId}" "{authToken, loginTime, ...}" EX 7200

이후 매 요청마다:
  Redis: GET "Session_{playerId}"
  응답값의 authToken == 쿠키의 authToken → 인증 성공
```

### 세션 데이터 구조

```csharp
// Models/RedisDB.cs
public class UserSession
{
    public Int64 PlayerId { get; set; }
    public string AuthToken { get; set; } = "";
    public string AppVersion { get; set; } = "";
    public string DataVersion { get; set; } = "";
    public DateTime LoginTime { get; set; }
}
```

### Redis 세션 CRUD

```csharp
// Repository/MemoryDb.cs
public class MemoryDb : IMemoryDb
{
    readonly RedisConnection _redisConn;
    readonly TimeSpan _sessionTtl = TimeSpan.FromHours(2); // 세션 유효 시간

    // 세션 저장 (로그인 시)
    public async Task<ErrorCode> StoreUserSession(Int64 playerId, UserSession session)
    {
        string key = MemoryDbKeyMaker.MakeSessionKey(playerId);
        var redisStr = _redisConn.GetRedisString<UserSession>(key);

        try
        {
            await redisStr.SetAsync(session, expiry: _sessionTtl);
            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"[MemoryDb.StoreUserSession] PlayerId={playerId}");
            return ErrorCode.LoginFailStoreSessionError;
        }
    }

    // 세션 조회 (매 요청 인증 시)
    public async Task<(ErrorCode, UserSession?)> GetUserSession(Int64 playerId)
    {
        string key = MemoryDbKeyMaker.MakeSessionKey(playerId);
        var redisStr = _redisConn.GetRedisString<UserSession>(key);

        try
        {
            var result = await redisStr.GetAsync();
            if (!result.HasValue)
            {
                return (ErrorCode.SessionNotFound, null);
            }
            return (ErrorCode.None, result.Value);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"[MemoryDb.GetUserSession] PlayerId={playerId}");
            return (ErrorCode.SessionGetError, null);
        }
    }

    // 세션 삭제 (로그아웃 시)
    public async Task<ErrorCode> DeleteUserSession(Int64 playerId)
    {
        string key = MemoryDbKeyMaker.MakeSessionKey(playerId);
        var redisStr = _redisConn.GetRedisString<UserSession>(key);

        try
        {
            await redisStr.DeleteAsync();
            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"[MemoryDb.DeleteUserSession] PlayerId={playerId}");
            return ErrorCode.LogoutFailDeleteSessionError;
        }
    }

    // 세션 갱신 (활동 시마다 TTL 연장 - 선택적)
    public async Task RefreshSessionTtl(Int64 playerId)
    {
        string key = MemoryDbKeyMaker.MakeSessionKey(playerId);
        await _redisConn.GetDatabase().KeyExpireAsync(key, _sessionTtl);
    }
}

// Repository/MemoryDbKeyMaker.cs
public static class MemoryDbKeyMaker
{
    public static string MakeSessionKey(Int64 playerId) => $"Session_{playerId}";
    public static string MakeUserLockKey(Int64 playerId) => $"ULock_{playerId}";
    public static string MakeMatchResultKey(Int64 playerId) => $"Match_{playerId}";
    public static string MakeGameStateKey(string roomId) => $"Game_{roomId}";
}
```

### 중복 로그인 처리

같은 계정이 두 곳에서 동시에 로그인하면 어떻게 처리할지 결정해야 한다.

```csharp
// 방법 1: 새 로그인이 기존 세션을 강제 종료 (대부분의 게임에서 채택)
public async Task<(ErrorCode, string)> Login(Int64 playerId, string hiveToken)
{
    // 기존 세션이 있으면 삭제
    await _memoryDb.DeleteUserSession(playerId);

    // 새 세션 생성
    string newAuthToken = Guid.NewGuid().ToString();
    var session = new UserSession { PlayerId = playerId, AuthToken = newAuthToken, ... };
    await _memoryDb.StoreUserSession(playerId, session);

    // 기존 클라이언트는 다음 요청 시 401을 받고 로그인 화면으로 이동하게 됨
    return (ErrorCode.None, newAuthToken);
}

// 방법 2: 중복 로그인 자체를 거부 (이미 세션이 있으면 에러 반환)
public async Task<(ErrorCode, string)> Login(Int64 playerId, string hiveToken)
{
    var (errorCode, existingSession) = await _memoryDb.GetUserSession(playerId);
    if (errorCode == ErrorCode.None && existingSession != null)
    {
        return (ErrorCode.LoginFailAlreadyLoggedIn, null);
    }

    // 새 세션 생성
    ...
}
```

### 쿠키 설정

```csharp
// Controllers/LoginController.cs
[HttpPost]
public async Task<LoginResponse> Post([FromBody] LoginRequest request)
{
    var (errorCode, authToken) = await _authService.Login(request.PlayerId, request.HiveToken);

    if (errorCode != ErrorCode.None)
    {
        return new LoginResponse { Result = errorCode };
    }

    // 쿠키에 저장
    // HttpOnly: JavaScript에서 접근 불가 → XSS 공격 방어
    // SameSite=Strict: 다른 사이트에서 쿠키가 전송되지 않음 → CSRF 방어
    var cookieOptions = new CookieOptions
    {
        HttpOnly = true,
        Secure = true,       // HTTPS에서만 전송
        SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.UtcNow.AddHours(2)
    };

    Response.Cookies.Append("AuthToken", authToken, cookieOptions);
    Response.Cookies.Append("PlayerId", request.PlayerId.ToString(), cookieOptions);

    return new LoginResponse { Result = ErrorCode.None };
}

// Controllers/LogoutController.cs
[HttpGet]
public async Task<LogoutResponse> Get()
{
    Int64 playerId = (Int64)HttpContext.Items["PlayerId"]!;

    // Redis 세션 삭제
    var errorCode = await _authService.Logout(playerId);

    // 쿠키 삭제
    Response.Cookies.Delete("AuthToken");
    Response.Cookies.Delete("PlayerId");

    return new LogoutResponse { Result = errorCode };
}
```

### 세션 패턴 전체 흐름 요약

```
[로그인]
클라이언트 → Login API → Hive 토큰 검증
                       → AuthToken 생성 (GUID)
                       → Redis에 UserSession 저장 (TTL=2시간)
                       → 쿠키에 AuthToken + PlayerId 설정

[일반 요청]
클라이언트 → 쿠키 포함 요청
           → CheckUserAuth 미들웨어
               → 쿠키에서 PlayerId, AuthToken 추출
               → Redis에서 UserSession 조회
               → AuthToken 일치 확인
               → 유저 Lock 획득
           → 컨트롤러 처리
           → 유저 Lock 해제

[로그아웃]
클라이언트 → Logout API
           → Redis에서 UserSession 삭제
           → 쿠키 삭제
```
