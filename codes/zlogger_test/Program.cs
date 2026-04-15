using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;

builder.Services.AddControllers();

// ZLogger 로깅 설정 (콘솔 + 롤링 파일, JSON 형식)
SettingLogger();

WebApplication app = builder.Build();


app.MapDefaultControllerRoute();

app.Run(configuration["ServerAddress"]);



void SettingLogger()
{
    ILoggingBuilder logging = builder.Logging;
    _ = logging.ClearProviders(); // 기본 로거 제거 (ZLogger만 사용)

    // appsettings.json의 "logdir" 설정에서 로그 파일 저장 경로를 가져옴
    string fileDir = configuration["logdir"];

    bool exists = Directory.Exists(fileDir);

    if (!exists)
    {
        _ = Directory.CreateDirectory(fileDir);
    }

    // 롤링 파일: 일 단위 교체, 1MB 초과 시 새 파일, JSON 형식
    // 생성 예: ./log/2024-10-10_000.log, ./log/2024-10-10_001.log
    _ = logging.AddZLoggerRollingFile(
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
