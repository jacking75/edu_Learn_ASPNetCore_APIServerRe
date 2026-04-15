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
