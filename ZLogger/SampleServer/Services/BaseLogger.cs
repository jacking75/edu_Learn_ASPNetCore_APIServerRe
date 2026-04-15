using System.Runtime.CompilerServices;
using ZLogger;

namespace SampleServer.Services;

/// <summary>
/// README "Metric Log in Services" 패턴 구현.
/// 서비스 계층에서 시스템 이벤트 기반 로깅을 위한 추상 클래스.
/// MetricLog, InformationLog, ExceptionLog, ErrorLog를 제공한다.
/// </summary>
public abstract class BaseLogger<T> where T : class
{
    protected readonly ILogger<T> _logger;

    protected BaseLogger(ILogger<T> logger)
    {
        _logger = logger;
    }

    // 시스템 메트릭 로그: 유저 액션이 아닌 시스템 이벤트 기록
    protected void MetricLog(string tag, object context)
    {
        _logger.ZLogInformation($"[{tag:json}] {context:json}");
    }

    // 정보 로그: 정상 흐름 알림
    protected void InformationLog(string message, object? context = default,
        [CallerMemberName] string? caller = null)
    {
        _logger.ZLogInformation($"[{caller}] {message} {context:json}");
    }

    // 예외 로그: Exception 객체(스택 트레이스) 포함
    protected void ExceptionLog(Exception ex, object? context = default,
        [CallerMemberName] string? caller = null)
    {
        _logger.ZLogError(ex, $"[{caller}] {context:json}");
    }

    // 에러 로그: ErrorCode 기반 서비스 오류
    protected void ErrorLog(UInt16 errorCode, object? context = default,
        [CallerMemberName] string? caller = null)
    {
        _logger.ZLogError($"[{caller}] {errorCode} {context:json}");
    }
}
