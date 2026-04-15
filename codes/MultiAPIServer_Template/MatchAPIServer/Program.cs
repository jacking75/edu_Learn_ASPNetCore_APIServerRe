using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MatchAPIServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;

// appsettings.json에서 매칭 설정(큐 주기 등)을 MatchingConfig 객체로 바인딩
builder.Services.Configure<MatchingConfig>(configuration.GetSection(nameof(MatchingConfig)));

// 매칭 워커: 백그라운드에서 매칭 큐를 처리하는 싱글톤 서비스
// GameAPIServer가 매칭 요청을 보내면, MatchWorker가 큐에서 2명을 꺼내 매칭을 성사시킨다
builder.Services.AddSingleton<IMatchWoker, MatchWoker>();

builder.Services.AddControllers();

WebApplication app = builder.Build();

app.MapDefaultControllerRoute();

app.Run(configuration["ServerAddress"]);
