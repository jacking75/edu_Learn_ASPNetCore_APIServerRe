Directory structure:
└── MetricsAPI/
    ├── README.md
    ├── EchoServer.md
    └── GameAPIServer.md

================================================
File: README.md
================================================
# .NET Metrics API 학습 가이드

## 왜 메트릭이 필요한가?

게임 서버를 운영하면 "지금 서버가 정상인가?", "어떤 API가 느린가?", "동접이 몇 명인가?"를 실시간으로 파악해야 합니다.
**로그**는 "무슨 일이 일어났는지" 기록하고, **메트릭**은 "얼마나 일어나고 있는지" 숫자로 측정합니다.

```
로그:  [2024-10-10 08:15:24] 유저 uid=1 로그인 성공
메트릭: api_requests_total = 15,234  (총 요청 수)
        api_response_time_ms = 23ms  (평균 응답 시간)
        api_active_users = 847       (현재 동접)
```

### .NET Metrics API vs prometheus-net

| | .NET Metrics API | prometheus-net |
|:---|:---|:---|
| **소속** | .NET 표준 라이브러리 (`System.Diagnostics.Metrics`) | NuGet 패키지 (`prometheus-net`) |
| **메트릭 정의** | `Meter`, `Counter<T>`, `Histogram<T>`, `ObservableGauge<T>` | `Metrics.CreateCounter()`, `Metrics.CreateGauge()` |
| **수집 방식** | EventListener, dotnet-counters, OpenTelemetry로 수집 | `/metrics` HTTP 엔드포인트 자동 노출 |
| **장점** | 표준 API, OpenTelemetry 호환, 소켓 서버에도 사용 가능 | 설정 간편, Prometheus 즉시 연동 |
| **적합한 경우** | 커스텀 비즈니스 메트릭, 소켓 서버, OpenTelemetry 활용 시 | ASP.NET Core에서 빠르게 Prometheus 연동할 때 |

> **본 학습에서는 두 가지를 모두 다룹니다.** GameAPIServer에서 .NET Metrics API로 커스텀 메트릭을 정의하고, prometheus-net으로 Prometheus에 노출합니다.

---

## 학습 자료 구조

```
MetricsAPI/
├── README.md                      # 이 문서 (Metrics API 이론 + 실습 가이드)
├── EchoServer/                    # TCP 소켓 서버에서의 Metrics API 활용
│   └── Program.cs                 # Counter, Histogram을 소켓 서버에 적용
└── GameAPIServer/                 # ASP.NET Core 게임 API 서버
    ├── Program.cs                 # 앱 초기화 + Prometheus 설정
    ├── MetricsRegistry.cs         # [학습 핵심] 메트릭 중앙 관리 (4가지 타입)
    ├── Middleware/
    │   └── RequestMetricsMiddleware.cs  # [학습 핵심] 모든 요청 자동 메트릭 수집
    ├── Controllers/               # 12개 API 컨트롤러
    ├── Services/                  # 비즈니스 로직
    ├── Repository/                # DB 접근 (FakeGameDb = Mock)
    └── httpTest.http              # API 테스트 파일
```

### 권장 학습 순서

| 단계 | 파일 | 학습 내용 |
|:---|:---|:---|
| **1단계** | README.md 이론 섹션 | Metrics API 개념, 4가지 메트릭 타입 |
| **2단계** | `MetricsRegistry.cs` | Counter, Histogram, Gauge, ObservableGauge 정의 |
| **3단계** | `Middleware/RequestMetricsMiddleware.cs` | 미들웨어로 모든 요청에 자동 메트릭 적용 |
| **4단계** | `Controllers/LoginController.cs` | 컨트롤러에서 수동 메트릭 기록 |
| **5단계** | `EchoServer/Program.cs` | 소켓 서버에서의 Metrics API 활용 |
| **6단계** | 서버 실행 후 `/metrics` 확인 | 브라우저에서 메트릭 엔드포인트 확인 |
| **7단계** | `dotnet-counters` 사용 | CLI로 실시간 메트릭 모니터링 |

### 실행 방법

```bash
# GameAPIServer 실행
cd MetricsAPI/GameAPIServer
dotnet run
# 서버 주소: http://localhost:11500

# 메트릭 확인 (브라우저에서)
# http://localhost:11500/metrics

# dotnet-counters로 실시간 모니터링 (별도 터미널)
dotnet tool install --global dotnet-counters
dotnet-counters monitor --process-id <PID> --counters ApiServer.Metrics
# PID는 서버 실행 시 출력됨
```

```bash
# EchoServer 실행 (별도 터미널)
cd MetricsAPI/EchoServer
dotnet run
# TCP 서버: localhost:32452

# dotnet-counters로 소켓 메트릭 확인
dotnet-counters monitor --process-id <PID> --counters SocketServer.Metrics
```

---

## Metrics API ?
  
### 1. Metrics API 개요
.NET의 **System.Diagnostics.Metrics** API는 애플리케이션의 동작을 관찰할 수 있도록 카운터(counter), 게이지(gauge), 히스토그램(histogram) 등의 계측 데이터를 정의하고 수집하는 기능을 제공한다. 이 데이터는 OpenTelemetry 같은 관측성(Observability) 도구와 연계되어 모니터링 및 분석에 활용할 수 있다. 서버 개발과 라이브 서비스 환경에서는 성능, 안정성, 비즈니스 지표를 추적하는 데 매우 유용하다.


### 2. 서버 개발 단계에서의 활용
개발 단계에서는 **성능 병목 지점 파악**과 **부하 테스트 대비**를 위해 Metrics API를 도입하는 것이 좋다.

* **요청 처리량 추적**: 초당 요청 수(RPS)를 Counter로 기록해 서버의 부하 분포를 확인한다.
* **응답 시간 측정**: Histogram을 활용해 평균 응답 시간뿐만 아니라 p95, p99 같은 고지연 분포를 모니터링할 수 있다.
* **리소스 사용량 확인**: 메모리, 큐 대기 길이 등을 ObservableGauge로 노출해 특정 조건에서 리소스가 어떻게 소비되는지 추적한다.
* **디버깅 보조**: 특정 API 호출 성공/실패 횟수를 Counter로 집계해 기능별 장애 여부를 빠르게 확인한다.

이를 통해 사전에 성능 저하 구간을 발견하고, 실제 서비스 배포 전에 튜닝할 수 있다.
  

### 3. 라이브 서비스 운영 단계에서의 활용
실서비스 환경에서는 Metrics API가 **실시간 모니터링 지표 제공자** 역할을 한다.

* **서비스 안정성 모니터링**: 에러율, 타임아웃 발생 횟수 등을 Counter로 노출해 알람 시스템과 연동한다.
* **비즈니스 KPI 추적**: 로그인 성공률, 결제 시도/성공 건수 등을 Counter/Histogram으로 기록하면 서비스 운영과 매출 분석에도 직접 활용할 수 있다.
* **자동 확장(Auto Scaling) 지표 제공**: 특정 메트릭(RPS, 처리 대기열 길이 등)을 기반으로 클라우드 오토스케일링 정책을 연동할 수 있다.
* **장애 대응**: 문제가 발생했을 때, Metrics 데이터를 분석하면 로그만으로는 알기 어려운 병목 지점과 영향 범위를 빠르게 파악할 수 있다.

  
### 4. OpenTelemetry 및 모니터링 스택 연계
Metrics API는 **OpenTelemetry .NET SDK**와 쉽게 연동된다. Prometheus, Grafana, Azure Monitor, AWS CloudWatch 같은 모니터링 플랫폼으로 내보내면, 대시보드 기반의 실시간 모니터링과 경고 알람을 구축할 수 있다.

* **개발 단계**: 로컬/테스트 환경에서 콘솔 Exporter, In-Memory Exporter를 활용
* **운영 단계**: Prometheus Exporter 또는 OpenTelemetry Collector를 통해 모니터링 스택과 통합


### 5. 활용 시 베스트 프랙티스
* **도메인별 Metrics 구분**: 네트워크, 데이터베이스, 비즈니스 로직 등 계층별로 구분해 메트릭을 설계한다.
* **고정된 이름 규칙 사용**: `game.server.requests.total` 같은 네이밍 컨벤션을 정해 일관성 유지
* **태그/라벨 적극 활용**: API 엔드포인트명, 지역, 사용자 타입 등을 라벨로 붙여 분석을 세분화
* **샘플링 고려**: 모든 요청을 전부 측정하면 오버헤드가 커질 수 있으므로, 샘플링 비율을 조정



## 개발 단계와 운영 단계에서 .NET Metrics API 활용

### 1. 개발 단계: 로컬/테스트 환경
다음 항목에서 별도로 설명하고 있어서 여기에서는 적지 않는다.


### 2. 운영 단계: 실제 서비스 환경
운영 환경에서는 단순 출력으로는 부족하다. 대규모 트래픽 상황에서 메트릭을 모아 **시각화, 알람, 장기 보관**이 가능해야 한다.

#### (1) Prometheus Exporter

* **동작 방식**:

  * 서버에 `/metrics` 엔드포인트를 자동으로 노출
  * Prometheus가 주기적으로 이 엔드포인트를 스크랩(pull)해서 메트릭을 수집
* **장점**:

  * Prometheus + Grafana와 바로 연동 가능 → 실시간 대시보드 제공
  * 경고 규칙(Alertmanager)을 통해 SLA/SLI 위반 시 알람 발송 가능
* **활용 예시**:

  * 게임 서버의 RPS, 에러율, 응답 지연 시간 분포를 Prometheus로 수집 후 Grafana 대시보드 시각화
  * 결제 성공률이 특정 임계치 밑으로 떨어지면 자동 알람 발송

```csharp
using OpenTelemetry;
using OpenTelemetry.Metrics;

var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("GameServer.Metrics")
    .AddPrometheusExporter(opt => opt.StartHttpListener = true)
    .Build();
```


#### (2) OpenTelemetry Collector

* **동작 방식**:

  * 애플리케이션에서 OpenTelemetry SDK로 메트릭을 수집 → Collector로 전송(export, 주로 OTLP 프로토콜 사용)
  * Collector가 수집한 데이터를 Prometheus, Grafana, CloudWatch, Azure Monitor 등 다양한 백엔드로 전달
* **장점**:

  * Exporter를 애플리케이션 안에 직접 두지 않고 Collector로 모아 중앙 집중 관리
  * 운영 환경에서 벤더 종속성을 줄이고, 백엔드 교체/확장이 용이
  * 로드밸런싱, 샘플링, 집계, 필터링 같은 기능을 Collector에서 처리 가능
* **활용 예시**:

  * 여러 리전(region)의 게임 서버에서 오는 메트릭을 Collector가 통합 수집
  * Collector에서 비즈니스 메트릭과 시스템 메트릭을 한 번에 AWS CloudWatch로 전달
  * 트래픽 과다 시 일부 메트릭만 샘플링해서 전송

Collector 구성 예시 (otel-collector-config.yaml):

```yaml
receivers:
  otlp:
    protocols:
      grpc:
      http:

exporters:
  prometheus:
    endpoint: "0.0.0.0:9464"
  logging:

service:
  pipelines:
    metrics:
      receivers: [otlp]
      exporters: [prometheus, logging]
```


### 3. 비교 요약

| 단계 | Exporter       | 주요 목적            | 장점              | 단점                 |
| -- | -------------- | ---------------- | --------------- | ------------------ |
| 개발 | Console        | 빠른 확인, 디버깅       | 설정 간단, 바로 확인    | 로그 많아지면 분석 어려움     |
| 개발 | In-Memory      | 단위 테스트, 코드 검증    | 자동화 테스트에 적합     | 운영 환경에는 부적합        |
| 운영 | Prometheus     | 실시간 모니터링         | 대시보드, 알람 연동     | Prometheus 서버 필요   |
| 운영 | OTel Collector | 중앙 집중, 멀티 백엔드 연동 | 유연성, 확장성, 벤더 독립 | Collector 운영 비용 발생 |

---

정리하자면, **개발 단계에서는 Console/In-Memory Exporter로 빠르게 검증**하고, **운영 단계에서는 Prometheus Exporter 혹은 OpenTelemetry Collector를 통해 모니터링 스택과 통합**하는 것이 가장 이상적이다.
  

## Metrics API 사용하기

### 1. Metrics API 개요
.NET 6부터 제공되는 **System.Diagnostics.Metrics**는 OpenTelemetry 같은 외부 프레임워크 없이도 사용할 수 있는 기본 계측 API다.

* 핵심 클래스는 `Meter`와 `Instrument` 계열(`Counter`, `Histogram`, `ObservableGauge`, `ObservableCounter`)이다.
* **Meter**: 메트릭을 정의하고 관리하는 단위
* **Instrument**: 측정값을 기록하는 실제 객체

즉, `Meter`를 만들고 그 안에 여러 개의 Counter/Histogram을 정의해서 데이터를 기록하는 구조다.

  
### 2. 주요 API 설명

#### (1) `Meter`
메트릭을 정의하는 컨테이너 역할을 한다.

```csharp
var meter = new Meter("GameServer.Metrics", "1.0");
```

#### (2) `Counter<T>`
* 단조 증가하는 값 기록 (예: 요청 수, 이벤트 발생 횟수)

```csharp
Counter<int> requestCounter = meter.CreateCounter<int>("requests_total");
requestCounter.Add(1); // 요청 처리 시마다 1 증가
```

#### (3) `Histogram<T>`
* 분포를 기록 (예: 응답 시간, 처리량 크기)

```csharp
Histogram<double> responseTime = meter.CreateHistogram<double>("response_time_ms");
responseTime.Record(123.4); // 밀리초 단위 응답시간 기록
```

#### (4) `ObservableGauge<T>`
* **현재 상태 값을 주기적으로 관찰** (예: 현재 큐 길이, 메모리 사용량)

```csharp
ObservableGauge<int> queueLength = meter.CreateObservableGauge("queue_length",
    () => new Measurement<int>(myQueue.Count));
```

#### (5) `ObservableCounter<T>`
* Gauge와 유사하지만 **누적 증가하는 값**을 주기적으로 관찰 (예: 총 바이트 전송량)

  
### 3. 개발 단계에서 활용
* **빠른 디버깅**:
  Counter/Histogram을 사용해서 특정 기능 호출 시 계측값을 기록해두면, 값이 제대로 올라가는지 확인 가능하다.
* **상태 점검**:
  ObservableGauge를 활용해 큐 길이나 동시 접속자 수를 즉시 확인할 수 있다.
* **테스트 코드 검증**:
  In-Memory Listener를 통해 테스트에서 계측값을 직접 검증할 수 있다.
   
예: 요청 카운트와 응답 시간 측정

```csharp
var meter = new Meter("GameServer.Metrics");
var requestCounter = meter.CreateCounter<int>("requests");
var responseTime = meter.CreateHistogram<double>("response_time");

void HandleRequest()
{
    var sw = Stopwatch.StartNew();
    requestCounter.Add(1);
    // ... 실제 로직 ...
    sw.Stop();
    responseTime.Record(sw.Elapsed.TotalMilliseconds);
}
```

### 4. 운영 단계에서 활용
.NET Metrics API는 자체적으로 저장소나 대시보드를 제공하지 않는다. 대신, **EventListener**를 통해 수집하고 다른 시스템으로 흘려보낼 수 있다.
(보기가 불편해서 비추한다)  
  
