using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using ZLogger;

namespace SampleServer.Controllers;

/// <summary>
/// README "ActionLog on Controllers" 패턴 구현.
/// 컨트롤러에서 유저 기반 이벤트 발생 시 ActionLog를 호출하여
/// [CallerMemberName]으로 실행된 메서드 이름과 context를 기록한다.
/// </summary>
public abstract class BaseController<T> : ControllerBase where T : class
{
    protected readonly ILogger<T> _logger;

    protected BaseController(ILogger<T> logger)
    {
        _logger = logger;
    }

    // 유저 액션 로그: 컨트롤러 메서드명이 tag로 자동 기록
    protected void ActionLog(object context, [CallerMemberName] string? tag = null)
    {
        _logger.ZLogInformation($"[{tag:json}] {context:json}");
    }

    // 정보 로그: 정상 흐름이나 특정 동작 수행 알림
    protected void InformationLog(string message, object? context = default,
        [CallerMemberName] string? caller = null)
    {
        _logger.ZLogInformation($"[{caller}] {message} {context:json}");
    }

    // 에러 로그: 서비스 오류 처리 (ErrorCode 기반)
    protected void ErrorLog(UInt16 errorCode, object? context = default,
        [CallerMemberName] string? caller = null)
    {
        _logger.ZLogError($"[{caller}] {errorCode}", context);
    }
}
