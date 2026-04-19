Directory structure:
└── Tutorial/
    ├── README.md
    ├── GameAPIServer/
    │   ├── ErrorCode.cs
    │   ├── GameAPIServer.csproj
    │   ├── GameAPIServer.sln
    │   ├── Program.cs
    │   ├── Security.cs
    │   ├── appsettings.Development.json
    │   ├── appsettings.json
    │   ├── httpTest.http
    │   ├── Controllers/
    │   │   ├── CreateAccountController.cs
    │   │   ├── FriendAcceptController.cs
    │   │   ├── FriendCancelReqController.cs
    │   │   ├── FriendDeleteController.cs
    │   │   ├── FriendListController.cs
    │   │   ├── FriendSendReqController.cs
    │   │   ├── GameDataLoadController.cs
    │   │   ├── LoginController.cs
    │   │   ├── MailDeleteController.cs
    │   │   ├── MailListController.cs
    │   │   ├── MailReceiveController.cs
    │   │   ├── MetricsExampleController.cs
    │   │   └── UserDataLoadController.cs
    │   ├── Models/
    │   │   ├── MasterDB.cs
    │   │   ├── RedisDB.cs
    │   │   ├── DAO/
    │   │   │   ├── Account.cs
    │   │   │   ├── Attendance.cs
    │   │   │   ├── Friend.cs
    │   │   │   ├── Game.cs
    │   │   │   ├── Item.cs
    │   │   │   ├── Mailbox.cs
    │   │   │   └── User.cs
    │   │   └── DTO/
    │   │       ├── AttendanceCheck.cs
    │   │       ├── AttendanceInfo.cs
    │   │       ├── CreateAccount.cs
    │   │       ├── ErrorCode.cs
    │   │       ├── FreindDelete.cs
    │   │       ├── FriendAccept.cs
    │   │       ├── FriendAdd.cs
    │   │       ├── FriendList.cs
    │   │       ├── GameDataLoad.cs
    │   │       ├── Header.cs
    │   │       ├── Login.cs
    │   │       ├── Logout.cs
    │   │       ├── MailDelete.cs
    │   │       ├── MailList.cs
    │   │       ├── MailReceive.cs
    │   │       ├── OtherUserInfo.cs
    │   │       ├── Ranking.cs
    │   │       ├── SocialDataLoad.cs
    │   │       ├── UserDataLoad.cs
    │   │       ├── UserRank.cs
    │   │       └── UserSetMainChar.cs
    │   ├── Properties/
    │   │   └── launchSettings.json
    │   ├── Repository/
    │   │   ├── FakeGameDb.cs
    │   │   └── IGameDb.cs
    │   └── Services/
    │       ├── AuthService.cs
    │       ├── DataLoadService.cs
    │       ├── FriendService.cs
    │       ├── GameService.cs
    │       ├── MailService.cs
    │       ├── UserService.cs
    │       └── Interfaces/
    │           ├── IAuthService.cs
    │           ├── IDataLoadService.cs
    │           ├── IFriendService.cs
    │           ├── IGameService.cs
    │           ├── IMailService.cs
    │           └── IUserService.cs
    └── TCPSocketServer/
        ├── Program.cs
        ├── TCPSocketServer.csproj
        └── TCPSocketServer.sln

================================================
File: README.md
================================================
# Prometheus 실습

## 1. 프로메테우스 설치

## 2. Prometheus.yml
  
```yaml
# my global config
global:
  scrape_interval: 15s # Set the scrape interval to every 15 seconds. Default is every 1 minute.
  evaluation_interval: 15s # Evaluate rules every 15 seconds. The default is every 1 minute.
  # scrape_timeout is set to the global default (10s).
 
# Alertmanager configuration
alerting:
  alertmanagers:
    - static_configs:
        - targets:
          # - alertmanager:9093
 
# Load rules once and periodically evaluate them according to the global 'evaluation_interval'.
rule_files:
  # - "first_rules.yml"
  # - "second_rules.yml"
 
# A scrape configuration containing exactly one endpoint to scrape:
# Here it's Prometheus itself.
scrape_configs:
  - job_name: "prometheus"
    # metrics_path defaults to '/metrics'
    # scheme defaults to 'http'.
    static_configs:
      - targets: ["localhost:9090"]
 
# 이 설정으로 스크랩한 모든 시계열엔 여기 있는 job 이름이 `job=<job_name>` 레이블로 추가된다.
  - job_name: "apiserver"
# 글로벌로 설정해둔 기본값을 재정의하며, 이 job에선 타겟을 5초 간격으로 스크랩한다.
    scrape_interval: 5s
    static_configs:
    # 프로메테우스가 읽어올 주소를 입력한다.
      - targets: ['localhost:5000']
    # labels를 통해 읽어온 데이터에 라벨링을 하여 관리할 수 있다.
        labels:
          groups: "server"
   
  - job_name: "gameserver"
    scrape_interval: 5s
    static_configs:
      - targets: ['localhost:5002']
        labels:
          type: "server"
 
  - job_name: "server_info"
    scrape_interval: 5s
    static_configs:
      - targets: ['localhost:9182']
        labels:
          type: "info"

```


## API 서버에 적용하기
Prometheus는 “Pull 방식” → Web API에서 `/metrics` 같은 엔드포인트를 열어주면 Prometheus 서버가 주기적으로 가져간다.  
  
ASP.NET Core에서는 보통 `prometheus-net` 라이브러리를 많이 사용한다.  
👉 NuGet 패키지: prometheus-net.AspNetCore  

아래와 같은 NuGet도 있다.      
Nuget package for general use and metrics export via HttpListener or to Pushgateway: prometheus-net  
`Install-Package prometheus-net`    
    
Nuget package for ASP.NET Core middleware and stand-alone Kestrel metrics server: prometheus-net.AspNetCore  
`Install-Package prometheus-net.AspNetCore`    
  
Nuget package for ASP.NET Core Health Check integration: prometheus-net.AspNetCore.HealthChecks  
`Install-Package prometheus-net.AspNetCore.HealthChecks`  
  
Nuget package for ASP.NET Core gRPC integration: prometheus-net.AspNetCore.Grpc  
`Install-Package prometheus-net.AspNetCore.Grpc`  
    
  
### 패키지 설치
터미널(또는 Visual Studio 패키지 매니저 콘솔)에서:  
```
dotnet add package prometheus-net.AspNetCore
```  
  

### Program.cs 수정 
  
```  
using Prometheus;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();

app.UseRouting();

// 1) 기본 HTTP 요청 미들웨어
app.UseHttpMetrics();   // 요청 관련 기본 메트릭 수집

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapMetrics(); // 2) /metrics 엔드포인트 등록
});

app.Run();
```

이제 http://localhost:5000/metrics (혹은 Kestrel/설정된 포트)에서 Prometheus 포맷 메트릭이 노출된다.  
위 주소의 port 번호는 api 서버에서 설정된 port 번호를 따르면 된다.  


### 커스텀 메트릭 추가하기

```
using Prometheus;

public class WeatherController : ControllerBase
{
    // Counter 메트릭 예시
    private static readonly Counter WeatherRequests = 
        Metrics.CreateCounter("weather_requests_total", "Total number of weather requests.");

    [HttpGet("weather")]
    public IActionResult GetWeather()
    {
        WeatherRequests.Inc(); // 호출할 때마다 카운터 증가
        return Ok(new { Temp = "24C", Status = "Sunny" });
    }
}
```  
  
- Counter: 단순 증가 값 (요청 수, 에러 수)
- Gauge: 현재 상태값 (메모리 사용량, 큐 길이)
-  Histogram/Summary: 분포/지연 시간 같은 값
  

