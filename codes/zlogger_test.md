Directory structure:
└── zlogger_test/
    ├── README.md
    ├── ErrorCode.cs
    ├── LogManager.cs
    ├── Program.cs
    ├── apiTest.http
    ├── appsettings.Development.json
    ├── appsettings.json
    ├── zlogger_test.csproj
    ├── zlogger_test.sln
    ├── Controllers/
    │   └── CreateAccountController.cs
    ├── Model/
    │   ├── DAO/
    │   │   ├── AccountDB.cs
    │   │   ├── GameDB_UserGameData.cs
    │   │   └── RedisDB.cs
    │   └── DTO/
    │       ├── CreateAccount.cs
    │       ├── CreateCharacter.cs
    │       ├── EnterField.cs
    │       └── Login.cs
    └── Properties/
        └── launchSettings.json

================================================
File: README.md
================================================
# ZLogger 로깅 테스트 프로젝트

ZLogger 라이브러리의 기본 사용법을 빠르게 확인하기 위한 간단한 예제 프로젝트.

> ZLogger의 체계적인 학습은 [`/ZLogger/`](../../ZLogger/) 디렉토리의 README.md와 SampleServer를 참고한다.

## 학습 포인트

| 파일 | 내용 |
|---|---|
| `Program.cs` | ZLogger 콘솔/롤링파일 출력 설정 (`AddZLoggerConsole`, `AddZLoggerRollingFile`) |
| `LogManager.cs` | EventId를 Dictionary로 관리하는 패턴 (이벤트별 고유 ID 부여) |
| `Controllers/CreateAccountController.cs` | `ZLogInformation` 3가지 사용법: 문자열 보간, `:json` 포맷, EventId 활용 |

### 로그 출력 예시

```json
// 기본 문자열 보간
{"Timestamp":"...","LogLevel":"Information","Message":"EventType:CreateAccount, Email:test@test.com"}

// :json 포맷 (객체를 JSON으로 직렬화)
{"Timestamp":"...","Message":"[EventType:CreateAccount] {\"Email\":\"test@test.com\",\"Password\":\"123qwe\"}"}

// EventId 활용
{"Timestamp":"...","Message":"CreateAccount: {\"Email\":\"test@test.com\"}","EventId":{"Id":101,"Name":"CreateAccount"}}
```

## 실행 방법

```bash
cd codes/zlogger_test
dotnet run
# 서버 주소: http://localhost:11500 (appsettings.json에서 설정)
```

## API 테스트

`apiTest.http` 파일을 VS Code REST Client 또는 Rider에서 열어 실행한다.

```bash
POST http://localhost:11500/CreateAccount
Content-Type: application/json

{ "Email": "test@test.com", "Password": "123qwe" }
```

서버 콘솔과 `./log/` 폴더의 로그 파일에서 JSON 형식의 로그 출력을 확인할 수 있다.



================================================
File: ErrorCode.cs
================================================
癤퓎sing System;

// 1000 ~ 19999
public enum ErrorCode : UInt16
{
    None = 0,

    // Common 1000 ~
    UnhandleException = 1001,
    RedisFailException = 1002,
    InValidRequestHttpBody = 1003,
    AuthTokenFailWrongAuthToken = 1006,

    // Account 2000 ~
    CreateAccountFailException = 2001,    
    LoginFailException = 2002,
    LoginFailUserNotExist = 2003,
    LoginFailPwNotMatch = 2004,
    LoginFailSetAuthToken = 2005,
    AuthTokenMismatch = 2006,
    AuthTokenNotFound = 2007,
    AuthTokenFailWrongKeyword = 2008,
    AuthTokenFailSetNx = 2009,
    AccountIdMismatch = 2010,
    DuplicatedLogin = 2011,
    CreateAccountFailInsert = 2012,
    LoginFailAddRedis = 2014,
    CheckAuthFailNotExist = 2015,
    CheckAuthFailNotMatch = 2016,
    CheckAuthFailException = 2017,

