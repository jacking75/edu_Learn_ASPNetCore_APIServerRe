using System.IO;
using GameAPIServer.Repository;
using GameAPIServer.Servicies;
using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;
using Prometheus;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;

// DI 등록: 서비스와 리포지토리 (FakeGameDb = DB 없이 테스트용 Mock)
builder.Services.AddTransient<IGameDb, FakeGameDb>();
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IFriendService, FriendService>();
builder.Services.AddTransient<IGameService, GameService>();
builder.Services.AddTransient<IMailService, MailService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IDataLoadService, DataLoadService>();
builder.Services.AddControllers();

SettingLogger();

WebApplication app = builder.Build();

ILoggerFactory loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

app.UseRouting();

// [메트릭 수집 미들웨어] 모든 요청에 대해 자동으로 Counter, Histogram, UpDownCounter 기록
// MetricsRegistry.cs에 정의된 .NET Metrics API 메트릭 사용
app.UseMiddleware<RequestMetricsMiddleware>();

// [prometheus-net] HTTP 요청 관련 기본 메트릭 자동 수집 (http_requests_received_total 등)
app.UseHttpMetrics();

app.MapDefaultControllerRoute();

// [prometheus-net] /metrics 엔드포인트 등록 — Prometheus가 이 주소를 수집(Pull)
// 브라우저에서 http://localhost:11500/metrics 로 확인 가능
app.MapMetrics();

app.Run(configuration["ServerAddress"]);



void SettingLogger()
{
    ILoggingBuilder logging = builder.Logging;
    logging.ClearProviders();

    var fileDir = configuration["logdir"];

    var exists = Directory.Exists(fileDir);

    if (!exists)
    {
        Directory.CreateDirectory(fileDir);
    }

    logging.AddZLoggerRollingFile(
        options =>
        {
            options.UseJsonFormatter();
            options.FilePathSelector = (timestamp, sequenceNumber) => $"{fileDir}{timestamp.ToLocalTime():yyyy-MM-dd}_{sequenceNumber:000}.log";
            options.RollingInterval = ZLogger.Providers.RollingInterval.Day;
            options.RollingSizeKB = 1024;
        });

    _ = logging.AddZLoggerConsole(options =>
    {
        options.UseJsonFormatter();
    });


}