#### (1) EventListener
.NET에서 `Meter`가 기록하는 데이터를 `EventListener`를 사용해 구독할 수 있다.

```csharp
class MyListener : EventListener
{
    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        Console.WriteLine($"{eventData.EventName}: {string.Join(", ", eventData.Payload)}");
    }
}
```

이를 통해 운영 환경에서도 **커스텀 로깅**이나 **내부 모니터링 시스템**으로 데이터를 전송할 수 있다.

#### (2) ETW(Event Tracing for Windows) / EventPipe
* Windows 환경에서는 ETW, Linux 환경에서는 EventPipe를 통해 메트릭 데이터를 외부 도구(PerfView, dotnet-counters 등)에서 수집 가능하다.
* 예: `dotnet-counters monitor --process-id <pid>` 실행 시 Counter/Histogram 값 실시간 모니터링 가능

 
### 5. 장단점

#### 장점

* .NET 런타임에 내장 → 외부 라이브러리 필요 없음
* 매우 가볍고 단순
* 기본적인 계측(카운터, 분포, 상태 값)은 충분히 커버

#### 단점

* 자체 저장소나 시각화 도구 없음
* 데이터를 장기 보관하거나 고급 분석하려면 EventListener → 다른 로깅/모니터링 시스템 연동 필요
* 운영 환경에서 Prometheus/Grafana 같은 표준 스택과 바로 통합하기는 어려움 (이때 OpenTelemetry가 필요해짐)

  
### ✅ 정리하면:
* **개발 단계**에서는 Counter/Histogram/ObservableGauge를 직접 써서 디버깅, 성능 점검, 테스트 검증에 활용
* **운영 단계**에서는 EventListener, EventPipe 같은 런타임 기능을 통해 데이터를 모니터링하거나 외부 시스템과 연동

 

## API 서버 예제 (Minimal API 기반)

```csharp
using System.Diagnostics.Metrics;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Meter 정의
var meter = new Meter("ApiServer.Metrics", "1.0");

// Counter: 총 요청 수
var requestCounter = meter.CreateCounter<int>("api_requests_total");

// Histogram: 응답 시간 (밀리초)
var responseTimeHistogram = meter.CreateHistogram<double>("api_response_time_ms");

// ObservableGauge: 현재 메모리 사용량
var memoryGauge = meter.CreateObservableGauge("process_memory_mb",
    () => new Measurement<double>(Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0)));

app.MapGet("/hello", () =>
{
    var sw = Stopwatch.StartNew();

    requestCounter.Add(1); // 요청 수 증가
    var result = $"Hello World! {DateTime.Now}";

    sw.Stop();
    responseTimeHistogram.Record(sw.Elapsed.TotalMilliseconds); // 응답 시간 기록

    return result;
});

app.Run("http://localhost:5000");
```

✅ 특징
* `/hello` 요청 시 Counter와 Histogram에 기록
* 현재 프로세스 메모리 사용량을 ObservableGauge로 주기적으로 수집


## API 서버 예제 (컨트룰러 클래스 사용)

### (1) 정적 클래스에 Meter 보관

```csharp
using System.Diagnostics.Metrics;

public static class MetricsRegistry
{
    public static readonly Meter Meter = new("ApiServer.Metrics", "1.0");

    public static readonly Counter<int> RequestCounter =
        Meter.CreateCounter<int>("api_requests_total");

    public static readonly Histogram<double> ResponseTimeHistogram =
        Meter.CreateHistogram<double>("api_response_time_ms");
}
```

* `MetricsRegistry`를 하나 만들어두고, 모든 컨트롤러에서 가져다 쓰면 됨
* `Meter`를 앱 전체에서 단 하나만 두는 것이 권장됨
 

### (2) DI(의존성 주입) 방식
좀 더 **ASP.NET Core 친화적**인 방법은 `Meter`와 `Instrument`를 `IServiceCollection`에 등록해서 필요할 때 컨트롤러에서 주입받는 방식이다.

```csharp
// Program.cs
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Meter>(new Meter("ApiServer.Metrics", "1.0"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<Meter>().CreateCounter<int>("api_requests_total"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<Meter>().CreateHistogram<double>("api_response_time_ms"));

var app = builder.Build();
app.MapControllers();
app.Run();
```


### 컨트롤러에서 사용 예시

#### (1) 정적 클래스 방식

```csharp
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

[ApiController]
[Route("[controller]")]
public class HelloController : ControllerBase
{
    [HttpGet]
    public string Get()
    {
        var sw = Stopwatch.StartNew();

        MetricsRegistry.RequestCounter.Add(1);

        var result = $"Hello from controller at {DateTime.Now}";

        sw.Stop();
        MetricsRegistry.ResponseTimeHistogram.Record(sw.Elapsed.TotalMilliseconds);

        return result;
    }
}
```

#### (2) DI 방식

```csharp
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Diagnostics.Metrics;

[ApiController]
[Route("[controller]")]
public class HelloController : ControllerBase
{
    private readonly Counter<int> _requestCounter;
    private readonly Histogram<double> _responseTime;

    public HelloController(Counter<int> requestCounter,
                           Histogram<double> responseTime)
    {
        _requestCounter = requestCounter;
        _responseTime = responseTime;
    }

    [HttpGet]
    public string Get()
    {
        var sw = Stopwatch.StartNew();

        _requestCounter.Add(1);
        var result = $"Hello from DI at {DateTime.Now}";

        sw.Stop();
        _responseTime.Record(sw.Elapsed.TotalMilliseconds);

        return result;
    }
}
```

### 어떤 방식이 더 좋을까?

* **작은 프로젝트/빠른 프로토타입** → 정적 클래스(`MetricsRegistry`) 사용이 간단
* **운영/대규모 프로젝트** → DI 방식이 더 바람직

  * 테스트하기 좋음 (Mock 계측기 주입 가능)
  * 다른 서비스와 동일한 관리 방식


✅ 정리

* Metrics API는 애플리케이션 내에서 **Meter 인스턴스를 공유**하는 게 핵심이다.
* 여러 컨트롤러(다른 파일)에서 쓰려면 `Meter`와 `Instrument`를 **정적 Registry** 또는 **DI 컨테이너**를 통해 관리하면 된다.


### MetricsRegistry를 싱글톤으로 등록하는 방식

```csharp
using System.Diagnostics.Metrics;

public class MetricsRegistry
{
    private readonly Meter _meter;

    public Counter<int> RequestCounter { get; }
    public Histogram<double> ResponseTime { get; }
    public ObservableGauge<double> MemoryGauge { get; }

    public MetricsRegistry()
    {
        _meter = new Meter("ApiServer.Metrics", "1.0");

        RequestCounter = _meter.CreateCounter<int>("api_requests_total");
        ResponseTime = _meter.CreateHistogram<double>("api_response_time_ms");
        MemoryGauge = _meter.CreateObservableGauge("process_memory_mb",
            () => new Measurement<double>(GC.GetTotalMemory(false) / (1024.0 * 1024.0)));
    }
}
```

#### Program.cs에서 등록

```csharp
var builder = WebApplication.CreateBuilder(args);

// MetricsRegistry를 싱글톤으로 등록
builder.Services.AddSingleton<MetricsRegistry>();

builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
```

#### 컨트롤러에서 사용

```csharp
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

[ApiController]
[Route("[controller]")]
public class HelloController : ControllerBase
{
    private readonly MetricsRegistry _metrics;

    public HelloController(MetricsRegistry metrics)
    {
        _metrics = metrics;
    }

    [HttpGet]
    public string Get()
    {
        var sw = Stopwatch.StartNew();

        _metrics.RequestCounter.Add(1);  // 카운터 증가
        var result = $"Hello from Controller at {DateTime.Now}";

        sw.Stop();
        _metrics.ResponseTime.Record(sw.Elapsed.TotalMilliseconds); // 응답시간 기록

        return result;
    }
}
```

#### 이 방식의 장점
* **DI 친화적**: 컨트롤러는 `MetricsRegistry` 하나만 주입받으면 됨
* **확장성**: 새로운 메트릭을 추가해도 컨트롤러 생성자 변경이 필요 없음
* **테스트 용이**: 단위 테스트에서는 `MetricsRegistry`의 Mock/Fake 객체를 주입하면 됨
* **일관성**: 모든 Meter와 Instrument가 중앙에서 정의되므로 네이밍/버전 관리가 쉬움

#### 주의할 점
* **Meter는 앱 전체에 하나만 존재**하는 게 바람직하므로, `MetricsRegistry`를 반드시 `Singleton`으로 등록해야 함
* `MetricsRegistry`가 커지면 역할별로 분리하는 것도 고려할 수 있음 (예: `ApiMetricsRegistry`, `SocketMetricsRegistry`)
  


## Socket 서버 예제 (TCP Echo Server)

```csharp
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using System.Text;

// Meter 정의
var meter = new Meter("SocketServer.Metrics", "1.0");

// Counter: 총 연결 수
var connectionCounter = meter.CreateCounter<int>("socket_connections_total");

// Counter: 받은 메시지 수
var messageCounter = meter.CreateCounter<int>("socket_messages_received_total");

// Histogram: 메시지 길이 분포
var messageLengthHistogram = meter.CreateHistogram<int>("socket_message_length");

var listener = new TcpListener(IPAddress.Any, 9000);
listener.Start();
Console.WriteLine("Socket server running on port 9000...");

while (true)
{
    var client = listener.AcceptTcpClient();
    connectionCounter.Add(1); // 연결 수 증가
    Console.WriteLine("Client connected");

    _ = Task.Run(async () =>
    {
        using var stream = client.GetStream();
        var buffer = new byte[1024];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            messageCounter.Add(1); // 메시지 수 증가
            messageLengthHistogram.Record(bytesRead); // 메시지 길이 기록

            var receivedText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Received: {receivedText}");

            // Echo back
            var response = Encoding.UTF8.GetBytes($"Echo: {receivedText}");
            await stream.WriteAsync(response, 0, response.Length);
        }
    });
}
```

✅ 특징

* 클라이언트가 연결될 때마다 `connectionCounter` 증가
* 메시지를 수신할 때마다 `messageCounter` 및 `messageLengthHistogram` 기록
* 단순 에코 서버 형태로 계측 데이터를 남김

  

## 콘솔에 출력하는 EventListener  
실시간으로 `Metrics API`에서 발생하는 계측값을 **콘솔에 출력하는 EventListener 예제** 이다.

### 1. EventListener 기본 예제

```csharp
using System.Diagnostics.Tracing;
using System.Diagnostics.Metrics;

class MyMetricsListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        // .NET Metrics API에서 Meter를 만들면 내부적으로 EventSource를 생성함
        if (eventSource.Name.StartsWith("ApiServer.Metrics") ||
            eventSource.Name.StartsWith("SocketServer.Metrics"))
        {
            Console.WriteLine($"Listening to: {eventSource.Name}");
            EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
         var payload = (eventData.Payload ?? Array.Empty<object>())
                  .Select(p => p?.ToString() ?? "null");

         Console.WriteLine($"[{eventData.EventSource.Name}] {eventData.EventName}: {string.Join(", ", payload)}");
    }
}
```

* `OnEventSourceCreated`에서 특정 `Meter` 이름과 매칭되는 EventSource를 감지
* `EnableEvents`로 이벤트 수신 시작
* `OnEventWritten`에서 계측값이 들어올 때마다 콘솔에 출력


### 2. API 서버 + Listener 통합 예제

```csharp
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Builder;

var listener = new MyMetricsListener(); // 리스너 시작

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var meter = new Meter("ApiServer.Metrics", "1.0");
var requestCounter = meter.CreateCounter<int>("api_requests_total");
var responseTimeHistogram = meter.CreateHistogram<double>("api_response_time_ms");

app.MapGet("/hello", () =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();

    requestCounter.Add(1);
    var result = $"Hello API {DateTime.Now}";

    sw.Stop();
    responseTimeHistogram.Record(sw.Elapsed.TotalMilliseconds);

    return result;
});

app.Run("http://localhost:5000");
```

✅ 이제 `/hello`를 호출하면 API 서버는 메트릭을 기록하고, EventListener가 이를 콘솔에 출력한다.


### 3. Socket 서버 + Listener 통합 예제

```csharp
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using System.Text;

var listener = new MyMetricsListener(); // 리스너 시작

var meter = new Meter("SocketServer.Metrics", "1.0");
var connectionCounter = meter.CreateCounter<int>("socket_connections_total");
var messageCounter = meter.CreateCounter<int>("socket_messages_received_total");
var messageLengthHistogram = meter.CreateHistogram<int>("socket_message_length");

var server = new TcpListener(IPAddress.Any, 9000);
server.Start();
Console.WriteLine("Socket server running on port 9000...");

while (true)
{
    var client = await server.AcceptTcpClientAsync();
    connectionCounter.Add(1);
    Console.WriteLine("Client connected");

    _ = Task.Run(async () =>
    {
        using var stream = client.GetStream();
        var buffer = new byte[1024];

        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead <= 0) break;

            messageCounter.Add(1);
            messageLengthHistogram.Record(bytesRead);

            var received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Received: {received}");

            var response = Encoding.UTF8.GetBytes($"Echo: {received}");
            await stream.WriteAsync(response, 0, response.Length);
        }
    });
}
```

### 4. 실행 결과 예시 (콘솔 출력)

API 서버에서 `/hello` 호출 →

```
Listening to: ApiServer.Metrics
[ApiServer.Metrics] api_requests_total: 1
[ApiServer.Metrics] api_response_time_ms: 12.34
```

Socket 서버에서 메시지 전송 →

```
Listening to: SocketServer.Metrics
[SocketServer.Metrics] socket_connections_total: 1
[SocketServer.Metrics] socket_messages_received_total: 1
[SocketServer.Metrics] socket_message_length: 42
```


### ✅ 정리

* **Metrics API**: Counter/Histogram/Gauge 정의 및 값 기록
* **EventListener**: 운영 도구 없이도 콘솔에서 메트릭 실시간 확인 가능
* 개발/테스트 단계에서는 이 패턴으로 충분히 디버깅 가능
  
 
  
## OpenTelemetry 과 .NET Metrics API
  
### 1. .NET Metrics API 단독 사용의 한계
* **수집 및 저장 기능 부재**: `System.Diagnostics.Metrics`는 단순히 값 기록만 지원하고, 장기 저장·시각화·알람 같은 운영 기능은 제공하지 않는다.
* **EventListener 의존**: 운영에서 데이터를 보려면 `EventListener`, `dotnet-counters`, ETW/EventPipe 같은 저수준 도구를 직접 써야 한다. 이는 실시간 대시보드나 알람 체계를 만들기엔 불편하다.
* **확장성 부족**: 여러 서버·여러 리전에 걸쳐 운영할 경우 데이터를 모아 관리하기 어려움

즉, 단순 디버깅·개발 확인용으로는 충분하지만 **운영 환경 전체 관측성(Observability) 구축에는 부족**하다.

  
### 2. OpenTelemetry와의 결합 장점
OpenTelemetry는 .NET Metrics API 위에 얹어져 동작하는 구조라서, **Metrics API → OTel Exporter → 모니터링 백엔드** 흐름을 만든다.