### Prometheus 서버 설정
Prometheus의 prometheus.yml에 Web API 주소를 등록합니다:

```yaml
scrape_configs:
  - job_name: 'aspnetcore-api'
    scrape_interval: 15s
    static_configs:
      - targets: ['localhost:5000']   # API 주소와 포트
```  
  
- Web API가 /metrics를 열고 있으므로 metrics_path는 기본값(/metrics) 그대로 둔다.
- Prometheus 재시작 후, http://localhost:9090/targets 에서 aspnetcore-api job이 UP 상태로 떠야 한다.


### 참고할 수 있는 메트릭 예시
- 요청 수: http_requests_received_total
- 처리 시간 히스토그램: http_request_duration_seconds_bucket
- 현재 실행 중 요청 수: http_requests_in_progress
  

### GC와 스레드풀 등의 정보도 수집하고 싶을 때
ASP.NET Core API 서버에서 Prometheus로 **GC, 스레드 풀 등 특정 런타임 메트릭만 수집**하고 싶다면, 두 가지 방법이 있다.
 
#### 1) 기본 prometheus-net.AspNetCore 미들웨어만 사용할 때
`app.UseHttpMetrics()` + `endpoints.MapMetrics()` 조합은 **HTTP 요청/응답 관련 메트릭**만 기본 제공한다.  
GC, Thread Pool, Working Set 같은 .NET 런타임 메트릭은 자동으로는 나오지 않는다.  


#### 2) .NET 런타임 메트릭 전용 라이브러리 사용
**패키지**: [`prometheus-net.DotNetRuntime`](https://github.com/dotnet/runtime)  
  
### 설치

```bash
dotnet add package prometheus-net.DotNetRuntime
```

##### Program.cs에서 활성화

```csharp
using Prometheus;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();

// .NET 런타임 메트릭 등록 (GC, 스레드 풀 등)
DotNetRuntimeStatsBuilder.Default().StartCollecting();
/*
// 원하는 정보만 모니터링
IDisposable collector = DotNetRuntimeStatsBuilder
    .Customize()
    .WithContentionStats()
    .WithJitStats()
    .WithThreadPoolStats()
    .WithGcStats()
    .WithExceptionStats()
    .StartCollecting();
*/


app.UseRouting();

// HTTP 요청 메트릭
app.UseHttpMetrics();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapMetrics();
});

app.Run();
```
  
#### 3) 수집되는 주요 런타임 메트릭 예시

* **GC 관련**
  * `dotnet_gc_collections_total` (세대별 GC 횟수)
  * `dotnet_gc_heap_size_bytes` (힙 크기)
  * `dotnet_gc_gen0_collections_total`, `gen1`, `gen2`
* **ThreadPool 관련**
  * `dotnet_threadpool_threads_total`
  * `dotnet_threadpool_queue_length`
* **JIT, Lock 등**
  * `dotnet_jit_methods_total`
  * `dotnet_contention_total`
  

#### 4) 특정 메트릭만 수집하고 싶을 때
Prometheus는 “pull + filter” 방식이라, 서버 측에서 노출을 막기보다는 **Prometheus 쪽에서 선택적으로 가져오는 방식**을 쓴다.  

##### (1) Prometheus 설정에서 특정 메트릭만 스크랩
  
```yaml
scrape_configs:
  - job_name: 'aspnetcore-api'
    static_configs:
      - targets: ['localhost:5000']
    metric_relabel_configs:
      - source_labels: [__name__]
        regex: "dotnet_gc_.*"
        action: keep
```
  
→ 이렇게 하면 `dotnet_gc_`로 시작하는 메트릭만 저장된다.  
  
##### (2) 코드에서 직접 필터링
`prometheus-net` 자체는 개별 메트릭 노출을 끄는 옵션이 없으므로, 특정 수치를 원치 않으면:

* 커스텀 미들웨어로 제한된 `/metrics` 엔드포인트 구현
* 또는 `metric_relabel_configs` 쪽에서 필터링하는 게 일반적이다.

  
#### 5) Grafana에서 활용

* `dotnet_gc_collections_total` → GC 빈도 추적
* `dotnet_threadpool_queue_length` → 대기 작업 적체 확인
* `dotnet_threadpool_threads_total` → 동시 처리 리소스 확인



### HTTP 요청 관련 메트릭 쿼리
ASP.NET Core Web API + `prometheus-net.AspNetCore` 

#### 1) 기본적으로 수집되는 HTTP 메트릭
`app.UseHttpMetrics()`를 쓰면 아래와 같은 메트릭이 자동 노출된다:  

* **총 요청 수**
  `http_requests_received_total{method="GET",code="200"}`
* **진행 중 요청 수**
  `http_requests_in_progress`
* **요청 처리 시간** (히스토그램)
  `http_request_duration_seconds_bucket`
  `http_request_duration_seconds_sum`
  `http_request_duration_seconds_count`


#### 2) PromQL 쿼리 예시

##### (1) 초당 요청 수 (QPS)

```promql
rate(http_requests_received_total[5m])
```

* 5분 구간 이동 평균 요청 속도
  
##### (2) 상태 코드별 요청 비율

```promql
sum(rate(http_requests_received_total[5m])) by (code)
```

* 200/400/500 코드별 요청 비율 확인 가능

##### (3) 평균 응답 시간

```promql
rate(http_request_duration_seconds_sum[5m])
/
rate(http_request_duration_seconds_count[5m])
```

* 요청당 평균 처리 시간 (초 단위)

##### (4) P90 응답 시간 (느린 요청 감지)

```promql
histogram_quantile(0.9, rate(http_request_duration_seconds_bucket[5m]))
```

* 응답 시간 분포에서 90% 구간 추정치

##### (5) 현재 처리 중인 요청 수

```promql
http_requests_in_progress
```
  

#### 3) Prometheus 웹 UI에서 실행 방법
1. 브라우저에서 `http://localhost:9090` 접속
2. 상단 탭에서 **Graph/Explore** 선택
3. 위 PromQL 쿼리 입력 후 **Execute**
4. Graph 버튼으로 그래프 확인 가능

  
#### 4) 응용 팁

* 특정 메서드만 보고 싶으면:

  ```promql
  rate(http_requests_received_total{method="POST"}[5m])
  ```
* 특정 경로만 보고 싶으면 (endpoint 라벨이 있을 때):

  ```promql
  rate(http_requests_received_total{route="/api/orders"}[5m])
  ```
   

### 지정한  **job_name**에만 쿼리 할 때
**라벨 필터(label filter)** 를 쓴다.  

#### 1) Prometheus 라벨 구조 복습
Prometheus는 수집할 때 자동으로 몇 가지 라벨을 붙여준다:

* `job` : `prometheus.yml`에서 정의한 `job_name`
* `instance` : `target` (예: `localhost:9182`)
* 그 외 exporter가 노출한 라벨들 (method, code, path 등)
  
즉, 쿼리할 때 `job="windows"` 처럼 필터링할 수 있다.  

#### 2) 쿼리 예시

##### (1) 특정 job의 모든 메트릭 가져오기

```promql
http_requests_received_total{job="aspnetcore-api"}
```

##### (2) job 단위로 요청 수 비교

```promql
sum(rate(http_requests_received_total[5m])) by (job)
```

→ 여러 job이 있을 때 job별 요청률을 한눈에 비교

##### (3) 특정 job + 특정 instance

```promql
rate(http_requests_received_total{job="aspnetcore-api",instance="localhost:5000"}[5m])
```

##### (4) 특정 job에서만 응답 시간 P90 구하기

```promql
histogram_quantile(
  0.9,
  sum(rate(http_request_duration_seconds_bucket{job="aspnetcore-api"}[5m])) by (le)
)
```
  
  
#### 3) 실전 팁

* **job 단위 대시보드**: Grafana 패널에서 변수(`$job`)를 만들어 두면 job 선택 드롭다운으로 전환 가능
* **비교용**: `by(job)` 집계를 쓰면 여러 job을 동시에 비교할 수 있습니다
* **필터 조합**: `{job="aspnetcore-api", method="POST"}` 같이 조합 가능



### 특정 http request를 Count하기
- 기본적으로 PromQL `http_request_duration_seconds_count`을 통해 모든 http request를 모니터링 할 수 있지만 특정 http request만을 모니터링 할 수 있도록 Counter 메트릭을 생성할 수 있다.  
  
```
namespace ApiServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IAccountDb _accountDb;
        private readonly ILogger<LoginController> _logger;
        private readonly IRedisDb _redisDb;
         
        // 프로메테우스 Counter 측정 항목 설명
        private static readonly Counter _LoginCounter = Metrics.CreateCounter("API_Server_LoginCounter", "API_Server_LoginCounter");
         
        public LoginController(ILogger<LoginController> logger, IAccountDb accountDb, IRedisDb redisDb)
        {
            _accountDb = accountDb;
            _logger = logger;
            _redisDb = redisDb;
        }
 
        [HttpPost]
        public async Task<LoginResponse> LoginPost(LoginRequest request)
        {
            //...
 
            // 프로메테우스 카운터 증가
            _LoginCounter.Inc();
             
            return response;
        }
    }
}
```
  
프로메테우스에서 "API_Server_LoginCounter" 쿼리를 통해 모니터링 할 수 있다.


## TCP 소켓 서버
핵심은 **“서버 로직은 TCP 소켓으로 처리하면서, 별도로 Prometheus가 가져갈 수 있는 `/metrics` HTTP 엔드포인트를 열어주는 것”** 이다.

### 1) NuGet 패키지 설치
Prometheus용 라이브러리 **prometheus-net**을 씁니다.

```bash
dotnet add package prometheus-net
dotnet add package prometheus-net.AspNetCore
```  
 
`prometheus-net.AspNetCore` 은 `KestrelMetricServer`을 위해 설치한다.  


### TCP 서버 코드 예시

아주 단순한 TCP 서버 (비동기 echo 서버) 예제이다.

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using Prometheus;

class Program
{
    // Prometheus Counter 메트릭
    private static readonly Counter TcpRequestsTotal =
        Metrics.CreateCounter("tcp_requests_total", "Total number of TCP requests handled.");

    static async Task Main(string[] args)
    {
        // 1) Prometheus 전용 HTTP 서버 시작 (/metrics 노출)
        // 포트 1234에서 metrics 엔드포인트 제공
        var metricServer = new KestrelMetricServer(port: 1234);
        metricServer.Start();

        // 2) TCP 서버 시작
        var listener = new TcpListener(IPAddress.Any, 9000);
        listener.Start();
        Console.WriteLine("TCP Server listening on port 9000. Prometheus on :1234/metrics");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleClient(client);
        }
    }

    private static async Task HandleClient(TcpClient client)
    {
        using var stream = client.GetStream();
        var buffer = new byte[1024];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"Received: {received}");

        // 3) 요청 카운터 증가
        TcpRequestsTotal.Inc();

        // 에코 응답
        byte[] response = Encoding.UTF8.GetBytes($"Echo: {received}");
        await stream.WriteAsync(response, 0, response.Length);
    }
}
```

### 확인
1. TCP 서버: `nc localhost 9000` → 메시지 보내면 echo 응답  
2. Prometheus 메트릭: `http://localhost:1234/metrics` 접속 →  
  
   ```
   # HELP tcp_requests_total Total number of TCP requests handled.
   # TYPE tcp_requests_total counter
   tcp_requests_total 5
   ```
  
