using System.ComponentModel.DataAnnotations;

namespace SampleServer.DTO;

public class LogRequest
{
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 로그 레벨: trace, debug, information, warning, error, critical
    /// </summary>
    public string Level { get; set; } = "information";
}
