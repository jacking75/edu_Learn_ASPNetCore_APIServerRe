using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using RedisExampleServer.Models;
using RedisExampleServer.Services;

namespace RedisExampleServer.Controllers;

/// <summary>
/// RedisString&lt;T&gt; 예제: 계정 생성, 로그인, 유저 데이터 로드.
///
/// Redis 명령어 매핑:
///   GetAsync()  → GET key          (값 조회)
///   SetAsync()  → SET key value    (값 저장)
///   SetAsync(expiry) → SET key value EX seconds  (만료 시간 포함 저장)
///
/// 참고: docs/04_레디스_데이터_구조_.md — RedisString&lt;T&gt;
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    readonly RedisService _redis;

    public AuthController(RedisService redis)
    {
        _redis = redis;
    }

    // ── 계정 생성 ───────────────────────────────────────
    // RedisString SET: 이메일을 키로, 해싱된 비밀번호를 값으로 저장
    [HttpPost("CreateAccount")]
    public async Task<CreateAccountResponse> CreateAccount(CreateAccountRequest req)
    {
        var key = RedisKeyBuilder.UserAccount(req.Email);
        var redis = _redis.GetString<string>(key);

        // 중복 확인
        var exists = await redis.GetAsync();
        if (exists.HasValue)
            return new CreateAccountResponse { Result = ErrorCode.DuplicateAccount };

        var hashed = HashPassword(req.Password);
        await redis.SetAsync(hashed);

        return new CreateAccountResponse { Result = ErrorCode.None };
    }

    // ── 로그인 ──────────────────────────────────────────
    // RedisString GET으로 비밀번호 검증, SET으로 인증 토큰 저장 (30분 만료)
    [HttpPost("Login")]
    public async Task<LoginResponse> Login(LoginRequest req)
    {
        var accountRedis = _redis.GetString<string>(RedisKeyBuilder.UserAccount(req.Email));
        var stored = await accountRedis.GetAsync();

        if (!stored.HasValue)
            return new LoginResponse { Result = ErrorCode.AccountNotFound };

        if (stored.Value != HashPassword(req.Password))
            return new LoginResponse { Result = ErrorCode.InvalidPassword };

        // 토큰 생성 및 Redis에 저장 (30분 만료)
        var userId = req.Email;
        var token = Guid.NewGuid().ToString("N");
        var tokenRedis = _redis.GetString<string>(RedisKeyBuilder.AuthToken(userId), TimeSpan.FromMinutes(30));
        await tokenRedis.SetAsync(token);

        return new LoginResponse { Result = ErrorCode.None, UserId = userId, Token = token };
    }

    // ── 유저 데이터 로드 ─────────────────────────────────
    // RedisString<T>에 복합 객체(UserGameData)를 JSON 직렬화하여 저장/로드
    [HttpPost("SetUserData")]
    public async Task<BaseResponse> SetUserData(SetUserDataRequest req)
    {
        var redis = _redis.GetString<UserGameData>(RedisKeyBuilder.UserData(req.UserId));
        await redis.SetAsync(new UserGameData { Level = req.Level, Exp = req.Exp, Money = req.Money });
        return new BaseResponse { Result = ErrorCode.None };
    }

    [HttpPost("GetUserData")]
    public async Task<GetUserDataResponse> GetUserData(GetUserDataRequest req)
    {
        var redis = _redis.GetString<UserGameData>(RedisKeyBuilder.UserData(req.UserId));
        var result = await redis.GetAsync();

        if (!result.HasValue)
            return new GetUserDataResponse { Result = ErrorCode.AccountNotFound };

        return new GetUserDataResponse { Result = ErrorCode.None, Data = result.Value };
    }

    static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}

// ── DTO ─────────────────────────────────────────────
public class CreateAccountRequest
{
    [Required][EmailAddress] public string Email { get; set; } = "";
    [Required][MinLength(1)][MaxLength(30)] public string Password { get; set; } = "";
}
public class CreateAccountResponse : BaseResponse { }

public class LoginRequest
{
    [Required] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}
public class LoginResponse : BaseResponse
{
    public string UserId { get; set; } = "";
    public string Token { get; set; } = "";
}

public class SetUserDataRequest
{
    [Required] public string UserId { get; set; } = "";
    public int Level { get; set; } = 1;
    public int Exp { get; set; }
    public long Money { get; set; }
}
public class GetUserDataRequest { [Required] public string UserId { get; set; } = ""; }
public class GetUserDataResponse : BaseResponse { public UserGameData? Data { get; set; } }

public class UserGameData
{
    public int Level { get; set; }
    public int Exp { get; set; }
    public long Money { get; set; }
}
