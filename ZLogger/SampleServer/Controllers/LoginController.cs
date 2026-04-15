using Microsoft.AspNetCore.Mvc;
using SampleServer.DTO;
using SampleServer.Models;
using SampleServer.Services;

namespace SampleServer.Controllers;

/// <summary>
/// BaseController를 상속하여 ActionLog, InformationLog, ErrorLog 패턴을 보여주는 예시.
/// README의 "ActionLog on Controllers" 시나리오를 실제 코드로 구현.
/// </summary>
[Route("[controller]")]
[ApiController]
public class LoginController : BaseController<LoginController>
{
    readonly IGameService _service;

    public LoginController(ILogger<LoginController> logger, IGameService service)
        : base(logger)
    {
        _service = service;
    }

    /// <summary>
    /// POST /login
    /// 로그인 성공 시: ActionLog + InformationLog
    /// 로그인 실패 시: ErrorLog
    /// </summary>
    [HttpPost]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        var response = new LoginResponse();

        var (errorCode, (uid, token)) = _service.LoginUser(request.PlayerId, request.Token);
        response.Result = (UInt16)errorCode;

        if (errorCode == ErrorCode.None)
        {
            response.Uid = uid;
            response.AccessToken = token;

            // 유저 액션 로그: 메서드명 "Login"이 tag로 자동 기록됨
            ActionLog(new { uid });

            // 정보 로그: 처리 결과 전체를 context에 첨부
            InformationLog("User Logged in", response);
        }
        else
        {
            // 에러 로그: 실패한 요청 정보와 오류코드 기록
            ErrorLog((UInt16)errorCode, request);
        }

        return Ok(response);
    }
}
