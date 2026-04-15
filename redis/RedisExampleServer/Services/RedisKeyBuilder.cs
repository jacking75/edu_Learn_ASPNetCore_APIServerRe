namespace RedisExampleServer.Services;

/// <summary>
/// Redis 키 네이밍 규칙을 관리하는 유틸리티.
/// 모든 키를 한 곳에서 관리하여 충돌과 오타를 방지한다.
/// </summary>
public static class RedisKeyBuilder
{
    // Auth
    public static string UserAccount(string email) => $"UA_{email}";
    public static string AuthToken(string userId) => $"Token_{userId}";
    public static string UserData(string userId) => $"UserData_{userId}";

    // Chat (RedisList)
    public static string ChatLobby(int lobbyId) => $"Chat_Lobby_{lobbyId}";

    // Like (RedisSet)
    public static string Likes(string targetUserId) => $"Likes_{targetUserId}";

    // Ranking (RedisSortedSet)
    public const string GlobalRanking = "Ranking_Global";

    // Lock
    public static string UserLock(string userId) => $"ULock_{userId}";

    // RateLimit
    public static string RateLimit(string userId, string action) => $"RateLimit_{userId}_{action}";
    public static string DailyEvent(string userId) => $"DailyEvent_{userId}_{DateTime.Now:yyyyMMdd}";
    public static string SmsCooldown(string userId) => $"SmsCooldown_{userId}";
}
