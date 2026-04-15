namespace RedisExampleServer.Models;

public enum ErrorCode : ushort
{
    None = 0,

    // Auth 1000~
    AuthFail = 1001,
    DuplicateAccount = 1002,
    AccountNotFound = 1003,
    InvalidPassword = 1004,
    TokenNotFound = 1005,

    // Chat 2000~
    ChatSendFail = 2001,

    // Like 3000~
    LikeFail = 3001,

    // Ranking 4000~
    RankingFail = 4001,
    RankNotFound = 4002,

    // Lock 5000~
    LockFail = 5001,
    AlreadyLocked = 5002,

    // RateLimit 6000~
    RateLimitExceeded = 6001,
    AlreadyParticipated = 6002,
    CooldownActive = 6003,

    // Redis 9000~
    RedisError = 9001,
}
