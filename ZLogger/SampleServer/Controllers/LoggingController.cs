using Microsoft.AspNetCore.Mvc;
using SampleServer.DTO;
using ZLogger;

namespace SampleServer.Controllers;

/// <summary>
/// 기본 ZLogger 사용 예시.
/// DI로 주입받은 ILogger를 사용하여 다양한 LogLevel 출력을 시연한다.
/// </summary>
[Route("[controller]")]
[ApiController]
public class LoggingController : ControllerBase
{
    readonly ILogger<LoggingController> _logger;

    public LoggingController(ILogger<LoggingController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// POST /logging
    /// 메시지와 LogLevel을 받아 해당 레벨로 로그를 출력한다.
    /// LogLevel별 ZLog 메서드 사용법을 보여준다.
    /// </summary>
    [HttpPost]
    public ActionResult Log([FromBody] LogRequest request)
    {
        switch (request.Level)
        {
            case "trace":
                _logger.ZLogTrace($"{request.Message}");
                break;
            case "debug":
                _logger.ZLogDebug($"{request.Message}");
                break;
            case "information":
                _logger.ZLogInformation($"{request.Message}");
                break;
            case "warning":
                _logger.ZLogWarning($"{request.Message}");
                break;
            case "error":
                _logger.ZLogError($"{request.Message}");
                break;
            case "critical":
                _logger.ZLogCritical($"{request.Message}");
                break;
            default:
                _logger.ZLogInformation($"{request.Message}");
                break;
        }

        return Ok(new { request.Message, request.Level });
    }

    /// <summary>
    /// POST /logging/scope
    /// BeginScope 중첩 사용 예시.
    /// 외부 scope(RequestId)와 내부 scope(Step)를 중첩하여 출력한다.
    /// </summary>
    [HttpPost("scope")]
    public ActionResult ScopeDemo([FromBody] LogRequest request)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];

        using (_logger.BeginScope("RequestId={RequestId}", requestId))
        {
            _logger.ZLogInformation($"Scope started: {request.Message}");

            using (_logger.BeginScope("Step={Step}", "Processing"))
            {
                _logger.ZLogInformation($"Nested scope: processing {request.Message}");
            }

            _logger.ZLogInformation($"Scope ended: {request.Message}");
        }

        return Ok(new { requestId, request.Message });
    }

    /// <summary>
    /// POST /logging/exception
    /// 예외 로깅 예시.
    /// try-catch에서 ZLogError에 Exception을 전달하여 스택 트레이스를 기록한다.
    /// </summary>
    [HttpPost("exception")]
    public ActionResult ExceptionDemo([FromBody] LogRequest request)
    {
        try
        {
            // 예외를 의도적으로 발생시킴
            throw new InvalidOperationException($"Simulated error: {request.Message}");
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Exception caught while processing request");
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}
