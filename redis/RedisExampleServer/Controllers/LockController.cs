using System.ComponentModel.DataAnnotations;
using CloudStructures.Structures;
using Microsoft.AspNetCore.Mvc;
using RedisExampleServer.Models;
using RedisExampleServer.Services;
using StackExchange.Redis;

namespace RedisExampleServer.Controllers;

/// <summary>
/// Redis 분산 락 예제: 동시 요청 방지.
/// RedisString + When.NotExists + TTL로 분산 락을 구현한다.
/// 같은 유저의 동시 요청을 직렬화하여 데이터 정합성을 보장한다.
///
/// Redis 명령어 매핑:
///   SetAsync("locked", expiry, When.NotExists) → SET key "locked" EX 15 NX
///     NX: 키가 없을 때만 SET (이미 있으면 false 반환 = 락 획득 실패)
///     EX 15: 15초 후 자동 삭제 (서버 크래시 시에도 락이 영구히 남지 않음)
///   DeleteAsync() → DEL key  (락 해제)
/// </summary>
[ApiController]
[Route("[controller]")]
public class LockController : ControllerBase
{
    readonly RedisService _redis;
    static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(15);

    public LockController(RedisService redis)
    {
        _redis = redis;
    }

    // ── 락을 걸고 작업 수행 ──────────────────────────────
    // SetAsync(When.NotExists)로 락 획득 시도 → 작업 수행 → 락 해제
    // TTL을 설정하여 서버 크래시 시에도 자동 해제되도록 한다
    [HttpPost("GetItem")]
    public async Task<LockResponse> GetItem(LockRequest req)
    {
        var lockKey = RedisKeyBuilder.UserLock(req.UserId);
        var lockRedis = _redis.GetString<string>(lockKey, LockExpiry);

        // 락 획득 시도 (NX: Not Exists일 때만 SET)
        var acquired = await lockRedis.SetAsync("locked", LockExpiry, When.NotExists);
        if (!acquired)
            return new LockResponse { Result = ErrorCode.AlreadyLocked, Message = "다른 요청이 처리 중입니다" };

        try
        {
            // 시간이 걸리는 작업 시뮬레이션 (DB 조회, 아이템 지급 등)
            await Task.Delay(req.WorkDurationMs);

            return new LockResponse
            {
                Result = ErrorCode.None,
                Message = $"작업 완료 ({req.WorkDurationMs}ms 소요)"
            };
        }
        finally
        {
            // 락 해제
            await lockRedis.DeleteAsync();
        }
    }
}

// ── DTO ─────────────────────────────────────────────
public class LockRequest
{
    [Required] public string UserId { get; set; } = "";
    public int WorkDurationMs { get; set; } = 3000; // 기본 3초
}

public class LockResponse : BaseResponse
{
    public string Message { get; set; } = "";
}