* **표준화된 Export**: Prometheus, Grafana, Jaeger, Zipkin, AWS CloudWatch, Azure Monitor 등 다양한 백엔드로 쉽게 전송 가능
* **통합 Observability**: Metrics뿐 아니라 **Tracing, Logging**까지 동일한 스펙으로 통합 → 장애 원인 분석 시 강력함
* **확장성**: OpenTelemetry Collector를 이용하면 여러 서비스/리전 데이터를 한 곳으로 모아 필터링, 샘플링, 라우팅 가능
* **운영 자동화**: 알람, SLA/SLI 측정, AutoScaling 지표 활용 등 운영 단계 기능과 바로 연동

  
### 3. 추천 전략

* **개발 초기/테스트 단계**: .NET Metrics API만으로도 충분.

  * Counter/Histogram/ObservableGauge를 정의하고 콘솔/메모리 Exporter로 검증
  * 빠른 피드백 루프를 얻는 것이 목적
* **운영 준비 단계**: OpenTelemetry SDK 도입

  * 기존 Metrics API 계측 코드는 그대로 유지하면서 OTel Exporter만 붙이면 됨
  * Collector를 통해 Prometheus/Grafana, 클라우드 모니터링에 연동
* **운영 환경**:

  * 메트릭 + 트레이스 + 로그를 모두 OpenTelemetry로 수집
  * Collector에서 경고·샘플링·필터링 정책 적용
  * 대시보드/알람/장기 분석까지 가능

  
### 4. 결론

* **순수 .NET Metrics API** → 단순·가벼움, 개발/디버깅에 최적
* **OpenTelemetry 연동** → 운영 관점에서 사실상 필수, 확장성·표준성·분석 기능 확보

👉 따라서 **개발 단계에서는 Metrics API만 사용하고, 운영 단계로 가면 OpenTelemetry와 통합하는 것**이 가장 합리적인 접근이라고 생각한다.
  


## .NET Metrics API → OpenTelemetry Exporter → Prometheus 연동
  
### 1. 기본 구조
흐름은 다음과 같다:

```
.NET Metrics API (Meter/Counter/Histogram 등)
        ↓
OpenTelemetry SDK (MeterProvider)
        ↓
Prometheus Exporter (HTTP /metrics 엔드포인트 노출)
        ↓
Prometheus 서버 → Grafana 시각화
```
 
  
### 2. 단계별 예제

#### (1) Metrics API로 계측 정의

```csharp
using System.Diagnostics.Metrics;

var meter = new Meter("GameServer.Metrics", "1.0");
var requestCounter = meter.CreateCounter<int>("requests_total");
var responseTime = meter.CreateHistogram<double>("response_time_ms");

void HandleRequest()
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    requestCounter.Add(1); // 요청 수 증가
    // ... 실제 처리 로직 ...
    sw.Stop();
    responseTime.Record(sw.Elapsed.TotalMilliseconds);
}
```

여기까지는 **순수 .NET Metrics API** 사용이다.

  
#### (2) OpenTelemetry SDK로 Exporter 연결

```csharp
using OpenTelemetry;
using OpenTelemetry.Metrics;

var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("GameServer.Metrics")        // 위에서 정의한 Meter 등록
    .AddPrometheusExporter(options =>
    {
        options.StartHttpListener = true;  // 내장 HTTP 리스너 실행
        options.HttpListenerPrefixes = new string[] { "http://localhost:9464/" };
    })
    .Build();

// 서버 실행 대기
Console.WriteLine("Prometheus metrics exposed on http://localhost:9464/metrics");
Console.ReadLine();
```

* `http://localhost:9464/metrics` 엔드포인트에서 메트릭 노출
* Prometheus 서버 설정에서 `scrape_configs`에 이 엔드포인트 추가

---

### (3) Prometheus 설정 예시 (`prometheus.yml`)

```yaml
scrape_configs:
  - job_name: "gameserver"
    scrape_interval: 5s
    static_configs:
      - targets: ["localhost:9464"]
```

* Prometheus가 5초마다 .NET 애플리케이션에서 `/metrics`를 스크랩한다.


#### (4) Grafana 시각화
* Prometheus를 데이터 소스로 추가
* 대시보드에서 `requests_total` 증가 추이, `response_time_ms` 히스토그램을 시각화 가능

  
### 3. Collector를 통한 확장 (선택)
규모가 커지면 OpenTelemetry Collector를 추가하는 편이 낫다.

* 앱에서는 OTLP Exporter 사용 → Collector로 전송
* Collector에서 Prometheus, CloudWatch, Azure Monitor 등 다양한 백엔드로 전달 가능
* 필터링/샘플링/리밸런싱 기능 제공

  
### 4. 요약

* **Metrics API만** → 개발, 디버깅 단계에 적합
* **Metrics API + OTel Exporter** → 운영에 필수, Prometheus와 쉽게 연동
* **Collector까지** → 대규모 서비스/멀티 환경에서 확장성 확보

---

👉 이렇게 하면 **로컬 개발 단계에서 정의한 Meter/Counter/Histogram 코드**를 그대로 유지하면서, 운영 단계에서는 OpenTelemetry Exporter를 붙여 Prometheus와 Grafana까지 연결할 수 있다.



## Matric 으로 측정한 데이터 보기
* 측정한 값이 볼 때는 프로메테우스 등과 연동하는 것을 추천한다.  

### 1. dotnet-counters 설치
먼저 전역 툴로 설치해야 한다.

```bash
dotnet tool install --global dotnet-counters
```

확인:

```bash
dotnet-counters --help
```

  
### 2. 서버 PID 확인
먼저 어떤 프로세스를 모니터링할지 알아야 한다.

```bash
dotnet-counters ps
```

출력 예:

```
12345   ApiServer   C:\MyServer\ApiServer.dll
67890   dotnet
```

여기서 **12345**가 서버 PID라고 하자.

  
### 3. 실시간 모니터링

#### 전체 카운터 보기

```bash
dotnet-counters monitor --process-id 12345
```

이렇게 하면 기본적으로 .NET 런타임 카운터(GC, CPU, 스레드 수 등)와 **Metrics API에서 정의한 Meter 값**이 함께 출력된다.


#### 특정 Meter만 보기
내가 서버 코드에서 이렇게 Meter를 정의했다고 하자:

```csharp
var meter = new Meter("SocketServer.Metrics", "1.0");
var connectionCounter = meter.CreateCounter<int>("socket_connections_total");
```

그럼 dotnet-counters에서 이렇게 실행할 수 있다:

```bash
dotnet-counters monitor --process-id 12345 --counters SocketServer.Metrics
```

출력 예:

```
[SocketServer.Metrics]
   socket_connections_total   5
   socket_messages_received_total   17
   socket_message_length   (Histogram) Avg=48.7 Min=12 Max=1024 Count=17
```


### 4. 파일로 저장해서 나중에 분석하기
실시간 출력 대신 수집만 하고 싶다면:

```bash
dotnet-counters collect --process-id 12345 --counters SocketServer.Metrics --format csv -o metrics.csv
```

👉 이렇게 하면 `metrics.csv` 파일에 기록되어, 나중에 Excel이나 Grafana Agent 같은 데서 분석할 수 있다.


### 5. 요약
1. `dotnet tool install --global dotnet-counters`
2. `dotnet-counters ps` → 서버 PID 확인
3. `dotnet-counters monitor --process-id <pid>` → 실시간 확인
4. `--counters <MeterName>` 옵션으로 내가 만든 Metrics만 필터링 가능
5. 필요하면 `collect` 모드로 파일 저장









================================================
File: EchoServer.md
================================================
Directory structure:
└── EchoServer/
    ├── EchoServer.csproj
    ├── EchoServer.sln
    └── Program.cs

================================================
File: EchoServer.csproj
================================================
﻿<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>



================================================
File: EchoServer.sln
================================================
﻿
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.14.36511.14 d17.14
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "EchoServer", "EchoServer.csproj", "{C4578E7A-553F-DEB6-B069-5968538D0593}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{C4578E7A-553F-DEB6-B069-5968538D0593}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{C4578E7A-553F-DEB6-B069-5968538D0593}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{C4578E7A-553F-DEB6-B069-5968538D0593}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{C4578E7A-553F-DEB6-B069-5968538D0593}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {4A8CEEEE-CA9D-4752-B28D-25FB3F8AB2AC}
	EndGlobalSection
EndGlobal



================================================
File: Program.cs
================================================
癤퓎sing System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Net;
using System.Net.Sockets;
using System.Text;


int pid = Process.GetCurrentProcess().Id;
Console.WriteLine($"Current Process ID: {pid}");

var meter = new Meter("SocketServer.Metrics", "1.0");
var connectionCounter = meter.CreateCounter<int>("socket_connections_total");
var messageCounter = meter.CreateCounter<int>("socket_messages_received_total"); 
var messageLengthHistogram = meter.CreateHistogram<int>("socket_message_length");

var server = new TcpListener(IPAddress.Any, 32452);
server.Start();
Console.WriteLine("Socket server running on port 32452...");

while (true)
{
    var client = await server.AcceptTcpClientAsync();
    connectionCounter.Add(1);
    Console.WriteLine("Client connected");

    _ = Task.Run(async () =>
    {
        using var stream = client.GetStream();
        var buffer = new byte[1024];

        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead <= 0) break;

            messageCounter.Add(1);
            messageLengthHistogram.Record(bytesRead);

            var received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Received: {received}");

            var response = Encoding.UTF8.GetBytes($"Echo: {received}");
            await stream.WriteAsync(response, 0, response.Length);
        }
    });
}








================================================
File: GameAPIServer.md
================================================
Directory structure:
└── GameAPIServer/
    ├── ErrorCode.cs
    ├── GameAPIServer.csproj
    ├── GameAPIServer.sln
    ├── MetricsRegistry.cs
    ├── Program.cs
    ├── Security.cs
    ├── appsettings.Development.json
    ├── appsettings.json
    ├── httpTest.http
    ├── Controllers/
    │   ├── CreateAccountController.cs
    │   ├── FriendAcceptController.cs
    │   ├── FriendCancelReqController.cs
    │   ├── FriendDeleteController.cs
    │   ├── FriendListController.cs
    │   ├── FriendSendReqController.cs
    │   ├── GameDataLoadController.cs
    │   ├── LoginController.cs
    │   ├── MailDeleteController.cs
    │   ├── MailListController.cs
    │   ├── MailReceiveController.cs
    │   └── UserDataLoadController.cs
    ├── Middleware/
    │   └── RequestMetricsMiddleware.cs
    ├── Models/
    │   ├── MasterDB.cs
    │   ├── RedisDB.cs
    │   ├── DAO/
    │   │   ├── Account.cs
    │   │   ├── Attendance.cs
    │   │   ├── Friend.cs
    │   │   ├── Game.cs
    │   │   ├── Item.cs
    │   │   ├── Mailbox.cs
    │   │   └── User.cs
    │   └── DTO/
    │       ├── AttendanceCheck.cs
    │       ├── AttendanceInfo.cs
    │       ├── CreateAccount.cs
    │       ├── ErrorCode.cs
    │       ├── FreindDelete.cs
    │       ├── FriendAccept.cs
    │       ├── FriendAdd.cs
    │       ├── FriendList.cs
    │       ├── GameDataLoad.cs
    │       ├── Header.cs
    │       ├── Login.cs
    │       ├── Logout.cs
    │       ├── MailDelete.cs
    │       ├── MailList.cs
    │       ├── MailReceive.cs
    │       ├── OtherUserInfo.cs
    │       ├── Ranking.cs
    │       ├── SocialDataLoad.cs
    │       ├── UserDataLoad.cs
    │       ├── UserRank.cs
    │       └── UserSetMainChar.cs
    ├── Properties/
    │   └── launchSettings.json
    ├── Repository/
    │   ├── FakeGameDb.cs
    │   └── IGameDb.cs
    └── Services/
        ├── AuthService.cs
        ├── DataLoadService.cs
        ├── FriendService.cs
        ├── GameService.cs
        ├── MailService.cs
        ├── UserService.cs
        └── Interfaces/
            ├── IAuthService.cs
            ├── IDataLoadService.cs
            ├── IFriendService.cs
            ├── IGameService.cs
            ├── IMailService.cs
            └── IUserService.cs

================================================
File: ErrorCode.cs
================================================
癤퓎sing System;

// 1000 ~ 19999
public enum ErrorCode : UInt16
{
    None = 0,

    // Common 1000 ~
    UnhandleException = 1001,
    RedisFailException = 1002,
    InValidRequestHttpBody = 1003,
    TokenDoesNotExist = 1004,
    UidDoesNotExist = 1005,
    AuthTokenFailWrongAuthToken = 1006,
    Hive_Fail_InvalidResponse = 1010,
    InValidAppVersion = 1011,
    InvalidMasterDataVersion = 1012,

    // Auth 2000 ~
    CreateUserFailException = 2001,
    CreateUserFailNoNickname = 2002,
    CreateUserFailDuplicateNickname = 2003,
    LoginFailException = 2004,
    LoginFailUserNotExist = 2005,
    LoginFailPwNotMatch = 2006,
    LoginFailSetAuthToken = 2007,
    LoginUpdateRecentLoginFail = 2008,
    LoginUpdateRecentLoginFailException = 2009,
    AuthTokenMismatch = 2010,
    AuthTokenKeyNotFound = 2011,
    AuthTokenFailWrongKeyword = 2012,
    AuthTokenFailSetNx = 2013,
    AccountIdMismatch = 2014,
    DuplicatedLogin = 2015,
    CreateUserFailInsert = 2016,
    LoginFailAddRedis = 2017,
    CheckAuthFailNotExist = 2018,
    CheckAuthFailNotMatch = 2019,
    CheckAuthFailException = 2020,
    LogoutRedisDelFail = 2021,
    LogoutRedisDelFailException= 2022,
    DeleteAccountFail = 2023,
    DeleteAccountFailException = 2024,
    InitNewUserGameDataFailException = 2025,
    InitNewUserGameDataFailCharacter = 2026,
    InitNewUserGameDataFailGameList = 2027,
    InitNewUserGameDataFailMoney = 2028,
    InitNewUserGameDataFailAttendance = 2029,
    CreateAccountFailInsert = 2051,
    CreateAccountFailException = 2052,

    // Friend 2100
    FriendSendReqFailUserNotExist = 2101,
    FriendSendReqFailInsert = 2102,
    FriendSendReqFailException = 2103,
    FriendSendReqFailAlreadyExist = 2104,
    SendFriendReqFailSameUid = 2105,
    FriendGetListFailOrderby = 2106,
    FriendGetListFailException = 2107,
    FriendGetRequestListFailException = 2108,
    FriendDeleteFailNotFriend = 2109,
    FriendDeleteFailDelete = 2110,
    FriendDeleteFailException = 2111,
    FriendDeleteFailSameUid = 2112,
    FriendDeleteReqFailNotFriend = 2113,
    FriendDeleteReqFailDelete = 2114,
    FriendDeleteReqFailException = 2115,
    FriendAcceptFailException = 2116,
    FriendAcceptFailSameUid = 2117,
    AcceptFriendRequestFailUserNotExist = 2118,
    AcceptFriendRequestFailAlreadyFriend = 2119,
    AcceptFriendRequestFailException = 2120,
    FriendSendReqFailNeedAccept = 2121,

