namespace RedisExampleServer.Models;

/// <summary>
/// 모든 API 응답의 공통 기반 클래스.
/// Result가 ErrorCode.None(0)이면 성공, 그 외는 실패.
/// </summary>
public class BaseResponse
{
    public ErrorCode Result { get; set; }
}
