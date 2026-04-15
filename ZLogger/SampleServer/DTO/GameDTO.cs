using System.ComponentModel.DataAnnotations;

namespace SampleServer.DTO;

// --- 로그인 ---
public class LoginRequest
{
    [Required] public Int64 PlayerId { get; set; }
    [Required] public string Token { get; set; } = string.Empty;
}

public class LoginResponse
{
    public UInt16 Result { get; set; }
    public Int64 Uid { get; set; }
    public string AccessToken { get; set; } = string.Empty;
}

// --- 게임 시작 ---
public class StartGameRequest
{
    [Required] public Int64 Uid { get; set; }
}

public class StartGameResponse
{
    public UInt16 Result { get; set; }
    public string GameGuid { get; set; } = string.Empty;
}
