using Microsoft.AspNetCore.Mvc;
using SampleServer.DTO;
using SampleServer.Models;
using SampleServer.Services;
using ZLogger;

namespace SampleServer.Controllers;

/// <summary>
/// BeginScope 활용 + 다양한 LogLevel 사용 예시.
/// 게임 시작 시 scope에 gameGuid를 포함하여 관련 로그를 추적 가능하게 한다.
/// </summary>
[Route("[controller]")]
[ApiController]
public class GameController : BaseController<GameController>
{
    readonly IGameService _service;

    public GameController(ILogger<GameController> logger, IGameService service)
        : base(logger)
    {
        _service = service;
    }

    /// <summary>
    /// POST /game/start
    /// BeginScope로 요청 단위의 컨텍스트를 설정하고,
    /// Debug/Information/Warning/Error 등 다양한 LogLevel을 시연한다.
    /// </summary>
    [HttpPost("start")]
    public ActionResult<StartGameResponse> StartGame([FromBody] StartGameRequest request)
    {
        var response = new StartGameResponse();

        // BeginScope: 요청 단위로 uid를 scope에 포함 → 이 범위 내 모든 로그에 uid가 붙음
        using (_logger.BeginScope(new { RequestUid = request.Uid }))
        {
            _logger.ZLogDebug($"Game start requested");

            if (request.Uid <= 0)
            {
                _logger.ZLogWarning($"Invalid uid received: {request.Uid}");
                response.Result = (UInt16)ErrorCode.GameNotFound;
                ErrorLog((UInt16)ErrorCode.GameNotFound, new { request.Uid });
                return BadRequest(response);
            }

            var (errorCode, gameGuid) = _service.StartGame(request.Uid);
            response.Result = (UInt16)errorCode;
            response.GameGuid = gameGuid;

            ActionLog(new { request.Uid, gameGuid });
        }

        return Ok(response);
    }
}
