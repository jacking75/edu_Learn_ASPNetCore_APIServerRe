using SampleServer.Services;
using System.Text.Json;
using ZLogger;
using ZLogger.Formatters;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

// [1] 콘솔 출력: JSON 포맷 + Scope 활성화
builder.Logging.AddZLoggerConsole(options =>
{
    options.IncludeScopes = true;
    options.UseJsonFormatter(formatter =>
    {
        formatter.JsonPropertyNames = JsonPropertyNames.Default with
        {
            Timestamp = JsonEncodedText.Encode("timestamp"),
            MemberName = JsonEncodedText.Encode("membername"),
            Exception = JsonEncodedText.Encode("exception"),
        };

        formatter.JsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        };

        formatter.KeyNameMutator = KeyNameMutator.LastMemberNameLowerFirstCharacter;
        formatter.IncludeProperties =
            IncludeProperties.Timestamp |
            IncludeProperties.LogLevel |
            IncludeProperties.ParameterKeyValues |
            IncludeProperties.MemberName |
            IncludeProperties.Message |
            IncludeProperties.ScopeKeyValues |
            IncludeProperties.Exception;
    });
});

// [2] 파일 출력: 롤링 파일 (일 단위 교체, 1MB 초과 시 새 파일)
builder.Logging.AddZLoggerRollingFile(options =>
{
    options.UseJsonFormatter();
    options.FilePathSelector = (timestamp, sequenceNumber)
        => $"logs/{timestamp.ToLocalTime():yyyy-MM-dd}_{sequenceNumber:000}.log";
    options.RollingInterval = ZLogger.Providers.RollingInterval.Day;
    options.RollingSizeKB = 1024;
});

// 서비스 등록
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddControllers();

WebApplication app = builder.Build();

app.UseRouting();
app.MapControllers();
app.Run();