### Prometheus 설정 (prometheus.yml)

```yaml
scrape_configs:
  - job_name: 'tcp-server'
    static_configs:
      - targets: ['localhost:1234']
```

### 확장 아이디어

* 연결 수 추적 (Gauge):

  ```csharp
  private static readonly Gauge ActiveConnections =
      Metrics.CreateGauge("tcp_active_connections", "Number of active TCP connections.");
  ```

  → 클라이언트 연결 시 `.Inc()`, 끊을 때 `.Dec()`.

* 처리 시간 측정 (Histogram):

  ```csharp
  private static readonly Histogram RequestDuration =
      Metrics.CreateHistogram("tcp_request_duration_seconds", "TCP request handling time.");

  using (RequestDuration.NewTimer())
  {
      // 요청 처리 코드
  }
  ```

### ✅ 정리

* **prometheus-net** 라이브러리의 `KestrelMetricServer`로 `/metrics` HTTP 엔드포인트를 열고,
* TCP 서버 로직에서 Counter/Gauge/Histogram 같은 메트릭을 업데이트하면 됩니다.
* Prometheus가 해당 포트를 스크랩해서 모니터링할 수 있습니다.


### TCP 서버 자체 상태(연결/요청) + 애플리케이션 로직(메시지 처리량, 큐 길이 등)
  
#### 1) 기본 구성
1. **TCP 서버 로직**은 그대로 유지 (예: `TcpListener`)
2. **KestrelMetricServer**를 별도로 띄워 `/metrics` HTTP 엔드포인트 제공
3. TCP 서버에서 이벤트가 발생할 때마다 Prometheus 메트릭을 업데이트

즉,  
* TCP 통신 = 비즈니스 로직
* KestrelMetricServer = 모니터링 HTTP 엔드포인트

#### 2) 추적할 메트릭 종류

🔹 TCP 서버 상태  
  
* **활성 연결 수 (Gauge)**

  ```csharp
  private static readonly Gauge ActiveConnections =
      Metrics.CreateGauge("tcp_active_connections", "Number of active TCP connections.");
  ```

* **총 처리 요청 수 (Counter)**

  ```csharp
  private static readonly Counter RequestsTotal =
      Metrics.CreateCounter("tcp_requests_total", "Total number of TCP requests handled.");
  ```

* **요청 처리 시간 (Histogram)**

  ```csharp
  private static readonly Histogram RequestDuration =
      Metrics.CreateHistogram("tcp_request_duration_seconds", "Request processing time in seconds.");
  ```
  
🔹 애플리케이션 로직 상태

* **메시지 처리량 (Counter)**
  → 메시지를 받을 때마다 `MessagesTotal.Inc()`

* **큐 길이 (Gauge)**
  → 메시지 큐에 push → `.Inc()`, 처리 시 → `.Dec()`

* **에러 발생 수 (Counter)**

  ```csharp
  private static readonly Counter ErrorsTotal =
      Metrics.CreateCounter("tcp_errors_total", "Number of errors during request processing.");
  ```

#### 3) 코드 예시 (간단 버전)

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using Prometheus;

class Program
{
    private static readonly Gauge ActiveConnections =
        Metrics.CreateGauge("tcp_active_connections", "Number of active TCP connections.");

    private static readonly Counter RequestsTotal =
        Metrics.CreateCounter("tcp_requests_total", "Total number of TCP requests handled.");

    private static readonly Histogram RequestDuration =
        Metrics.CreateHistogram("tcp_request_duration_seconds", "TCP request handling time.");

