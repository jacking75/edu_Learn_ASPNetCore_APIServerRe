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