    // Game 2200
    MiniGameListFailException = 2201,
    GameSetNewUserListFailException = 2202,
    GameSetNewUserListFailInsert = 2203,
    MiniGameUnlockFailInsert = 2204,
    MiniGameUnlockFailException = 2205,
    MiniGameInfoFailException = 2206,
    MiniGameSaveFailException = 2207,
    MiniGameSaveFailGameLocked = 2208,
    MiniGameUnlockFailAlreadyUnlocked = 2209,
    MiniGameSetPlayCharFailUpdate = 2210,
    MiniGameSetPlayCharFailException = 2211,
    MiniGameSaveFailFoodDecrement = 2212,

    SetUserScoreFailException = 2301,
    GetRankingFailException = 2302,
    GetUserRankFailException = 2303,

    // Item 3000 ~
    CharReceiveFailInsert = 3011,
    CharReceiveFailLevelUP = 3012,
    CharReceiveFailIncrementCharCnt = 3013,
    CharReceiveFailException= 3014,
    CharListFailException = 3015,
    CharNotExist = 3016,
    CharSetCostumeFailUpdate = 3017,
    CharSetCostumeFailException = 3018,

    SkinReceiveFailAlreadyOwn = 3021,
    SkinReceiveFailInsert = 3022,
    SkinReceiveFailException = 3023,
    SkinListFailException = 3024,

    CostumeReceiveFailInsert = 3031,
    CostumeReceiveFailLevelUP = 3032,
    CostumeReceiveFailIncrementCharCnt = 3033,
    CostumeReceiveFailException = 3034,
    CostumeListFailException = 3035,
    CharSetCostumeFailHeadNotExist= 3036,
    CharSetCostumeFailFaceNotExist = 3037,
    CharSetCostumeFailHandNotExist = 3038,

    FoodReceiveFailInsert = 3041,
    FoodReceiveFailIncrementFoodQty = 3042,
    FoodReceiveFailException = 3043,
    FoodListFailException = 3044,
    FoodGearReceiveFailInsert = 3045,
    FoodGearReceiveFailIncrementFoodGear = 3046,
    FoodGearReceiveFailException = 3047,

    GachaReceiveFailException= 3051,


    //GameDb 4000~ 
    GetGameDbConnectionFail = 4002,


    // MasterDb 5000 ~
    MasterDB_Fail_LoadData = 5001,
    MasterDB_Fail_InvalidData = 5002,

    // User
    UserInfoFailException = 6001,
    UserMoneyInfoFailException = 6002,
    UserUpdateJewelryFailIncremnet = 6003,
    SetMainCharFailException = 6004,
    GetOtherUserInfoFailException = 6005,
    UserNotExist = 6006,

    // Mail
    MailListFailException = 8001,
    MailReceiveFailException = 8002,
    MailReceiveFailAlreadyReceived = 8003,
    MailReceiveFailMailNotExist = 8004,
    MailReceiveFailUpdateReceiveDt = 8005,
    MailRewardListFailException = 8006,
    MailDeleteFailDeleteMail = 8007,
    MailDeleteFailDeleteMailReward = 8008,
    MailDeleteFailException = 8009,
    MailReceiveFailNotMailOwner = 8010,
    MailReceiveRewardsFailException = 8011,

    // Attendance
    AttendanceInfoFailException = 9001,
    AttendanceCheckFailAlreadyChecked = 9002,
    AttendanceCheckFailException = 9003,

    GetRewardFailException = 9004,
}


================================================
File: GameAPIServer.csproj
================================================
<Project Sdk="Microsoft.NET.Sdk.Web">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>

    <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
        <OutputPath>..\00_ServerBin\GameAPIServer\</OutputPath>
    </PropertyGroup>

    <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|AnyCPU'">
        <OutputPath>..\00_ServerBin\GameAPIServer\</OutputPath>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="CloudStructures" Version="3.4.1" />
        <PackageReference Include="MySqlConnector" Version="2.4.0" />
        <PackageReference Include="prometheus-net.AspNetCore" Version="8.2.1" />
        <PackageReference Include="SqlKata" Version="4.0.1" />
        <PackageReference Include="SqlKata.Execution" Version="4.0.1" />
        <PackageReference Include="ZLogger" Version="2.5.10" />
    </ItemGroup>

</Project>



================================================
File: GameAPIServer.sln
================================================
﻿
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.8.34330.188
MinimumVisualStudioVersion = 10.0.40219.1
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "GameAPIServer", "GameAPIServer.csproj", "{C4BF4730-21F7-4F00-A236-706420265F0D}"
EndProject

Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{C4BF4730-21F7-4F00-A236-706420265F0D}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{C4BF4730-21F7-4F00-A236-706420265F0D}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{C4BF4730-21F7-4F00-A236-706420265F0D}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{C4BF4730-21F7-4F00-A236-706420265F0D}.Release|Any CPU.Build.0 = Release|Any CPU
		{EDAEE952-47EB-4524-B8C9-00C73A782988}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{EDAEE952-47EB-4524-B8C9-00C73A782988}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{EDAEE952-47EB-4524-B8C9-00C73A782988}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{EDAEE952-47EB-4524-B8C9-00C73A782988}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal



================================================
File: MetricsRegistry.cs
================================================
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GameAPIServer;

/// <summary>
/// .NET Metrics API를 사용한 메트릭 중앙 관리 클래스.
///
/// 4가지 메트릭 타입을 모두 보여준다:
///   [1] Counter  — 단조 증가 (총 요청 수, 총 에러 수)
///   [2] Histogram — 값의 분포 (응답 시간)
///   [3] UpDownCounter — 증가/감소 가능 (동접 수)
///   [4] ObservableGauge — 콜백으로 현재값 자동 보고 (메모리 사용량)
///
/// 확인 방법:
///   1) http://localhost:11500/metrics 에서 Prometheus 형식으로 노출
///   2) dotnet-counters monitor --process-id {PID} --counters ApiServer.Metrics
/// </summary>
public static class MetricsRegistry
{
    // Meter: 메트릭 그룹의 컨테이너. 이름으로 dotnet-counters에서 필터링 가능.
    public static readonly Meter Meter = new("ApiServer.Metrics", "1.0");

    // ─────────────────────────────────────────────────────────
    // [1] Counter<T>: 단조 증가만 하는 값. 서버 재시작 시 0으로 리셋.
    //     PromQL: rate(api_requests_total[5m]) → 5분간 초당 요청 수
    // ─────────────────────────────────────────────────────────
    public static readonly Counter<int> RequestCounter =
        Meter.CreateCounter<int>(
            "api_requests_total",
            description: "Total number of API requests");

    // 에러 카운터: 태그(라벨)로 에러 코드별 집계 가능
    // 사용법: ErrorCounter.Add(1, new KeyValuePair<string, object?>("error_code", "1001"))
    public static readonly Counter<int> ErrorCounter =
        Meter.CreateCounter<int>(
            "api_errors_total",
            description: "Total number of API errors");

    // ─────────────────────────────────────────────────────────
    // [2] Histogram<T>: 값의 분포를 버킷으로 측정.
    //     PromQL: histogram_quantile(0.9, rate(api_response_time_ms_bucket[5m])) → P90
    // ─────────────────────────────────────────────────────────
    public static readonly Histogram<double> ResponseTimeHistogram =
        Meter.CreateHistogram<double>(
            "api_response_time_ms",
            unit: "ms",
            description: "API response time distribution in milliseconds");

    // ─────────────────────────────────────────────────────────
    // [3] UpDownCounter<T>: 증가/감소 모두 가능. Gauge와 유사.
    //     현재 처리 중인 요청 수 추적.
    //     PromQL: api_requests_in_flight
    // ─────────────────────────────────────────────────────────
    public static readonly UpDownCounter<int> RequestsInFlight =
        Meter.CreateUpDownCounter<int>(
            "api_requests_in_flight",
            description: "Number of requests currently being processed");

    // ─────────────────────────────────────────────────────────
    // [4] ObservableGauge<T>: 콜백 함수로 현재값을 자동 보고.
    //     수집 시점마다 콜백이 호출되어 최신값을 반환한다.
    //     수동으로 Inc/Dec 하지 않고, 시스템 상태를 자동으로 가져올 때 사용.
    //     PromQL: api_memory_usage_bytes
    // ─────────────────────────────────────────────────────────
    public static readonly ObservableGauge<long> MemoryUsageGauge =
        Meter.CreateObservableGauge(
            "api_memory_usage_bytes",
            () => Process.GetCurrentProcess().WorkingSet64,
            unit: "bytes",
            description: "Current working set memory usage");
}



================================================
File: Program.cs
================================================
using System.IO;
using GameAPIServer.Repository;
using GameAPIServer.Servicies;
using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;
using Prometheus;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;

// DI 등록: 서비스와 리포지토리 (FakeGameDb = DB 없이 테스트용 Mock)
builder.Services.AddTransient<IGameDb, FakeGameDb>();
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IFriendService, FriendService>();
builder.Services.AddTransient<IGameService, GameService>();
builder.Services.AddTransient<IMailService, MailService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IDataLoadService, DataLoadService>();
builder.Services.AddControllers();

SettingLogger();

WebApplication app = builder.Build();

ILoggerFactory loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

app.UseRouting();

// [메트릭 수집 미들웨어] 모든 요청에 대해 자동으로 Counter, Histogram, UpDownCounter 기록
// MetricsRegistry.cs에 정의된 .NET Metrics API 메트릭 사용
app.UseMiddleware<RequestMetricsMiddleware>();

// [prometheus-net] HTTP 요청 관련 기본 메트릭 자동 수집 (http_requests_received_total 등)
app.UseHttpMetrics();

app.MapDefaultControllerRoute();

// [prometheus-net] /metrics 엔드포인트 등록 — Prometheus가 이 주소를 수집(Pull)
// 브라우저에서 http://localhost:11500/metrics 로 확인 가능
app.MapMetrics();

app.Run(configuration["ServerAddress"]);



void SettingLogger()
{
    ILoggingBuilder logging = builder.Logging;
    logging.ClearProviders();

    var fileDir = configuration["logdir"];

    var exists = Directory.Exists(fileDir);

    if (!exists)
    {
        Directory.CreateDirectory(fileDir);
    }

    logging.AddZLoggerRollingFile(
        options =>
        {
            options.UseJsonFormatter();
            options.FilePathSelector = (timestamp, sequenceNumber) => $"{fileDir}{timestamp.ToLocalTime():yyyy-MM-dd}_{sequenceNumber:000}.log";
            options.RollingInterval = ZLogger.Providers.RollingInterval.Day;
            options.RollingSizeKB = 1024;
        });

    _ = logging.AddZLoggerConsole(options =>
    {
        options.UseJsonFormatter();
    });


}



================================================
File: Security.cs
================================================
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GameAPIServer;

public class Security
{
    const String AllowableCharacters = "abcdefghijklmnopqrstuvwxyz0123456789";

    public static String MakeHashingPassWord(String saltValue, String pw)
    {
        var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.ASCII.GetBytes(saltValue + pw));
        var stringBuilder = new StringBuilder();
        foreach (var b in hash)
        {
            stringBuilder.AppendFormat("{0:x2}", b);
        }

        return stringBuilder.ToString();
    }

    public static String SaltString()
    {
        var bytes = new Byte[64];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }

        return new String(bytes.Select(x => AllowableCharacters[x % AllowableCharacters.Length]).ToArray());
    }

    public static String CreateAuthToken()
    {
        var bytes = new Byte[25];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }

        return new String(bytes.Select(x => AllowableCharacters[x % AllowableCharacters.Length]).ToArray());
    }

}


================================================
File: appsettings.Development.json
================================================
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Debug"
    }
  },
  "AllowedHosts": "*",
  "ServerAddress": "http://localhost:11500",
  "HiveServerAddress": "http://localhost:11501",
  "logdir": "./log/",
  "DbConfig": {
    "Redis": "localhost",
    "GameDb": "Server=localhost;Port=3306;user=root;Password=sykim2312;Database=game_db;Pooling=true;Min Pool Size=0;Max Pool Size=100;AllowUserVariables=True;",
    "MasterDb": "Server=localhost;Port=3306;user=root;Password=sykim2312;Database=master_db;Pooling=true;Min Pool Size=0;Max Pool Size=100;AllowUserVariables=True;"
  }
}



================================================
File: appsettings.json
================================================
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Debug"
    },
    "ZLoggerConsole": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "AllowedHosts": "*",
  "ServerAddress": "http://localhost:11500",
  "HiveServerAddress": "http://localhost:11501",
  "logdir": "./log/"
  
}



================================================
File: httpTest.http
================================================
@host = http://localhost:11500

### ============================================================
### 인증 API
### ============================================================

### 계정 생성
POST {{host}}/CreateAccount
Content-Type: application/json

{
  "ID":"jacking751",
  "PW":"123qwe",
  "NickName": "aaa"
}

### 로그인 (MetricsRegistry의 Counter, Histogram 기록됨)
POST {{host}}/Login
Content-Type: application/json

{
  "ID":"jacking751",
  "PW":"123qwe"
}


### ============================================================
### 친구 API
### ============================================================

### 친구 요청 보내기
POST {{host}}/FriendSendReq
Content-Type: application/json

{
  "Uid": 1,
  "AuthToken": "test-token",
  "FriendUid": 2
}

### 친구 목록 조회
POST {{host}}/FriendList
Content-Type: application/json

{
  "Uid": 1,
  "AuthToken": "test-token"
}


### ============================================================
### 메일 API
### ============================================================

### 메일 목록 조회
POST {{host}}/MailList
Content-Type: application/json

{
  "Uid": 1,
  "AuthToken": "test-token"
}


### ============================================================
### 데이터 로드 API
### ============================================================

### 유저 데이터 로드
POST {{host}}/UserDataLoad
Content-Type: application/json

{
  "Uid": 1,
  "AuthToken": "test-token"
}

### 게임 데이터 로드
POST {{host}}/GameDataLoad
Content-Type: application/json

{
  "Uid": 1,
  "AuthToken": "test-token"
}


### ============================================================
### 메트릭 확인
### ============================================================
### 위의 API를 여러 번 호출한 후, 아래 주소를 브라우저에서 열면
### Prometheus 형식의 메트릭을 확인할 수 있다:
###
### http://localhost:11500/metrics
###
### 확인할 메트릭:
###   api_requests_total          — 총 요청 수 (method, path, status_code 태그)
###   api_response_time_ms        — 응답 시간 분포 (히스토그램)
###   api_requests_in_flight      — 현재 처리 중인 요청 수
###   api_errors_total            — 에러 수 (4xx, 5xx)
###   api_memory_usage_bytes      — 서버 메모리 사용량 (ObservableGauge)
###
### dotnet-counters로 실시간 확인:
###   dotnet-counters monitor --process-id <PID> --counters ApiServer.Metrics



