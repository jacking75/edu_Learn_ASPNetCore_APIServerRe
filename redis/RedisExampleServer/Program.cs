using RedisExampleServer.Services;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// RedisService를 싱글톤으로 등록 (앱 전체에서 하나의 Redis 연결을 공유)
// 참고: docs/02_레디스_연결_관리자_.md
builder.Services.AddSingleton<RedisService>();
builder.Services.AddControllers();

var app = builder.Build();

// Redis 초기화: appsettings.json의 "RedisAddress"(기본값 127.0.0.1)로 연결
// RedisService.Init()이 호출되면 내부적으로 CloudStructures의 RedisConnection이 생성됨
var redisService = app.Services.GetRequiredService<RedisService>();
redisService.Init(configuration["RedisAddress"]!);

app.MapControllers();

// appsettings.json의 "ServerAddress"(기본값 http://0.0.0.0:11600)에서 서버 시작
app.Run(configuration["ServerAddress"]);
