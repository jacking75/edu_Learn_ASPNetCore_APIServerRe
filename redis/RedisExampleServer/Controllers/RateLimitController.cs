using System.ComponentModel.DataAnnotations;
using CloudStructures.Structures;
using Microsoft.AspNetCore.Mvc;
using RedisExampleServer.Models;
using RedisExampleServer.Services;
using StackExchange.Redis;

namespace RedisExampleServer.Controllers;

/// <summary>
/// Redis 키 만료(TTL) 예제: Rate Limiting, 일일 이벤트, SMS 쿨다운.
/// 키의 자동 만료(Expiry)를 활용한 시간 기반 제한 패턴들.
///
/// 핵심 Redis 패턴:
///   [카운터 패턴] INCR key → 값 1 증가 (키 없으면 0에서 시작). EXPIRE key 120 → TTL 설정
///   [1회 제한]    SET key value EX ttl NX → 키가 없을 때만 설정 + 자동 만료
///   [쿨다운]      SET key value EX 60 NX → 60초 동안 키 존재 = 재요청 차단
/// </summary>
[ApiController]
[Route("[controller]")]
public class RateLimitController : ControllerBase
{
    readonly RedisService _redis;

    public RateLimitController(RedisService redis)
    {
        _redis = redis;
    }

    // ── Rate Limiting: 2분에 3번 제한 ────────────────────
    // 패턴: INCR로 카운터 증가 → 값이 1이면(첫 요청) EXPIRE로 TTL 설정
    // Redis 명령: INCR key → 키 없으면 0에서 시작하여 1 반환. EXPIRE key 120
    [HttpPost("ChangeNickname")]
    public async Task<RateLimitResponse> ChangeNickname(RateLimitRequest req)
    {
        var key = RedisKeyBuilder.RateLimit(req.UserId, "nickname");
        var counter = new RedisString<long>(_redis.Connection, key, null);

        // 카운터 증가 (키가 없으면 0에서 시작하여 1 반환)
        var count = await counter.IncrementAsync(1);

        // 첫 요청이면 TTL 설정 (120초 후 카운터 자동 리셋)
        if (count == 1)
        {
            await counter.ExpireAsync(TimeSpan.FromSeconds(120));
        }

        if (count > 3)
            return new RateLimitResponse
            {
                Result = ErrorCode.RateLimitExceeded,
                Message = "2분 내 닉네임 변경 횟수(3회)를 초과했습니다"
            };

        var remaining = 3 - count;
        return new RateLimitResponse
        {
            Result = ErrorCode.None,
            Message = $"닉네임 변경 성공 (남은 횟수: {remaining})"
        };
    }

    // ── 일일 이벤트: 하루 1회 참여 ───────────────────────
    // SetAsync(When.NotExists) + 자정까지 TTL: 오늘 이미 참여했으면 실패
    [HttpPost("DailyEvent")]
    public async Task<RateLimitResponse> DailyEvent(RateLimitRequest req)
    {
        var key = RedisKeyBuilder.DailyEvent(req.UserId);
        var midnight = DateTime.Today.AddDays(1) - DateTime.Now;
        var redis = _redis.GetString<string>(key, midnight);

        var set = await redis.SetAsync("participated", midnight, When.NotExists);
        if (!set)
            return new RateLimitResponse
            {
                Result = ErrorCode.AlreadyParticipated,
                Message = "오늘 이미 참여한 이벤트입니다"
            };

        return new RateLimitResponse
        {
            Result = ErrorCode.None,
            Message = "일일 이벤트 참여 완료! 보상이 지급되었습니다"
        };
    }

    // ── SMS 쿨다운: 60초 간격 제한 ──────────────────────
    // SetAsync(When.NotExists) + 60초 TTL: 60초 내 재요청 차단
    [HttpPost("RequestSmsCode")]
    public async Task<RateLimitResponse> RequestSmsCode(RateLimitRequest req)
    {
        var key = RedisKeyBuilder.SmsCooldown(req.UserId);
        var redis = _redis.GetString<string>(key, TimeSpan.FromSeconds(60));

        var set = await redis.SetAsync("cooldown", TimeSpan.FromSeconds(60), When.NotExists);
        if (!set)
            return new RateLimitResponse
            {
                Result = ErrorCode.CooldownActive,
                Message = "60초 후에 다시 요청할 수 있습니다"
            };

        var code = Random.Shared.Next(100000, 999999);
        return new RateLimitResponse
        {
            Result = ErrorCode.None,
            Message = $"인증 코드가 발송되었습니다: {code}"
        };
    }
}

// ── DTO ─────────────────────────────────────────────
public class RateLimitRequest
{
    [Required] public string UserId { get; set; } = "";
}

public class RateLimitResponse : BaseResponse
{
    public string Message { get; set; } = "";
}
