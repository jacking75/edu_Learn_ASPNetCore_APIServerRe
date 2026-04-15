using System.IO;
using GameAPIServer.Repository;
using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Services;
using GameAPIServer.Servicies;
using GameAPIServer.Servicies.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;

// appsettings.json에서 DB 접속 정보를 DbConfig 객체로 바인딩
builder.Services.Configure<DbConfig>(configuration.GetSection(nameof(DbConfig)));

// ── DI 등록: Repository(DB 접근) + Service(비즈니스 로직) ──
builder.Services.AddTransient<IGameDb, GameDb>();       // MySQL 게임 DB
builder.Services.AddSingleton<IMemoryDb, MemoryDb>();   // Redis 캐시
builder.Services.AddSingleton<IMasterDb, MasterDb>();   // 마스터 데이터 (기획 테이블)
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IFriendService, FriendService>();
builder.Services.AddTransient<IGameService, GameService>();
builder.Services.AddTransient<IItemService, ItemService>();
builder.Services.AddTransient<IMailService, MailService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IAttendanceService, AttendanceService>();
builder.Services.AddTransient<IDataLoadService, DataLoadService>();
builder.Services.AddControllers();

// ZLogger 로깅 설정 (콘솔 + 롤링 파일, JSON 형식)
SettingLogger();

WebApplication app = builder.Build();

// 마스터 데이터 로드 (앱 시작 시 1회 — 실패하면 서버 종료)
if(!await app.Services.GetService<IMasterDb>().Load())
{
    return;
}

ILoggerFactory loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

// ── 미들웨어 체인 (요청 → 버전 체크 → 인증 체크 → 컨트롤러) ──
app.UseMiddleware<GameAPIServer.Middleware.VersionCheck>();               // 클라이언트 버전 확인
app.UseMiddleware<GameAPIServer.Middleware.CheckUserAuthAndLoadUserData>(); // 토큰 검증 + 유저 데이터 로드

app.UseRouting();

app.MapDefaultControllerRoute();

IMasterDb masterDataDB = app.Services.GetRequiredService<IMasterDb>();
await masterDataDB.Load();

app.Run(configuration["ServerAddress"]);

void SettingLogger()
{
    ILoggingBuilder logging = builder.Logging;
    logging.ClearProviders(); // 기본 로거 제거 (ZLogger만 사용)

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