    private static readonly Gauge QueueLength =
        Metrics.CreateGauge("tcp_message_queue_length", "Messages waiting in queue.");

    static async Task Main(string[] args)
    {
        // Prometheus metrics endpoint (http://localhost:1234/metrics)
        var metricServer = new KestrelMetricServer(port: 1234);
        metricServer.Start();

        var listener = new TcpListener(IPAddress.Any, 9000);
        listener.Start();
        Console.WriteLine("TCP Server listening on port 9000");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleClient(client);
        }
    }

    private static async Task HandleClient(TcpClient client)
    {
        ActiveConnections.Inc();
        try
        {
            using (client)
            {
                var buffer = new byte[1024];
                using (var timer = RequestDuration.NewTimer())
                {
                    var stream = client.GetStream();
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    RequestsTotal.Inc();
                    QueueLength.Inc();   // 메시지 큐에 들어감

                    // 처리 로직 (예: echo)
                    byte[] response = Encoding.UTF8.GetBytes($"Echo: {msg}");
                    await stream.WriteAsync(response, 0, response.Length);

                    QueueLength.Dec();   // 처리 완료
                }
            }
        }
        catch
        {
            // 에러 카운터 올리기 가능
        }
        finally
        {
            ActiveConnections.Dec();
        }
    }
}
```
  
#### 4) Prometheus 설정

```yaml
scrape_configs:
  - job_name: 'tcp-server'
    static_configs:
      - targets: ['localhost:1234']
```
  
#### 5) Grafana에서 활용

* **tcp\_active\_connections** → 현재 연결 수 모니터링
* **rate(tcp\_requests\_total\[5m])** → 초당 요청 처리량(QPS)
* **histogram\_quantile(0.9, rate(tcp\_request\_duration\_seconds\_bucket\[5m]))** → 90% 응답 시간
* **tcp\_message\_queue\_length** → 메시지 적체 여부 확인


#### ✅ 요약

* **TCP 레벨 상태**: 연결 수(Gauge), 요청 수(Counter), 처리 시간(Histogram)
* **애플리케이션 로직**: 메시지 처리량(Counter), 큐 길이(Gauge), 에러 수(Counter)
* Prometheus는 `/metrics` 엔드포인트에서 수집 → Grafana로 시각화
  
 
### 전체 단위 메트릭과 클라이언트별 라벨 메트릭
  
#### 🔹 1. 서버 전체 단위 메트릭
**정의**: 서버 전체 상태를 하나의 수치로 집계 → “서버가 얼마나 바쁘냐”를 보는 용도.

* 예시 메트릭:

  * 활성 연결 수

    ```csharp
    private static readonly Gauge ActiveConnections =
        Metrics.CreateGauge("tcp_active_connections_total", "Active TCP connections.");
    ```

    * 클라이언트가 접속하면 `ActiveConnections.Inc();`
    * 끊어지면 `ActiveConnections.Dec();`
  * 총 요청 수

    ```csharp
    private static readonly Counter RequestsTotal =
        Metrics.CreateCounter("tcp_requests_total", "Total handled TCP requests.");
    ```

    * 요청 처리할 때마다 `RequestsTotal.Inc();`
  * 평균 처리 시간 (히스토그램)

    ```csharp
    private static readonly Histogram RequestDuration =
        Metrics.CreateHistogram("tcp_request_duration_seconds", "Request processing time.");
    ```

👉 장점:

* 데이터 개수가 적어 **가볍고 빠름**
* 전체 부하 추세 파악에 적합 (Grafana 대시보드에서 한눈에 보기 좋음)

👉 단점:

* 어떤 클라이언트(IP)가 문제를 일으키는지 파악하기 어려움
  
#### 🔹 2. 클라이언트별 라벨 메트릭
**정의**: 메트릭에 `client_ip` 같은 라벨을 붙여서, 특정 클라이언트 단위로 상세 모니터링.

* 예시 코드:

  ```csharp
  private static readonly Counter RequestsByClient =
      Metrics.CreateCounter("tcp_requests_by_client_total",
          "Total requests per client.",
          new CounterConfiguration
          {
              LabelNames = new[] { "client_ip" }
          });

  private static readonly Gauge ActiveConnectionsByClient =
      Metrics.CreateGauge("tcp_active_connections_by_client",
          "Active connections per client.",
          new GaugeConfiguration
          {
              LabelNames = new[] { "client_ip" }
          });
  ```

* 사용 예시:

  ```csharp
  string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
  RequestsByClient.WithLabels(clientIp).Inc();
  ActiveConnectionsByClient.WithLabels(clientIp).Inc();
  ```

👉 장점:
* **문제 클라이언트 추적 가능** (예: 특정 IP가 요청을 과도하게 보낼 때)
* 트래픽 분포, Top N 클라이언트 분석 가능

👉 단점:
* 클라이언트가 많아질수록 **라벨 조합 폭발(label cardinality)** 문제가 생김 → Prometheus 성능 저하
* 수천 개 이상의 클라이언트 IP를 모두 저장하면 메모리/스토리지 부담
  
#### 🔹 3. 운영 시 고려
* **전체 단위 메트릭**은 항상 필수 → 서버 전체 부하/상태 감지용
* **클라이언트별 라벨 메트릭**은 선택적 →

  * 내부 서비스처럼 클라이언트 수가 제한적일 때 유용
  * 외부 불특정 다수 클라이언트가 접속하는 서버라면 위험 (메트릭 폭발)

👉 그래서 보통은:

1. 전체 단위 메트릭 = Prometheus 기본 수집
2. 클라이언트별 메트릭 =

   * 샘플링해서 저장
   * Top-N 클라이언트만 추적
   * 또는 로그 기반 분석 툴(ELK, Loki 등)과 병행
  
#### 🔹 4. PromQL 예시

* 전체 요청률:

  ```promql
  rate(tcp_requests_total[5m])
  ```
* 클라이언트별 요청률:

  ```promql
  rate(tcp_requests_by_client_total[5m]) by (client_ip)
  ```
* 특정 클라이언트(IP=192.168.0.10)의 연결 수:

  ```promql
  tcp_active_connections_by_client{client_ip="192.168.0.10"}
  ```

#### ✅ **정리**

* **전체 단위 메트릭** → 항상 안정적, 서버 상태를 빠르게 알 수 있음
* **클라이언트별 라벨 메트릭** → 상세 분석에 유용하지만 라벨 폭발 주의
* 따라서 운영에서는 두 가지를 **병행**하되, 클라이언트별 메트릭은 **제한적/샘플링**해서 쓰는 게 안전합니다.
  
   

### JIT, GC, 예외(Exception) 메트릭을 수집
  
#### 🔹 1. NuGet 패키지 추가
  
```bash
dotnet add package prometheus-net.DotNetRuntime
```
  
* [`prometheus-net.DotNetRuntime`](https://github.com/djluck/prometheus-net.DotNetRuntime) 은 CLR 이벤트를 구독해서 **GC, JIT, ThreadPool, Exception** 같은 런타임 메트릭을 자동으로 노출한다.
* 기존 `prometheus-net`과 호환되며 `/metrics` 엔드포인트에 추가된다.

  
#### 🔹 2. Program.cs 예시 (소켓 서버 + 런타임 메트릭)
 
```csharp
using Prometheus;