================================================
File: Controllers/CreateAccountController.cs
================================================
癤퓎sing System.Threading.Tasks;
using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class CreateAccountController : ControllerBase
{
    readonly ILogger<CreateAccountController> _logger;
    readonly IAuthService _authService;

    public CreateAccountController(ILogger<CreateAccountController> logger, IAuthService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    [HttpPost]
    public async Task<CreateHiveAccountResponse> Create([FromBody]CreateHiveAccountRequest request)
    {
        CreateHiveAccountResponse response = new();

        response.Result = await _authService.CreateAccount(request.UserID, request.Password);

        return response;
    }

    

    
}




================================================
File: Controllers/FriendAcceptController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class FriendAcceptController : ControllerBase
{
    readonly ILogger<FriendAcceptController> _logger;
    readonly IFriendService _friendService;

    public FriendAcceptController(ILogger<FriendAcceptController> logger, IFriendService friendService)
    {
        _logger = logger;
        _friendService = friendService;
    }

    /// <summary>
    /// 친구 요청을 수락하는 API </br>
    /// 요청이 왔는지, 이미 친구 인지 확인 후 친구 요청을 수락합니다.
    /// </summary>
    [HttpPost]
    public async Task<FriendAcceptResponse> AcceptFriend([FromHeader] Header header, FriendAcceptRequest request)
    {
        FriendAcceptResponse response = new();
        
        response.Result = await _friendService.AcceptFriendReq(header.Uid, request.FriendUid);

        _logger.ZLogInformation($"[FriendAccept] Uid : {header.Uid}");
        return response;
    }
}



================================================
File: Controllers/FriendCancelReqController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class FriendCancelReqController : ControllerBase
{
    readonly ILogger<FriendCancelReqController> _logger;
    readonly IFriendService _friendService;

    public FriendCancelReqController(ILogger<FriendCancelReqController> logger, IFriendService friendService)
    {
        _logger = logger;
        _friendService = friendService;
    }

    /// <summary>
    /// 친구 요청 취소 API
    /// 보낸 친구 요청을 취소합니다.
    /// </summary>
    [HttpPost]
    public async Task<FriendDeleteResponse> CancelFriendReq([FromHeader] Header header, FriendDeleteRequest request)
    {
        FriendDeleteResponse response = new();

        response.Result = await _friendService.CancelFriendReq(header.Uid, request.FriendUid);

        _logger.ZLogInformation($"[FriendCancelReq] Uid : {header.Uid}");
        return response;
    }
}



================================================
File: Controllers/FriendDeleteController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

[ApiController]
[Route("[controller]")]
public class FriendDeleteController : ControllerBase
{
    readonly ILogger<FriendDeleteController> _logger;
    readonly IFriendService _friendService;

    public FriendDeleteController(ILogger<FriendDeleteController> logger, IFriendService friendService)
    {
        _logger = logger;
        _friendService = friendService;
    }

    /// <summary>
    /// 친구를 삭제하는 API
    /// 서로 친구를 삭제합니다.
    /// </summary>
    [HttpPost]
    public async Task<FriendDeleteResponse> DeleteFriend([FromHeader] Header header, FriendDeleteRequest request)
    {
        FriendDeleteResponse response = new();

        response.Result = await _friendService.DeleteFriend(header.Uid, request.FriendUid);

        _logger.ZLogInformation($"[FriendDelete] Uid : {header.Uid}");
        return response;
    }
}


================================================
File: Controllers/FriendListController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class FriendListController : ControllerBase
{
    readonly ILogger<FriendListController> _logger;
    readonly IFriendService _friendService;

    public FriendListController(ILogger<FriendListController> logger, IFriendService friendService)
    {
        _logger = logger;
        _friendService = friendService;
    }

    /// <summary>
    /// 친구 목록 조회 API
    /// 보낸 친구 요청, 받은 친구 요청, 친구 목록을 조회합니다.
    /// </summary>
    [HttpPost]
    public async Task<FriendListResponse> GetFriendList([FromHeader] Header header)
    {
        FriendListResponse response = new();

        (response.Result, response.FriendList) = await _friendService.GetFriendList(header.Uid);

        _logger.ZLogInformation($"[FriendList] Uid : {header.Uid}");
        return response;
    } 
}



================================================
File: Controllers/FriendSendReqController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class FriendSendReqController : ControllerBase
{
    readonly ILogger<FriendSendReqController> _logger;
    readonly IFriendService _friendService;

    public FriendSendReqController(ILogger<FriendSendReqController> logger, IFriendService friendService)
    {
        _logger = logger;
        _friendService = friendService;
    }

    /// <summary>
    /// 친구 요청 API </br>
    /// 상대방에게 친구 요청을 보냅니다.
    /// </summary>
    [HttpPost]
    public async Task<SendFriendReqResponse> SendFriendReq([FromHeader] Header header, SendFriendReqRequest request)
    {
        SendFriendReqResponse response = new();

        response.Result = await _friendService.SendFriendReq(header.Uid, request.FriendUid);

        _logger.ZLogInformation($"[FriendSendReq] Uid : {header.Uid}");
        return response;
    }
}





================================================
File: Controllers/GameDataLoadController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class GameDataLoadController : ControllerBase
{
    readonly ILogger<GameDataLoadController> _logger;
    readonly IDataLoadService _dataLoadService;

    public GameDataLoadController(ILogger<GameDataLoadController> logger, IDataLoadService dataLoadService)
    {
        _logger = logger;
        _dataLoadService = dataLoadService;
    }

    /// <summary>
    /// 아이템 데이터 로드 API
    /// 게임에 필요한 아이템 정보(보유한 게임, 캐릭터, 스킨, 코스튬, 푸드)를 조회합니다.
    /// </summary>
    [HttpPost]
    public async Task<GameDataLoadResponse> LoadGameData([FromHeader] Header header)
    {
        GameDataLoadResponse response = new();

        (response.Result, response.GameData) = await _dataLoadService.LoadGameData(header.Uid);

        _logger.ZLogInformation($"[GameDataLoad] Uid : {header.Uid}");
        return response;
    }
}



================================================
File: Controllers/LoginController.cs
================================================
﻿using System.Diagnostics;
using System.Threading.Tasks;
using GameAPIServer.Models.DTO;
using GameAPIServer.Servicies.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZLogger;


namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController : ControllerBase
{
    readonly ILogger<LoginController> _logger;
    readonly IAuthService _authService;
    readonly IGameService _gameService;
    readonly IDataLoadService _dataLoadService;

    public LoginController(ILogger<LoginController> logger, IAuthService authService, IGameService gameService, IDataLoadService dataLoadService)
    {
        _logger = logger;
        _authService = authService;
        _gameService = gameService;
        _dataLoadService = dataLoadService;
    }

    /// <summary>
    /// 로그인 API </br>
    /// 하이브 토큰을 검증하고, 유저가 없다면 생성, 토큰 발급, 로그인 시간 업데이트, 유저 데이터 로드를 합니다. 
    /// </summary>
    [HttpPost]
    public async Task<LoginResponse> Login(LoginRequest request)
    {
        var sw = Stopwatch.StartNew();

        MetricsRegistry.RequestCounter.Add(1);


        LoginResponse response = new();

        //TODO: 컨트룰러에 구현 코드가 너무 많이 노출 되어 있다. 아래 코드에서는 서비스 객체의 메소드를 여러개 호출하고 있는데 1개 정도의 메소드로 묶어서 호출하는 방법을 생각해보자.
        // 즉 구현은 대부분 서비스 객체에 있어야 하고, 컨트룰러는 필요한 서비스 객체를 호출하고, 응답만 보내는 역할을 해야 한다.

        
        //하이브 토큰 체크
        var (errorCode,uid, authToken) = await _authService.Login(request.UserID, request.Password);
        if (errorCode == ErrorCode.None)
        {
            _logger.ZLogInformation($"[Login] Uid : {uid}, Token : {authToken}");
        }

        response.Result = errorCode;
        response.AuthToken = authToken;


        sw.Stop();
        MetricsRegistry.ResponseTimeHistogram.Record(sw.Elapsed.TotalMilliseconds);


        return response;
    }
}



================================================
File: Controllers/MailDeleteController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class MailDeleteController : ControllerBase
{
    readonly ILogger<MailDeleteController> _logger;
    readonly IMailService _mailService;

    public MailDeleteController(ILogger<MailDeleteController> logger, IMailService mailService)
    {
        _logger = logger;
        _mailService = mailService;
    }

    /// <summary>
    /// 메일 삭제 API
    /// 메일함에서 메일을 삭제합니다.
    /// </summary>
    [HttpPost]
    public async Task<MailDeleteResponse> DeleteMail([FromHeader] Header header, MailDeleteRequest request)
    {
        MailDeleteResponse response = new();

        response.Result = await _mailService.DeleteMail(header.Uid, request.MailSeq);

        _logger.ZLogInformation($"[MailDelete] Uid : {header.Uid}");
        return response;
    }
}



================================================
File: Controllers/MailListController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class MailListController : ControllerBase
{
    readonly ILogger<MailListController> _logger;
    readonly IMailService _mailService;

    public MailListController(ILogger<MailListController> logger, IMailService mailService)
    {
        _logger = logger;
        _mailService = mailService;
    }

    /// <summary>
    /// 메일 목록 정보 API
    /// 유저의 메일 목록 정보를 가져옵니다.
    /// </summary>
    [HttpPost]
    public async Task<MailboxInfoResponse> GetMailList([FromHeader] Header header)
    {
        MailboxInfoResponse response = new();

        (response.Result, response.MailList) = await _mailService.GetMailList(header.Uid);

        _logger.ZLogInformation($"[MailList] Uid : {header.Uid}");
        return response;
    }
}



================================================
File: Controllers/MailReceiveController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class MailReceiveController : ControllerBase
{
    readonly ILogger<MailReceiveController> _logger;
    readonly IMailService _mailService;

    public MailReceiveController(ILogger<MailReceiveController> logger, IMailService mailService)
    {
        _logger = logger;
        _mailService = mailService;
    }

    /// <summary>
    /// 메일 보상 수령 API
    /// 메일에 포함된 보상을 모두 수령하고, 수령한 보상을 반환합니다.
    /// </summary>
    [HttpPost]
    public async Task<MailReceiveResponse> ReceiveMail([FromHeader] Header header, MailReceiveRequest request)
    {
        MailReceiveResponse response = new();

        (response.Result, response.Rewards) = await _mailService.ReceiveMail(header.Uid, request.MailSeq);

        _logger.ZLogInformation($"[MailReceive] Uid : {header.Uid}");
        return response;
    }
}



================================================
File: Controllers/UserDataLoadController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class UserDataLoadController : ControllerBase
{
    readonly ILogger<UserDataLoadController> _logger;
    readonly IDataLoadService _dataLoadService;

    public UserDataLoadController(ILogger<UserDataLoadController> logger, IDataLoadService dataLoadService)
    {
        _logger = logger;
        _dataLoadService = dataLoadService;
    }

    /// <summary>
    /// 유저 데이터 로드 API
    /// 게임에 필요한 유저 정보(유저의 정보(점수,재화), 출석 정보)를 조회합니다.
    /// </summary>
    [HttpPost]
    public async Task<UserDataLoadResponse> LoadUserData([FromHeader] Header header)
    {
        UserDataLoadResponse response = new();

        (response.Result, response.UserData) = await _dataLoadService.LoadUserData(header.Uid);

        _logger.ZLogInformation($"[UserDataLoad] Uid : {header.Uid}");
        return response;
    }
}



================================================
File: Middleware/RequestMetricsMiddleware.cs
================================================
#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GameAPIServer.Middleware;

/// <summary>
/// 모든 HTTP 요청에 대해 자동으로 메트릭을 수집하는 미들웨어.
///
/// 수집 항목:
///   - api_requests_total: 총 요청 수 (태그: method, path, status_code)
///   - api_response_time_ms: 응답 시간 분포 (태그: method, path)
///   - api_requests_in_flight: 현재 처리 중인 요청 수
///   - api_errors_total: 에러 수 (status >= 400, 태그: method, path, status_code)
///
/// 개별 컨트롤러에서 수동으로 메트릭을 기록하지 않아도
/// 이 미들웨어가 모든 요청에 대해 자동 측정한다.
///
/// 등록 방법 (Program.cs):
///   app.UseMiddleware&lt;RequestMetricsMiddleware&gt;();
/// </summary>
public class RequestMetricsMiddleware
{
    readonly RequestDelegate _next;

    public RequestMetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // /metrics 엔드포인트 자체는 측정하지 않음
        if (context.Request.Path.StartsWithSegments("/metrics"))
        {
            await _next(context);
            return;
        }

        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";

        // [3] UpDownCounter: 처리 중 요청 수 증가
        MetricsRegistry.RequestsInFlight.Add(1);

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var statusCode = context.Response.StatusCode.ToString();

            // [1] Counter: 총 요청 수 (태그로 method, path, status_code 구분)
            MetricsRegistry.RequestCounter.Add(1,
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("path", path),
                new KeyValuePair<string, object?>("status_code", statusCode));

            // [2] Histogram: 응답 시간 분포
            MetricsRegistry.ResponseTimeHistogram.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("path", path));

            // 에러 카운터 (4xx, 5xx)
            if (context.Response.StatusCode >= 400)
            {
                MetricsRegistry.ErrorCounter.Add(1,
                    new KeyValuePair<string, object?>("method", method),
                    new KeyValuePair<string, object?>("path", path),
                    new KeyValuePair<string, object?>("status_code", statusCode));
            }

            // [3] UpDownCounter: 처리 중 요청 수 감소
            MetricsRegistry.RequestsInFlight.Add(-1);
        }
    }
}



================================================
File: Models/MasterDB.cs
================================================
癤퓎sing System.Collections.Generic;

namespace GameAPIServer.Models;

public class AttendanceRewardData : RewardData
{
    public int day_seq { get; set; }
}

public class RewardData
{
    public int reward_key { get; set; }
    public int reward_qty { get; set; }
    public string reward_type { get; set; }
}

public class CharacterData
{
    public int char_key { get; set; }
    public string char_name { get; set; }
    public string char_grade { get; set; }
    public int stat_run { get; set; }
    public int stat_power { get; set; }
    public int stat_jump { get; set; }
    public int game_key { get; set; }
}

public class SkinData
{
    public int skin_key { get; set; }
    public string skin_name { get; set; }
    public int char_key { get; set; }
    public int skin_bonus_percent { get; set; }
}

public class CostumeData
{
    public int costume_key { get; set; }
    public string costume_name { get; set; }
    public int costume_type { get; set; }
    public int set_key { get; set; }
}

public class CostumeSetData
{
    public int set_key { get; set; }
    public int char_key { get; set; }
    public string set_name { get; set; }
    public int set_bonus_percent { get; set; }
    public int char_bonus_percent { get; set; }
}

public class FoodData
{
    public int food_key { get; set; }
    public string food_name { get; set; }
    public int game_key { get; set; }
}

public class SkillData
{
    public int skill_key { get; set; }
    public int act_prob_percent { get; set; }
    public int char_key { get; set; } = 0;
}

public class GachaRewardData
{
    public GachaRewardInfo gachaRewardInfo { get; set; }
    public List<RewardData> gachaRewardList { get; set; }
}

public class GachaRewardInfo
{
    public int gacha_reward_key { get; set; }
    public int char_prob_percent { get; set; }
    public int skin_prob_percent { get; set; }
    public int costume_prob_percent { get; set; }
    public int food_prob_percent { get; set; }
    public int food_gear_prob_percent { get; set; }
    public int gacha_count { get; set; }
    public string gacha_reward_name { get; set; }
}

public class ItemLevelData
{
    public int level { get; set; }
    public int item_cnt { get; set; }
}

