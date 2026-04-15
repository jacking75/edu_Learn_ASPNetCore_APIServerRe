# ZLogger 사용 가이드

> ZLogger는 `Microsoft.Extensions.Logging` 위에 구축된 고성능 로거로,
> C# 10.0의 향상된 String Interpolation을 활용하여 로깅 속도를 높이고 메모리 할당을 최소화합니다.

간편하고 적합한 [ZLogger](https://github.com/Cysharp/ZLogger) 사용을 위하여, 구성 및 사용 방법을 정리합니다.

### 목차

- [왜 로깅 프레임워크를 사용하는가?](#왜-로깅-프레임워크를-사용하는가)
- [왜 ZLogger인가?](#왜-zlogger인가)
- [SampleServer 프로젝트 구조](#sampleserver-프로젝트-구조)
- [SampleServer 실행 및 테스트](#sampleserver-실행-및-테스트)
- [ZLogger 시작하기](#zlogger-시작-하기)
  - [설치하기](#설치하기)
  - [프로젝트에 적용하기](#프로젝트에-적용하기)
    - [Dependency Injection](#didependency-injection)
    - [LoggerFactory](#loggerfactory)
    - [Global LoggerFactory](#global-loggerfactory)
- [LogLevel - 로그 심각도 이해하기](#loglevel---로그-심각도-이해하기)
- [ZLog\* vs Log\* - 무엇이 다른가?](#zlog-vs-log---무엇이-다른가)
- [로그의 기본 구성](#로그의-기본-구성)
- [로그 출력 방법 선택](#logging-providers)
  - [Provider 종류](#provider-types)
  - [ZLoggerRollingFile 설정 예시](#zloggerrollingfile-설정-예시)
- [로그 출력 옵션](#provider-options)
  - [공통 옵션](#공통-옵션-zloggeroptions)
  - [콘솔 출력용 옵션](#콘솔-출력-전용-옵션-zloggerconsoleoptions)
- [로그 형식 지정](#zlogger-formatter-configuration)
  - [JSON](#json)
  - [로그 별 사용자 지정](#로그별-형식-사용자-지정하기)
- [로그 구조화](#zlogger를-활용한-로그-구조화)

---

# 왜 로깅 프레임워크를 사용하는가?

게임 서버 개발에서 `Console.WriteLine()`만으로 디버깅하는 것은 여러 한계가 있습니다.

```csharp
// 이렇게 하면 안 됩니다
Console.WriteLine("유저 로그인 성공");
Console.WriteLine($"에러 발생: {ex.Message}");
```

**`Console.WriteLine`의 문제점:**

| 문제 | 설명 |
|:---|:---|
| **시간 정보 없음** | 언제 발생한 로그인지 알 수 없음 |
| **심각도 구분 불가** | 정상 로그와 에러 로그가 구분되지 않음 |
| **검색/필터링 불가** | 수만 줄의 로그에서 특정 유저의 문제를 찾기 어려움 |
| **파일 저장 불가** | 콘솔이 꺼지면 로그가 사라짐 |
| **성능 저하** | 동기 호출로 서버 처리량에 영향 |
| **구조화 불가** | Grafana, Elasticsearch 등 모니터링 도구와 연동 불가 |

**로깅 프레임워크를 쓰면:**

```json
{
  "timestamp": "2024-10-10T07:46:08.948+00:00",
  "LogLevel": "Information",
  "membername": "Login",
  "message": "User Logged in",
  "context": { "uid": 1, "access_token": "abc..." }
}
```

- 시간, 심각도, 호출 위치가 자동 기록됨
- JSON 형식으로 모니터링 도구와 바로 연동 가능
- 파일, 콘솔, 메모리 등 원하는 곳에 동시 출력 가능
- 비동기 처리로 서버 성능에 영향 최소화

---

# 왜 ZLogger인가?

.NET 생태계에는 여러 로깅 라이브러리가 있습니다. ZLogger의 차별점을 비교합니다.

| 항목 | 기본 로거 (`ILogger`) | Serilog | **ZLogger** |
|:---|:---|:---|:---|
| **성능** | 보통 | 보통 | **매우 빠름** (Zero Allocation) |
| **메모리 할당** | 문자열 할당 발생 | 문자열 할당 발생 | **String Interpolation 최적화로 할당 최소화** |
| **구조화 로깅** | 제한적 | 우수 | 우수 |
| **JSON 출력** | 직접 구현 필요 | Sink 필요 | **내장 지원** (`UseJsonFormatter`) |
| **ASP.NET Core 통합** | 기본 | 설정 필요 | **`ILogger` 인터페이스 그대로 사용** |
| **학습 곡선** | 낮음 | 중간 | **낮음** (Microsoft.Extensions.Logging과 동일 API) |
| **개발사** | Microsoft | 커뮤니티 | **Cysharp** (UniTask, MessagePack 등 게임 특화 라이브러리 개발사) |

**ZLogger의 핵심 장점:**

1. **Zero Allocation** — `ZLogInformation($"Hello {name}")` 호출 시 문자열 객체를 생성하지 않아 GC 부담이 없음. 게임 서버처럼 초당 수천 건의 로그가 발생하는 환경에서 특히 유리합니다.
2. **`ILogger` 호환** — 기존 `Microsoft.Extensions.Logging` 인터페이스를 그대로 사용하므로 DI, 미들웨어 등 ASP.NET Core 표준 패턴과 완벽 호환됩니다.
3. **게임 서버 친화적** — Cysharp에서 게임 서버 환경을 고려하여 설계하였으며, Unity에서도 사용 가능합니다.

---

# SampleServer 프로젝트 구조

본 학습 프로젝트의 구조와 각 파일의 역할입니다.

```
ZLogger/
├── README.md                          # 이 문서 (학습 가이드)
├── ZLoggerSolution.sln                # Visual Studio 솔루션 파일
└── SampleServer/
    ├── Program.cs                     # 앱 진입점: ZLogger 제공자 설정 (Console + RollingFile)
    ├── SampleServer.csproj            # 프로젝트 설정, ZLogger 패키지 참조
    ├── apiTest.http                   # API 테스트 파일 (VS Code REST Client / Rider)
    ├── appsettings.json               # 기본 설정
    ├── Controllers/
    │   ├── BaseController.cs          # [학습 핵심] ActionLog/InformationLog/ErrorLog 추상화
    │   ├── LoggingController.cs       # LogLevel별 출력, BeginScope 중첩, 예외 로깅 예시
    │   ├── LoginController.cs         # BaseController 상속 - 로그인 성공/실패 시나리오
    │   └── GameController.cs          # BeginScope + 다양한 LogLevel 시연
    ├── Services/
    │   ├── BaseLogger.cs              # [학습 핵심] MetricLog/ExceptionLog/ErrorLog 추상화
    │   ├── IGameService.cs            # 게임 서비스 인터페이스
    │   └── GameService.cs             # BaseLogger 상속 - MetricLog, BeginScope 시연
    ├── DTO/
    │   ├── LogRequest.cs              # 로깅 API 요청 DTO
    │   └── GameDTO.cs                 # 로그인/게임시작 요청/응답 DTO
    └── Models/
        └── ErrorCode.cs               # UInt16 에러코드 enum
```

**학습 흐름:**

1. `Program.cs` → ZLogger 설정 방법 이해
2. `LoggingController.cs` → 기본 사용법 (LogLevel, BeginScope, Exception)
3. `BaseController.cs` / `BaseLogger.cs` → 실전 추상화 패턴
4. `LoginController.cs` / `GameService.cs` → 실전 시나리오 적용

---

# SampleServer 실행 및 테스트

### 빌드 및 실행

```bash
cd ZLogger
dotnet build
dotnet run --project SampleServer
```

서버가 `http://localhost:8080`에서 시작됩니다.

### API 테스트 방법

**방법 1: apiTest.http 파일 사용** (권장)

VS Code의 [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) 확장 또는 JetBrains Rider에서 `apiTest.http` 파일을 열고 각 요청 위의 `Send Request`를 클릭합니다.

**방법 2: curl 사용**

```bash
# 기본 로그 출력
curl -X POST http://localhost:8080/logging \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello ZLogger!", "level": "information"}'

# 로그인 성공 (콘솔에서 ActionLog + InformationLog 확인)
curl -X POST http://localhost:8080/login \
  -H "Content-Type: application/json" \
  -d '{"playerId": 1, "token": "valid-token"}'

# 로그인 실패 (콘솔에서 ErrorLog 확인)
curl -X POST http://localhost:8080/login \
  -H "Content-Type: application/json" \
  -d '{"playerId": 2, "token": "invalid"}'

# 게임 시작 (콘솔에서 BeginScope + MetricLog 확인)
curl -X POST http://localhost:8080/game/start \
  -H "Content-Type: application/json" \
  -d '{"uid": 100}'

# BeginScope 중첩 예시
curl -X POST http://localhost:8080/logging/scope \
  -H "Content-Type: application/json" \
  -d '{"message": "Testing scopes"}'

# 예외 로깅 예시 (스택 트레이스 포함)
curl -X POST http://localhost:8080/logging/exception \
  -H "Content-Type: application/json" \
  -d '{"message": "Trigger exception"}'
```

### 출력 확인

- **콘솔**: 서버 실행 터미널에서 JSON 형식의 로그 출력 확인
- **파일**: 프로젝트 루트의 `logs/` 폴더에 `yyyy-MM-dd_000.log` 파일 생성 확인

---

# ZLogger 시작 하기

## 설치하기

프로젝트 경로에서 아래 커맨드를 실행 또는 NuGet 패키지 매니저를 활용하여 설치 할 수 있습니다.

```bash
dotnet add package ZLogger
```

## 프로젝트에 적용하기

ASP .NET Core에서 ZLogger를 적용하는 가장 간단한 방법은 아래와 같습니다.

```csharp
using ZLogger;

var builder = WebApplication.CreateBuilder(args);

// 기본 로거 설정 제외
builder.Logging.ClearProviders();       // 전체 제외 추천

// 로그 제공자 추가
builder.Logging.AddZLoggerConsole();    // 콘솔 출력
```

위 예시에서는 로그를 콘솔에 출력하는 `ZLoggerConsole`을 사용합니다.

#### `ClearProviders()`는 왜 필요한가?

ASP.NET Core는 기본적으로 Console, Debug, EventSource 등 여러 로깅 제공자를 자동 등록합니다.
ZLogger를 사용할 때 이것들을 제거하지 않으면 **같은 로그가 중복 출력**됩니다.

```
[기본 Console 제공자] Hello World    ← 기본 형식
{"timestamp":"...","Message":"Hello World"}  ← ZLogger JSON 형식
```

`ClearProviders()`로 기본 제공자를 모두 제거한 뒤, ZLogger만 등록하면 깔끔한 출력을 얻을 수 있습니다.

추가적인 로그 제공자 정보는 [Logging Providers](#logging-providers)에서 확인 가능합니다.

### DI(Dependency Injection)

제공자 설정 이후 DI 사용을 통해 원하는 클래스에서 로그 출력 메서드를 쓸 수 있습니다.

```csharp
using Microsoft.Extensions.Logging;
using ZLogger;

public class SomeService(ILogger<MyClass> logger)
{
    public void SomeServiceLog(string name, string city, int age)
    {
        logger.ZLogInformation($"Hello, {name} lives in {city} {age} years old.");
    }
}
```

> **DI가 처음이라면:** `ILogger<T>`는 ASP.NET Core의 의존성 주입(DI) 컨테이너가 자동으로 생성하여 생성자에 넣어줍니다. `<T>`의 클래스 이름이 로그의 `Category`로 기록되어 어떤 클래스에서 발생한 로그인지 구분할 수 있습니다.

출력 예시:

```powershell
# name = "Bill", city = "Kumamoto", age = 21
> Hello, Bill lives in Kumamoto 21 years old.
```

`Log*`가 아닌 `ZLog*` 형식의 메서드 사용을 통하여 출력하도록 합니다.
(`ZLog*`와 `Log*`의 차이는 [ZLog\* vs Log\*](#zlog-vs-log---무엇이-다른가) 참고)

### LoggerFactory

또는 LoggerFactory를 직접 생성하여 만들 수 있습니다.

```csharp
var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.SetMinimumLevel(LogLevel.Trace);
});

var logger = loggerFactory.CreateLogger<YourClass>();

var name = "foo";
logger.ZLogInformation($"Hello, {name}!");
```

### Global LoggerFactory

LoggerFactory를 전역 설정하여, DI 없이 타입별 로거를 가져와 사용할 수도 있습니다.

```csharp
// LogManager.cs
public static class LogManager
{
    static ILogger globalLogger = default!;
    static ILoggerFactory loggerFactory = default!;

    public static void SetLoggerFactory(ILoggerFactory factory, string categoryName)
    {
        loggerFactory = factory;
        globalLogger = factory.CreateLogger(categoryName);
    }

    public static ILogger Logger => globalLogger;

    public static ILogger<T> GetLogger<T>() where T : class => loggerFactory.CreateLogger<T>();
    public static ILogger GetLogger(string categoryName) => loggerFactory.CreateLogger(categoryName);
}
```

이렇게 사용자 정의 매니저 클래스를 만든 후

```csharp
// Program.cs
using var host = Host.CreateDefaultBuilder()
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddZLoggerConsole();
    })
    .Build();

var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

LogManager.SetLoggerFactory(loggerFactory, "Global");
```

`Program.cs`에서 위와 같이 설정하고 다음과 같이 사용할 수 있습니다.

```csharp
public class Foo
{
    static readonly ILogger<Foo> logger = LogManager.GetLogger<Foo>();

    public void Foo(int x)
    {
        logger.ZLogDebug($"do do do: {x}");
    }
}
```

---

# LogLevel - 로그 심각도 이해하기

`Microsoft.Extensions.Logging`은 6단계의 로그 레벨을 제공합니다.
ZLogger도 이 표준을 따르며, 각 레벨에 대응하는 `ZLog*` 메서드가 있습니다.

| LogLevel | ZLogger 메서드 | 설명 | 게임 서버 사용 예시 |
|:---|:---|:---|:---|
| `Trace` (0) | `ZLogTrace` | 가장 상세한 정보. 개발 중에만 사용 | 패킷 바이트 덤프, 변수 값 추적 |
| `Debug` (1) | `ZLogDebug` | 디버깅용 정보. 개발/테스트 환경에서 사용 | 매칭 큐 상태, DB 쿼리 파라미터 |
| `Information` (2) | `ZLogInformation` | 정상적인 흐름 기록. 운영에서도 사용 | 유저 로그인 성공, 게임 시작/종료 |
| `Warning` (3) | `ZLogWarning` | 비정상이지만 처리 가능한 상황 | 잘못된 요청 파라미터, 재시도 발생 |
| `Error` (4) | `ZLogError` | 오류 발생. 즉시 확인 필요 | DB 연결 실패, 외부 API 오류 |
| `Critical` (5) | `ZLogCritical` | 시스템 중단 수준의 치명적 오류 | 서버 메모리 부족, 핵심 서비스 다운 |

**레벨 필터링 예시:**

```csharp
// Program.cs에서 최소 로그 레벨 설정
builder.Logging.SetMinimumLevel(LogLevel.Information);
// → Trace, Debug는 출력되지 않음
// → Information, Warning, Error, Critical만 출력됨
```

운영 환경에서는 `Information` 이상만, 개발 환경에서는 `Debug` 이상으로 설정하는 것이 일반적입니다.

**사용 예시:**

```csharp
// SampleServer의 GameController.cs 참고
_logger.ZLogDebug($"Game start requested");                    // 개발 중 흐름 추적
_logger.ZLogInformation($"Game started successfully");         // 정상 이벤트 기록
_logger.ZLogWarning($"Invalid uid received: {request.Uid}");   // 비정상 입력 경고
_logger.ZLogError($"Database connection failed");              // 오류 발생
```

---

# ZLog* vs Log* - 무엇이 다른가?

`ILogger`에는 기본 `Log*` 메서드와 ZLogger의 `ZLog*` 메서드가 있습니다.
**반드시 `ZLog*` 메서드를 사용해야** ZLogger의 성능 이점을 얻을 수 있습니다.

### 기본 `Log*` — 문자열 할당 발생

```csharp
// Microsoft.Extensions.Logging의 기본 메서드
logger.LogInformation("User {Name} logged in at {Time}", name, DateTime.Now);
```

이 방식은 내부에서 `string.Format()`과 유사하게 **문자열 객체를 새로 생성**(힙 할당)합니다.
초당 수천 건의 로그가 발생하면 GC(Garbage Collection) 부담이 커집니다.

### ZLogger의 `ZLog*` — Zero Allocation

```csharp
// ZLogger의 최적화된 메서드
logger.ZLogInformation($"User {name} logged in at {DateTime.Now}");
```

겉보기에는 일반 String Interpolation(`$""`)과 같지만, ZLogger는 C# 10.0의 **`Interpolated String Handler`** 를 활용하여 **문자열 객체를 생성하지 않고** 직접 버퍼에 기록합니다.

```
[기본 Log*]  → string.Format() → 문자열 객체 생성(힙 할당) → GC 대상
[ZLog*]      → InterpolatedStringHandler → 버퍼에 직접 기록 → 할당 없음
```

> **핵심:** 같은 `$""` 문법이지만, `ZLog*` 메서드의 매개변수 타입이 `Interpolated String Handler`로 선언되어 있어 컴파일러가 자동으로 최적화된 코드를 생성합니다. 개발자는 사용법의 차이를 신경 쓸 필요 없이 `ZLog*`만 사용하면 됩니다.

---

## 로그의 기본 구성

Zlogger를 통해 제공받을 수 있는 로그의 구성은 다음과 같습니다.

| Key Name     | Description                                                                                                                                                                                                            |
| :----------- | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Category `  | 생성된 Logger의 할당된 카테고리 이름, 주로 클래스나 모듈의 전체 이름을 나타냅니다. <br/>**ex:** ILogger<HomeController\> 를 통해 쓰인 로그의 Category는 `App.Controllers.HomeController`가 됩니다                      |
| `Timestamp ` | 로그가 기록된 시간을 나타냅니다.                                                                                                                                                                                       |
| `LogLevel`   | `Microsoft.Extensions.Logging`에서 제공되는 로그의 심각도를 나타냅니다. ([LogLevel 설명](#loglevel---로그-심각도-이해하기) 참고)                                                                                       |
| `EventId `   | `Microsoft.Extensions.Logging`에서 제공되는 이벤트 ID입니다. 각 로그 항목에 대한 고유 식별자 역할을 합니다.                                                                                                            |
| `ScopeState` | `ILogger.BeginScope(...)`를 통해 설정된 추가 속성들을 포함합니다. 이는 로그 항목과 관련된 추가 정보를 저장합니다. (`ZLoggerOptions.IncludeScopes = true`일 경우에만 해당, [Provider Options](#provider-options) 참고). |
| `ThreadInfo` | 로그 항목이 생성될 당시의 쓰레드 ID 및 관련 쓰레드 컨텍스트를 저장합니다.                                                                                                                                              |
| `Context`    | 로그 기록 시 전달된 추가 객체입니다.                                                                                                                                                                                   |
| `MemberName` | 로그 호출 시의 멤버 이름을 나타냅니다. `CallerMemberName`을 통해 자동으로 설정되며, 호출된 메서드의 이름이 기록됩니다.                                                                                                 |
| `FilePath`   | 로그 항목이 호출된 소스 파일의 전체 경로를 나타냅니다.                                                                                                                                                                 |
| `LineNumber` | 로그가 생성된 소스 파일의 줄 번호를 나타냅니다                                                                                                                                                                         |

> 로그 샘플

```csharp
builder.AddZLoggerConsole(options =>
{
	options.IncludeScopes = true;
	options.UseJsonFormatter(formatter =>
	{
		formatter.JsonPropertyNames = JsonPropertyNames.Default with
		{
			Timestamp = JsonEncodedText.Encode("timestamp"),
			MemberName = JsonEncodedText.Encode("membername"),
			Exception = JsonEncodedText.Encode("exception"),
		};
		formatter.JsonSerializerOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			WriteIndented = true
		};
		formatter.KeyNameMutator = KeyNameMutator.LastMemberNameLowerFirstCharacter;
		formatter.IncludeProperties = IncludeProperties.All;
	});
});
```

제공자 옵션에서 `IncludeProperties.All`을 활용하여 로그 구성 전체를 출력해보겠습니다.

```csharp
using Microsoft.Extensions.Logging;
using ZLogger;

public class LoggingService
{
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(ILogger<LoggingService> logger)
    {
        _logger = logger;
    }

    public void UserEnter()
    {
        using (_logger.BeginScope(new { GameGuid = Guid.NewGuid() }))
        {
            _logger.ZLogInformation($"User Logged in");
        }
    }

    public void StartGame(string guid)
    {
        using (_logger.BeginScope(new { GameGuid = guid }))
        {
            _logger.ZLogInformation($"Game Started");
        }
    }
}

```

위 코드에서 `UserEnter()` 와 `StartGame()`이 실행 되는 경우,

출력 되는 로그의 예시입니다. (`JSON`)

```json
{
  "timestamp": "2024-11-05T09:23:56.6886296+09:00",
  "LogLevel": "Information",
  "Category": "SampleServer.Services.LoggingService",
  "EventId": 0,
  "EventIdName": null,
  "membername": "UserEnter",
  "FilePath": "C:\\Users\\GSY\\source\\repos\\ZLoggerSolution\\SampleServer\\Services\\LoggingService.cs",
  "LineNumber": 84,
  "Message": "User Logged in ",
  "scope": { "game_guid": "f2abfb13-4795-4f5b-96f7-e4c273c63c33" }
}
```

```json
{
  "timestamp": "2024-11-05T09:23:56.6891689+09:00",
  "LogLevel": "Information",
  "Category": "SampleServer.Services.LoggingService",
  "EventId": 0,
  "EventIdName": null,
  "membername": "StartGame",
  "FilePath": "C:\\Users\\GSY\\source\\repos\\ZLoggerSolution\\SampleServer\\Services\\LoggingService.cs",
  "LineNumber": 89,
  "Message": "Game Started",
  "scope": { "game_guid": "f2abfb13-4795-4f5b-96f7-e4c273c63c33" }
}
```

# Logging Providers

ZLogger 로그 제공자 선택을 통해 원하는 방식으로 로그를 출력할 수 있습니다.

## Provider Types

| Provider Alias        | Description                                                                                                                                                                                                                                                                                                                                          |
| :-------------------- | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ZLoggerConsole`      | 로그를 콘솔에 출력하도록 합니다.                                                                                                                                                                                                                                                                                                                     |
| `ZLoggerFile`         | 로그를 파일에 기록합니다.                                                                                                                                                                                                                                                                                                                            |
| `ZLoggerRollingFile`  | 특정 간격에 따라 로그파일을 교체(rolling)하여 생성합니다.                                                                                                                                                                                                                                                                                            |
| `ZLoggerStream`       | 로그를 지정 stream으로 출력합니다.                                                                                                                                                                                                                                                                                                                   |
| `ZLoggerInMemory`     | 로그를 메모리에 저장하여 테스트나 지정 프로세스 내 구독 이벤트 처리에 활용할 수 있습니다.                                                                                                                                                                                                                                                            |
| `ZLoggerLogProcessor` | 커스텀 `IAsyncLogProcessor` 활용을 통해 로그 출력을 사용자 정의할 수 있게 합니다. `IZLoggerEntry` 인스턴스를 사용하여 로그를 개별 처리하거나, 로그 일괄 처리를 위한 `BatchingAsyncLogProcessor`를 활용하여 한 번의 HTTP 요청으로 여러 로그를 전송할 수 있습니다. 기본적으로 `IZLoggerEntry`는 풀링되므로 항상 `Return()`을 호출하여 반환해야 합니다. |

ASP .NET Core 에서는 `Add` + **Provider Alias** + `()`형식의 확장 메서드 사용을 통해 손쉽게 원하는 제공자를 구성할 수 있습니다.

```csharp
builder.Logging.AddZLoggerConsole();
```

> **여러 제공자를 동시에 사용할 수 있습니다.** 예를 들어 콘솔과 파일에 동시에 출력하려면:
> ```csharp
> builder.Logging.AddZLoggerConsole();       // 콘솔 출력
> builder.Logging.AddZLoggerRollingFile();   // 파일 출력
> ```
> 하나의 `ZLogInformation()` 호출로 두 곳에 동시 기록됩니다.

## ZLoggerRollingFile 설정 예시

운영 환경에서 가장 많이 사용하는 `ZLoggerRollingFile`의 설정 예시입니다.
일정 간격 또는 파일 크기 초과 시 자동으로 새 파일을 생성합니다.

```csharp
// Program.cs (SampleServer 참고)
builder.Logging.AddZLoggerRollingFile(options =>
{
    // JSON 형식으로 기록
    options.UseJsonFormatter();

    // 파일 경로 규칙: logs/2024-11-05_000.log, logs/2024-11-05_001.log, ...
    options.FilePathSelector = (timestamp, sequenceNumber)
        => $"logs/{timestamp.ToLocalTime():yyyy-MM-dd}_{sequenceNumber:000}.log";

    // 일 단위로 파일 교체
    options.RollingInterval = ZLogger.Providers.RollingInterval.Day;

    // 1MB 초과 시 새 파일 생성 (같은 날이라도)
    options.RollingSizeKB = 1024;
});
```

**파일 생성 예시:**

```
logs/
├── 2024-11-05_000.log    ← 11월 5일 첫 번째 파일
├── 2024-11-05_001.log    ← 1MB 초과 시 자동 생성된 두 번째 파일
├── 2024-11-06_000.log    ← 11월 6일 자동 교체
└── 2024-11-07_000.log
```

---

# Provider Options

옵션은 공통적으로 사용할 수 있는 옵션 `ZLoggerOptions`과, 제공자별 전용 옵션으로 나뉩니다.

## 제공자 옵션 활용 가이드

모든 제공자는 `ZLoggerOptions`를 변경할 수 있는 Action을 제공 받습니다.

예를 들어 본 프로젝트에서는 아래와 같이 `UseJsonFormatter()` 옵션을 사용하여 로그가 JSON 형식으로 출력되도록 합니다.

```csharp
builder.Logging.AddZLoggerConsole(options =>
{
    options.UseJsonFormatter(); // JSON 형식으로 출력
});
```

#### 공통 옵션 `ZLoggerOptions`

\*`ZLoggerInMemoryProvider`는 공통 옵션에서 제외됩니다.

| Option Name                               | Description                                                                                                                                                                                     |
| :---------------------------------------- | :---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [`IncludeScopes`](#includescopes)         | `BeginScope` 메서드 활성화 여부를 선택합니다. `BeginScope`를 사용하면 여러 로그 항목에 걸쳐 동일한 컨텍스트 데이터를 포함할 수 있어, 로그 범위 설정과 맥락 파악에 유용합니다. (기본값: `false`) |
| [`TimeProvider`](#timeprovider)           | 로그 출력 시간대 소스를 직접 설정할 수 있습니다. (기본값: `DateTime.UtcNow`)                                                                                                                    |
| `FullMode`                                | 비동기 버퍼가 가득 찼을 때 처리 방식을 설정합니다. `Grow`, `Block`, `Drop` 중 선택 가능하며, 각각 대기, 큐 확장, 또는 초과 항목 삭제의 동작을 정의합니다. (기본값: `Grow`)                      |
| `BackgroundBufferCapacity`                | 비동기 처리에서 사용되는 버퍼의 최대 용량을 설정합니다. FullMode가 `Grow`인 경우 이 옵션은 무시됩니다. (기본값: `10000`)                                                                        |
| `IsFormatLogImmediatelyInStandardLog`     | 로그 포맷을 즉시 적용 여부. 즉시 포맷팅을 선택하면 로그가 기록될 때마다 완전한 포맷으로 즉시 저장되지만, 성능에 부정적인 영향을 줄 수 있습니다. (기본값: `false`)                               |
| `CaptureThreadInfo`                       | 로그에 쓰레드 정보 포함 여부. (기본값: `false`)                                                                                                                                                 |
| `UseFormatter()`                          | 사용자 지정 형식을 정의하여 출력합니다.                                                                                                                                                         |
| `UsePlainTextFormatter()`                 | 기본 텍스트 형식으로 출력합니다.                                                                                                                                                                |
| [`UseJsonFormatter()`](#usejsonformatter) | `System.Text.Json`을 사용하여 JSON 형식으로 출력합니다.                                                                                                                                         |

#### IncludeScopes

```csharp
builder.Logging.AddZLoggerConsole(options =>
{
    options.IncludeScopes = true;
});
```

`IncludeScopes`를 활성화 하면 `BeginScope` 메서드 사용을 통해 특정 범위 내의 모든 로그 항목에 데이터를 추가할 수 있습니다.

> **`BeginScope`란?** 여러 로그에 걸쳐 동일한 컨텍스트 정보(예: 요청 ID, 유저 ID)를 자동으로 포함시키는 기능입니다. 매번 로그에 직접 넣지 않아도 scope 안의 모든 로그에 자동으로 붙습니다.

예를 들어, Id 값이 해당 범위 내의 각 로그 항목에 포함되도록 작성합니다.

```csharp
using (logger.BeginScope("{Id}", id))
{
    logger.ZLogInformation($"Scoped log {name}");
}
```

출력 예시:

```powershell
# id = 123
# name = "SampleName"
> [Information] Scoped log SampleName {Id=123}
```

스코프는 중첩하여 사용할 수도 있습니다.

```csharp
// SampleServer의 LoggingController.cs - ScopeDemo() 참고
using (logger.BeginScope("A={A}", 100))
{
    logger.ZLogInformation($"Message 1");         // scope: A=100
    using (logger.BeginScope("B={B}", 200))
    {
        logger.ZLogInformation($"Message 2");     // scope: A=100, B=200 (중첩!)
    }
}
```

출력 예시:

```powershell
[Information] Message 1 {A=100}
[Information] Message 2 {A=100, B=200}
```

> **게임 서버에서의 활용:** SampleServer의 `GameService.cs`에서 `BeginScope`로 `GameGuid`를 설정하면, 해당 게임 세션 동안 발생하는 모든 로그에 게임 식별자가 자동 포함되어 문제 추적이 쉬워집니다.

#### TimeProvider

다음은 TimeProvider를 고정 시간으로 설정하는 예제입니다.

```csharp
using Microsoft.Extensions.Logging;
using ZLogger;
using System;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddZLoggerConsole(options =>
    {
        options.TimeProvider = () => DateTime.UtcNow.AddHours(9);       // JST (UTC+9)
        options.UseJsonFormatter((                                      // JSON 형식 사용 (선택 사항)
            formatter.IncludeProperties = IncludeProperties.Timestamp  // Timestamp만 출력되도록 구성
        ));
    });
});

var logger = loggerFactory.CreateLogger<Program>();

logger.ZLogInformation($"Test Log");

```

위 코드에서 TimeProvider는 `DateTime.UtcNow.AddHours(9)`로 설정되어, 일본 표준시(JST, UTC+9)로 로그가 기록되도록 구성됩니다.

출력되는 로그는 다음과 같습니다:

```json
{
  "Timestamp": "2024-11-05T21:34:56.789+09:00"
}
```

#### UseJsonFormatter

`ZLoggerOptions`의 `UseJsonFormatter()` 옵션을 사용하면 로그를 `JSON` 형식으로 출력할 수 있습니다.

이 옵션을 활성화하면 로그 데이터를 구조화된 JSON 형식으로 저장하여 로그 파싱 및 분석에 유용합니다.

```csharp
using Microsoft.Extensions.Logging;
using ZLogger;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddZLoggerConsole(options =>
    {
		options.UseJsonFormatter(formatter =>
		{
			formatter.JsonPropertyNames = JsonPropertyNames.Default with
			{
				Timestamp = JsonEncodedText.Encode("timestamp"),
				MemberName = JsonEncodedText.Encode("membername"),
				Exception = JsonEncodedText.Encode("exception"),
			};

			formatter.JsonSerializerOptions = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			};

			formatter.KeyNameMutator = KeyNameMutator.LastMemberNameLowerFirstCharacter;
			formatter.IncludeProperties =
			IncludeProperties.Timestamp |
			IncludeProperties.ParameterKeyValues |
			IncludeProperties.MemberName |
			IncludeProperties.Message |
			IncludeProperties.Exception;
		});
    });
});

var logger = loggerFactory.CreateLogger<Program>();

int userId = 123;
string userName = "Alice";
string action = "UserLoggedIn";
var User = new { UserId = userId, UserName = userName, Action = action };

// 키-값 파라미터를 포함하여 로그 작성
logger.ZLogInformation($"{User}");
```

아래는 해당 설정에서 출력되는 결과입니다.

```json
{
  "timestamp": "2024-11-05T09:13:29.0409654+09:00",
  "membername": "Log",
  "Message": "{ UserId = 123, UserName = Alice, Action = UserLoggedIn }",
  "user": { "user_id": 123, "user_name": "Alice", "action": "UserLoggedIn" }
}
```

## ZLoggerConsole

#### 콘솔 출력 전용 옵션 `ZLoggerConsoleOptions`

콘솔 출력(`ZLoggerConsole`) 전용 옵션입니다.

| Provider Name                   | Description                                                                                                                                                           |
| :------------------------------ | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `OutputEncodingToUtf8`          | `Console.OutputEncoding = new UTF8Encoding(false)` 설정으로 provider를 생성하여, BOM(Byte Order Mark)이 없는 UTF-8 형식으로 로그를 출력합니다. (기본값: `true`)       |
| `ConfigureEnableAnsiEscapeCode` | 콘솔에서 가상 터미널 처리를 구성하여 ANSI 코드를 사용할 수 있게합니다. ANSI 코드를 활성화하면 텍스트 색상, 굵기등의 특수 텍스트 형식을 지원합니다. (기본값: `false`) |
| `LogToStandardErrorThreshold`   | 설정된 로그 레벨 이상일 경우 표준 오류 출력(`stderr`)으로 로그를 보냅니다. (기본값: `LogLevel.None`)                                                                  |

---

# ZLogger Formatter Configuration

## JSON

#### JSON 형식 출력 세부 설정 `SystemTextJsonZLoggerFormatter`

앞서 언급한 `UseJsonFormatter()`의 세부 설정을 통해,

필요한 로그가 원하는 형식으로 출력되도록 합니다.

```csharp
builder.Logging.AddZLoggerConsole(options =>
{
	options.UseJsonFormatter(formatter =>
	{
		// 로그 기본 키네임 변경
		formatter.JsonPropertyNames = JsonPropertyNames.Default with
		{
			Timestamp = JsonEncodedText.Encode("timestamp"),
			MemberName = JsonEncodedText.Encode("membername"),
			Exception = JsonEncodedText.Encode("exception"),
		};
		// System.Text.Json 제공 옵션 변경 (JSON 직렬화시 적용되는 설정)
		formatter.JsonSerializerOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
		};
		// 로그 기본 키네임 컨벤션 변경
		formatter.KeyNameMutator = KeyNameMutator.LastMemberNameLowerFirstCharacter;
		// 로그 구성 선택
		formatter.IncludeProperties = IncludeProperties.Timestamp | IncludeProperties.ParameterKeyValues;
	});
});
```

설정 가능한 항목은 아래와 같습니다.

| Provider Name                             | Description                                                                          |
| :---------------------------------------- | :----------------------------------------------------------------------------------- |
| [`JsonPropertyNames`](#jsonpropertynames) | ([로그 기본 구성](#로그의-기본-구성)만 해당) 각 속성의 키 이름을 설정할 수 있습니다. |
| [`IncludeProperties`](#includeproperties) | 수집할 속성을 추가하거나 제한합니다.                                                 |
| `JsonSerializerOptions`                   | `System.Text.Json`의 JSON 직렬화 옵션을 설정할 수 있습니다.                          |
| `AdditionalFormatter`                     | 로그 기본 구성외의 추가적인 데이터의 포맷팅을 설정할 수 있습니다.                    |
| `PropertyKeyValuesObjectName`             | 전달되는 `ParameterKeyValues` 를 지정된 이름 아래에 중첩하여 출력하도록 합니다.      |
| [`KeyNameMutator`](#keynamemutator)       | 각 속성의 키 네이밍 컨벤션을 지정합니다.                                             |
| `UseUtcTimestamp`                         | 타임스탬프를 UTC 형식으로 출력합니다. (기본값: `false`)                              |

이전 단계에서 서버에 ZLogger 적용을 완료 하였으면, 아래와 같이 `ZLogInformation` 메서드를 사용할 수 있습니다.

```csharp
var user = new User(1, "Alice");
logger.ZLogInformation($"Name: {user.Name}");
```

### KeyNameMutator

위의 예시를 별도의 설정없이 `JSON` 형식으로만 출력할 경우, 위 코드는 아래와 같이 출력됩니다.

```json
{ "user.Name": "Alice" }
```

기본설정으로 `JSON` 형식 출력 시, 입력 당시의 객체의 키값을 그대로 사용하기 때문에

`KeyNameMutator`옵션 사용을 통해 기본 네이밍 규칙을 변경할 수 있습니다.

예를 들어, `KeyNameMutator`을 `LastMemberName`으로 설정하면 해당 값의 키로 마지막 멤버 이름만을 가져오게 합니다.

```json
{ "Name": "Alice" }
```

여기서 `LastMemberNameLowerFirstCharacter` 옵션을 통해 소문자 형식을 추가로 지정할 수 있습니다.

```csharp
formatter.KeyNameMutator = KeyNameMutator.LastMemberNameLowerFirstCharacter;
```

해당 옵션을 적용할경우 동일한 로그가 아래와 같이 출력됩니다.

```json
{ "name": "Alice" }
```

### JsonPropertyNames

`JsonPropertyNames` 는 로그 속성 키네임 전체를 대체할 때 사용됩니다.

네이밍 규칙 변경만으로 원하는 키값을 출력하기 어려울때 사용하면 됩니다.

예를 들어, 아래와 같이 `TimeStamp` 와 `MemberName`을 변경합니다.

```csharp
formatter.JsonPropertyNames = JsonPropertyNames.Default with
{
    Timestamp = JsonEncodedText.Encode("timestamp"),
    MemberName = JsonEncodedText.Encode("membername"),
};
```

이렇게 설정한 속성들의 키 값은 아래와 같이 출력됩니다.

```json
{
  "timestamp": "2024-10-02T08:29:50.7544882+00:00",
  "membername": "ActionLog"
}
```

### IncludeProperties

`IncludeProperties`는 [로그 기본 구성](#로그의-기본-구성)키 값으로 이루어진 `enum`입니다.

> ZLogger.IncludeProperties

```csharp
[Flags]
public enum IncludeProperties
{
    None = 0,
    Timestamp = 1,
    LogLevel = 2,
    CategoryName = 4,
    EventIdValue = 8,
    EventIdName = 0x10,
    Message = 0x20,
    Exception = 0x40,
    ScopeKeyValues = 0x80,
    ParameterKeyValues = 0x100,
    MemberName = 0x200,
    FilePath = 0x400,
    LineNumber = 0x800,
    Default = 0x1E7,
    All = 0xFFF
}
```

옵션 값 지정을 통해 원하는 로그의 구성을 선택할 수 있습니다.

예를들어 이렇게 설정할 경우,

```csharp
formatter.IncludeProperties = IncludeProperties.Timestamp;
```

아래와 같이 로그는 `Timestamp` 속성만 출력되게 됩니다.

```json
{
  "Timestamp": "2024-10-02T08:29:50.7544882+00:00"
}
```

> **`|` 연산자로 여러 속성을 조합할 수 있습니다:**
> ```csharp
> // Timestamp와 Message만 출력
> formatter.IncludeProperties = IncludeProperties.Timestamp | IncludeProperties.Message;
>
> // 개발 중에는 All로 전체 출력, 운영에서는 필요한 것만 선택
> formatter.IncludeProperties = IncludeProperties.All;
> ```

---

## 로그별 형식 사용자 지정하기

로그별로 메서드 호출시에 syntax 활용을 통해 로그 형식을 변경할 수 있습니다.

### Types of syntax

#### 사용자 정의 형식 `:`

`:` 구문은 변수에 사용자 정의 형식을 적용하여 로그 메시지에 표시할 때 사용됩니다.

예를 들어 아래 `ActionLog` 메서드에서 사용되는 context는 object 타입이지만, 아래처럼 `:json`을 활용해 객체를 JSON으로 직렬화합니다.

```csharp
// SampleServer의 BaseController.cs 참고
protected void ActionLog(object context, [CallerMemberName] string? tag = null)
{
    _logger.ZLogInformation($"[{tag:json}] {context:json}");
}
```

> **`:json`이란?** ZLogger의 String Interpolation 안에서 사용하는 특수 포맷 지시자입니다. 일반 `$"{context}"`는 `ToString()`을 호출하지만, `$"{context:json}"`은 `System.Text.Json`으로 직렬화하여 구조화된 JSON 객체로 기록합니다. 익명 객체(`new { uid = 1 }`)를 로그에 넣을 때 특히 유용합니다.

#### 명시적 이름 변경 `:@`

`:@` 구문은 구조화된 데이터로 로깅할 때 변수의 이름을 명시적으로 변경할 수 있습니다.

기본적으로 ZLogger는 변수 이름을 구조화된 로그의 속성 키로 사용하지만, `:@newname`을 사용하면 해당 변수의 key name을 로그 출력에서 `newname`으로 지정할 수 있습니다.

```csharp
var userId = 42;
logger.ZLogInformation($"User logged in: {userId:@uid}");
// JSON 출력: { "uid": 42 }  ← "userId" 대신 "uid"로 기록됨
```

#### 사용자 정의 형식과 명시적 이름 변경 조합

`:@`(명시적 이름 변경)과 `:`(사용자 정의 형식)을 함께 사용하여 변수의 이름을 지정하고, 동시에 형식을 적용할 수도 있습니다.

```csharp
logger.ZLogDebug($"Today is {DateTime.Now:@date:yyyy-MM-dd}.");
```

위 예시에서는 property 이름을 `date`로 변경하고, 날짜 형식을 `yyyy-MM-dd`로 지정합니다.

```json
{ "date": "2024-11-05", "Message": "Today is 2024-11-05." }
```

---

# Zlogger를 활용한 로그 구조화

통일화된 로그 형태를 위해 ZLogger를 사용하는 서버의 메서드들을 추상화 합니다.

> **왜 추상화하는가?** 프로젝트 전체에서 로그 형식이 제각각이면 로그 검색과 모니터링이 어렵습니다. `BaseController`와 `BaseLogger`로 로깅 메서드를 통일하면:
> - 모든 로그가 동일한 JSON 구조를 가짐
> - `membername`으로 로그 종류(ActionLog, ErrorLog 등)를 즉시 구분 가능
> - `caller`로 어떤 메서드에서 호출했는지 자동 기록
> - 새로운 컨트롤러/서비스를 만들 때 상속만 하면 로깅 패턴이 자동 적용

### ActionLog on Controllers

```csharp
// SampleServer의 Controllers/BaseController.cs
public abstract class BaseController<T> : ControllerBase where T : class
{
    protected readonly ILogger<T> _logger;

    protected BaseController(ILogger<T> logger)
    {
        _logger = logger;
    }

    // [CallerMemberName]: 이 메서드를 호출한 메서드의 이름이 자동으로 tag에 들어감
    protected void ActionLog(object context, [CallerMemberName] string? tag = null)
    {
        _logger.ZLogInformation($"[{tag:json}] {context:json}");
    }
}
```

유저 기반 이벤트가 발생할때에는 컨트롤러에서 `ActionLog`를 호출하여

`[CallerMemberName]`을 통해 실행된 메서드 이름과 함께 로그 상세정보(`context`)를 기록합니다.

```csharp
// SampleServer의 Controllers/LoginController.cs
[Route("[controller]")]
[ApiController]
public class LoginController : BaseController<LoginController>
{
    ...

    [HttpPost]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        var response = new LoginResponse();
        var (errorCode, (uid, token)) = _service.LoginUser(request.PlayerId, request.Token);

        if (errorCode == ErrorCode.None)
        {
            // ActionLog 호출 → tag에 "Login"이 자동 기록됨
            ActionLog(new
            {
                uid
            });
        }

        ...
    }
}
```

예를 들어, 로그인에 성공했을 경우, 위와 같이 ActionLog가 실행됩니다.

해당 메서드는 아래와 같이 콘솔에 출력합니다.

```json
{
  "timestamp": "2024-10-02T08:29:50.7544882+00:00",
  "membername": "ActionLog",
  "tag": "Login",
  "context": {
    "uid": 1
  }
}
```

### Metric Log in Services

Metric Log는 유저 액션 기반이 아닌 시스템 이벤트 발생 시점으로 부터 기록합니다.

```csharp
// SampleServer의 Services/BaseLogger.cs
public abstract class BaseLogger<T> where T : class
{
    protected readonly ILogger<T> _logger;

    protected BaseLogger(ILogger<T> logger)
    {
        _logger = logger;
    }

    protected void MetricLog(string tag, object context)
    {
        _logger.ZLogInformation($"[{tag:json}] {context:json}");
    }
}
```

SampleServer의 `GameService.cs`에서 게임이 생성될 때 MetricLog를 호출합니다.

```csharp
// SampleServer의 Services/GameService.cs
public (ErrorCode, string gameGuid) StartGame(Int64 uid)
{
    var gameGuid = Guid.NewGuid().ToString();

    using (_logger.BeginScope(new { GameGuid = gameGuid }))
    {
        MetricLog("GameCreated", new { gameGuid, uid });
    }

    return (ErrorCode.None, gameGuid);
}
```

해당 메서드 실행을 통해 아래와 같은 로그가 출력됩니다.

```json
{
  "timestamp": "2024-10-02T08:29:50.7544882+00:00",
  "membername": "MetricLog",
  "tag": "GameCreated",
  "context": {
    "game_guid": "...",
    "uid": 100
  },
  "scope": {
    "game_guid": "..."
  }
}
```

### 출력용 메서드

디버깅 목적으로 출력되는 로그 메서드 입니다.

공통 항목

| Parameter    | Description                                                                                             |
| :----------- | :------------------------------------------------------------------------------------------------------ |
| `caller`     | 로깅 메서드를 호출한 함수 이름입니다. 자동으로 메서드명이 기록됩니다.                                   |
| `context`    | 로그와 관련된 추가적인 객체나 데이터를 포함할 수 있습니다.                                              |
| `membername` | ZLogger 메서드를 호출한 함수 이름입니다. 로깅 메서드 종류를 구분합니다. 자동으로 메서드명이 기록됩니다. |

#### InformationLog

```csharp
// BaseController.cs, BaseLogger.cs 모두에 정의
protected void InformationLog(string message, object? context = default, [CallerMemberName] string? caller =null)
{
	_logger.ZLogInformation($"[{caller}] {message} {context:json}");
}
```

정상적인 흐름이나 특정 동작이 수행되었음을 알리고자 할때 사용됩니다.

이전에 로그인 성공시 호출되던 ActionLog 와 함께 Information Log를 호출하여 보겠습니다.

```csharp
// SampleServer의 LoginController.cs 참고
public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
{
    // .. 로그인.. 생략

    if (errorCode == ErrorCode.None)
    {
        response.AccessToken = token;
        response.Uid = uid;

        // context: 유저 UID
        ActionLog(new
        {
            uid
        });

        // context: 처리 결과 전체, 디버깅용
        InformationLog("User Logged in", response);
    }
}
```

RESULT:

```json
{
  "timestamp": "2024-10-10T07:46:08.9486195+00:00",
  "membername": "InformationLog",
  "caller": "Login",
  "message": "User Logged in",
  "context": {
    "uid": 1,
    "access_token": "...",
    "result": 0
  }
}
```

`InformationLog`에 첨부된 `response`를 `context`에서 확인할 수 있습니다.

#### ExceptionLog

```csharp
// BaseLogger.cs에 정의
protected void ExceptionLog(Exception ex, object? context = default, [CallerMemberName] string? caller = null)
{
	_logger.ZLogError(ex, $"[{caller}] {context:json}");
}
```

시스템 오류 예외 처리 과정에서 사용됩니다.

Exception 객체(스택 추적 포함)를 통해 문제가 발생한 위치를 파악하고 진단하는 데 도움을 줍니다.

> **`ZLogError`의 첫 번째 인자로 `Exception`을 전달하면**, 스택 트레이스가 자동으로 JSON의 `exception` 필드에 기록됩니다. 이는 운영 환경에서 오류 원인을 추적할 때 핵심적인 정보입니다.

다음은 MemoryDb에서 Redis CloudStructures를 활용하여 RedisConnection으로부터 값을 가져오는 `GetAsync` 메서드입니다.

```csharp
public async Task<(ErrorCode, T?)> GetAsync<T>(string key)
{
    try
    {
        RedisString<T> redisData = new(_redisConnection, key, null);
        RedisResult<T> result = await redisData.GetAsync();
		return (ErrorCode.None, result.Value);
	}
    catch (Exception e)
    {
        ExceptionLog(e, $"{typeof(T).Name}:{key}");
        return (ErrorCode.RedisGetException, default(T));
    }
}
```

MemoryDb에서 존재하지 않는 값을 조회하려 할 때 발생하는 `InvalidOperationException`에 대한 출력 예시입니다.

```json
{
  "timestamp": "2024-10-10T08:15:24.8677434+00:00",
  "exception": {
    "Name": "System.InvalidOperationException",
    "Message": "has no value.",
    "StackTrace": "   at CloudStructures.RedisResult\u00601.get_Value()\n   at ServerShared.Repository.MemoryDb.GetAsync[T](String key)",
    "InnerException": null
  },
  "membername": "ExceptionLog",
  "caller": "GetAsync",
  "context": "RedisUserSession:US_1"
}
```

RedisKeyValue `US_1`를 이용하여 `RedisUserSession`을 조회하려 했을 때 발생한 것을 확인할 수 있습니다.

> **SampleServer에서 간단히 테스트하려면** `/logging/exception` 엔드포인트를 호출하면 `InvalidOperationException`이 발생하여 스택 트레이스 포함 에러 로그를 확인할 수 있습니다.

#### ErrorLog

```csharp
// BaseController.cs, BaseLogger.cs 모두에 정의
protected void ErrorLog(UInt16 errorCode, object? context = default, [CallerMemberName] string? caller =null)
{
	_logger.ZLogError($"[{caller}] {errorCode} {context:json}");
}
```

서비스 오류 처리 과정에서 사용됩니다.

오류가 발생 했을때 `ErrorCode`(오류 유형)과 관련 정보를 통해 디버깅을 돕습니다.

> **ExceptionLog vs ErrorLog의 차이:**
> - **ExceptionLog**: `try-catch`에서 잡힌 `Exception` 객체가 있을 때 사용. 스택 트레이스가 포함됨
> - **ErrorLog**: Exception 없이 비즈니스 로직에서 오류를 감지했을 때 사용. ErrorCode로 오류 유형 구분

로그인 요청시 발생하는 오류를 예시로 살펴보겠습니다.

```csharp
// SampleServer의 LoginController.cs 참고
public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
{
    var response = new LoginResponse();

    var (errorCode, (uid, token)) = _service.LoginUser(request.PlayerId, request.Token);

    if (errorCode == ErrorCode.None)
    {
        response.Uid = uid;
        response.AccessToken = token;

        ActionLog(new { uid });
        InformationLog("User Logged in", response);
    }
    else
    {
        ErrorLog((UInt16)errorCode, request);
    }
}
```

이전에 명시되었던 로그인 요청 함수에, 실패시 `ErrorLog`를 남기는 부분이 추가되었습니다.

SampleServer의 `GameService`에서 잘못된 토큰으로 로그인하면 `ErrorLog`가 기록됩니다.

```csharp
// SampleServer의 GameService.cs
public (ErrorCode, (Int64 uid, string token)) LoginUser(Int64 playerId, string token)
{
    try
    {
        if (string.IsNullOrEmpty(token) || token == "invalid")
        {
            ErrorLog((UInt16)ErrorCode.LoginTokenInvalid, new { playerId });
            return (ErrorCode.LoginTokenInvalid, (0, string.Empty));
        }
        // ...
    }
    catch (Exception ex)
    {
        ExceptionLog(ex, new { playerId });
        return (ErrorCode.InternalError, (0, string.Empty));
    }
}
```

서비스에서의 `ErrorLog` 출력 예시:

```json
{
  "timestamp": "2024-10-10T08:21:58.0954751+00:00",
  "membername": "ErrorLog",
  "caller": "LoginUser",
  "errorCode": 1001,
  "context": {
    "player_id": 2
  }
}
```

컨트롤러에서의 `ErrorLog` 출력 예시:

```json
{
  "timestamp": "2024-10-10T08:21:58.0954838+00:00",
  "membername": "ErrorLog",
  "caller": "Login",
  "errorCode": 1001,
  "context": {
    "player_id": 2,
    "token": "invalid"
  }
}
```

이렇게 서비스와 컨트롤러 양쪽에서 ErrorLog를 남기면, **오류가 어디서 발생하여 어디로 전파되었는지** 추적할 수 있습니다.