    // Character 3000 ~
    CreateCharacterRollbackFail = 3001,
    CreateCharacterFailNoSlot = 3002,
    CreateCharacterFailException = 3003,
    CharacterNotExist = 3004,
    CountCharactersFail = 3005,
    DeleteCharacterFail = 3006,
    GetCharacterInfoFail = 3007,
    InvalidCharacterInfo = 3008,
    GetCharacterItemsFail = 3009,
    CharacterCountOver = 3010,
    CharacterArmorTypeMisMatch = 3011,
    CharacterHelmetTypeMisMatch = 3012,
    CharacterCloakTypeMisMatch = 3012,
    CharacterDressTypeMisMatch = 3013,
    CharacterPantsTypeMisMatch = 3012,
    CharacterMustacheTypeMisMatch = 3012,
    CharacterArmorCodeMisMatch = 3013,
    CharacterHelmetCodeMisMatch = 3014,
    CharacterCloakCodeMisMatch = 3015,
    CharacterDressCodeMisMatch = 3016,
    CharacterPantsCodeMisMatch = 3017,
    CharacterMustacheCodeMisMatch = 3018,
    CharacterHairCodeMisMatch = 3019,
    CharacterCheckCodeError = 3010,
    CharacterLookTypeError = 3011,

    CharacterStatusChangeFail = 3012,
    CharacterIsExistGame = 3013,
    GetCharacterListFail = 3014,

    //GameDb 4000~ 
    GetGameDbConnectionFail = 4002
}


================================================
File: LogManager.cs
================================================
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

public static class LogManager
{
    public enum EventType
    {
        CreateAccount = 101,
    }

    public static Dictionary<EventType, EventId> EventIdDic = new()
    {
        { EventType.CreateAccount, new EventId((int)EventType.CreateAccount, nameof(EventType.CreateAccount)) },
    };
}



================================================
File: Program.cs
================================================
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



================================================
File: apiTest.http
================================================
癤풮OST http://localhost:11500/Login
Content-Type: application/json

{
  "ID":"jacking751",
  "PW":"123qwe"
}

###
POST http://localhost:11500/CreateAccount
Content-Type: application/json

{
  "Email":"jacking751@gmail.com",
  "Password":"123qwe"
}





================================================
File: appsettings.Development.json
================================================
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Debug"
    }
  },
  "AllowedHosts": "*",
  "ServerAddress": "http://0.0.0.0:11500",
  "logdir": "./log/", 
  "DbConfig": {
    "Redis": "127.0.0.1",
    "GameDb": "Server=127.0.0.1;user=root;Password=123qwe;Database=game_db;Pooling=true;Min Pool Size=0;Max Pool Size=40;AllowUserVariables=True;",
    "AccountDb": "Server=127.0.0.1;user=root;Password=123qwe;Database=account_db;Pooling=true;Min Pool Size=0;Max Pool Size=40;AllowUserVariables=True;", 
  }
}



================================================
File: appsettings.json
================================================
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }, 
	"ZLoggerConsole": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "AllowedHosts": "*",
  "ServerAddress": "http://0.0.0.0:11500",
  "logdir": "./log/",
    "DbConfig": {
        "Redis": "127.0.0.1",
        "GameDb": "Server=127.0.0.1;user=root;Password=123qwe;Database=game_db;Pooling=true;Min Pool Size=0;Max Pool Size=40;AllowUserVariables=True;",
        "AccountDb": "Server=127.0.0.1;user=root;Password=123qwe;Database=account_db;Pooling=true;Min Pool Size=0;Max Pool Size=40;AllowUserVariables=True;",
        "MasterDataDb": "Server=127.0.0.1;user=root;Password=123qwe;Database=master_data_db;Pooling=false;Min Pool Size=1;Max Pool Size=1;AllowUserVariables=True;"
    }
}



================================================
File: zlogger_test.csproj
================================================
<Project Sdk="Microsoft.NET.Sdk.Web">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>

    <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
        <OutputPath>bin\</OutputPath>
    </PropertyGroup>

    <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|AnyCPU'">
        <OutputPath>bin\</OutputPath>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="CloudStructures" Version="3.3.0" />
        <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.1" />
        <PackageReference Include="MySqlConnector" Version="2.3.6" />
        <PackageReference Include="SqlKata" Version="2.4.0" />
        <PackageReference Include="SqlKata.Execution" Version="2.4.0" />
        <PackageReference Include="System.Configuration.ConfigurationManager" Version="8.0.0" />
        <PackageReference Include="System.Net.Security" Version="4.3.2" />
        <PackageReference Include="ZLogger" Version="2.4.1" />
    </ItemGroup>

    <ItemGroup>
      <Folder Include="Model\DTO\" />
      <Folder Include="Model\DAO\" />
    </ItemGroup>


