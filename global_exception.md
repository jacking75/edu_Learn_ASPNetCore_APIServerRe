# 글로벌 예외 처리 가이드

게임 서버에서 처리하지 않은 예외(Unhandled Exception)가 발생하면 클라이언트가 응답을 받지 못하거나
알 수 없는 오류를 받게 된다. 글로벌 예외 처리 미들웨어는 이를 일관되게 처리한다.

---

## 목차

1. [왜 글로벌 예외 처리가 필요한가?](#1-왜-글로벌-예외-처리가-필요한가)
2. [글로벌 예외 처리 미들웨어 구현](#2-글로벌-예외-처리-미들웨어-구현)
3. [컨트롤러/서비스 예외 처리 패턴](#3-컨트롤러서비스-예외-처리-패턴)
4. [ErrorCode 설계 원칙](#4-errorcode-설계-원칙)
5. [예외 처리 흐름 전체 요약](#5-예외-처리-흐름-전체-요약)

---

## 1. 왜 글로벌 예외 처리가 필요한가?

### 처리하지 않은 예외의 문제

```csharp
// 만약 Service에서 예외가 발생하면?
public async Task<ErrorCode> BuyItem(long uid, int itemCode)
{
    var item = await _db.GetItem(itemCode); // DB 오류 시 예외 발생
    // 예외를 잡지 않으면 ASP.NET Core가 500 Internal Server Error를 반환
    // 클라이언트는 빈 응답이나 HTML 에러 페이지를 받게 됨
    // 게임 클라이언트는 JSON을 기대하는데 HTML이 오면 파싱 오류 발생
}
```

```
처리하지 않을 때 클라이언트가 받는 응답:
HTTP 500
Content-Type: text/html
Body: <html>An unhandled exception occurred...</html>  ← JSON이 아님!
```

```
글로벌 예외 처리 후 클라이언트가 받는 응답:
HTTP 200
Content-Type: application/json
Body: { "Result": 999 }  ← 파싱 가능한 에러코드
```

### 글로벌 예외 처리기의 역할

1. 예외를 잡아서 서버가 중단되지 않게 한다
2. 모든 에러 응답을 동일한 JSON 형식으로 반환한다
3. 예외 내용을 상세히 로깅한다 (스택 트레이스 포함)
4. 클라이언트에게는 내부 정보를 노출하지 않는다

---

## 2. 글로벌 예외 처리 미들웨어 구현

### 기본 구현

```csharp
// Middleware/GlobalExceptionHandler.cs
public class GlobalExceptionHandler
{
    readonly RequestDelegate _next;
    readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context); // 다음 미들웨어 / 컨트롤러 실행
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // 1. 에러 정보를 상세히 로깅 (스택 트레이스 포함)
        _logger.ZLogError(ex,
            $"[GlobalException] Path={context.Request.Path}, " +
            $"Method={context.Request.Method}, " +
            $"Exception={ex.GetType().Name}");

        // 2. 클라이언트에게 JSON으로 에러 응답 (내부 정보는 노출하지 않음)
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";

        // 게임 서버는 HTTP 상태코드보다 Result 필드로 에러를 전달하는 경우가 많다
        // 클라이언트가 항상 200을 받고 Result로 성공/실패를 판단하는 방식
        var errorResponse = new BaseResponse { Result = ErrorCode.ServerException };
        await context.Response.WriteAsJsonAsync(errorResponse);
    }
}
```

```csharp
// Program.cs - 미들웨어 파이프라인 맨 앞에 등록
var app = builder.Build();

// 글로벌 예외 처리는 반드시 다른 미들웨어보다 먼저 등록해야
// 이후 미들웨어에서 발생하는 예외도 잡을 수 있다
app.UseMiddleware<GlobalExceptionHandler>();

// 이후 미들웨어들
app.UseMiddleware<VersionCheck>();
app.UseMiddleware<CheckUserAuth>();
app.MapControllers();
```

### 예외 종류별 처리

```csharp
// Middleware/GlobalExceptionHandler.cs - 예외 종류별로 다른 처리
async Task HandleExceptionAsync(HttpContext context, Exception ex)
{
    ErrorCode errorCode;
    string logLevel;

    switch (ex)
    {
        // DB 연결 오류: 인프라 문제 → Critical 수준으로 로깅
        case MySqlException mysqlEx when mysqlEx.Number == 1042:
            errorCode = ErrorCode.DatabaseConnectionError;
            logLevel = "CRITICAL";
            _logger.ZLogCritical(ex,
                $"[GlobalException] MySQL 연결 실패 - 즉시 확인 필요! Path={context.Request.Path}");
            break;

        // 타임아웃: 부하 문제 → Warning 수준
        case OperationCanceledException:
        case TimeoutException:
            errorCode = ErrorCode.ServerTimeout;
            logLevel = "WARNING";
            _logger.ZLogWarning(ex,
                $"[GlobalException] 타임아웃 발생 Path={context.Request.Path}");
            break;

        // 잘못된 JSON 등 요청 파싱 오류 → Bad Request
        case JsonException:
            errorCode = ErrorCode.InvalidRequestFormat;
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            _logger.ZLogWarning(ex,
                $"[GlobalException] 요청 파싱 오류 Path={context.Request.Path}");
            break;

        // 그 외 모든 예외: 버그일 가능성 → Error 수준
        default:
            errorCode = ErrorCode.ServerException;
            _logger.ZLogError(ex,
                $"[GlobalException] 처리되지 않은 예외 Path={context.Request.Path}, " +
                $"ExceptionType={ex.GetType().Name}");
            break;
    }

    context.Response.ContentType = "application/json";
    if (context.Response.StatusCode == 200) // 이미 설정된 경우 덮어쓰지 않음
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
    }

    await context.Response.WriteAsJsonAsync(new BaseResponse { Result = errorCode });
}
```

---

## 3. 컨트롤러/서비스 예외 처리 패턴

### Service에서 예외 처리하기

글로벌 핸들러가 있더라도 Service 계층에서는 try-catch로 예외를 잡고 ErrorCode로 변환하는 것이 권장된다.
이렇게 해야 **어느 Service에서 예외가 발생했는지 정확한 로그**를 남길 수 있다.

```csharp
// Services/AttendanceService.cs
public class AttendanceService : IAttendanceService
{
    readonly ILogger<AttendanceService> _logger;
    readonly IGameDb _gameDb;
    readonly IMasterDb _masterDb;

    public async Task<(ErrorCode, AttendanceReward?)> CheckAttendance(long uid)
    {
        try
        {
            // 1. 오늘 이미 출석했는지 확인
            var attendance = await _gameDb.GetAttendance(uid);
            if (attendance?.LastAttendance?.Date == DateTime.UtcNow.Date)
            {
                return (ErrorCode.AttendanceAlreadyCheckedToday, null);
            }

            // 2. 연속 출석일 계산
            int newConsecutiveDays = CalculateConsecutiveDays(attendance);

            // 3. DB 업데이트
            int rowCount = await _gameDb.UpdateAttendance(uid, DateTime.UtcNow);
            if (rowCount != 1)
            {
                _logger.ZLogError(
                    $"[Attendance.CheckAttendance] 출석 업데이트 실패, Uid={uid}, RowCount={rowCount}");
                return (ErrorCode.AttendanceFailUpdateAttendance, null);
            }

            // 4. 보상 지급
            var reward = _masterDb.GetAttendanceReward(newConsecutiveDays);
            if (reward != null)
            {
                await _gameDb.GiveReward(uid, reward.RewardItemCode, reward.RewardQuantity);
            }

            return (ErrorCode.None, reward);
        }
        catch (Exception e)
        {
            // 예외를 잡아서 로깅하고 ErrorCode로 변환
            // 스택 트레이스가 포함된 로그 → 어디서 에러가 났는지 정확히 파악 가능
            _logger.ZLogError(e,
                $"[Attendance.CheckAttendance] 예외 발생, Uid={uid}");
            return (ErrorCode.AttendanceFailException, null);
        }
    }
}
```

### 예외를 잡지 말아야 하는 경우

```csharp
// 나쁜 예: 예외를 잡아서 아무것도 안 함 (예외를 묻어버림)
try
{
    await _db.UpdateAttendance(uid, DateTime.UtcNow);
}
catch (Exception)
{
    // 아무것도 안 함 ← 매우 나쁜 패턴! 에러가 발생했는데 성공인 척 진행됨
}

// 나쁜 예: 예외를 잡아서 재포장 없이 다시 던짐 (의미 없음)
try
{
    await _db.UpdateAttendance(uid, DateTime.UtcNow);
}
catch (Exception e)
{
    throw e; // ← 스택 트레이스가 사라짐. throw; 만 써야 보존됨
}

// 좋은 예: 예외를 잡아서 로깅 후 ErrorCode로 변환
try
{
    await _db.UpdateAttendance(uid, DateTime.UtcNow);
}
catch (Exception e)
{
    _logger.ZLogError(e, $"[xxx] 출석 업데이트 실패, Uid={uid}");
    return ErrorCode.AttendanceFailUpdateAttendance;
}
```

---

## 4. ErrorCode 설계 원칙

ErrorCode는 서버와 클라이언트가 에러 상황을 약속으로 정의한 것이다.

### ErrorCode 범위 분리

```csharp
public enum ErrorCode
{
    // ─────────────────────────────────
    // 0: 성공
    // ─────────────────────────────────
    None = 0,

    // ─────────────────────────────────
    // 1~999: 공통 에러 (모든 서버 공통)
    // ─────────────────────────────────
    ServerException = 1,           // 처리되지 않은 서버 예외
    DatabaseConnectionError = 2,   // DB 연결 실패
    ServerTimeout = 3,             // 서버 타임아웃
    InvalidRequestFormat = 4,      // 잘못된 요청 형식
    TooManyRequests = 5,           // Rate Limit 초과

    // 인증 관련
    AuthTokenNotFound = 101,
    AuthTokenInvalid = 102,
    AuthTokenExpired = 103,
    AuthTokenAlreadyInUse = 104,   // 중복 요청 (Lock)

    // 버전 관련
    MismatchAppVersion = 201,
    MismatchDataVersion = 202,

    // ─────────────────────────────────
    // 1000~1999: 계정/로그인
    // ─────────────────────────────────
    LoginFailHiveTokenVerification = 1001,
    LoginFailStoreSessionError = 1002,
    LoginFailAlreadyLoggedIn = 1003,
    LogoutFailDeleteSessionError = 1101,
    CreateAccountFailDuplicateId = 1201,
    CreateAccountFailDuplicateNickname = 1202,
    SessionNotFound = 1301,
    SessionGetError = 1302,

    // ─────────────────────────────────
    // 2000~2999: 출석
    // ─────────────────────────────────
    AttendanceAlreadyCheckedToday = 2001,
    AttendanceFailUpdateAttendance = 2002,
    AttendanceFailGiveReward = 2003,
    AttendanceFailException = 2099,     // 범위 마지막에 일반 예외 코드

    // ─────────────────────────────────
    // 3000~3999: 우편함
    // ─────────────────────────────────
    MailReceiveFailMailNotExist = 3001,
    MailReceiveFailNotMailOwner = 3002,
    MailReceiveFailAlreadyReceived = 3003,
    MailDeleteFailDeleteMail = 3011,
    MailDeleteFailDeleteMailReward = 3012,
    MailReceiveRewardsFailException = 3098,
    MailDeleteFailException = 3099,

    // ─────────────────────────────────
    // 4000~4999: 상점/구매
    // ─────────────────────────────────
    ShopItemNotFound = 4001,
    ShopInsufficientMoney = 4002,
    ShopFailException = 4099,

    // ─────────────────────────────────
    // 5000~5999: 게임 플레이 (오목 등)
    // ─────────────────────────────────
    GameRoomNotFound = 5001,
    GameNotYourTurn = 5002,
    GameInvalidPosition = 5003,
    GameAlreadyEnded = 5004,
}
```

### ErrorCode 사용 원칙

```csharp
// 원칙 1: 일반 예외는 범위 마지막 코드로 (ex: xxxFailException = xx99)
// → 로그에서 xxx99가 보이면 "catch에서 잡힌 예외"임을 즉시 알 수 있음

// 원칙 2: 같은 작업의 에러는 연속된 번호 사용
// AttendanceFailUpdateAttendance = 2002
// AttendanceFailGiveReward       = 2003
// → 로그 분석 시 2000번대 = 출석 관련임을 즉시 파악 가능

// 원칙 3: ErrorCode와 로그 메시지는 함께
_logger.ZLogError(e,
    $"[Attendance.CheckAttendance] ErrorCode: {ErrorCode.AttendanceFailException}, Uid={uid}");
// → 로그에서 ErrorCode를 검색하면 해당 코드의 발생 위치를 즉시 찾을 수 있음
```

---

## 5. 예외 처리 흐름 전체 요약

```
클라이언트 요청
    │
    ▼
GlobalExceptionHandler (미들웨어)
    │ try {
    ▼
VersionCheck (미들웨어)
    │ 버전 불일치 → 바로 응답 반환
    ▼
CheckUserAuth (미들웨어)
    │ 인증 실패 → 바로 응답 반환
    │ 유저 Lock 획득
    ▼
Controller
    │ Service 호출
    ▼
Service (비즈니스 로직)
    │ try-catch로 예외 처리
    │ 성공 → ErrorCode.None 반환
    │ 실패 → ErrorCode.xxxFail 반환
    │ 예외 → 로깅 후 ErrorCode.xxxFailException 반환
    ▼
Repository (DB/Redis 접근)
    │ DB/Redis와 통신
    │ 예외 발생 가능
    ▼
Controller (응답 반환)
    │
    ▼
CheckUserAuth (미들웨어)
    │ 유저 Lock 해제 (finally)
    │ } catch (Exception) {
    │     에러 로깅 + ErrorCode.ServerException JSON 응답
    │ }
    ▼
클라이언트 응답
```

### 에러 응답 형식

모든 에러 응답은 동일한 형식을 유지한다.

```json
// 성공 응답
{
    "Result": 0,
    "Data": { ... }
}

// 실패 응답 (어떤 에러든 동일한 구조)
{
    "Result": 2001
}
// 클라이언트는 Result가 0이 아니면 에러로 처리하고,
// 에러 코드에 따라 UI 메시지를 표시한다
```
