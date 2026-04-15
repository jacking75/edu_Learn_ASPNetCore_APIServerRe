using SampleServer.Models;
using ZLogger;

namespace SampleServer.Services;

/// <summary>
/// BaseLogger를 상속하여 ExceptionLog, ErrorLog, MetricLog 등을 활용하는 예시.
/// 실제 DB/Redis 연동 없이 ZLogger 학습용 시나리오를 제공한다.
/// </summary>
public class GameService : BaseLogger<GameService>, IGameService
{
    public GameService(ILogger<GameService> logger) : base(logger)
    {
    }

    /// <summary>
    /// 로그인 처리 예시.
    /// - 성공: InformationLog 기록
    /// - 토큰 오류: ErrorLog 기록
    /// - 예외 발생: ExceptionLog 기록
    /// </summary>
    public (ErrorCode, (Int64 uid, string token)) LoginUser(Int64 playerId, string token)
    {
        try
        {
            // 토큰 검증 시뮬레이션
            if (string.IsNullOrEmpty(token) || token == "invalid")
            {
                ErrorLog((UInt16)ErrorCode.LoginTokenInvalid, new { playerId });
                return (ErrorCode.LoginTokenInvalid, (0, string.Empty));
            }

            // 로그인 성공 시뮬레이션
            var uid = playerId * 100;
            var accessToken = Guid.NewGuid().ToString("N");

            InformationLog("User verified successfully", new { uid, playerId });

            return (ErrorCode.None, (uid, accessToken));
        }
        catch (Exception ex)
        {
            ExceptionLog(ex, new { playerId });
            return (ErrorCode.InternalError, (0, string.Empty));
        }
    }

    /// <summary>
    /// 게임 시작 예시.
    /// - MetricLog로 시스템 이벤트(게임 생성) 기록
    /// - BeginScope로 gameGuid를 범위에 포함하여 관련 로그를 추적 가능하게 함
    /// </summary>
    public (ErrorCode, string gameGuid) StartGame(Int64 uid)
    {
        var gameGuid = Guid.NewGuid().ToString();

        // BeginScope 활용: 동일 scope 내 모든 로그에 gameGuid가 포함됨
        using (_logger.BeginScope(new { GameGuid = gameGuid }))
        {
            _logger.ZLogDebug($"Initializing game resources for uid:{uid}");

            MetricLog("GameCreated", new { gameGuid, uid });

            _logger.ZLogInformation($"Game started successfully for uid:{uid}");
        }

        return (ErrorCode.None, gameGuid);
    }
}
