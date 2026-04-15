using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RedisExampleServer.Models;
using RedisExampleServer.Services;

namespace RedisExampleServer.Controllers;

/// <summary>
/// RedisSet&lt;T&gt; 예제: 좋아요 시스템.
/// RedisSet은 중복 없는 집합이므로 "누가 좋아요 했는지" 관리에 적합하다.
///
/// Redis 명령어 매핑:
///   AddAsync()      → SADD key member      (멤버 추가, 이미 있으면 무시)
///   RemoveAsync()   → SREM key member      (멤버 삭제)
///   ContainsAsync() → SISMEMBER key member  (멤버 존재 여부, O(1))
///   MembersAsync()  → SMEMBERS key          (모든 멤버 조회)
///   LengthAsync()   → SCARD key             (집합 크기)
///
/// 참고: docs/04_레디스_데이터_구조_.md — RedisSet&lt;T&gt;
/// </summary>
[ApiController]
[Route("[controller]")]
public class LikeController : ControllerBase
{
    readonly RedisService _redis;

    public LikeController(RedisService redis)
    {
        _redis = redis;
    }

    // ── 좋아요 토글 ─────────────────────────────────────
    // Contains로 이미 좋아요 했는지 확인, Add 또는 Remove로 토글
    [HttpPost("Toggle")]
    public async Task<LikeToggleResponse> Toggle(LikeToggleRequest req)
    {
        var set = _redis.GetSet<string>(RedisKeyBuilder.Likes(req.TargetUserId));

        var isMember = await set.ContainsAsync(req.UserId);
        if (isMember)
        {
            await set.RemoveAsync(req.UserId);
            return new LikeToggleResponse { Result = ErrorCode.None, Liked = false };
        }

        await set.AddAsync(req.UserId);
        return new LikeToggleResponse { Result = ErrorCode.None, Liked = true };
    }

    // ── 좋아요 목록 ─────────────────────────────────────
    // Members로 전체 좋아요한 유저 목록 조회
    [HttpPost("List")]
    public async Task<LikeListResponse> List(LikeListRequest req)
    {
        var set = _redis.GetSet<string>(RedisKeyBuilder.Likes(req.TargetUserId));
        var members = await set.MembersAsync();

        return new LikeListResponse
        {
            Result = ErrorCode.None,
            Users = members,
            Count = members.Length
        };
    }

    // ── 좋아요 수 ───────────────────────────────────────
    // Length로 집합의 크기(좋아요 수) 조회
    [HttpPost("Count")]
    public async Task<LikeCountResponse> Count(LikeCountRequest req)
    {
        var set = _redis.GetSet<string>(RedisKeyBuilder.Likes(req.TargetUserId));
        var count = await set.LengthAsync();

        return new LikeCountResponse { Result = ErrorCode.None, Count = count };
    }
}

// ── DTO ─────────────────────────────────────────────
public class LikeToggleRequest
{
    [Required] public string UserId { get; set; } = "";
    [Required] public string TargetUserId { get; set; } = "";
}
public class LikeToggleResponse : BaseResponse { public bool Liked { get; set; } }

public class LikeListRequest { [Required] public string TargetUserId { get; set; } = ""; }
public class LikeListResponse : BaseResponse
{
    public string[] Users { get; set; } = [];
    public long Count { get; set; }
}

public class LikeCountRequest { [Required] public string TargetUserId { get; set; } = ""; }
public class LikeCountResponse : BaseResponse { public long Count { get; set; } }
