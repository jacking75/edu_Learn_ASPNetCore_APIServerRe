namespace SampleServer.Models;

public enum ErrorCode : UInt16
{
    None = 0,

    // 로그인 관련 1000~
    LoginFail = 1000,
    LoginTokenInvalid = 1001,

    // 게임 관련 2000~
    GameNotFound = 2000,
    GameAlreadyStarted = 2001,

    // 내부 오류 9000~
    InternalError = 9000,
    RedisGetException = 9001,
    DbException = 9002,
}