class Program
{
    static async Task Main(string[] args)
    {
        // 1) .NET 런타임 메트릭 수집 시작 (GC, JIT, Exception 등)
        DotNetRuntimeStatsBuilder.Default().StartCollecting();

        // 2) Prometheus metrics endpoint 열기 (예: http://localhost:1234/metrics)
        var metricServer = new KestrelMetricServer(port: 1234);
        metricServer.Start();

        Console.WriteLine("Socket server with Prometheus metrics running...");

        // 3) TCP 서버 실행 로직
        var listener = new TcpListener(System.Net.IPAddress.Any, 9000);
        listener.Start();
        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleClient(client);
        }
    }

    private static async Task HandleClient(TcpClient client)
    {
        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string received = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

            Console.WriteLine($"Received: {received}");

            var response = System.Text.Encoding.UTF8.GetBytes($"Echo: {received}");
            await stream.WriteAsync(response, 0, response.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            // 예외 발생 수는 DotNetRuntimeStatsBuilder가 자동 수집
        }
    }
}
```

  
#### 🔹 3. 수집되는 주요 메트릭

##### GC

* `dotnet_gc_collections_total{generation="0"}`
* `dotnet_gc_heap_size_bytes`
* `dotnet_gc_time_seconds_total`

##### JIT

* `dotnet_jit_methods_total`
* `dotnet_jit_time_seconds_total`

##### Exception

* `dotnet_exceptions_total{type="System.InvalidOperationException"}`

##### ThreadPool

* `dotnet_threadpool_threads_total`
* `dotnet_threadpool_queue_length`

 
#### 🔹 4. Prometheus 설정
  
```yaml
scrape_configs:
  - job_name: 'socket-server'
    static_configs:
      - targets: ['localhost:1234']
```
  
#### 🔹 5. 주의할 점
* 런타임 메트릭은 **애플리케이션 성능 특성**을 볼 때 유용하지만, 너무 잦은 스크랩은 Prometheus 부담 → 보통 `scrape_interval: 15s` 정도가 적당합니다.
* Exception 메트릭은 **발생한 예외 타입별로 라벨**이 붙는데,

  * 예외 타입이 너무 다양하면 라벨 카디널리티 문제 가능성 → 운영에서는 주요 예외만 잡히도록 조율 필요
  
  
#### ✅ 정리

* **prometheus-net.DotNetRuntime**을 쓰면 소켓 서버에서도 **GC, JIT, Exception, ThreadPool** 메트릭을 자동 수집 가능
* `/metrics` 엔드포인트에 자동 추가 → Prometheus에서 그대로 스크랩
* Exception 라벨 폭발 문제만 주의

  

## 매트릭 지표 

### 🔹 1. GC 관련 메트릭

| 메트릭                                               | 의미            | 활용                                                             |
| ------------------------------------------------- | ------------- | -------------------------------------------------------------- |
| `dotnet_gc_collections_total{generation="0/1/2"}` | 세대별 GC 발생 횟수  | - **Gen0**가 많으면 일시적 객체 생성이 많음<br>- **Gen2**가 자주 발생하면 메모리 압박 크다 |
| `dotnet_gc_heap_size_bytes`                       | 힙 크기          | - 전체 메모리 사용량 모니터링<br>- GC 직후에도 줄지 않으면 **대형 객체/메모리 누수** 가능성     |
| `dotnet_gc_time_seconds_total`                    | GC에 소비된 누적 시간 | - CPU를 GC에 쓰고 있는 시간 비율 확인<br>- 서비스 성능 저하 원인 진단에 도움             |
| `dotnet_gc_committed_memory_bytes`                | GC 커밋된 메모리    | - 메모리 압박 추세 확인 (실제 OS 메모리 사용량 반영)                              |

👉 **튜닝 포인트**

* Gen2/LOH 컬렉션이 잦으면 → **객체 생명주기 관리/메모리 풀링** 고려
* `gcServer` 모드(`runtimeconfig.json`) 켜서 서버용 GC로 바꾸면 멀티코어 환경에서 효율↑


### 🔹 2. JIT 메트릭

| 메트릭                             | 의미             | 활용                                                             |
| ------------------------------- | -------------- | -------------------------------------------------------------- |
| `dotnet_jit_methods_total`      | JIT 컴파일된 메서드 수 | 서버가 오래 켜져 있는데 값이 계속 증가 → **Dynamic Code Generation** 과다 사용 가능성 |
| `dotnet_jit_time_seconds_total` | JIT에 소요된 시간    | 기동 직후 높다가 안정되는 게 정상                                            |

👉 **튜닝 포인트**

* 성능 민감한 경우 **ReadyToRun(R2R) 빌드** 또는 **Tiered Compilation 최적화** 사용


### 🔹 3. 예외 메트릭 (성능 측면)

* `dotnet_exceptions_total{type="..."}`
* 예외는 발생 시마다 스택 트레이스 수집으로 **비용이 크다**
* 특정 타입 예외가 빈번하면 try-catch 로직 개선 or validation 사전 체크 필요

### 🔹 4. ThreadPool / 대기열 메트릭

| 메트릭                               | 의미                 | 활용                                       |
| --------------------------------- | ------------------ | ---------------------------------------- |
| `dotnet_threadpool_threads_total` | ThreadPool 총 쓰레드 수 | 증가 추세 → 요청량 급증 or 작업이 블로킹됨               |
| `dotnet_threadpool_queue_length`  | 대기 중인 작업 개수        | 값이 계속 높음 → ThreadPool이 backlog 처리 못하고 있음 |

👉 **튜닝 포인트**

* I/O 작업은 async/await로 처리해 ThreadPool 점유 최소화
* CPU bound 작업은 `Task.Run` 대신 별도 `System.Threading.Channels` / 전용 워커 스레드 고려

### 🔹 5. PromQL 예시 (Grafana 대시보드에서 활용)

* **GC 비율 (전체 CPU 대비 GC 시간)**

  ```promql
  rate(dotnet_gc_time_seconds_total[5m]) 
  / rate(process_cpu_seconds_total[5m]) * 100
  ```

* **세대별 GC 발생률**

  ```promql
  rate(dotnet_gc_collections_total{generation="2"}[5m])
  ```

* **메모리 사용량 추세**

  ```promql
  dotnet_gc_heap_size_bytes
  ```

* **스레드풀 대기열 모니터링**

  ```promql
  dotnet_threadpool_queue_length
  ```

  
### ✅ 정리
성능 최적화(메모리/GC 튜닝)에서는 아래 메트릭을 중점적으로 봐야 한다:

* GC: **세대별 컬렉션 빈도, 힙 크기, GC 시간 비율**
* JIT: **JIT 소요 시간**, 장기적으로 **동적 코드 증가 여부**
* Exception: **빈번한 예외 발생** → 성능 손실 원인
* ThreadPool: **대기열 길이, 스레드 수 변화**

👉 운영에서는 **Grafana 대시보드**를 만들어 “GC 동작 패턴 + 메모리 사용 + ThreadPool 상태”를 한눈에 볼 수 있게 하는 게 베스트 프랙티스이다.

  

## 새로운 서버가 증가할 때
Prometheus를 직접 써보면 제일 불편한 게 **“새 서버가 늘어날 때마다 prometheus.yml을 수정 → 서버 재시작”** 부분이다.
이를 해결하기 위해 Prometheus는 **Service Discovery(서비스 디스커버리)** 기능을 지원한다.  


### 🔹 1. Service Discovery (정석 방법)
Prometheus는 여러 환경에 맞는 디스커버리를 내장하고 있다:

* **Kubernetes**: `kubernetes_sd_configs`
  → 새 Pod/Service가 생기면 자동으로 타깃 추가
* **Consul**: `consul_sd_configs`
  → Consul에 등록된 서비스 목록 자동 감지
* **EC2, GCP, Azure, OpenStack**: 클라우드 VM 자동 등록
* **Docker Swarm / ECS**: 컨테이너 기반 디스커버리

👉 운영 환경이 위 중 하나라면 “새 서버 추가 → Prometheus 자동 인지”가 가능하다.

### 🔹 2. File-based Service Discovery (가장 쉬운 방법)
직접 yaml을 수정해서 Prometheus를 재시작하는 대신, **외부 파일 하나만 갱신**하면 Prometheus가 자동 반영하게 만들 수 있다.

#### `prometheus.yml`

```yaml
scrape_configs:
  - job_name: 'my-servers'
    file_sd_configs:
      - files:
        - targets.json
