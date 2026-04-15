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