</Project>



================================================
File: zlogger_test.sln
================================================
﻿
Microsoft Visual Studio Solution File, Format Version 12.00
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "zlogger_test", "zlogger_test.csproj", "{C4BF4730-21F7-4F00-A236-706420265F0D}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{C4BF4730-21F7-4F00-A236-706420265F0D}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{C4BF4730-21F7-4F00-A236-706420265F0D}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{C4BF4730-21F7-4F00-A236-706420265F0D}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{C4BF4730-21F7-4F00-A236-706420265F0D}.Release|Any CPU.Build.0 = Release|Any CPU
		{AC7CEAA7-C4E2-4940-BDAE-0DCFDB5FC273}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{AC7CEAA7-C4E2-4940-BDAE-0DCFDB5FC273}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{AC7CEAA7-C4E2-4940-BDAE-0DCFDB5FC273}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{AC7CEAA7-C4E2-4940-BDAE-0DCFDB5FC273}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal



================================================
File: Controllers/CreateAccountController.cs
================================================
癤퓎sing System;
using System.Threading.Tasks;
using APIServer.Model.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZLogger;
using static LogManager;

namespace APIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class CreateAccount : ControllerBase
{
    private readonly ILogger<CreateAccount> _logger;

    public CreateAccount(ILogger<CreateAccount> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<CreateAccountRes> Post(CreateAccountReq request)
    {
        var response = new CreateAccountRes();

        /*
         {"Timestamp":"2024-04-17T13:46:25.3337031+09:00","LogLevel":"Information","Category":"APIServer.Controllers.CreateAccount","Message":"EventType:CreateAccount, Email:jacking751@gmail.com","request.Email":"jacking751@gmail.com"}

        {"Timestamp":"2024-04-17T13:46:25.3340705+09:00","LogLevel":"Information","Category":"APIServer.Controllers.CreateAccount","Message":"[EventType:CreateAccount] {\u0022Email\u0022:\u0022jacking751@gmail.com\u0022,\u0022Password\u0022:\u0022123qwe\u0022}","request":{"Email":"jacking751@gmail.com","Password":"123qwe"}}

        {"Timestamp":"2024-04-17T13:46:25.3354687+09:00","LogLevel":"Information","Category":"APIServer.Controllers.CreateAccount","Message":"CreateAccount: {\u0022Email\u0022:\u0022jacking751@gmail.com\u0022}","EventIdDic[EventType.CreateAccount]":{"Id":101,"Name":"CreateAccount"},"new { request.Email }":{"Email":"jacking751@gmail.com"}} 
         */
        _logger.ZLogInformation($"EventType:CreateAccount, Email:{request.Email}");
        _logger.ZLogInformation($"[EventType:CreateAccount] {request:json}");
        _logger.ZLogInformation($"{EventIdDic[EventType.CreateAccount]}: {new { request.Email }:json}");
        return response;
    }
}



================================================
File: Model/DAO/AccountDB.cs
================================================
﻿namespace APIServer.Model.DAO;

//AccountDB의 객체는 객체 이름 앞에 Adb를 붙인다.

public class AdbUser
{
    public long user_id { get; set; }