public class ReceivedReward
{
    public ReceivedReward(int key, List<RewardData> datas)
    {
        rewardKey = key;
        rewardDatas = datas;
    }

    public int rewardKey { get; set; }
    public List<RewardData> rewardDatas { get; set; }
}

public class VersionDAO
{
    public string app_version { get; set; } = "";
    public string master_data_version { get; set; } = "";
}



================================================
File: Models/RedisDB.cs
================================================
﻿using System;

namespace GameAPIServer.Models;

//RedisDB의 객체는 객체 이름 앞에 Rdb를 붙인다.

public class RdbAuthUserData
{
    public Int64 Uid { get; set; } = 0;
    public string Token { get; set; } = "";
}

public class RdbUserScoreData
{
    public Int64 uid { get; set; } = 0;
    public int total_bestscore { get; set; } = 0;
}



================================================
File: Models/DAO/Account.cs
================================================
癤퓎sing System;

namespace GameAPIServer.Models.DAO;


public class Account
{
    public Int64 player_id { get; set; }
    public string user_id { get; set; }
    public string pw { get; set; }
    public string salt_value { get; set; }
    public string recent_login_dt { get; set; }
    public string create_dt { get; set; }
}


================================================
File: Models/DAO/Attendance.cs
================================================
癤퓎sing System;

namespace GameAPIServer.Models.DAO;

public class GdbAttendanceInfo
{
    public int uid { get; set; }
    public int attendance_cnt { get; set; }
    public DateTime recent_attendance_dt { get; set; }
}



================================================
File: Models/DAO/Friend.cs
================================================
癤퓎sing System;

namespace GameAPIServer.Models.DAO;


public class GdbFriendInfo
{
    public int uid { get; set; }

    public string friend_uid { get; set; }
    public bool friend_yn { get; set; }
    public DateTime create_dt { get; set; }
}



================================================
File: Models/DAO/Game.cs
================================================
癤퓎sing System;

namespace GameAPIServer.Models.DAO;

public class GdbMiniGameInfo
{
    public int game_key { get; set; }
    public int play_char_key { get; set; }
    public int bestscore { get; set; }
    public DateTime create_dt { get; set; }
    public DateTime new_record_dt { get; set; }
    public DateTime recent_play_dt { get; set; }
    public int bestscore_cur_season { get; set; }
    public int bestscore_prev_season { get; set; }
}



================================================
File: Models/DAO/Item.cs
================================================
癤퓎sing System;

namespace GameAPIServer.Models.DAO;

public class GdbUserCharInfo
{
    public int uid { get; set; }
    public int char_key { get; set; }
    public int char_level { get; set; }
    public int char_cnt { get; set; }
    public int skin_key { get; set; }
    public string costume_json { get; set; }
}

public class GdbUserCostumeInfo
{
    public int uid { get; set; }
    public int costume_key { get; set; }
    public int costume_level { get; set; }
    public int costume_cnt { get; set; }
    public DateTime create_dt { get; set; }
}

public class GdbUserCharRandomSkillInfo
{
    public int uid { get; set; }
    public int char_key { get; set; }
    public int index_num { get; set; }
    public int skill_key { get; set; }
    public DateTime create_dt { get; set; }
}

public class GdbUserSkinInfo
{
    public int uid { get; set; }
    public int skin_key { get; set; }
    public DateTime create_dt { get; set; }
}

public class GdbUserFoodInfo
{
    public int uid { get; set; }
    public int food_key { get; set; }
    public int food_qty { get; set; }
    public int food_level { get; set; }
    public int food_gear_qty { get; set; }
    public DateTime create_dt { get; set; }
}

public class CharCostumeInfo
{
    public int Head { get; set; }
    public int Face { get; set; }
    public int Hand { get; set; }
}



================================================
File: Models/DAO/Mailbox.cs
================================================
癤퓎sing System;

namespace GameAPIServer.Models.DAO;

public class GdbMailboxInfo
{
    public int mail_seq { get; set; }
    public int uid { get; set; }
    public string mail_title { get; set; }
    public DateTime create_dt { get; set; }
    public DateTime expire_dt { get; set; }
    public DateTime receive_dt { get; set; }
    public bool receive_yn { get; set; }
}

public class GdbMailboxRewardInfo : RewardData
{
    public int mail_seq { get; set; }
}



================================================
File: Models/DAO/User.cs
================================================
癤퓎sing System;

namespace GameAPIServer.Models.DAO;

public class GdbUserInfo
{
    public int uid { get; set; }
    public string player_id { get; set; }
    public string nickname { get; set; }
    public int main_char_key { get; set; }
    public DateTime create_dt { get; set; }
    public DateTime recent_login_dt { get; set; }
    public int total_bestscore { get; set; }
    public int total_bestscore_cur_season { get; set; }
    public int total_bestscore_prev_season { get; set; }
    public int star_point { get; set; }
}

public class GdbUserMoneyInfo
{
    public int uid { get; set; }
    public int jewelry { get; set; }
    public int gold_medal { get; set; }
    public int sunchip { get; set; }
    public int cash { get; set; }
}




================================================
File: Models/DTO/AttendanceCheck.cs
================================================
癤퓎sing System.Collections.Generic;

namespace GameAPIServer.Models.DTO;

public class AttendanceCheckResponse : ErrorCode
{
    public List<ReceivedReward> Rewards { get; set; }
}



================================================
File: Models/DTO/AttendanceInfo.cs
================================================
癤퓎sing GameAPIServer.Models.DAO;


namespace GameAPIServer.Models.DTO;

public class AttendanceInfoResponse : ErrorCode
{
    public GdbAttendanceInfo AttendanceInfo { get; set; }
}


================================================
File: Models/DTO/CreateAccount.cs
================================================
using System.ComponentModel.DataAnnotations;


namespace GameAPIServer.Models.DTO;

public class CreateHiveAccountRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "EMAIL CANNOT BE EMPTY")]
    [StringLength(50, ErrorMessage = "EMAIL IS TOO LONG")]
    [RegularExpression("^[a-zA-Z0-9_\\.-]+@([a-zA-Z0-9-]+\\.)+[a-zA-Z]{2,6}$", ErrorMessage = "E-mail is not valid")]
    public string UserID { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "PASSWORD CANNOT BE EMPTY")]
    [StringLength(30, ErrorMessage = "PASSWORD IS TOO LONG")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}


public class CreateHiveAccountResponse
{
    [Required]
    public global::ErrorCode Result { get; set; } = global::ErrorCode.None;
}



================================================
File: Models/DTO/ErrorCode.cs
================================================
癤퓆amespace GameAPIServer.Models.DTO;

public class ErrorCode
{
    public global::ErrorCode Result { get; set; } = global::ErrorCode.None;
}



================================================
File: Models/DTO/FreindDelete.cs
================================================
癤퓎sing System.ComponentModel.DataAnnotations;

namespace GameAPIServer.Models.DTO;

public class FriendDeleteRequest
{
    [Required]
    public int FriendUid { get; set; }
}


public class FriendDeleteResponse : ErrorCode
{
}





================================================
File: Models/DTO/FriendAccept.cs
================================================
癤퓎sing System.ComponentModel.DataAnnotations;

namespace GameAPIServer.Models.DTO;

public class FriendAcceptRequest
{
    [Required]
    public int FriendUid { get; set; }
}

public class FriendAcceptResponse : ErrorCode
{
}



================================================
File: Models/DTO/FriendAdd.cs
================================================
癤퓎sing System.ComponentModel.DataAnnotations;

namespace GameAPIServer.Models.DTO;

public class SendFriendReqRequest
{
    [Required]
    public int FriendUid { get; set; }
}


public class SendFriendReqResponse : ErrorCode
{
}





================================================
File: Models/DTO/FriendList.cs
================================================
癤퓎sing System.Collections.Generic;


namespace GameAPIServer.Models.DTO;

public class FriendListResponse : ErrorCode
{
    public IEnumerable<DAO.GdbFriendInfo> FriendList { get; set; }
}



================================================
File: Models/DTO/GameDataLoad.cs
================================================
﻿
namespace GameAPIServer.Models.DTO;

public class GameDataLoadResponse : ErrorCode
{
    public DataLoadGameInfo GameData { get; set; }
}

public class DataLoadGameInfo
{        
}

public class UserCharInfo
{
    public DAO.GdbUserCharInfo CharInfo { get; set; }
  
}



================================================
File: Models/DTO/Header.cs
================================================
癤퓎sing Microsoft.AspNetCore.Mvc;

namespace GameAPIServer.Models.DTO;

public class Header
{
    [FromHeader]
    public int Uid { get; set; }
}



================================================
File: Models/DTO/Login.cs
================================================
癤퓎sing System.ComponentModel.DataAnnotations;


namespace GameAPIServer.Models.DTO;

public class LoginRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "EMAIL CANNOT BE EMPTY")]
    [StringLength(50, ErrorMessage = "EMAIL IS TOO LONG")]
    [RegularExpression("^[a-zA-Z0-9_\\.-]+@([a-zA-Z0-9-]+\\.)+[a-zA-Z]{2,6}$", ErrorMessage = "E-mail is not valid")]
    public string UserID { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "PASSWORD CANNOT BE EMPTY")]
    [StringLength(30, ErrorMessage = "PASSWORD IS TOO LONG")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}

public class LoginResponse
{
    [Required] public global::ErrorCode Result { get; set; } = global::ErrorCode.None;
    [Required] public string AuthToken { get; set; } = "";
    [Required] public long Uid { get; set; } = 0;

    public DataLoadUserInfo userData { get; set; }
}


================================================
File: Models/DTO/Logout.cs
================================================
癤퓆amespace GameAPIServer.Models.DTO;

public class LogoutResponse : ErrorCode
{
}



================================================
File: Models/DTO/MailDelete.cs
================================================
癤퓎sing System.ComponentModel.DataAnnotations;

namespace GameAPIServer.Models.DTO;

public class MailDeleteRequest
{
    [Required]
    public int MailSeq { get; set; }
}
public class MailDeleteResponse : ErrorCode
{
}



================================================
File: Models/DTO/MailList.cs
================================================
癤퓎sing System.Collections.Generic;

namespace GameAPIServer.Models.DTO;

public class MailboxInfoResponse : ErrorCode
{
    public List<UserMailInfo> MailList { get; set; }
}



================================================
File: Models/DTO/MailReceive.cs
================================================
癤퓎sing System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GameAPIServer.Models.DTO;


public class MailReceiveRequest
{
    [Required]
    public int MailSeq { get; set; }
}

public class MailReceiveResponse : ErrorCode
{
    public List<ReceivedReward> Rewards { get; set; }
}



================================================
File: Models/DTO/OtherUserInfo.cs
================================================
癤퓎sing System;
using System.ComponentModel.DataAnnotations;

namespace GameAPIServer.Models.DTO;

public class OtherUserInfoRequest
{
    [Required]
    public int Uid { get; set; }
}

public class OtherUserInfoResponse : ErrorCode
{
    public OtherUserInfo UserInfo { get; set; }
}

public class OtherUserInfo
{
    public int uid { get; set; }
    public string nickname { get; set; }
    public int total_bestscore { get; set; }
    public int total_bestscore_cur_season { get; set; }
    public int total_bestscore_prev_season { get; set; }
    public int main_char_key { get; set; }
    public int main_char_skin_key { get; set; }
    public string main_char_costume_json { get; set; }
    public long rank;
}



================================================
File: Models/DTO/Ranking.cs
================================================
癤퓎sing System.Collections.Generic;

namespace GameAPIServer.Models.DTO;

public class RankingResponse : ErrorCode
{
    public List<RankData> RankingData { get; set; }
}

public class RankData
{
    public long rank { get; set; }
    public int uid { get; set; }
    public int score { get; set; }
}



================================================
File: Models/DTO/SocialDataLoad.cs
================================================
癤퓎sing System.Collections.Generic;

namespace GameAPIServer.Models.DTO;

public class SocialDataLoadResponse : ErrorCode
{
    public DataLoadSocialInfo SocialData { get; set; }
}

public class DataLoadSocialInfo
{
    public IEnumerable<DAO.GdbFriendInfo> FriendList { get; set; }
    public List<UserMailInfo> MailList { get; set; }

}

public class UserMailInfo
{
    public DAO.GdbMailboxInfo MailInfo { get; set; }
    public IEnumerable<DAO.GdbMailboxRewardInfo> MailItems { get; set; }
}



================================================
File: Models/DTO/UserDataLoad.cs
================================================
癤퓆amespace GameAPIServer.Models.DTO;

public class UserDataLoadResponse : ErrorCode
{
    public DataLoadUserInfo UserData { get; set; }
}

public class DataLoadUserInfo
{
    public DAO.GdbUserInfo UserInfo { get; set; }
    public DAO.GdbUserMoneyInfo MoneyInfo { get; set; }
    public DAO.GdbAttendanceInfo AttendanceInfo { get; set; }
}



================================================
File: Models/DTO/UserRank.cs
================================================
癤퓆amespace GameAPIServer.Models.DTO;

public class UserRankResponse : ErrorCode
{
    public long Rank { get; set; }
}



================================================
File: Models/DTO/UserSetMainChar.cs
================================================
癤퓎sing System.ComponentModel.DataAnnotations;

namespace GameAPIServer.Models.DTO;

public class UserSetMainCharRequest
{
    [Required]
    public int CharKey { get; set; }
}

public class UserSetMainCharResponse : ErrorCode
{
}



