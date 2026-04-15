using GameServer.Repository;
using GameServer.Services.Interfaces;
using GameServer.Services;
using MatchServer.Services;
using GameServer.Repository.Interfaces;
using ZLogger;
using System.Text.Json;
using ZLogger.Formatters;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// CORS 정책 추가 - Blazor 등 외부 클라이언트에서 호출 가능하도록
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.Configure<DbConfig>(builder.Configuration.GetSection("ConnectionStrings")); 
builder.Services.Configure<DbConfig>(builder.Configuration.GetSection("RedisConfig")); 

builder.Services.AddScoped<IGameDb, GameDb>();
builder.Services.AddSingleton<IMemoryDb, MemoryDb>();
builder.Services.AddSingleton<IMasterDb, MasterDb>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IMatchingService, MatchingService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IPlayerInfoService, PlayerInfoService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IFriendService, FriendService>();

builder.Services.AddHttpClient();

// 기본 로깅 설정 (주석 처리)
//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
//builder.Logging.SetMinimumLevel(LogLevel.Information);

// 로깅 설정 (ZLogger를 통한 JSON 로그 출력)
builder.Logging.ClearProviders();
builder.Logging.AddZLoggerConsole(options =>
{
    options.UseJsonFormatter(formatter =>
    {
        formatter.JsonPropertyNames = JsonPropertyNames.Default with
        {
            Category = JsonEncodedText.Encode("category"),
            Timestamp = JsonEncodedText.Encode("timestamp"),
            LogLevel = JsonEncodedText.Encode("logLevel"),
            Message = JsonEncodedText.Encode("message")
        };

        formatter.IncludeProperties = IncludeProperties.Timestamp | IncludeProperties.ParameterKeyValues;
    });
});

// 로그 최소 레벨 설정
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddControllers();

var app = builder.Build();

// Prometheus 미들웨어 추가
app.UseMetricServer();   // /metrics 엔드포인트 제공 (Prometheus가 이 주소를 수집)
app.UseHttpMetrics();     // HTTP 요청에 대한 자동 메트릭 수집 (요청 수, 응답 시간 등)

app.UseCors("AllowAllOrigins");

app.UseMiddleware<GameServer.Middleware.CheckVersion>(); // HTTP 요청의 클라이언트 버전 체크
app.UseMiddleware<GameServer.Middleware.CheckAuth>();    // HTTP 요청의 인증 토큰 체크

app.MapControllers();

app.Run();