```

#### `targets.json`

```json
[
  {
    "targets": ["server1:9182", "server2:9182"],
    "labels": {
      "env": "prod"
    }
  }
]
```

➡️ Prometheus는 `targets.json` 파일을 주기적으로 다시 읽는다.
즉, 새 서버가 늘어나면 JSON 파일만 수정하면 되고 **Prometheus 재시작이 필요 없다**.

### 🔹 3. Pushgateway (임시/배치 잡에 적합)
만약 서버 수가 들쭉날쭉하거나, 짧게 돌았다가 사라지는 잡(Job)이라면 →
Prometheus가 일일이 discovery 하기 어려우므로 **Pushgateway**를 써서 서버 쪽에서 직접 메트릭을 push하도록 만들 수 있다.

하지만 Pushgateway는 “항상 떠 있는 서버” 모니터링에는 권장되지 않고, **배치 작업이나 임시 잡**에 적합하다.
  
### 🔹 4. Service Discovery + Service Registry 조합
규모가 커지면 보통:

* **Consul / Etcd / ZooKeeper** 같은 서비스 레지스트리
* 또는 **Kubernetes / ECS / Docker Swarm** 같은 오케스트레이터

→ Prometheus가 여기랑 연동해서 자동으로 타깃 추가/삭제
  
### ✅ 요약
* 지금처럼 **직접 yaml 수정 + 재시작** → 불편하고 확장성 없음
* **가장 쉬운 개선책** → `file_sd_configs` + JSON 파일 관리 (재시작 필요 없음)
* **운영 환경에 따라 최적**

  * Kubernetes → `kubernetes_sd_configs`
  * Consul → `consul_sd_configs`
  * 클라우드 환경 → EC2/GCP/Azure 디스커버리
  
  
## File-based Service Discovery 
온프레미스 환경에서는 보통 **서버 IP나 호스트 이름이 고정**되어 있고, Kubernetes 같은 자동 디스커버리가 없으니 **File-based Service Discovery** 방식이 가장 적합하다.  
  
### 🔹 1. 기본 prometheus.yml

```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'onprem-servers'
    file_sd_configs:
      - files:
          - targets.json   # 별도의 파일에서 서버 목록을 불러옴
```

여기서 `targets.json`만 관리하면 Prometheus를 재시작하지 않고도 서버를 추가/삭제할 수 있다.

### 🔹 2. targets.json 예시

```json
[
  {
    "targets": ["192.168.10.11:9182", "192.168.10.12:9182"],
    "labels": {
      "env": "prod",
      "role": "app"
    }
  },
  {
    "targets": ["192.168.20.21:9182"],
    "labels": {
      "env": "staging",
      "role": "db"
    }
  }
]
```

* `"targets"`: Prometheus가 스크랩할 서버 목록 (IP:포트)
* `"labels"`: 라벨 추가 (Grafana 대시보드나 쿼리에서 환경/역할 구분 가능)

### 🔹 3. 동작 방식
* Prometheus는 `targets.json`을 **몇 초마다 자동 재로드**합니다.
* 새 서버를 추가하려면 JSON에 IP만 넣고 저장하면 됩니다.
* Prometheus 자체를 재시작할 필요가 없습니다.

### 🔹 4. 확장 아이디어

* **자동 생성 스크립트**:
  새 서버가 추가될 때 Ansible, Chef, Puppet 같은 배포 툴이 `targets.json`을 업데이트하도록 자동화할 수 있습니다.
* **DNS 서비스 디스커버리**:
  온프레미스라도 `A 레코드`나 `SRV 레코드`를 잘 관리하면 `dns_sd_configs`를 써서 자동 발견도 가능합니다.

예:

```yaml
scrape_configs:
  - job_name: 'onprem-dns'
    dns_sd_configs:
      - names: ['exporters.mycompany.local']
        type: 'A'
        port: 9182