================================================
File: Properties/launchSettings.json
================================================
﻿{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:55883",
      "sslPort": 44384
    }
  },
  "profiles": {
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "launchUrl": "weatherforecast",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "API Server": {
      "commandName": "Project",
      "launchBrowser": false,
      "launchUrl": "Login",
      "applicationUrl": "http://0.0.0.0:11500;",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}



================================================
File: Repository/FakeGameDb.cs
================================================
癤퓎sing System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GameAPIServer.Models;
using GameAPIServer.Models.DAO;

namespace GameAPIServer.Repository;

public class FakeGameDb : IGameDb
{
    public Task<ErrorCode> CreateAccount(string userID, string pw)
        => Task.FromResult(ErrorCode.None);

    public Task<(ErrorCode, long)> VerifyUser(string userID, string pw)
        => Task.FromResult((ErrorCode.None, 1L));

    public Task<GdbUserInfo> GetUserByPlayerId(long playerId)
        => Task.FromResult(new GdbUserInfo { uid = 1, player_id = "player1", nickname = "FakeUser" });

    public Task<GdbUserInfo> GetUserByUid(int uid)
        => Task.FromResult(new GdbUserInfo { uid = uid, player_id = "player1", nickname = "FakeUser" });

    public Task<GdbUserInfo> GetUserByNickname(string nickname, IDbTransaction transaction)
        => Task.FromResult(new GdbUserInfo { uid = 1, player_id = "player1", nickname = nickname });

    public Task<int> InsertUser(long playerId, string nickname, IDbTransaction transaction)
        => Task.FromResult(1);

    public Task<int> UpdateRecentLogin(int uid)
        => Task.FromResult(1);

    public Task<GdbFriendInfo> GetFriendReqInfo(int uid, int friendUid)
        => Task.FromResult(new GdbFriendInfo { uid = uid, friend_uid = friendUid.ToString(), friend_yn = false, create_dt = DateTime.UtcNow });

    public Task<int> InsertFriendReq(int uid, int friendUid, bool accept = false)
        => Task.FromResult(1);

    public Task<int> InsertFriendReq(int uid, int friendUid, IDbTransaction transaction, bool accept = false)
        => Task.FromResult(1);

    public Task<int> UpdateFriendReqAccept(int uid, int friendUid, IDbTransaction transaction, bool accept = false)
        => Task.FromResult(1);

    public Task<IEnumerable<GdbFriendInfo>> GetFriendInfoList(int uid)
        => Task.FromResult<IEnumerable<GdbFriendInfo>>(new List<GdbFriendInfo>());

    public Task<int> DeleteFriendEachOther(int uid, int friendUid)
        => Task.FromResult(1);

    public Task<int> DeleteFriendReq(int uid, int friendUid)
        => Task.FromResult(1);

    public Task<int> InsertInitMoneyInfo(int uid, IDbTransaction transaction)
        => Task.FromResult(1);

    public Task<int> InsertInitAttendance(int uid, IDbTransaction transaction)
        => Task.FromResult(1);

    public Task<IEnumerable<GdbMailboxInfo>> GetMailList(int uid)
        => Task.FromResult<IEnumerable<GdbMailboxInfo>>(new List<GdbMailboxInfo>());

    public Task<GdbMailboxInfo> GetMailInfo(int mailSeq)
        => Task.FromResult(new GdbMailboxInfo { mail_seq = mailSeq, uid = 1, mail_title = "Test Mail", create_dt = DateTime.UtcNow, expire_dt = DateTime.UtcNow.AddDays(7), receive_dt = DateTime.MinValue, receive_yn = false });

    public Task<IEnumerable<GdbMailboxRewardInfo>> GetMailRewardList(int mailSeq)
        => Task.FromResult<IEnumerable<GdbMailboxRewardInfo>>(new List<GdbMailboxRewardInfo>());

    public Task<int> DeleteMail(int mailSeq)
        => Task.FromResult(1);

    public Task<int> DeleteMailReward(int mailSeq)
        => Task.FromResult(1);

    public Task<int> UpdateReceiveMail(int mailSeq)
        => Task.FromResult(1);

    public Task<GdbAttendanceInfo> GetAttendanceById(int uid)
        => Task.FromResult(new GdbAttendanceInfo { uid = uid, attendance_cnt = 0, recent_attendance_dt = DateTime.UtcNow });

    public Task<GdbUserMoneyInfo> GetUserMoneyById(int uid)
        => Task.FromResult(new GdbUserMoneyInfo { uid = uid, jewelry = 0, gold_medal = 0, sunchip = 0, cash = 0 });

    public Task<int> UpdateMainChar(int uid, int charKey)
        => Task.FromResult(1);

    public Task<IEnumerable<RdbUserScoreData>> SelectAllUserScore()
        => Task.FromResult<IEnumerable<RdbUserScoreData>>(new List<RdbUserScoreData>());

    public Task<int> CheckAttendanceById(int uid)
        => Task.FromResult(1);

    public Task<int> UpdateUserjewelry(int uid, int rewardQty)
        => Task.FromResult(1);

    public IDbConnection GDbConnection()
        => null;
}



================================================
File: Repository/IGameDb.cs
================================================
癤퓎sing GameAPIServer.Models;
using GameAPIServer.Models.DAO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace GameAPIServer.Repository;

public interface IGameDb
{
    public Task<ErrorCode> CreateAccount(string userID, string pw);
    public Task<(ErrorCode, Int64)> VerifyUser(string userID, string pw);


    public Task<GdbUserInfo> GetUserByPlayerId(Int64 playerId);
    public Task<GdbUserInfo> GetUserByUid(int uid);
    public Task<GdbUserInfo> GetUserByNickname(string nickname, IDbTransaction transaction);
    public Task<int> InsertUser(Int64 playerId, string nickname, IDbTransaction transaction);
    public Task<int> UpdateRecentLogin(int uid);
    public Task<GdbFriendInfo> GetFriendReqInfo(int uid, int friendUid);
    public Task<int> InsertFriendReq(int uid, int friendUid, bool accept = false);
    public Task<int> InsertFriendReq(int uid, int friendUid, IDbTransaction transaction, bool accept = false);
    public Task<int> UpdateFriendReqAccept(int uid, int friendUid, IDbTransaction transaction, bool accept = false);
    public Task<IEnumerable<GdbFriendInfo>> GetFriendInfoList(int uid);
    public Task<int> DeleteFriendEachOther(int uid, int friendUid);
    public Task<int> DeleteFriendReq(int uid, int friendUid);
    

    public Task<int> InsertInitMoneyInfo(int uid, IDbTransaction transaction);
    public Task<int> InsertInitAttendance(int uid, IDbTransaction transaction);
  
    
    public Task<IEnumerable<GdbMailboxInfo>> GetMailList(int uid);
    public Task<GdbMailboxInfo> GetMailInfo(int mailSeq);
    public Task<IEnumerable<GdbMailboxRewardInfo>> GetMailRewardList(int mailSeq);
    public Task<int> DeleteMail(int mailSeq);
    public Task<int> DeleteMailReward(int mailSeq);
    public Task<int> UpdateReceiveMail(int mailSeq);
    public Task<GdbAttendanceInfo> GetAttendanceById(int uid);
    public Task<GdbUserMoneyInfo> GetUserMoneyById(int uid);
    public Task<int> UpdateMainChar(int uid, int charKey);
    public Task<IEnumerable<RdbUserScoreData>> SelectAllUserScore();
    public Task<int> CheckAttendanceById(int uid);
    public Task<int> UpdateUserjewelry(int uid, int rewardQty);
    public IDbConnection GDbConnection();
}


================================================
File: Services/AuthService.cs
================================================
癤퓎sing GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DAO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ZLogger;
using GameAPIServer.Repository;

namespace GameAPIServer.Servicies;

public class AuthService : IAuthService
{
    readonly ILogger<AuthService> _logger;
    readonly IGameDb _gameDb;
    string _hiveServerAPIAddress;

    public AuthService(ILogger<AuthService> logger, IConfiguration configuration, IGameDb gameDb)
    {
        _gameDb = gameDb;
        _logger = logger;
        _hiveServerAPIAddress = configuration.GetSection("HiveServerAddress").Value + "/verifytoken";
    }

    public async Task<ErrorCode> CreateAccount(string userID, string passWord)
    {
        var result = await _gameDb.CreateAccount(userID, passWord);
        return result;
    }
    
    public async Task<(ErrorCode, Int64, string)> Login(string userID, string passWord)
    {
        (var result, var uid) = await _gameDb.VerifyUser(userID, passWord);
        if (result != ErrorCode.None)
        {
            return (result, 0, "");
        }

        var token = Security.CreateAuthToken();
        result = ErrorCode.None;

        return (result, uid, token);
    }

    

}



================================================
File: Services/DataLoadService.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DTO;
using System.Threading.Tasks;

namespace GameAPIServer.Servicies;

public class DataLoadService : IDataLoadService
{
    readonly IGameService _gameService;
    readonly IFriendService _friendService;
    readonly IUserService _userService;
    readonly IMailService _mailService;
    

    public DataLoadService(IMailService mailService, IUserService userService, IGameService gameService, IFriendService friendService)
    {
        _mailService = mailService;
        _userService = userService;
        _gameService = gameService;
        _friendService = friendService;
    }

    public async Task<(ErrorCode, DataLoadUserInfo)> LoadUserData(int uid)
    {
        DataLoadUserInfo loadData = new();
        (var errorCode, loadData.UserInfo) = await _userService.GetUserInfo(uid);
        if (errorCode != ErrorCode.None)
        {
            return (errorCode,null);
        }

        (errorCode, loadData.MoneyInfo) = await _userService.GetUserMoneyInfo(uid);
        if (errorCode != ErrorCode.None)
        {
            return (errorCode, null);
        }
                
        return (ErrorCode.None, loadData);
    }

    public async Task<(ErrorCode, DataLoadGameInfo)> LoadGameData(int uid)
    {
        DataLoadGameInfo loadData = new();

        //TODO 게임 데이터 로딩을 구현한다
        await Task.CompletedTask;

        return (ErrorCode.None, loadData);
    }

    public async Task<(ErrorCode, DataLoadSocialInfo)> LoadSocialData(int uid)
    {
        DataLoadSocialInfo loadData = new();

        (var errorCode, loadData.MailList) = await _mailService.GetMailList(uid);
        if (errorCode != ErrorCode.None)
        {
            return (errorCode, null);
        }

        (errorCode, loadData.FriendList) = await _friendService.GetFriendList(uid);
        if (errorCode != ErrorCode.None)
        {
            return (errorCode, null);
        }

        return (ErrorCode.None, loadData);
    }
}



================================================
File: Services/FriendService.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DAO;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZLogger;
using GameAPIServer.Repository;

namespace GameAPIServer.Servicies;

public class FriendService : IFriendService
{
    readonly ILogger<FriendService> _logger;
    readonly IGameDb _gameDb;

    public FriendService(ILogger<FriendService> logger, IGameDb gameDb)
    {
        _gameDb = gameDb;
        _logger = logger;
    }

    public async Task<ErrorCode> SendFriendReq(int uid, int friendUid)
    {
        try
        {
            if (uid == friendUid)
            {
                _logger.ZLogDebug(
                $"[Friend.AcceptFriendRequest] ErrorCode: {ErrorCode.FriendAcceptFailSameUid}, Uid: {uid}, FriendUid : {friendUid}");
                return ErrorCode.FriendAcceptFailSameUid;
            }

            GdbUserInfo userInfo = await _gameDb.GetUserByUid(friendUid);
            //없는 유저일 때
            if (userInfo is null)
            {
                _logger.ZLogDebug(
                $"[Friend.SendFriendReq] ErrorCode: {ErrorCode.FriendSendReqFailUserNotExist}, Uid: {uid}, FriendUid : {friendUid}");
                return ErrorCode.FriendSendReqFailUserNotExist;
            }
            //이미 친구신청 했거나 친구일 때
            GdbFriendInfo friendReqInfo = await _gameDb.GetFriendReqInfo(uid, friendUid);
            if (friendReqInfo is not null)
            {
                _logger.ZLogDebug(
                $"[Friend.SendFriendReq] ErrorCode: {ErrorCode.FriendSendReqFailAlreadyExist}, Uid: {uid}, FriendUid : {friendUid}");
                return ErrorCode.FriendSendReqFailAlreadyExist;
            }
            //상대의 친구요청이 와있을 때
            friendReqInfo = await _gameDb.GetFriendReqInfo(friendUid, uid);
            if(friendReqInfo is not null)
            {
                _logger.ZLogDebug(
                                   $"[Friend.SendFriendReq] ErrorCode: {ErrorCode.FriendSendReqFailNeedAccept}, Uid: {uid}, FriendUid : {friendUid}");
                return ErrorCode.FriendSendReqFailNeedAccept;
            }
            //친구 요청
            var rowCount = await _gameDb.InsertFriendReq(uid, friendUid);
            if (rowCount != 1)
            {
                _logger.ZLogDebug(
                $"[Friend.SendFriendReq] ErrorCode: {ErrorCode.FriendSendReqFailInsert}, Uid: {uid}, FriendUid : {friendUid}");
                return ErrorCode.FriendSendReqFailInsert;
            }
            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Friend.SendFriendReq] ErrorCode: {ErrorCode.FriendSendReqFailException}, Uid: {uid}, FriendUid : {friendUid}");
            return ErrorCode.FriendSendReqFailException;
        }
    }

    public async Task<ErrorCode> AcceptFriendReq(int uid, int friendUid)
    {
        try
        {
            if (uid == friendUid)
            {
                _logger.ZLogDebug(
                $"[Friend.AcceptFriendRequest] ErrorCode: {ErrorCode.FriendAcceptFailSameUid}, Uid: {uid}, FriendUid : {friendUid}");
                return ErrorCode.FriendAcceptFailSameUid;
            }

            GdbUserInfo userInfo = await _gameDb.GetUserByUid(friendUid);
            //없는 유저일 때
            if (userInfo is null)
            {
                _logger.ZLogDebug(
                $"[Friend.AcceptFriendRequest] ErrorCode: {ErrorCode.FriendSendReqFailUserNotExist}, Uid: {uid}, FriendUid : {friendUid}");
                return ErrorCode.AcceptFriendRequestFailUserNotExist;
            }
            //친구 요청이 안왔거나, 이미 친구 일 때
            GdbFriendInfo friendReqInfo = await _gameDb.GetFriendReqInfo(friendUid, uid);
            if (friendReqInfo is null || friendReqInfo.friend_yn == true)
            {
                _logger.ZLogDebug(
                $"[Friend.AcceptFriendRequest] ErrorCode: {ErrorCode.FriendSendReqFailAlreadyExist}, Uid: {uid}, FriendUid : {friendUid}");
                return ErrorCode.AcceptFriendRequestFailAlreadyFriend;
            }
            //친구 요청 수락
            return await AcceptRequest(uid, friendUid);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Friend.AcceptFriendRequest] ErrorCode: {ErrorCode.AcceptFriendRequestFailException}, Uid: {uid}, FriendUid : {friendUid}");
            return ErrorCode.AcceptFriendRequestFailException;
        }
    }

    public async Task<(ErrorCode, IEnumerable<GdbFriendInfo>)> GetFriendList(int uid)
    {
        try
        {
            return (ErrorCode.None, await _gameDb.GetFriendInfoList(uid));
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                               $"[Friend.GetFriendList] ErrorCode: {ErrorCode.FriendGetListFailException}, Uid: {uid}");
            return (ErrorCode.FriendGetListFailException, null);
        }
    }

    public async Task<ErrorCode> DeleteFriend(int uid, int friendUid)
    {
        try
        {
            if (uid == friendUid)
            {
                _logger.ZLogDebug(
                $"[Friend.AcceptFriendRequest] ErrorCode: {ErrorCode.FriendAcceptFailSameUid}, Uid: {uid}, FriendUid : {friendUid}");
                return ErrorCode.FriendAcceptFailSameUid;
            }

            //친구가 아닐 때
            GdbFriendInfo friendInfo = await _gameDb.GetFriendReqInfo(uid, friendUid);
            if (friendInfo is null || friendInfo.friend_yn==false)
            {
                _logger.ZLogDebug(
                $"[Friend.DeleteFriend] ErrorCode: {ErrorCode.FriendDeleteFailNotFriend}, Uid: {uid}, FriendUid : {friendUid}");
                return ErrorCode.FriendDeleteFailNotFriend;
            }

            var rowCount = await _gameDb.DeleteFriendEachOther(uid, friendUid);
            if(rowCount != 2)
            {
                _logger.ZLogDebug(
                                   $"[Friend.DeleteFriend] ErrorCode: {ErrorCode.FriendDeleteFailDelete}, Uid: {uid}, FriendUid : {friendUid}");
                return ErrorCode.FriendDeleteFailDelete;
            }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"[Friend.DeleteFriend] ErrorCode: {ErrorCode.FriendDeleteFailException}, Uid: {uid}, FriendUid : {friendUid}");
            return ErrorCode.FriendDeleteFailException;
        }
    }

    public async Task<ErrorCode> CancelFriendReq(int uid, int friendUid)
    {
        try
        {
            if (uid == friendUid)
            {
                _logger.ZLogDebug(
                $"[Friend.AcceptFriendRequest] ErrorCode: {ErrorCode.FriendAcceptFailSameUid}, Uid: {uid}, FriendUid : {friendUid}");
                return ErrorCode.FriendAcceptFailSameUid;
            }
            //친구 요청을 안했거나 친구 상태 일때
            GdbFriendInfo friendInfo = await _gameDb.GetFriendReqInfo(uid, friendUid);
            if (friendInfo is null || friendInfo.friend_yn == true)
            {
                _logger.ZLogDebug(
                $"[Friend.DeleteFriendReq] ErrorCode: {ErrorCode.FriendDeleteReqFailNotFriend}, Uid: {uid}, FriendUid : {friendUid}");
                return ErrorCode.FriendDeleteReqFailNotFriend;
            }

            var rowCount = await _gameDb.DeleteFriendReq(uid, friendUid);
            if (rowCount != 1)
            {
                _logger.ZLogDebug(
                $"[Friend.DeleteFriendReq] ErrorCode: {ErrorCode.FriendDeleteReqFailDelete}, Uid: {uid}, FriendUid : {friendUid}");
            }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"[Friend.DeleteFriendReq] ErrorCode: {ErrorCode.FriendDeleteReqFailException}, Uid: {uid}, FriendUid : {friendUid}");
            return ErrorCode.FriendDeleteReqFailException;
        }
    }

    async Task<ErrorCode> AcceptRequest(int uid, int friendUid)
    {
        var transaction = _gameDb.GDbConnection().BeginTransaction();
        try
        {
            var rowCount = await _gameDb.InsertFriendReq(uid, friendUid, transaction, true);
            if (rowCount != 1)
            {
                _logger.ZLogDebug(
                $"[Friend.AcceptFriendRequest] ErrorCode: {ErrorCode.FriendSendReqFailInsert}, Uid: {uid}, FriendUid : {friendUid}");
                transaction.Rollback();
                return ErrorCode.FriendSendReqFailInsert;
            }

            rowCount = await _gameDb.UpdateFriendReqAccept(uid, friendUid, transaction, true);
            if (rowCount != 1)
            {
                _logger.ZLogDebug(
                $"[Friend.AcceptFriendRequest] ErrorCode: {ErrorCode.FriendSendReqFailInsert}, Uid: {uid}, FriendUid : {friendUid}");
                transaction.Rollback();
                return ErrorCode.FriendSendReqFailInsert;
            }

            transaction.Commit();
            return ErrorCode.None;
        }
        catch(Exception e)
        {
            _logger.ZLogError(e,
                $"[Friend.AcceptFriendRequest] ErrorCode: {ErrorCode.FriendAcceptFailException}, Uid: {uid}, FriendUid : {friendUid}");
            return ErrorCode.FriendAcceptFailException;
        }
        finally
        {
            transaction.Dispose();
        }
    }
}



