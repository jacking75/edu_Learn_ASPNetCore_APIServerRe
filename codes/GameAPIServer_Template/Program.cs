using System.IO;
using GameAPIServer.Repository;
using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Servicies;
using GameAPIServer.Servicies.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;

// ── appsettings.json에서 DB 접속 정보를 DbConfig 객체로 바인딩 ──
builder.Services.Configure<DbConfig>(configuration.GetSection(nameof(DbConfig)));

// ── DI(의존성 주입) 등록 ──────────────────────────────
// Repository 계층: DB 접근을 담당
builder.Services.AddTransient<IGameDb, GameDb>();       // MySQL 게임 DB (요청마다 새 인스턴스)
builder.Services.AddSingleton<IMemoryDb, MemoryDb>();   // Redis (앱 전체에서 1개 연결 공유)
builder.Services.AddSingleton<IMasterDb, MasterDb>();   // 마스터 데이터 (앱 시작 시 1회 로드)

// Service 계층: 비즈니스 로직을 담당
builder.Services.AddTransient<IAuthService, AuthService>();
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

// ── 마스터 데이터 로드 (앱 시작 시 1회) ───────────────
// 아이템 테이블, 스테이지 테이블 등 기획 데이터를 DB에서 메모리로 로드
if(!await app.Services.GetService<IMasterDb>().Load())
{
    return;
}

ILoggerFactory loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

// ── 미들웨어 체인 (요청이 컨트롤러에 도달하기 전에 순서대로 실행) ──
// 1) 클라이언트 버전 체크: 앱 버전이 서버와 일치하는지 확인
app.UseMiddleware<GameAPIServer.Middleware.VersionCheck>();
// 2) 인증 + 유저 데이터 로드: 토큰 검증 후 Redis에서 유저 정보 로드
app.UseMiddleware<GameAPIServer.Middleware.CheckUserAuthAndLoadUserData>();

app.UseRouting();

app.MapDefaultControllerRoute();

IMasterDb masterDataDB = app.Services.GetRequiredService<IMasterDb>();
await masterDataDB.Load();

app.Run(configuration["ServerAddress"]);

// ── ZLogger 설정 ─────────────────────────────────────
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

    // 롤링 파일: 일 단위 교체, 1MB 초과 시 새 파일, JSON 형식
    logging.AddZLoggerRollingFile(
        options =>
        {
            options.UseJsonFormatter();
            options.FilePathSelector = (timestamp, sequenceNumber) => $"{fileDir}{timestamp.ToLocalTime():yyyy-MM-dd}_{sequenceNumber:000}.log";
            options.RollingInterval = ZLogger.Providers.RollingInterval.Day;
            options.RollingSizeKB = 1024;
        });

    // 콘솔 출력: JSON 형식 (개발 중 터미널에서 확인)
    _ = logging.AddZLoggerConsole(options =>
    {
        options.UseJsonFormatter();
    });


}