```

### ✅ 정리

* **온프레미스 서버**에서는 `file_sd_configs` + `targets.json` 방식이 가장 현실적
* 서버가 늘어나면 JSON만 수정 → Prometheus는 자동 반영
* 더 고급 환경이면 **DNS 기반 디스커버리**나 **배포 자동화 툴**과 연계

  




================================================
File: GameAPIServer/ErrorCode.cs
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
File: GameAPIServer/GameAPIServer.csproj
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
        <PackageReference Include="System.Configuration.ConfigurationManager" Version="10.0.0-rc.1.25451.107" />
        <PackageReference Include="ZLogger" Version="2.5.10" />
    </ItemGroup>

</Project>



================================================
File: GameAPIServer/GameAPIServer.sln
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
File: GameAPIServer/Program.cs
================================================
using System.IO;
using GameAPIServer.Repository;
using GameAPIServer.Servicies;
using GameAPIServer.Servicies.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;
using Prometheus;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;

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


//log setting
ILoggerFactory loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

app.UseRouting();

app.UseHttpMetrics();   // HTTP 요청 관련 기본 메트릭 수집

app.MapDefaultControllerRoute();

app.MapMetrics(); // /metrics 엔드포인트 등록 (Prometheus가 이 주소를 수집)

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
File: GameAPIServer/Security.cs
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
File: GameAPIServer/appsettings.Development.json
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
File: GameAPIServer/appsettings.json
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
File: GameAPIServer/httpTest.http
================================================
﻿@host = http://localhost:11500

### ============================================================
### 기존 API
### ============================================================

### 로그인
POST {{host}}/Login
Content-Type: application/json

{
  "ID":"jacking751",
  "PW":"123qwe"
}

### 계정 생성
POST {{host}}/CreateAccount
Content-Type: application/json

{
  "ID":"jacking751",
  "PW":"123qwe",
  "NickName": "aaa"
}


### ============================================================
### Prometheus 메트릭 예시 (MetricsExampleController)
### ============================================================

### [Counter] 로그인 성공 (api_login_total{status="success"} 증가)
POST {{host}}/MetricsExample/Login
Content-Type: application/json

{ "UserId": "user1" }

### [Counter] 로그인 실패 (api_login_total{status="fail"} 증가)
POST {{host}}/MetricsExample/Login
Content-Type: application/json

{ "UserId": "error" }

### [Gauge] 유저 접속 (api_active_users 증가)
POST {{host}}/MetricsExample/Connect

### [Gauge] 유저 접속 해제 (api_active_users 감소)
POST {{host}}/MetricsExample/Disconnect

### [Histogram + Summary] 100ms 작업 (api_request_duration_seconds에 분포 기록)
POST {{host}}/MetricsExample/SlowWork
Content-Type: application/json

{ "DelayMs": 100 }

### [Histogram + Summary] 500ms 작업 (느린 요청 시뮬레이션)
POST {{host}}/MetricsExample/SlowWork
Content-Type: application/json

{ "DelayMs": 500 }

### [Histogram + Summary] 2000ms 작업 (매우 느린 요청)
POST {{host}}/MetricsExample/SlowWork
Content-Type: application/json

{ "DelayMs": 2000 }

### ============================================================
### 메트릭 확인: 브라우저에서 아래 주소 접속
### http://localhost:11500/metrics
### → api_login_total, api_active_users,
###   api_request_duration_seconds_bucket 등 확인 가능
### ============================================================



================================================
File: GameAPIServer/Controllers/CreateAccountController.cs
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
File: GameAPIServer/Controllers/FriendAcceptController.cs
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
File: GameAPIServer/Controllers/FriendCancelReqController.cs
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
File: GameAPIServer/Controllers/FriendDeleteController.cs
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
File: GameAPIServer/Controllers/FriendListController.cs
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
File: GameAPIServer/Controllers/FriendSendReqController.cs
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
File: GameAPIServer/Controllers/GameDataLoadController.cs
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
File: GameAPIServer/Controllers/LoginController.cs
================================================
﻿using System.Threading.Tasks;
using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models.DTO;
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
        LoginResponse response = new();

        //TODO: 컨트룰러에 구현 코드가 너무 많이 노출 되어 있다. 아래 코드에서는 서비스 객체의 메소드를 여러개 호출하고 있는데 1개 정도의 메소드로 묶어서 호출하는 방법을 생각해보자.
        // 즉 구현은 대부분 서비스 객체에 있어야 하고, 컨트룰러는 필요한 서비스 객체를 호출하고, 응답만 보내는 역할을 해야 한다.

        
        //하이브 토큰 체크
        var (errorCode,uid, authToken) = await _authService.Login(request.UserID, request.Password);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        response.Result = errorCode;
        response.AuthToken = authToken;

        
        //TODO: 첫 로그인 유저인 경우 아직 게임데이터가 생성 되어 있지 않으니 여기서 생성하도록 한다.
        _logger.ZLogInformation($"[Login] Uid : {uid}, Token : {authToken}");
        return response;
    }
}



================================================
File: GameAPIServer/Controllers/MailDeleteController.cs
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
File: GameAPIServer/Controllers/MailListController.cs
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
File: GameAPIServer/Controllers/MailReceiveController.cs
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
File: GameAPIServer/Controllers/MetricsExampleController.cs
================================================
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace GameAPIServer.Controllers;

/// <summary>
/// Prometheus 4가지 메트릭 타입(Counter, Gauge, Histogram, Summary)의 실제 사용 예시.
///
/// 서버 실행 후 http://localhost:{port}/metrics 에서 아래 메트릭들이 노출되는 것을 확인할 수 있다.
/// Prometheus가 이 엔드포인트를 주기적으로 수집(Pull)하여 시계열 DB에 저장한다.
/// </summary>
[ApiController]
[Route("[controller]")]
public class MetricsExampleController : ControllerBase
{
    // ─────────────────────────────────────────────────────────
    // [1] Counter: 단조 증가만 하는 값. 절대 감소하지 않는다.
    //     서버 재시작 시 0으로 리셋. rate()로 초당 증가율 계산 가능.
    //     용도: 총 요청 수, 총 에러 수, 총 로그인 수
    //     PromQL 예: rate(api_login_total[5m]) → 5분간 초당 로그인 수
    // ─────────────────────────────────────────────────────────
    static readonly Counter LoginCounter = Metrics.CreateCounter(
        "api_login_total",                // 메트릭 이름 (Prometheus에서 이 이름으로 쿼리)
        "Total number of login attempts", // 설명 (help 텍스트)
        new CounterConfiguration
        {
            LabelNames = new[] { "status" } // 라벨: 성공/실패 구분 가능
        });

    // ─────────────────────────────────────────────────────────
    // [2] Gauge: 증가/감소 모두 가능한 현재값.
    //     용도: 현재 동접 수, 메모리 사용량, 큐 길이, 활성 게임 수
    //     PromQL 예: api_active_users → 현재 동접 수
    // ─────────────────────────────────────────────────────────
    static readonly Gauge ActiveUsers = Metrics.CreateGauge(
        "api_active_users",
        "Number of currently active users");

    // ─────────────────────────────────────────────────────────
    // [3] Histogram: 값의 분포를 버킷(bucket)으로 측정.
    //     자동으로 _bucket, _sum, _count 3가지 시계열이 생성됨.
    //     용도: API 응답 시간 분포, 패킷 크기 분포
    //     PromQL 예: histogram_quantile(0.9, rate(api_request_duration_seconds_bucket[5m]))
    //               → P90 응답 시간 (90%의 요청이 이 시간 이내에 처리됨)
    // ─────────────────────────────────────────────────────────
    static readonly Histogram RequestDuration = Metrics.CreateHistogram(
        "api_request_duration_seconds",
        "API request processing time in seconds",
        new HistogramConfiguration
        {
            // 버킷 경계: 10ms, 50ms, 100ms, 250ms, 500ms, 1s, 2.5s, 5s, 10s
            Buckets = new[] { 0.01, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0 }
        });

    // ─────────────────────────────────────────────────────────
    // [4] Summary: Histogram과 비슷하지만 클라이언트(서버 앱)에서 백분위를 계산.
    //     용도: 응답 시간의 P50, P90, P99를 서버 측에서 직접 계산하여 노출
    //     주의: Summary는 Prometheus 서버에서 집계 불가 (여러 인스턴스 합산 불가)
    //           → 대부분의 경우 Histogram을 권장
    // ─────────────────────────────────────────────────────────
    static readonly Summary RequestSummary = Metrics.CreateSummary(
        "api_request_summary_seconds",
        "API request processing time summary",
        new SummaryConfiguration
        {
            Objectives = new[]
            {
                new QuantileEpsilonPair(0.5, 0.05),   // P50 (중앙값)
                new QuantileEpsilonPair(0.9, 0.01),   // P90
                new QuantileEpsilonPair(0.99, 0.001), // P99
            }
        });

    /// <summary>
    /// POST /MetricsExample/Login
    /// Counter 예시: 로그인 시도마다 카운터 증가. 라벨로 성공/실패 구분.
    /// </summary>
    [HttpPost("Login")]
    public IActionResult Login([FromBody] MetricsLoginRequest req)
    {
        if (req.UserId == "error")
        {
            LoginCounter.WithLabels("fail").Inc();
            return BadRequest(new { Result = "LoginFail" });
        }

        LoginCounter.WithLabels("success").Inc();
        return Ok(new { Result = "Success", UserId = req.UserId });
    }

    /// <summary>
    /// POST /MetricsExample/Connect
    /// Gauge 예시: 유저 접속 시 동접 수 증가.
    /// </summary>
    [HttpPost("Connect")]
    public IActionResult Connect()
    {
        ActiveUsers.Inc(); // 동접 +1
        return Ok(new { Result = "Connected", ActiveUsers = ActiveUsers.Value });
    }

    /// <summary>
    /// POST /MetricsExample/Disconnect
    /// Gauge 예시: 유저 접속 해제 시 동접 수 감소.
    /// </summary>
    [HttpPost("Disconnect")]
    public IActionResult Disconnect()
    {
        ActiveUsers.Dec(); // 동접 -1
        return Ok(new { Result = "Disconnected", ActiveUsers = ActiveUsers.Value });
    }

    /// <summary>
    /// POST /MetricsExample/SlowWork
    /// Histogram + Summary 예시: 작업 소요 시간을 측정하여 분포를 기록.
    /// delayMs 파라미터로 지연 시간을 조절하여 다양한 응답 시간 분포를 만들 수 있다.
    /// </summary>
    [HttpPost("SlowWork")]
    public async Task<IActionResult> SlowWork([FromBody] SlowWorkRequest req)
    {
        // Histogram: 타이머로 소요 시간 자동 측정
        using (RequestDuration.NewTimer())
        // Summary: 동시에 Summary에도 기록
        using (RequestSummary.NewTimer())
        {
            // 지연 시뮬레이션 (실제로는 DB 조회, 외부 API 호출 등)
            await Task.Delay(req.DelayMs);
        }

        return Ok(new { Result = "Done", DelayMs = req.DelayMs });
    }
}

// ── DTO ─────────────────────────────────────────────
public class MetricsLoginRequest
{
    public string UserId { get; set; } = "";
}

public class SlowWorkRequest
{
    public int DelayMs { get; set; } = 100;
}



================================================
File: GameAPIServer/Controllers/UserDataLoadController.cs
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
File: GameAPIServer/Models/MasterDB.cs
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
File: GameAPIServer/Models/RedisDB.cs
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
File: GameAPIServer/Models/DAO/Account.cs
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
File: GameAPIServer/Models/DAO/Attendance.cs
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
File: GameAPIServer/Models/DAO/Friend.cs
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
File: GameAPIServer/Models/DAO/Game.cs
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
File: GameAPIServer/Models/DAO/Item.cs
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
File: GameAPIServer/Models/DAO/Mailbox.cs
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
File: GameAPIServer/Models/DAO/User.cs
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
File: GameAPIServer/Models/DTO/AttendanceCheck.cs
================================================
癤퓎sing System.Collections.Generic;

namespace GameAPIServer.Models.DTO;

public class AttendanceCheckResponse : ErrorCode
{
    public List<ReceivedReward> Rewards { get; set; }
}



================================================
File: GameAPIServer/Models/DTO/AttendanceInfo.cs
================================================
癤퓎sing GameAPIServer.Models.DAO;


namespace GameAPIServer.Models.DTO;

public class AttendanceInfoResponse : ErrorCode
{
    public GdbAttendanceInfo AttendanceInfo { get; set; }
}


================================================
File: GameAPIServer/Models/DTO/CreateAccount.cs
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
File: GameAPIServer/Models/DTO/ErrorCode.cs
================================================
癤퓆amespace GameAPIServer.Models.DTO;

public class ErrorCode
{
    public global::ErrorCode Result { get; set; } = global::ErrorCode.None;
}



================================================
File: GameAPIServer/Models/DTO/FreindDelete.cs
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
File: GameAPIServer/Models/DTO/FriendAccept.cs
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
File: GameAPIServer/Models/DTO/FriendAdd.cs
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
File: GameAPIServer/Models/DTO/FriendList.cs
================================================
癤퓎sing System.Collections.Generic;


namespace GameAPIServer.Models.DTO;

public class FriendListResponse : ErrorCode
{
    public IEnumerable<DAO.GdbFriendInfo> FriendList { get; set; }
}



================================================
File: GameAPIServer/Models/DTO/GameDataLoad.cs
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
File: GameAPIServer/Models/DTO/Header.cs
================================================
癤퓎sing Microsoft.AspNetCore.Mvc;

namespace GameAPIServer.Models.DTO;

public class Header
{
    [FromHeader]
    public int Uid { get; set; }
}



================================================
File: GameAPIServer/Models/DTO/Login.cs
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
File: GameAPIServer/Models/DTO/Logout.cs
================================================
癤퓆amespace GameAPIServer.Models.DTO;

public class LogoutResponse : ErrorCode
{
}



================================================
File: GameAPIServer/Models/DTO/MailDelete.cs
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
File: GameAPIServer/Models/DTO/MailList.cs
================================================
癤퓎sing System.Collections.Generic;

namespace GameAPIServer.Models.DTO;

public class MailboxInfoResponse : ErrorCode
{
    public List<UserMailInfo> MailList { get; set; }
}



================================================
File: GameAPIServer/Models/DTO/MailReceive.cs
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
File: GameAPIServer/Models/DTO/OtherUserInfo.cs
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
File: GameAPIServer/Models/DTO/Ranking.cs
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
File: GameAPIServer/Models/DTO/SocialDataLoad.cs
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
File: GameAPIServer/Models/DTO/UserDataLoad.cs
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
File: GameAPIServer/Models/DTO/UserRank.cs
================================================
癤퓆amespace GameAPIServer.Models.DTO;

public class UserRankResponse : ErrorCode
{
    public long Rank { get; set; }
}



================================================
File: GameAPIServer/Models/DTO/UserSetMainChar.cs
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
File: GameAPIServer/Properties/launchSettings.json
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
File: GameAPIServer/Repository/FakeGameDb.cs
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
File: GameAPIServer/Repository/IGameDb.cs
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
File: GameAPIServer/Services/AuthService.cs
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
File: GameAPIServer/Services/DataLoadService.cs
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
File: GameAPIServer/Services/FriendService.cs
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
File: GameAPIServer/Services/GameService.cs
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
File: GameAPIServer/Services/MailService.cs
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
File: GameAPIServer/Services/UserService.cs
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
File: GameAPIServer/Services/Interfaces/IAuthService.cs
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
File: GameAPIServer/Services/Interfaces/IDataLoadService.cs
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
File: GameAPIServer/Services/Interfaces/IFriendService.cs
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
File: GameAPIServer/Services/Interfaces/IGameService.cs
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
File: GameAPIServer/Services/Interfaces/IMailService.cs
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
File: GameAPIServer/Services/Interfaces/IUserService.cs
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



================================================
File: TCPSocketServer/Program.cs
================================================
﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using Prometheus;



Counter TcpRequestsTotal = Metrics.CreateCounter("tcp_requests_total", "Total number of TCP requests handled.");


// 1) Prometheus 전용 HTTP 서버 시작 (/metrics 노출)
// 포트 1234에서 metrics 엔드포인트 제공
var metricServer = new KestrelMetricServer(port: 1234);
metricServer.Start();

// 2) TCP 서버 시작
var listener = new TcpListener(IPAddress.Any, 9000);
listener.Start();
Console.WriteLine("TCP Server listening on port 9000. Prometheus on :1234/metrics");

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = HandleClient(client);
}


async Task HandleClient(TcpClient client)
{
    using var stream = client.GetStream();
    var buffer = new byte[1024];
    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

    string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
    Console.WriteLine($"Received: {received}");

    // 3) 요청 카운터 증가
    TcpRequestsTotal.Inc();

    // 에코 응답
    byte[] response = Encoding.UTF8.GetBytes($"Echo: {received}");
    await stream.WriteAsync(response, 0, response.Length);
}


================================================
File: TCPSocketServer/TCPSocketServer.csproj
================================================
﻿<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="prometheus-net" Version="8.2.1" />
    <PackageReference Include="prometheus-net.AspNetCore" Version="8.2.1" />
  </ItemGroup>

</Project>



================================================
File: TCPSocketServer/TCPSocketServer.sln
================================================
﻿
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.14.36511.14 d17.14
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TCPSocketServer", "TCPSocketServer.csproj", "{C0F016BC-0CE9-CF75-53DA-DF95F0DD2DB8}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{C0F016BC-0CE9-CF75-53DA-DF95F0DD2DB8}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{C0F016BC-0CE9-CF75-53DA-DF95F0DD2DB8}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{C0F016BC-0CE9-CF75-53DA-DF95F0DD2DB8}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{C0F016BC-0CE9-CF75-53DA-DF95F0DD2DB8}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {05B5E361-FDD0-4A67-B3AE-C1593D20BAFD}
	EndGlobalSection
EndGlobal