================================================
File: Services/GameService.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DAO;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ZLogger;
using GameAPIServer.Repository;

namespace GameAPIServer.Servicies;

public class GameService :IGameService
{
    readonly ILogger<GameService> _logger;
    readonly IGameDb _gameDb;
  

    public GameService(ILogger<GameService> logger, IGameDb gameDb)
    {
        _logger = logger;
        _gameDb = gameDb;
    }


    public async Task<(ErrorCode, int)> InitNewUserGameData(Int64 playerId, string nickname)
    {
        var transaction = _gameDb.GDbConnection().BeginTransaction();
        try
        {
            var (errorCode, uid) = await CreateUserAsync(playerId, nickname, transaction);
            if(errorCode != ErrorCode.None)
            {
                transaction.Rollback();
                return (errorCode,0);
            }

            
            var rowCount = await _gameDb.InsertInitMoneyInfo(uid, transaction);
            if (rowCount != 1)
            {
                transaction.Rollback();
                return (ErrorCode.InitNewUserGameDataFailMoney, 0);
            }

            rowCount = await _gameDb.InsertInitAttendance(uid, transaction);
            if (rowCount != 1)
            {
                transaction.Rollback();
                return (ErrorCode.InitNewUserGameDataFailAttendance, 0);
            }

            transaction.Commit();
            return (ErrorCode.None, uid);
        }
        catch (Exception e)
        {
            transaction.Rollback();
            _logger.ZLogError(e,
                $"[Game.InitNewUserGameData] ErrorCode: {ErrorCode.InitNewUserGameDataFailException}, PlayerId : {playerId}");
            return (ErrorCode.GameSetNewUserListFailException, 0);
        }
        finally
        {
            transaction.Dispose();
        }
    }

    async Task<(ErrorCode,int)> CreateUserAsync(Int64 playerId, string nickname, IDbTransaction transaction)
    {
        try
        {
            if (string.IsNullOrEmpty(nickname))
            {
                _logger.ZLogError($"[CreateAccount] ErrorCode: {ErrorCode.CreateUserFailNoNickname}, nickname : {nickname}");
                return (ErrorCode.CreateUserFailNoNickname,0);
            }
            //nickname 중복 체크
            var existUser = await _gameDb.GetUserByNickname(nickname, transaction);
            if (existUser is not null)
            {
                _logger.ZLogError($"[CreateAccount] ErrorCode: {ErrorCode.CreateUserFailDuplicateNickname}, nickname : {nickname}");
                return (ErrorCode.CreateUserFailDuplicateNickname,0);
            }

            //유저 생성
            return (ErrorCode.None, await _gameDb.InsertUser(playerId, nickname, transaction));
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[CreateAccount] ErrorCode: {ErrorCode.CreateUserFailException}, PlayerId: {playerId}");
            return (ErrorCode.CreateUserFailException, 0);
        }
    }

}



================================================
File: Services/MailService.cs
================================================
﻿using GameAPIServer.Models;
using GameAPIServer.Models.DTO;
using GameAPIServer.Repository;
using GameAPIServer.Servicies.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Servicies;

public class MailService : IMailService
{
    readonly ILogger<MailService> _logger;
    readonly IGameDb _gameDb;
   

    public MailService(ILogger<MailService> logger, IGameDb gameDb )
    {
        _logger = logger;
        _gameDb = gameDb;
    }

    public async Task<(ErrorCode,List<UserMailInfo>)> GetMailList(int uid)
    {
        try
        {
            List<UserMailInfo> userMailInfoList = new();
            
            var mailList = await _gameDb.GetMailList(uid);
            foreach (var mail in mailList)
            {
                UserMailInfo userMailInfo = new();
                userMailInfo.MailInfo = mail;
                userMailInfo.MailItems = await _gameDb.GetMailRewardList(mail.mail_seq);
                userMailInfoList.Add(userMailInfo);
            }

            return (ErrorCode.None, userMailInfoList);
        }
        catch (System.Exception e)
        {
            _logger.ZLogError(e,
                                   $"[Mail.GetMailList] ErrorCode: {ErrorCode.MailListFailException}, Uid: {uid}");
            return (ErrorCode.MailListFailException, null);
        }
    }

    public async Task<(ErrorCode,List<ReceivedReward>)> ReceiveMail(int uid, int mailSeq)
    {
        try
        {
            //메일의 존재, 수령여부, 소유권 확인
            var mailInfo = await _gameDb.GetMailInfo(mailSeq);
            if(mailInfo == null)
            {
                return (ErrorCode.MailReceiveFailMailNotExist, null);
            }
            if (mailInfo.receive_yn == true)
            {
                return (ErrorCode.MailReceiveFailAlreadyReceived, null);
            }
            if(mailInfo.uid != uid)
            {
                return ((ErrorCode.MailReceiveFailNotMailOwner, null));
            }

            //메일 보상 확인
            var mailRewards = await _gameDb.GetMailRewardList(mailSeq);

            //메일 보상 수령
            return await ReceiveMailRewards(mailInfo.uid, mailSeq, mailRewards);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                   $"[Mail.ReceiveMail] ErrorCode: {ErrorCode.MailReceiveFailException}, Uid: {uid}, MailSeq: {mailSeq}");
            return (ErrorCode.MailReceiveFailException, null);
        }
    }

    async Task<(ErrorCode,List<ReceivedReward>)> ReceiveMailRewards(int uid, int mailSeq, IEnumerable<RewardData> mailRewards)
    {
        try
        {
            List<ReceivedReward> totalRewards = new();

            
            //수령일자 및 수령여부 업데이트
            var rowCount = await _gameDb.UpdateReceiveMail(mailSeq);
            if (rowCount != 1)
            {
                return (ErrorCode.MailReceiveFailUpdateReceiveDt, null);
            }

            return (ErrorCode.None, totalRewards);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Mail.ReceiveMail] ErrorCode: {ErrorCode.MailReceiveRewardsFailException}, MailSeq: {mailSeq}");
            return (ErrorCode.MailReceiveRewardsFailException, null);
        }
    }

    public async Task<ErrorCode> DeleteMail(int uid, int mailSeq)
    {
        try
        {
            //메일의 존재, 소유권 확인
            var mailInfo = await _gameDb.GetMailInfo(mailSeq);
            if (mailInfo == null)
            {
                return ErrorCode.MailReceiveFailMailNotExist;
            }
            if (mailInfo.uid != uid)
            {
                return ErrorCode.MailReceiveFailNotMailOwner;
            }

            //메일 삭제
            var rowCount = await _gameDb.DeleteMail(mailSeq);
            if (rowCount != 1)
            {
                return ErrorCode.MailDeleteFailDeleteMail;
            }

            //메일 보상 삭제
            rowCount = await _gameDb.DeleteMailReward(mailSeq);
            if (rowCount < 1)
            {
                return ErrorCode.MailDeleteFailDeleteMailReward;
            }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Mail.DeleteMail] ErrorCode: {ErrorCode.MailDeleteFailException}, Uid: {uid}, MailSeq: {mailSeq}");
            return ErrorCode.MailDeleteFailException;
        }
    }
}



================================================
File: Services/UserService.cs
================================================
癤퓎sing GameAPIServer.Models.DAO;
using GameAPIServer.Models.DTO;
using GameAPIServer.Repository;
using GameAPIServer.Servicies.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Servicies;

public class UserService : IUserService
{
    readonly ILogger<UserService> _logger;
    readonly IGameDb _gameDb;
    

    public UserService(ILogger<UserService> logger, IGameDb gameDb)
    {
        _logger = logger;
        _gameDb = gameDb;
    }

    public async Task<(ErrorCode, GdbUserInfo)> GetUserInfo(int uid)
    {
        try
        {
            return (ErrorCode.None, await _gameDb.GetUserByUid(uid));
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[User.GetUserInfo] ErrorCode: {ErrorCode.UserInfoFailException}, Uid: {uid}");
            return (ErrorCode.UserInfoFailException, null);
        }
    }

    public async Task<(ErrorCode, GdbUserMoneyInfo)> GetUserMoneyInfo(int uid)
    {
        try
        {
            return (ErrorCode.None, await _gameDb.GetUserMoneyById(uid));
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[User.GetUserMoneyInfo] ErrorCode: {ErrorCode.UserMoneyInfoFailException}, Uid: {uid}");
            return (ErrorCode.UserMoneyInfoFailException, null);
        }
    }

    

    public async Task<(ErrorCode, OtherUserInfo)> GetOtherUserInfo(int uid)
    {
        try
        {
            var userInfo = await _gameDb.GetUserByUid(uid);
            if (userInfo == null)
            {
                _logger.ZLogError($"[User.GetOtherUserInfo] ErrorCode: {ErrorCode.UserNotExist}, Uid: {uid}");
                return (ErrorCode.UserNotExist, null);
            }

            
            ErrorCode errorCode = ErrorCode.None;

            if(errorCode != ErrorCode.None)
            {
                _logger.ZLogError($"[User.GetOtherUserInfo] ErrorCode: {errorCode}, Uid: {uid}");
                return (errorCode, null);
            }

            return (ErrorCode.None, new OtherUserInfo
            {
                uid = uid,
                nickname = userInfo.nickname,                    
            });

        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[User.GetOtherUserInfo] ErrorCode: {ErrorCode.GetOtherUserInfoFailException}, Uid: {uid}");
            return (ErrorCode.GetOtherUserInfoFailException, null);
        }
    }
}



================================================
File: Services/Interfaces/IAuthService.cs
================================================
癤퓎sing System.Threading.Tasks;
using System;

namespace GameAPIServer.Servicies.Interfaces;

public interface IAuthService
{
    public Task<ErrorCode> CreateAccount(string userID, string passWord);
        
    public Task<(ErrorCode, Int64, string)> Login(string userID, string passWord);

    //public Task<ErrorCode> UpdateLastLoginTime(int uid);

    //public Task<(ErrorCode, string)> RegisterToken(int uid);
}



================================================
File: Services/Interfaces/IDataLoadService.cs
================================================
癤퓎sing GameAPIServer.Models.DTO;
using System.Threading.Tasks;

namespace GameAPIServer.Servicies.Interfaces;

public interface IDataLoadService
{
    public Task<(ErrorCode, DataLoadUserInfo)> LoadUserData(int uid);
    public Task<(ErrorCode, DataLoadGameInfo)> LoadGameData(int uid);
    public Task<(ErrorCode, DataLoadSocialInfo)> LoadSocialData(int uid);
}



================================================
File: Services/Interfaces/IFriendService.cs
================================================
癤퓎sing System.Threading.Tasks;
using System.Collections.Generic;
using GameAPIServer.Models.DAO;

namespace GameAPIServer.Servicies.Interfaces;

public interface IFriendService
{
    public Task<ErrorCode> SendFriendReq(int uid, int friendUid);
    public Task<ErrorCode> AcceptFriendReq(int uid, int friendUid);
    public Task<(ErrorCode, IEnumerable<GdbFriendInfo>)> GetFriendList(int uid);
    public Task<ErrorCode> DeleteFriend(int uid, int friendUid);
    public Task<ErrorCode> CancelFriendReq(int uid, int friendUid);
}



================================================
File: Services/Interfaces/IGameService.cs
================================================
癤퓎sing GameAPIServer.Models.DAO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameAPIServer.Servicies.Interfaces;

public interface IGameService
{
    public Task<(ErrorCode, int)> InitNewUserGameData(Int64 playerId, string nickname);
   
}
 


================================================
File: Services/Interfaces/IMailService.cs
================================================
癤퓎sing GameAPIServer.Models;
using GameAPIServer.Models.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameAPIServer.Servicies.Interfaces;

public interface IMailService
{
    public Task<(ErrorCode, List<UserMailInfo>)> GetMailList(int uid);
    public Task<(ErrorCode, List<ReceivedReward>)> ReceiveMail(int uid, int mailSeq);
    public Task<ErrorCode> DeleteMail(int uid, int mailSeq);
}



================================================
File: Services/Interfaces/IUserService.cs
================================================
癤퓎sing GameAPIServer.Models.DAO;
using GameAPIServer.Models.DTO;
using System.Threading.Tasks;

namespace GameAPIServer.Servicies.Interfaces;

public interface IUserService
{
    public Task<(ErrorCode, GdbUserInfo)> GetUserInfo(int uid);
    
    public Task<(ErrorCode, GdbUserMoneyInfo)> GetUserMoneyInfo(int uid);
    
    public Task<(ErrorCode, OtherUserInfo)> GetOtherUserInfo(int uid);
}




