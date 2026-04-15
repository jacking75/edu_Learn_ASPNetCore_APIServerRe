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
