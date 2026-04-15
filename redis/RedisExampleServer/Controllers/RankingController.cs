using System.ComponentModel.DataAnnotations;
using CloudStructures.Structures;
using Microsoft.AspNetCore.Mvc;
using RedisExampleServer.Models;
using RedisExampleServer.Services;
using StackExchange.Redis;

namespace RedisExampleServer.Controllers;

/// <summary>
/// RedisSortedSet&lt;T&gt; 예제: 랭킹 시스템 (Top 10, 내 순위, 주변 순위).
/// SortedSet은 점수(Score)로 자동 정렬되므로 리더보드에 최적이다.
///
/// Redis 명령어 매핑:
///   AddAsync(member, score)               → ZADD key score member  (추가/갱신, O(logN))
///   RangeByRankWithScoresAsync(0, 9, Desc)→ ZREVRANGE key 0 9 WITHSCORES (Top 10)
///   RankAsync(member, Desc)               → ZREVRANK key member    (내림차순 순위, 0-based)
///   ScoreAsync(member)                    → ZSCORE key member      (점수 조회)
///
/// 참고: docs/04_레디스_데이터_구조_.md — RedisSortedSet&lt;T&gt;
/// </summary>
[ApiController]
[Route("[controller]")]
public class RankingController : ControllerBase
{
    readonly RedisService _redis;

    public RankingController(RedisService redis)
    {
        _redis = redis;
    }

    // ── 점수 등록/갱신 ──────────────────────────────────
    // Add: 유저 점수를 등록. 이미 있으면 갱신된다.
    [HttpPost("SetScore")]
    public async Task<BaseResponse> SetScore(SetScoreRequest req)
    {
        var sortedSet = _redis.GetSortedSet<string>(RedisKeyBuilder.GlobalRanking);
        await sortedSet.AddAsync(req.UserId, req.Score);

        return new BaseResponse { Result = ErrorCode.None };
    }

    // ── Top 10 조회 ─────────────────────────────────────
    // RangeByRankWithScoresAsync: 순위 범위로 멤버+점수 조회 (내림차순)
    [HttpPost("Top10")]
    public async Task<RankingListResponse> Top10()
    {
        var sortedSet = _redis.GetSortedSet<string>(RedisKeyBuilder.GlobalRanking);
        var entries = await sortedSet.RangeByRankWithScoresAsync(0, 9, Order.Descending);

        var list = entries.Select((e, i) => new RankEntry
        {
            Rank = i + 1,
            UserId = e.Value,
            Score = (long)e.Score
        }).ToArray();

        return new RankingListResponse { Result = ErrorCode.None, Rankings = list };
    }

    // ── 내 순위 조회 ────────────────────────────────────
    // Rank: 특정 멤버의 순위를 반환 (0-based, 내림차순)
    [HttpPost("MyRank")]
    public async Task<MyRankResponse> MyRank(MyRankRequest req)
    {
        var sortedSet = _redis.GetSortedSet<string>(RedisKeyBuilder.GlobalRanking);
        var rank = await sortedSet.RankAsync(req.UserId, Order.Descending);

        if (!rank.HasValue)
            return new MyRankResponse { Result = ErrorCode.RankNotFound };

        var score = await sortedSet.ScoreAsync(req.UserId);

        return new MyRankResponse
        {
            Result = ErrorCode.None,
            Rank = rank.Value + 1, // 1-based
            Score = score.HasValue ? (long)score.Value : 0
        };
    }

    // ── 주변 순위 조회 (내 순위 ±2) ──────────────────────
    // Rank로 내 위치를 찾고, RangeByRank로 주변을 가져온다
    [HttpPost("Neighbors")]
    public async Task<RankingListResponse> Neighbors(MyRankRequest req)
    {
        var sortedSet = _redis.GetSortedSet<string>(RedisKeyBuilder.GlobalRanking);
        var myRank = await sortedSet.RankAsync(req.UserId, Order.Descending);

        if (!myRank.HasValue)
            return new RankingListResponse { Result = ErrorCode.RankNotFound };

        var start = Math.Max(0, myRank.Value - 2);
        var stop = myRank.Value + 2;

        var entries = await sortedSet.RangeByRankWithScoresAsync(start, stop, Order.Descending);

        var list = entries.Select((e, i) => new RankEntry
        {
            Rank = (int)start + i + 1,
            UserId = e.Value,
            Score = (long)e.Score
        }).ToArray();

        return new RankingListResponse { Result = ErrorCode.None, Rankings = list };
    }
}

// ── DTO & Model ─────────────────────────────────────
public class SetScoreRequest
{
    [Required] public string UserId { get; set; } = "";
    public long Score { get; set; }
}

public class MyRankRequest { [Required] public string UserId { get; set; } = ""; }
public class MyRankResponse : BaseResponse
{
    public long Rank { get; set; }
    public long Score { get; set; }
}

public class RankEntry
{
    public int Rank { get; set; }
    public string UserId { get; set; } = "";
    public long Score { get; set; }
}

public class RankingListResponse : BaseResponse
{
    public RankEntry[] Rankings { get; set; } = [];
}
