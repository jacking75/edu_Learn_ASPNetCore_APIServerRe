using System.ComponentModel.DataAnnotations;
using CloudStructures.Structures;
using Microsoft.AspNetCore.Mvc;
using RedisExampleServer.Models;
using RedisExampleServer.Services;

namespace RedisExampleServer.Controllers;

/// <summary>
/// RedisList&lt;T&gt; 예제: 로비 채팅 (최근 50개 메시지 유지).
/// RedisList는 순서가 보장되므로 채팅 이력, 최근 활동 로그 등에 적합하다.
///
/// Redis 명령어 매핑:
///   LeftPushAsync() → LPUSH key value   (왼쪽에 추가 = 최신이 맨 앞)
///   RangeAsync()    → LRANGE key 0 N    (인덱스 범위로 조회)
///   TrimAsync()     → LTRIM key 0 49    (범위 밖 요소 삭제 = 크기 제한)
///
/// 참고: docs/04_레디스_데이터_구조_.md — RedisList&lt;T&gt;
/// </summary>
[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    readonly RedisService _redis;
    const int MaxMessages = 50;

    public ChatController(RedisService redis)
    {
        _redis = redis;
    }

    // ── 메시지 전송 ─────────────────────────────────────
    // LeftPush로 최신 메시지를 앞에 추가, Trim으로 50개 제한 유지
    [HttpPost("Send")]
    public async Task<BaseResponse> Send(ChatSendRequest req)
    {
        var key = RedisKeyBuilder.ChatLobby(req.LobbyId);
        var list = _redis.GetList<ChatMessage>(key);

        var message = new ChatMessage
        {
            Sender = req.UserId,
            Content = req.Message,
            Timestamp = DateTime.UtcNow
        };

        await list.LeftPushAsync(message);
        await list.TrimAsync(0, MaxMessages - 1); // 최대 50개만 유지

        return new BaseResponse { Result = ErrorCode.None };
    }

    // ── 채팅 이력 조회 ──────────────────────────────────
    // Range로 최근 N개 메시지를 가져온다
    [HttpPost("History")]
    public async Task<ChatHistoryResponse> History(ChatHistoryRequest req)
    {
        var key = RedisKeyBuilder.ChatLobby(req.LobbyId);
        var list = _redis.GetList<ChatMessage>(key);

        var count = Math.Min(req.Count, MaxMessages);
        var messages = await list.RangeAsync(0, count - 1);

        return new ChatHistoryResponse
        {
            Result = ErrorCode.None,
            Messages = messages
        };
    }
}

// ── DTO & Model ─────────────────────────────────────
public class ChatMessage
{
    public string Sender { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class ChatSendRequest
{
    [Required] public string UserId { get; set; } = "";
    public int LobbyId { get; set; } = 1;
    [Required][MaxLength(200)] public string Message { get; set; } = "";
}

public class ChatHistoryRequest
{
    public int LobbyId { get; set; } = 1;
    public int Count { get; set; } = 20;
}

public class ChatHistoryResponse : BaseResponse
{
    public ChatMessage[] Messages { get; set; } = [];
}