    public string email { get; set; }
    public string hashed_pw { get; set; }
    public string salt_value { get; set; }
}



================================================
File: Model/DAO/GameDB_UserGameData.cs
================================================
﻿namespace APIServer.Model.DAO;

//GameDB의 객체는 객체 이름 앞에 Gdb를 붙인다.

public class GdbUserGameData
{
    public long id { get; set; }
    public long money { get; set; }
}



================================================
File: Model/DAO/RedisDB.cs
================================================
﻿namespace APIServer.Model.DAO;

//RedisDB의 객체는 객체 이름 앞에 Rdb를 붙인다.

public class RdbAuthUserData
{
    public string Email { get; set; } = "";
    public string AuthToken { get; set; } = "";
    public long AccountId { get; set; } = 0;
    public string State { get; set; } = ""; // enum UserState    
}



================================================
File: Model/DTO/CreateAccount.cs
================================================
癤퓎sing System;
using System.ComponentModel.DataAnnotations;

namespace APIServer.Model.DTO;

public class CreateAccountReq
{
    [Required]
    [MinLength(1, ErrorMessage = "EMAIL CANNOT BE EMPTY")]
    [StringLength(50, ErrorMessage = "EMAIL IS TOO LONG")]
    [RegularExpression("^[a-zA-Z0-9_\\.-]+@([a-zA-Z0-9-]+\\.)+[a-zA-Z]{2,6}$", ErrorMessage = "E-mail is not valid")]
    public string Email { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "PASSWORD CANNOT BE EMPTY")]
    [StringLength(30, ErrorMessage = "PASSWORD IS TOO LONG")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}

public class CreateAccountRes
{
    public ErrorCode Result { get; set; } = ErrorCode.None;
}


================================================
File: Model/DTO/CreateCharacter.cs
================================================
﻿//TODO 사용하지 않으면 삭제할 것
using System;
using System.ComponentModel.DataAnnotations;

namespace APIServer.Model.DTO;

public class CreateCharacterReq
{
    [Required] public string Email { get; set; }

    [Required] public string AuthToken { get; set; }

    public string NickName { get; set; }
}

public class CreateCharacterRes
{
    public ErrorCode Result { get; set; } = ErrorCode.None;
}

public class CreateCharacterInfo
{
    [Required]
    [MinLength(1, ErrorMessage = "NICKNAME CANNOT BE EMPTY")]
    [StringLength(8, ErrorMessage = "NICKNAME CANNOT EXCEED 18 CHARACTERS")]
    public string Nickname { get; set; }

    public int Eye { get; set; }

    public int HairStyle { get; set; }

    public int Mustache { get; set; }

    public int Cloak { get; set; }
    public int Pants { get; set; }
    public int Dress { get; set; }
    public int Armor { get; set; }
    public int Helmet { get; set; }
}


================================================
File: Model/DTO/EnterField.cs
================================================
﻿//TODO 사용하지 않으면 삭제할 것
using System;
using System.ComponentModel.DataAnnotations;

namespace APIServer.Model.DTO;

public class EnterFieldReq
{
    [Required] public string Email { get; set; }
    [Required] public string AuthToken { get; set; }
}

public class EnterFieldRes
{
    [Required] public ErrorCode Result { get; set; }
    [Required] public string EnterFieldToken { get; set; }
    [Required] public string WorldAddressIp { get; set; }
    [Required] public string WorldAddressPort { get; set; }
}


================================================
File: Model/DTO/Login.cs
================================================
癤퓎sing System;
using System.ComponentModel.DataAnnotations;


namespace APIServer.Model.DTO;

public class LoginRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "EMAIL CANNOT BE EMPTY")]
    [StringLength(50, ErrorMessage = "EMAIL IS TOO LONG")]
    [RegularExpression("^[a-zA-Z0-9_\\.-]+@([a-zA-Z0-9-]+\\.)+[a-zA-Z]{2,6}$", ErrorMessage = "E-mail is not valid")]
    public string Email { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "PASSWORD CANNOT BE EMPTY")]
    [StringLength(30, ErrorMessage = "PASSWORD IS TOO LONG")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}

public class LoginResponse
{
    [Required] public ErrorCode Result { get; set; } = ErrorCode.None;
    [Required] public string AuthToken { get; set; } = "";
}


================================================
File: Properties/launchSettings.json
================================================
﻿{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:55883",
      "sslPort": 44384
    }
  },
  "profiles": {
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "launchUrl": "weatherforecast",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "API Server": {
      "commandName": "Project",
      "launchBrowser": false,
      "launchUrl": "Login",
      "applicationUrl": "http://0.0.0.0:11500;",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}


