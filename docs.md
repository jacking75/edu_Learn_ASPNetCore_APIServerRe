Directory structure:
└── docs/
    ├── guides/
    │   ├── HttpClientFactory.md
    │   ├── UsingVSCode.md
    │   ├── ai_coding_tools_guide.md
    │   ├── appsettings_environment.md
    │   ├── aspnetcore_basics.md
    │   ├── how_to_db_transaction.md
    │   ├── integration_testing.md
    │   ├── mysql_sqlkata.md
    │   └── project_structure.md
    ├── patterns/
    │   ├── Cache-Aside_pattern.md
    │   ├── error_code_design.md
    │   └── why_di.md
    └── references/
        ├── APIServer_Directory.md
        ├── Serilog.md
        ├── aspnet_core_tips.md
        ├── dotnet_build.md
        ├── infographic.md
        └── sqlkata.md

================================================
File: guides/HttpClientFactory.md
================================================
##  API 서버간 통신 때 HttpClientFactory 사용하기
[MS Docs: ASP.NET Core에서 IHttpClientFactory를 사용하여 HTTP 요청  만들기](https://learn.microsoft.com/ko-kr/aspnet/core/fundamentals/http-requests?view=aspnetcore-8.0 )  
    
```
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("Google", httpClient =>{
    httpClient.BaseAddress = new Uri("https://www.google.com/");
});
builder.Services.AddHttpClient("BaiDu", httpClient =>{
    httpClient.BaseAddress = new Uri("https://www.baidu.com/");
});
```
  
```
interface IGoogleService{
    Task<string> GetContentAsync();
}
class GoogleService : IGoogleService{
    private readonly IHttpClientFactory _httpClientFactory;

    public GoogleService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
	
    public async Task<string> GetContentAsync()
    {
        var googleClient = _httpClientFactory.CreateClient("Google");
		var response = await googleClient.GetAsync("some-endpoint");
		...
    }
}
```        
    
IHttpClientFactory는 ASP.NET Core에서 HTTP 클라이언트를 관리하는 데 많은 장점을 제공한다.  
- 클라이언트 인스턴스 풀링: IHttpClientFactory는 HttpClient 인스턴스를 풀링하여 재사용합니다. 이로 인해 매 요청마다 새로운 HttpClient를 생성하는 비용을 줄이고 성능을 향상시킵니다.
- 수명 관리: IHttpClientFactory는 HttpClient 인스턴스의 수명을 관리합니다. 요청이 완료되면 HttpClient를 반환하고 재사용할 수 있도록 합니다. 이로 인해 메모리 누수를 방지하고 리소스를 효율적으로 사용할 수 있습니다.
- 구성 가능한 클라이언트: IHttpClientFactory를 사용하면 여러 클라이언트를 구성할 수 있습니다. 위의 코드에서 “Google” 및 “BaiDu” 클라이언트를 등록했습니다. 각 클라이언트는 서로 다른 BaseAddress를 가지며 다른 서비스에 대한 요청을 보낼 수 있습니다.
- DI (의존성 주입) 지원: IHttpClientFactory는 DI 컨테이너와 통합되어 의존성 주입을 통해 HttpClient를 쉽게 주입할 수 있습니다.
- 테스트 용이성: IHttpClientFactory를 사용하면 HttpClient를 목(Mock)으로 대체하여 단위 테스트를 수행할 수 있습니다. 이는 외부 서비스에 의존하는 코드를 테스트할 때 유용합니다.
    
요약하자면, IHttpClientFactory는 성능, 메모리 관리, 구성 가능성 및 테스트 용이성 측면에서 HTTP 클라이언트를 효율적으로 관리하는 데 도움이 됩니다.  
  
### 동시 요청을 많이 보내고 싶을 때   
ASP.NET Core Web API 서버에서 `HttpClientFactory`를 사용하여 다른 서버에 많은 요청을 동시에 보내야 하는 경우, 다음과 같은 사항들을 고려해야 합니다.    
  
---    

#### 1. 기본 연결 제한 (MaxConnectionsPerServer) 조정  
.NET의 기본 `SocketsHttpHandler`에는 서버당 최대 동시 연결 수가 제한되어 있습니다. 기본값은 일반적으로 2로 설정되어 있어, 많은 요청을 동시에 보낼 경우 병목현상이 발생할 수 있습니다. 이를 해결하기 위해 **MaxConnectionsPerServer** 값을 늘려야 합니다.  
  
예를 들어, 다음과 같이 HttpClientFactory를 통해 생성되는 HttpClient에 대해 최대 연결 수를 늘릴 수 있습니다:  
```csharp
services.AddHttpClient("MyClient")
    .ConfigurePrimaryHttpMessageHandler(() =>
        new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 100 // 필요에 맞게 설정 (예: 100 또는 그 이상)
        });
```  
  
이렇게 설정하면, 동일한 서버로의 동시 연결 수가 증가하여 많은 요청을 동시에 처리할 수 있습니다.  
  
---  
  
#### 2. HttpMessageHandler 재사용 및 관리  
`HttpClient`는 재사용이 가능한 인스턴스를 만드는 것이 중요합니다. `IHttpClientFactory`를 사용하면 HttpMessageHandler를 효율적으로 관리할 수 있으며, 사용 후에도 Dispose 호출로 인한 소켓 포트 소진(Socket exhaustion)을 예방할 수 있습니다.  
  
- **핵심 포인트:**  
  - HttpClient 인스턴스를 재사용하거나, `HttpClientFactory`를 통해 관리하도록 하여 연결 재사용을 극대화합니다.
  - 필요하다면 **PooledConnectionLifetime** 또는 **PooledConnectionIdleTimeout**을 설정하여 오래된 연결을 주기적으로 닫을 수 있습니다.  
  
예시:  
```csharp
services.AddHttpClient("MyClient")
    .ConfigurePrimaryHttpMessageHandler(() =>
        new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 100,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5), // 최대 연결 수명
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2) // 유휴 연결 유지 시간
        });
```  
  
---
  
#### 3. 폴리시 및 타임아웃 적용   
많은 요청을 동시에 보낼 때는 잠재적인 네트워크 지연이나 실패를 고려해 **타임아웃**과 **재시도(retry) 정책**등을 적용하는 것이 좋습니다.  
  
- **타임아웃 설정:**  
  HttpClient의 Timeout 속성을 조정하여 매 요청의 최대 대기 시간을 설정합니다.
  
- **폴리시 설정:**  
  [Polly 라이브러리](https://github.com/App-vNext/Polly)를 사용하여 재시도, 회로 차단기(Circuit Breaker) 등의 폴리시를 추가할 수 있습니다.
  
예시 (Polly를 사용한 재시도 정책):  
```csharp
services.AddHttpClient("MyClient")
    .ConfigurePrimaryHttpMessageHandler(() =>
        new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 100,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
        })
    .AddTransientHttpErrorPolicy(builder =>
        builder.WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
        )
    );
```
  
---
  
#### 4. 적절한 부하 테스트 및 모니터링   
- `HttpClientFactory` 사용 시, 설정한 옵션들이 실제 환경에서 기대한 대로 동작하는지 부하 테스트를 수행합니다.
- ASP.NET Core의 로깅 및 모니터링 기능을 통해 연결 상태, 응답 시간, 오류 등을 체크하여 필요한 경우 추가적인 조정을 수행합니다.
  
---

#### 결론  
많은 요청을 동시에 처리하기 위해서는 기본 연결 제한 값을 늘리고, HttpClient 및 HttpMessageHandler 재사용, 적절한 타임아웃과 폴리시 적용, 그리고 이후 모니터링을 통한 지속적인 관리가 필수적입니다. 이러한 설정을 통해 ASP.NET Core 웹 API에서 `HttpClientFactory`를 효율적으로 사용하여 높은 동시성의 요청을 안정적으로 처리할 수 있습니다.
  


================================================
File: guides/UsingVSCode.md
================================================
# VS Code에서 C# Dev Kit 없이 ASP.NET Core 개발 환경 구성하기

이 문서는 VS Code에서 C# Dev Kit 없이 ASP.NET Core 개발을 위한 환경 구성 방법을 안내합니다.

## 목차

1. [필요 사항](#필요-사항)
2. [기본 설정](#기본-설정)
   - [.NET SDK 설치](#net-sdk-설치)
   - [VS Code 설치](#vs-code-설치)
   - [필수 확장 프로그램 설치](#필수-확장-프로그램-설치)
3. [프로젝트 설정](#프로젝트-설정)
   - [프로젝트 생성](#프로젝트-생성)
   - [디버깅 설정](#디버깅-설정)
4. [동작 테스트](#동작-테스트)
5. [문제 해결 및 팁](#문제-해결-및-팁)

  
## 필요 사항

- Windows, macOS 또는 Linux 운영 체제
- .NET 9 SDK (또는 필요한 버전)
- Visual Studio Code
- 인터넷 연결 (확장 프로그램 및 패키지 설치용)

## 기본 설정

### .NET SDK 설치

1. [.NET 다운로드 페이지](https://dotnet.microsoft.com/download)에서 .NET SDK를 다운로드하고 설치합니다.
2. 설치 후, 다음 명령어로 설치가 제대로 되었는지 확인합니다:

```powershell 
dotnet --version
```

### VS Code 설치

1. [Visual Studio Code 웹사이트](https://code.visualstudio.com/)에서 VS Code를 다운로드하고 설치합니다.

### 확장 프로그램 설치 (필수)

* C# Dev Kit은 사용하지 않습니다.

1. **C# (Micrososft)** - 기본적인 C# 지원 
   - 설치: VS Code 확장 마켓플레이스에서 "C#"을 검색하여 Microsoft에서 제공하는 확장 프로그램 설치
   - 기능: 구문 강조, 기본 IntelliSense, 디버깅 지원


3. **REST Client** - API 테스트를 위한 도구 (Http 파일)
   - 설치: VS Code 확장 마켓플레이스에서 "REST Client" 검색
   - 기능: VS Code 내에서 직접 HTTP 요청을 작성하고 테스트
   - 사용법: [사용법](#rest-client를-사용한-테스트)



### 확장 프로그램 설치 (선택)

1. **Resharper (Jetbrains)** - C# 코드 분석기 (아직 데모 버전)
   - 설치: VS Code 확장 마켓플레이스에서 "Resharper"를 검색하여 설치. 이후 **C# 확장프로그램 제거**
   - 기능: C# 디버깅 및 코드 리팩토링을 편하게 하는 툴
   - **한계점** : 아직 테스트 중인 툴
        - 엉뚱한 디버깅
        - C# 확장과 호환이 안됨


## 프로젝트 설정

### 프로젝트 생성

터미널에서 다음 명령을 사용하여 새 ASP.NET Core 웹 API 프로젝트를 생성합니다:

```powershell
# 새 디렉토리 생성 및 이동
mkdir GameAPIServer
cd GameAPIServer

# 웹 API 프로젝트 생성
dotnet new webapi

# VS Code로 현재 디렉토리 열기
code .
```

<div align="center">
  <img src="../../images/VSCode_APIServer/project_creation_result.png" alt="프로젝트 생성 결과" width="70%">
  <p><em>그림 1: VS Code에서 웹 API 프로젝트가 생성된 결과 화면</em></p>
</div>

## 동작 테스트

프로젝트가 제대로 설정되었는지 확인하려면 간단한 테스트를 수행합니다:

### 빌드 및 실행

1. 터미널에서 다음 명령어로 프로젝트를 빌드합니다:

```powershell
dotnet build
```

<div align="center">
  <img src="../../images/VSCode_APIServer/dotnet_build.png" alt="빌드 명령 실행" width="70%">
  <p><em>dotnet build 명령으로 프로젝트를 빌드하는 화면</em></p>
</div>

혹은 VS Code Terminal -> Run Build Task로 빌드합니다.

<div align="center">
  <img src="../../images/VSCode_APIServer/vsbuild.gif" alt="디버깅 데모" width="70%">
  <p><em>VS Code에서 메뉴를 통한 빌드 장면 시연</em></p>
</div>

2. 빌드가 성공하면 다음 명령어로 실행합니다:

```powershell
dotnet run
```

<div align="center">
  <img src="../../images/VSCode_APIServer/dotnet_run.png" alt="실행 명령 및 결과" width="70%">
  <p><em>dotnet run 명령으로 애플리케이션을 실행한 결과 화면</em></p>
</div>

혹은 VS Code Run -> Run Without Debugging으로 실행합니다.

<div align="center">
  <img src="../../images/VSCode_APIServer/vscoderun.gif" alt="디버깅 데모" width="70%">
  <p><em>VS Code에서 메뉴를 통한 run장면 시연</em></p>
</div>


3. 애플리케이션이 시작되면 표시되는 URL을 브라우저에서 열거나 다음 명령어를 사용하여 기본 API 엔드포인트에 요청을 보낼 수 있습니다:

```powershell
curl http://localhost:5000/weatherforecast
```

### 디버깅 설정

VS Code에서 디버깅을 설정하려면:

1. VS Code에서 프로젝트를 연 후, `F5` 키를 누르고 .Net Core 디버거를 선택합니다. 이후, 디버그 탭을 눌러 디버깅을 진행합니다.
   
   <div align="center">
     <img src="../../images/VSCode_APIServer/select_debugger.png" alt="VS Code에서 F5을 누를 경우" width="70%">
     <p><em>VS Code에서 F5을 누르고, Suggested된 .NET Core 디버거 선택</em></p>
   </div>
      - .NET Code 디버거를 선택합니다.

   <div align="center">
     <img src="../../images/VSCode_APIServer/debugger_tab.png" alt="디버그 탭 클릭" width="70%">
     <p><em>VS Code에서 디버그 탭 누르고, 초록 화살표 누름</em></p>
   </div>
      - 이후 디버거 탭을 누르고, 초록 화살표를 누르면 디버깅이 진행됩니다.
---
2. 중단점을 설정해서 디버깅을 진행할 수 있고,  
디버거 탭을 통해, 변수를 추적하거나, Callstack, BreakPoints확인, 현재 Local 변수를 확인할 수 있다.

   <div align="center">
     <img src="../../images/VSCode_APIServer/debugger_tab_explain.png" alt="디버그 탭 클릭" width="70%">
     <p><em>VS Code에서 디버깅 화면</em></p>
   </div>

   <div align="center">
     <img src="../../images/VSCode_APIServer/debug_settings.gif" alt="디버그 탭 클릭" width="70%">
     <p><em>VS Code에서 중단점 설정 및 변수 추적 설정</em></p>
   </div>

   <div align="center">
     <img src="../../images/VSCode_APIServer/debug.gif" alt="디버그 탭 클릭" width="70%">
     <p><em>VS Code에서 디버깅</em></p>
   </div>





---
3. 디버거를 설정했다면, `.vscode` 폴더에 `launch.json` 파일이 자동으로 생성됩니다. 이 폴더는 VS Code의 프로젝트별 설정을 저장하는 곳입니다. 없다면 수동으로 생성할 수 있습니다:

### 파일설명 
`.vscode/launch.json` 파일: (디버깅 구성을 정의하는 파일로, 디버깅 시작 시 어떻게 실행할지 설정합니다)

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch (web)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/bin/Debug/net9.0/GameAPIServer.dll",
            "args": [],
            "cwd": "${workspaceFolder}",
            "stopAtEntry": false,
            "serverReadyAction": {
                "action": "openExternally",
                "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
            },
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            },
            "sourceFileMap": {
                "/Views": "${workspaceFolder}/Views"
            }
        },
        {
            "name": ".NET Core Attach",
            "type": "coreclr",
            "request": "attach"
        }
    ]
}
```

launch.json 주요 설정 항목:
- version: 구성 파일의 버전을 지정 (현재 버전은 0.2.0)
- **configurations**: 디버깅 구성 목록을 정의하는 배열
  - **name**: 디버그 구성의 이름 (VS Code UI에 표시됨)
  - **type**: 사용할 디버거 타입 (coreclr은 .NET Core용 디버거)
  - **request**: 디버깅 요청 유형 (`launch`는 새로 시작, `attach`는 실행 중인 프로세스에 연결)
  - **preLaunchTask**: 디버깅 전에 실행할 작업 (tasks.json에 정의된 작업명, 보통 build) 
  - **program**: 실행할 프로그램의 경로 (빌드된 DLL 파일 위치)
  - **args**: 프로그램 실행 시 전달할 명령줄 인수 배열
  - cwd: 프로그램 실행의 작업 디렉토리 경로
  - stopAtEntry: 프로그램 시작 지점에서 즉시 중단점 설정 여부 (true/false)
  - **serverReadyAction**: 서버가 준비되었을 때 취할 동작
    - action: 수행할 동작 (예: openExternally는 외부 브라우저 열기)
    - pattern: 서버 준비 상태를 감지할 정규식 패턴
  - **env**: 디버깅 세션에 적용할 환경 변수 (키-값 쌍) 
  - sourceFileMap: 소스 파일 경로 매핑 (원격 디버깅 시 유용)

---

`.vscode/tasks.json` 파일: (VS Code가 실행할 작업들을 정의하는 파일로, 빌드, 게시 등의 명령을 설정합니다)

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "${workspaceFolder}/GameAPIServer.csproj",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary;ForceNoAlign"
            ],
            "problemMatcher": "$msCompile"
        },
        {
            "label": "publish",
            "command": "dotnet",
            "type": "process",
            "args": [
                "publish",
                "${workspaceFolder}/GameAPIServer.csproj",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary;ForceNoAlign"
            ],
            "problemMatcher": "$msCompile"
        },
        {
            "label": "watch",
            "command": "dotnet",
            "type": "process",
            "args": [
                "watch",
                "run",
                "--project",
                "${workspaceFolder}/GameAPIServer.csproj"
            ],
            "problemMatcher": "$msCompile"
        }
    ]
}
```

tasks.json 주요 설정 항목:
- version: 작업 정의 파일의 버전 (현재 버전은 2.0.0)
- **tasks**: 정의된 작업 목록을 포함하는 배열 
  - **label**: 작업의 이름 (VS Code UI에 표시되고 launch.json에서 참조됨) 
  - **command**: 실행할 명령어 (예: dotnet) 
  - type: 작업 실행 방식 (process는 새 프로세스로 실행)
  - **args**: 명령에 전달할 인수 배열 
    - **build**: 빌드 명령어
    - **${workspaceFolder}/GameAPIServer.csproj**: 대상 프로젝트 파일 


주요 작업 설명:
- **build**: dotnet build 명령을 실행하여 프로젝트를 컴파일 (주로 사용) 


### REST Client를 사용한 테스트

REST Client 확장을 설치했다면, `.http` 파일을 생성하여 API 엔드포인트를 테스트할 수 있습니다:

1. 프로젝트 폴더에 `GameAPIServer.http` 파일을 생성하고 다음 내용을 추가합니다:

```
@host = http://localhost:5000

### 기본 API 테스트
GET {{host}}/weatherforecast
```

2. REST Client확장을 설치했다면, `Send Request`버튼이 활성화 됩니다.  
버튼을 클릭하여 요청을 실행합니다.

   <div align="center">
     <img src="../../images/VSCode_APIServer/http_test.png" alt="http테스트" width="70%">
     <p><em>REST Client를 이용한 http 테스트</em></p>
   </div>





================================================
File: guides/ai_coding_tools_guide.md
================================================
# AI 코딩 도구를 활용한 ASP.NET Core API 게임 서버 학습 가이드

이 문서는 Claude Code, OpenAI Codex(ChatGPT), Gemini CLI 등의 AI 코딩 도구를 활용하여 본 저장소의 학습 자료를 효과적으로 학습하고, API 게임 서버를 구현하는 방법을 안내한다.

---

## 1. 도구별 특성과 설치

### 1.1 Claude Code (Anthropic)

CLI 기반 에이전트. 프로젝트 디렉토리에서 직접 코드를 읽고, 편집하고, 빌드까지 수행한다.

```bash
# 설치
npm install -g @anthropic-ai/claude-code

# 프로젝트 디렉토리에서 실행
cd edu_Learn_ASPNetCore_APIServer-main
claude
```

**장점**: 파일 탐색/편집/빌드를 대화 안에서 바로 수행. CLAUDE.md를 읽어 프로젝트 맥락을 자동 파악.
**적합한 작업**: 코드 분석, 리팩토링, 새 기능 구현, 빌드 오류 수정, 프로젝트 전반 질문.

### 1.2 ChatGPT / OpenAI Codex

웹 또는 API 기반. 코드 조각을 붙여넣거나 설명을 요청하는 방식.

- **ChatGPT**: https://chat.openai.com (코드 해석, 설계 질문, 개념 설명)
- **GitHub Copilot**: VS Code/JetBrains 확장 (자동 완성, 인라인 코드 생성)
- **Codex CLI**: `npm install -g @openai/codex` (터미널 기반, 2025년 출시)

**장점**: 범용 질문, 개념 비교, 다양한 언어 지원, Copilot의 실시간 자동 완성.
**적합한 작업**: 개념 학습, 코드 리뷰, 알고리즘 설명, 테스트 코드 생성.

### 1.3 Gemini CLI (Google)

터미널 기반 AI 코딩 도구. 로컬 파일 접근과 명령 실행이 가능하다.

```bash
# 설치
npm install -g @anthropic-ai/claude-code  # Claude Code
npm install -g @google/gemini-cli         # Gemini CLI

# 프로젝트 디렉토리에서 실행
cd edu_Learn_ASPNetCore_APIServer-main
gemini
```

**장점**: Google 생태계 통합, 대용량 컨텍스트, 무료 사용 가능.
**적합한 작업**: 코드 분석, 문서 생성, 대규모 코드베이스 탐색.

### 1.4 도구 선택 가이드

| 상황 | 추천 도구 |
|------|----------|
| 프로젝트 코드 분석/수정 | Claude Code, Gemini CLI |
| 개념 학습/설계 질문 | ChatGPT, Claude Code |
| 코드 작성 중 자동완성 | GitHub Copilot |
| 빌드 오류 디버깅 | Claude Code, Gemini CLI |
| 코드 리뷰 | ChatGPT, Claude Code |
| DB 스키마/SQL 작성 | ChatGPT, Claude Code |

> **팁**: 도구 하나에 의존하지 말고 상황에 따라 병행 사용한다. 예를 들어 Claude Code로 코드를 수정하고, ChatGPT에서 설계 의견을 구하는 식으로 활용한다.

---

## 2. 학습 단계별 활용법

### 2.1 1단계: 프로젝트 구조 파악

저장소를 클론한 후 AI 도구로 전체 구조를 파악한다.

#### Claude Code / Gemini CLI에서

```
> 이 저장소의 전체 구조를 설명해줘. 어떤 프로젝트들이 있고 각각 무엇을 하는지 알려줘.
> codes/GameAPIServer_Template의 Program.cs를 분석해서 DI 등록, 미들웨어, 엔드포인트를 정리해줘.
> codes/GameAPIServer_Template과 codes/practice_MiniGameHeavenAPIServer의 구조 차이를 비교해줘.
```

#### ChatGPT에서

Program.cs 코드를 붙여넣고:
```
아래 ASP.NET Core Program.cs 코드를 분석해줘.
서비스 등록 순서, 미들웨어 파이프라인, 설정 로드 방식을 설명해줘.
[코드 붙여넣기]
```

#### 학습 포인트
- `codes/GameAPIServer_Template/README.md` — 템플릿 구조 파악
- `docs/references/APIServer_Directory.md` — 표준 디렉토리 컨벤션 이해
- `coding_rule.md` — 코딩 규칙 숙지

---

### 2.2 2단계: 핵심 개념 학습

AI 도구에 구체적인 질문을 던져 개념을 학습한다.

#### DI (의존성 주입)

```
> docs/patterns/why_di.md를 읽고, 이 문서에서 설명하는 DI 패턴을
  GameAPIServer_Template의 Program.cs에서 어떻게 적용했는지 예시와 함께 설명해줘.
```

#### 미들웨어

```
> codes/GameAPIServer_Template/Middleware/ 안의 파일들을 읽고,
  요청이 들어올 때 VersionCheck → CheckUserAuthAndLoadUserData → Controller 순서로
  처리되는 흐름을 단계별로 설명해줘.
```

#### Redis Cache-Aside 패턴

```
> docs/patterns/Cache-Aside_pattern.md를 읽고,
  codes/api_server_training_dungeon_farming에서 Redis를 실제로 어떻게 사용하는지 코드로 보여줘.
```

#### DB 트랜잭션

```
> docs/guides/how_to_db_transaction.md를 읽고,
  SqlKata에서 트랜잭션을 처리하는 코드 패턴을 설명해줘.
```

---

### 2.3 3단계: 참고 프로젝트 분석

완성된 프로젝트를 AI와 함께 깊이 분석한다.

#### 추천 분석 순서

1. **GameAPIServer_Template** — 기본 구조
2. **practice_MiniGameHeavenAPIServer** — 가장 완성도 높은 프로젝트
3. **api_server_training_dungeon_farming** — Redis + 마스터 데이터 고급 패턴
4. **practice_omok_game-1 / omok_game-2** — 롱폴링 실시간 게임

#### 효과적인 분석 프롬프트

```
> codes/practice_MiniGameHeavenAPIServer/APIServer/Controllers/ 안의 컨트롤러들을 분류해줘.
  인증, 유저, 아이템, 우편, 출석, 친구, 미니게임 카테고리별로 정리하고
  각 컨트롤러가 어떤 서비스를 호출하는지 매핑해줘.

> codes/practice_omok_game-1/SequenceDiagram/ 의 다이어그램을 읽고,
  롱폴링으로 실시간 게임을 구현하는 방식을 설명해줘.
  일반적인 WebSocket 방식과 비교하면 장단점이 뭐야?

> codes/api_server_training_dungeon_farming의 ErrorCode.cs를 분석해서
  에러 코드 체계(범위별 분류, 네이밍 규칙)를 설명해줘.
```

---

### 2.4 4단계: 직접 API 게임 서버 만들기

#### 프로젝트 생성

```bash
# CLI에서 새 프로젝트 생성
dotnet new webapi -n MyGameServer
cd MyGameServer
```

AI 코딩 도구(Claude Code 또는 Gemini CLI)를 프로젝트 디렉토리에서 실행:

```
> GameAPIServer_Template의 구조를 참고해서 이 프로젝트에 다음을 세팅해줘:
  1. Controllers, Services/Interfaces, Repository/Interfaces, Middleware, DTOs, Models 디렉토리 생성
  2. ErrorCode.cs 생성 (UInt16 enum, 범위별 카테고리)
  3. appsettings.json에 DbConfig (Redis, GameDb, MasterDb) 섹션 추가
  4. Program.cs에 DI 등록 구조 세팅
```

#### 기능 구현 (단계적으로)

AI에게 한 번에 모든 것을 요구하지 말고, **한 기능씩** 구현한다.

```
> 1단계: CreateAccount API를 만들어줘.
  - POST /CreateAccount
  - 요청: { "UserID": string, "Password": string }
  - 비밀번호는 SHA256 + salt로 해싱
  - AccountDb에 저장
  - GameAPIServer_Template의 AuthService 패턴을 참고해줘.

> 2단계: Login API를 만들어줘.
  - POST /Login
  - 비밀번호 검증 후 Redis에 인증 토큰 저장
  - 응답에 uid, token 포함
  - MemoryDb의 토큰 관리 패턴을 참고해줘.

> 3단계: 인증 미들웨어를 만들어줘.
  - 헤더에서 uid, token을 읽어 Redis에서 검증
  - Login, CreateAccount는 건너뜀
  - 요청 중복 방지를 위한 락 처리 포함
  - GameAPIServer_Template/Middleware/CheckUserAuthAndLoadUserData.cs를 참고해줘.
```

#### Redis 실습

`redis_training/README.md`의 챌린지를 AI와 함께 풀어본다.

```
> redis_training/README.md를 읽고, 1번 과제(로그인 시 인증키 저장)를 구현해줘.
  CloudStructures 라이브러리를 사용하고, API 엔드포인트로 만들어줘.

> 5번 과제(Rate Limiting: 2분에 3번만 요청 가능)를 Redis로 구현해줘.
  Sorted Set을 사용하는 방식과 String + TTL을 사용하는 방식 두 가지를 비교해줘.
```

---

### 2.5 5단계: 코드 리뷰와 개선

작성한 코드를 AI에게 리뷰 요청한다.

```
> 내가 작성한 LoginController.cs를 리뷰해줘.
  coding_rule.md의 코딩 규칙을 기준으로 네이밍, 구조, 보안 문제를 체크해줘.

> 내 GameDb 클래스의 SqlKata 쿼리를 보고 SQL 인젝션 위험이 있는지,
  트랜잭션 처리가 올바른지 확인해줘.

> 현재 내 서비스 코드에서 예외 처리가 부족한 부분을 찾아줘.
  ErrorCode enum과 연계해서 적절한 에러 코드를 반환하도록 개선해줘.
```

---

## 3. 실전 워크플로우

### 3.1 학습 세션 예시 (2시간)

```
[0:00 - 0:20] 개념 학습
  → AI에게 오늘 학습할 주제(예: Redis Cache-Aside) 질문
  → docs/patterns/ 문서를 함께 읽으며 이해

[0:20 - 0:50] 참고 코드 분석
  → AI와 함께 관련 프로젝트 코드를 분석
  → "이 부분은 왜 이렇게 구현했어?" 식의 질문

[0:50 - 1:40] 직접 구현
  → AI에게 구현 방향을 설명하고, 코드 생성 요청
  → 생성된 코드를 이해한 후 수정/개선
  → dotnet build로 빌드 확인

[1:40 - 2:00] 복습 및 정리
  → AI에게 오늘 구현한 코드 리뷰 요청
  → 개선점 반영, 커밋
```

### 3.2 기능 구현 워크플로우

```
1. 기획    → AI에게 "우편 시스템을 만들려는데 DB 스키마를 어떻게 설계하면 좋을까?"
2. 설계    → AI에게 시퀀스 다이어그램 생성 요청 (Mermaid 문법)
3. 스키마  → AI에게 SQL CREATE TABLE 문 생성 요청
4. 코드    → Repository → Service → Controller 순서로 한 레이어씩 구현
5. 테스트  → AI에게 apiTest.http 테스트 케이스 생성 요청
6. 리뷰    → AI에게 전체 코드 리뷰 요청
```

### 3.3 디버깅 워크플로우

```
# 빌드 오류 시
> dotnet build 결과 아래 오류가 발생했어. 원인과 해결 방법을 알려줘.
  [오류 메시지 붙여넣기]

# 런타임 오류 시
> API 호출 시 500 에러가 발생해. 로그는 다음과 같아:
  [로그 붙여넣기]
  원인을 분석하고 수정 코드를 제안해줘.

# 성능 문제 시 (CLI 도구)
> 이 컨트롤러의 응답이 느린데, DB 쿼리와 Redis 호출을 분석해서
  병목 지점을 찾아줘.
```

---

## 4. 효과적인 프롬프트 작성법

### 4.1 좋은 프롬프트의 구조

```
[맥락] + [구체적 요청] + [참고 자료] + [제약 조건]
```

#### 나쁜 예
```
우편 기능 만들어줘.
```

#### 좋은 예
```
우편 수령 API를 만들어줘.
- POST /MailReceive, 요청 본문에 MailSeq (long) 포함
- 우편의 아이템을 유저 인벤토리에 추가하고, 우편 상태를 "수령 완료"로 변경
- 트랜잭션으로 두 작업을 묶어야 해
- codes/GameAPIServer_Template/Services/MailService.cs의 패턴을 참고해줘
- ErrorCode는 기존 ErrorCode.cs의 8000번대를 사용해줘
```

### 4.2 단계별 요청

AI에게 한 번에 모든 것을 요청하면 품질이 떨어진다. 레이어별로 나눠서 요청한다.

```
1차: "우편 수령의 DB 스키마와 Repository 메서드를 만들어줘"
2차: "Repository를 사용하는 MailService.ReceiveMail 메서드를 만들어줘"
3차: "MailReceiveController를 만들어줘"
4차: "apiTest.http에 테스트 요청을 추가해줘"
```

### 4.3 AI 응답을 그대로 쓰지 않기

AI가 생성한 코드를 반드시 이해한 후 사용한다.

- **이해 확인**: "이 코드에서 `SetAsync`의 `When.NotExists` 옵션은 무슨 역할이야?"
- **대안 비교**: "이 구현 말고 다른 방법은 없어? 장단점을 비교해줘"
- **코딩 규칙 준수**: coding_rule.md 기준으로 네이밍/스타일 직접 수정

---

## 5. 도구별 CLAUDE.md / 설정 활용

### 5.1 Claude Code — CLAUDE.md

프로젝트 루트의 `CLAUDE.md`에 프로젝트 정보를 작성하면 Claude Code가 자동으로 읽어 맥락을 파악한다. 본 저장소에는 이미 작성되어 있다.

자신의 프로젝트에서도 CLAUDE.md를 작성하면 효과적이다:

```markdown
# MyGameServer
## 기술 스택
- .NET 8.0 / ASP.NET Core Web API
- MySQL + SqlKata, Redis + CloudStructures, ZLogger
## 코딩 규칙
- coding_rule.md 준수
- 디렉토리: Controllers, Services, Repository, Middleware, DTOs, Models
## 현재 작업
- 우편 시스템 구현 중
```

### 5.2 Gemini CLI — GEMINI.md

Gemini CLI도 프로젝트 루트의 `GEMINI.md`를 참조한다. CLAUDE.md와 유사한 형식으로 작성한다.

### 5.3 GitHub Copilot — .github/copilot-instructions.md

Copilot Custom Instructions를 설정하면 자동 완성 품질이 향상된다:

```markdown
- C# / ASP.NET Core Web API 프로젝트
- dotnet/runtime 코딩 스타일 (PascalCase 클래스/메서드, _camelCase 필드)
- SqlKata 쿼리 빌더 사용 (EF Core 미사용)
- CloudStructures Redis 라이브러리 사용
- ZLogger 구조화 로깅
```

---

## 6. 주의사항

### 6.1 AI 코드의 한계

- **보안**: AI가 생성한 SQL 쿼리에 인젝션 취약점이 있을 수 있다. SqlKata의 파라미터 바인딩을 반드시 사용한다.
- **패키지 버전**: AI가 오래된 API를 제안할 수 있다. 본 저장소의 패키지 버전(ZLogger 2.4.1, CloudStructures 3.3.0 등)을 기준으로 확인한다.
- **할루시네이션**: 존재하지 않는 메서드나 클래스를 제안할 수 있다. `dotnet build`로 반드시 빌드 확인한다.
- **비즈니스 로직**: AI는 게임 기획 의도를 모른다. 기획 요구사항은 직접 판단한다.

### 6.2 학습 효과를 높이려면

- **복사-붙여넣기 금지**: AI가 생성한 코드를 그대로 복사하지 말고, 이해한 후 직접 타이핑한다.
- **"왜?"를 물어라**: 코드가 동작하는 것에 만족하지 말고 "왜 이렇게 구현했는지" 질문한다.
- **다른 도구와 비교**: 같은 질문을 Claude Code와 ChatGPT에 각각 던져보고 답변을 비교한다.
- **오류를 직접 해결해보기**: 빌드 오류가 나면 바로 AI에게 물어보지 말고, 먼저 5분간 직접 해결을 시도한다.

---

## 7. 학습 경로별 추천 프롬프트 모음

### 초급: ASP.NET Core 입문

```
> ASP.NET Core의 미들웨어 파이프라인이 뭔지 쉽게 설명해줘. 
  HTTP 요청이 들어왔을 때 어떤 순서로 처리되는지 그림으로 보여줘.

> DI(의존성 주입)에서 AddTransient, AddScoped, AddSingleton의 차이를 
  게임 서버 예시로 설명해줘. 각각 언제 쓰는 게 적절해?

> codes/GameAPIServer_Template/Program.cs를 한 줄씩 설명해줘.
  각 서비스 등록이 왜 Transient인지 Singleton인지 이유도 알려줘.
```

### 중급: DB + Redis 연동

```
> SqlKata로 MySQL INSERT, SELECT, UPDATE, DELETE 쿼리를 작성하는 
  기본 패턴을 보여줘. 파라미터 바인딩도 포함해서.

> CloudStructures로 Redis String, Hash, SortedSet을 다루는 기본 코드를 보여줘.
  각 자료형은 게임 서버에서 어떤 용도로 쓰여?

> redis_training/README.md의 11번 과제(랭킹 시스템)를 구현해줘.
  Top 10 조회, 내 순위 조회, 내 순위 ±2명 조회를 각각 API로 만들어줘.
```

### 고급: 실시간 게임 + 성능

```
> 롱폴링으로 턴제 게임(오목)을 구현하는 원리를 설명해줘.
  codes/practice_omok_game-1의 구현을 분석해서 알려줘.

> 게임 서버에서 동시 요청을 처리할 때 Redis 락을 어떻게 구현해?
  분산 락의 원리와 codes/GameAPIServer_Template의 구현을 비교해줘.

> 내 API 서버의 부하 테스트를 위한 시나리오를 설계해줘.
  로그인 → 게임 데이터 로드 → 아이템 사용 → 우편 확인 순서로.
```



================================================
File: guides/appsettings_environment.md
================================================
# Development, Production 용 appsettings.json 사용하기

## 실행 시 환경 선택 방법

### 로컬 (CLI)

* 개발:

  ```bash
  ASPNETCORE_ENVIRONMENT=Development dotnet run
  ```
* 프로덕션(리릴리스 빌드 예시):

  ```bash
  ASPNETCORE_ENVIRONMENT=Production dotnet publish -c Release
  ASPNETCORE_ENVIRONMENT=Production dotnet ./bin/Release/net8.0/MyApp.dll
  ```

Windows PowerShell에서는:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
```

> `DOTNET_ENVIRONMENT`를 써도 동일하게 동작한다.


### launchSettings.json 프로필

개발 시 프로필별로 미리 고정할 수 있다.

```json
{
  "profiles": {
    "MyApp (Dev)": {
      "commandName": "Project",
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" }
    },
    "MyApp (Prod)": {
      "commandName": "Project",
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Production" }
    }
  }
}
```

Visual Studio나 `dotnet run --launch-profile "MyApp (Prod)"`로 선택해서 실행한다.


### Linux systemd

`/etc/systemd/system/myapp.service`:

```ini
[Service]
Environment=ASPNETCORE_ENVIRONMENT=Production
ExecStart=/usr/bin/dotnet /var/www/myapp/MyApp.dll
```

`sudo systemctl daemon-reload && sudo systemctl restart myapp`로 적용한다.
  
  
### IIS (Windows)
환경 변수는 **시스템 환경 변수**로 설정하거나, 애플리케이션 풀의 “환경 변수”에 `ASPNETCORE_ENVIRONMENT=Production`을 추가한다.
  
  
### Docker / docker-compose

```yaml
services:
  web:
    image: myapp:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
```
  
  
### Kubernetes (Deployment)

```yaml
env:
  - name: ASPNETCORE_ENVIRONMENT
    value: "Production"
```
  
  
### Azure App Service

구성(설정) → **애플리케이션 설정**에 `ASPNETCORE_ENVIRONMENT=Production`을 추가한다.

  
  
## 환경별 코드 분기(선택)
환경에 따라 코드 흐름을 바꾸고 싶을 때:

```csharp
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
```

  
## 자주 하는 실수와 체크리스트

* `appsettings.Production.json`을 만들고도 환경 변수를 안 바꿔서 적용이 안 되는 경우가 많다 → **프로세스 시작 시점에 환경 변수가 설정되어 있어야 한다**.
* 민감한 값(비밀번호, 연결 문자열)은 환경 파일 대신 **환경 변수**나 **비밀 관리자/Key Vault**를 권장한다.
* 라이브로 환경을 바꾸는 기능은 없다 → **환경 변경 시 재시작**이 필요하다.
* 환경 이름 오타 주의(`production` 대신 `Production`) → 대소문자 구분은 플랫폼마다 다르지만, **표준 표기**를 쓰는 게 안전하다.


## 요약
* 서버 시작 시 **`ASPNETCORE_ENVIRONMENT`(또는 `DOTNET_ENVIRONMENT`)를 `Development`/`Production`으로 지정**해서 원하는 버전을 선택하면 된다.
* 그에 맞춰 `appsettings.{Environment}.json`이 자동 로드되고, 코드에서도 `app.Environment.IsDevelopment()` 등으로 분기하면 된다.



================================================
File: guides/aspnetcore_basics.md
================================================
# ASP.NET Core 기초 개념

게임 API 서버를 이해하기 위해 알아야 할 ASP.NET Core의 3가지 핵심 개념을 설명한다.

---

## 1. 미들웨어 파이프라인

### 미들웨어란?

HTTP 요청이 컨트롤러에 도달하기 전에 **순서대로 통과하는 처리 단계**를 미들웨어라고 한다. 각 미들웨어는 요청을 검사하고, 다음 단계로 넘기거나 즉시 응답을 반환할 수 있다.

```
클라이언트 요청
    │
    ▼
┌──────────────────┐
│  VersionCheck    │  ← 앱 버전이 맞는지 확인. 틀리면 여기서 에러 응답.
└────────┬─────────┘
         │ (통과)
┌────────▼─────────┐
│  CheckUserAuth   │  ← 인증 토큰이 유효한지 확인. 틀리면 여기서 에러 응답.
└────────┬─────────┘
         │ (통과)
┌────────▼─────────┐
│   Controller     │  ← 비즈니스 로직 실행, 응답 생성
└────────┬─────────┘
         │
    ▼ 응답 반환
```

### Program.cs에서의 등록 순서

```csharp
// 미들웨어는 등록 순서대로 실행된다 (순서가 중요!)
app.UseMiddleware<VersionCheck>();                    // 1번째: 버전 체크
app.UseMiddleware<CheckUserAuthAndLoadUserData>();    // 2번째: 인증 + 유저 데이터 로드
app.UseRouting();                                     // 3번째: 라우팅
app.MapDefaultControllerRoute();                      // 4번째: 컨트롤러 매핑
```

> **참고 코드:** `codes/GameAPIServer_Template/Middleware/VersionCheck.cs`, `CheckUserAuth.cs`

### 미들웨어의 기본 구조

```csharp
public class VersionCheck
{
    readonly RequestDelegate _next;  // 다음 미들웨어를 호출하는 델리게이트

    public VersionCheck(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // 요청 검사 로직
        if (버전이_맞지_않으면)
        {
            context.Response.StatusCode = 426;
            return;  // 여기서 끊김 — 다음 미들웨어로 넘어가지 않음
        }

        await _next(context);  // 다음 미들웨어 호출
    }
}
```

### 게임 서버에서 자주 사용하는 미들웨어

| 미들웨어 | 역할 | 예시 |
|:---|:---|:---|
| **VersionCheck** | 클라이언트 앱 버전 확인 | 구버전 클라이언트 접속 차단 |
| **CheckUserAuth** | 인증 토큰 검증 | Redis에서 토큰 유효성 확인 |
| **RequestLock** | 동일 유저의 동시 요청 방지 | Redis 분산 락 |
| **UseHttpMetrics** | Prometheus 메트릭 수집 | 요청 수, 응답 시간 자동 기록 |

---

## 2. DI (의존성 주입)

### DI란?

클래스가 필요로 하는 객체(의존성)를 **직접 생성하지 않고, 외부에서 주입받는** 패턴이다.

```csharp
// ❌ DI 없이 (직접 생성) — 테스트/교체 어려움
public class LoginController
{
    readonly GameDb _db = new GameDb("connection-string");  // 직접 생성
}

// ✅ DI 사용 — 인터페이스로 받으므로 구현체 교체 가능
public class LoginController
{
    readonly IGameDb _db;

    public LoginController(IGameDb db)  // 생성자에서 주입받음
    {
        _db = db;
    }
}
```

### Program.cs에서의 DI 등록

```csharp
// Repository 계층 (DB 접근)
builder.Services.AddTransient<IGameDb, GameDb>();       // 요청마다 새 인스턴스
builder.Services.AddSingleton<IMemoryDb, MemoryDb>();   // 앱 전체에서 1개 공유
builder.Services.AddSingleton<IMasterDb, MasterDb>();   // 앱 전체에서 1개 공유

// Service 계층 (비즈니스 로직)
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IGameService, GameService>();
```

### 3가지 수명 주기

| 수명 | 메서드 | 언제 사용 | 게임 서버 예시 |
|:---|:---|:---|:---|
| **Transient** | `AddTransient` | 요청마다 새 인스턴스 | GameDb (DB 커넥션), Service 클래스 |
| **Singleton** | `AddSingleton` | 앱 전체에서 1개만 | Redis 연결(MemoryDb), 마스터 데이터 |
| **Scoped** | `AddScoped` | HTTP 요청 1개 동안 1개 | 트랜잭션이 필요한 DB 컨텍스트 |

> **핵심 규칙:** DB 연결처럼 상태가 있는 것은 `Transient`나 `Scoped`, Redis나 마스터 데이터처럼 공유해야 하는 것은 `Singleton`.

> **참고 코드:** `codes/GameAPIServer_Template/Program.cs`

---

## 3. 라우팅

### 라우팅이란?

HTTP 요청의 URL을 보고 **어떤 컨트롤러의 어떤 메서드를 실행할지** 결정하는 것이다.

### 어트리뷰트 라우팅

본 저장소의 모든 프로젝트는 어트리뷰트 라우팅을 사용한다.

```csharp
[ApiController]
[Route("[controller]")]          // URL: /Login
public class LoginController : ControllerBase
{
    [HttpPost]                    // POST /Login
    public async Task<LoginResponse> Login(LoginRequest request)
    {
        // ...
    }
}
```

| 어트리뷰트 | 의미 |
|:---|:---|
| `[Route("[controller]")]` | 클래스 이름에서 "Controller"를 뺀 것이 URL이 됨 (`LoginController` → `/Login`) |
| `[HttpPost]` | HTTP POST 메서드에 매핑 |
| `[HttpGet("detail")]` | GET /Login/detail 에 매핑 |
| `[FromBody]` | 요청 본문(JSON)을 파라미터로 바인딩 |

### 요청/응답 흐름

```
클라이언트:
  POST /Login
  Content-Type: application/json
  { "UserID": "test", "Password": "123" }

          ↓ ASP.NET Core가 자동으로:

  1) URL "/Login"  → LoginController 선택
  2) HTTP "POST"   → Login() 메서드 선택
  3) Body JSON     → LoginRequest 객체로 역직렬화
  4) 메서드 실행   → LoginResponse 객체 반환
  5) 응답 JSON     → { "Result": 0, "AuthToken": "abc..." }
```

---

## 다음 단계

이 기초 개념을 이해했다면:

1. `codes/GameAPIServer_Template/Program.cs`를 읽고 전체 구조를 파악한다
2. `docs/guides/project_structure.md`에서 Controller → Service → Repository 패턴을 학습한다
3. `docs/patterns/why_di.md`에서 DI를 더 깊이 이해한다



================================================
File: guides/how_to_db_transaction.md
================================================
# MySQL 라이브러리에서 지원하는 트랜잭션 기능 사용하기
코드에서 트랜잭션 기능을 사용할 때는 아래 코드처럼 사용하면 된다   
```
public async Task<ErrorCode> AttendanceCheck(long playerUid)
{
    var lastAttendanceDate = await _gameDb.GetRecentAttendanceDate(playerUid);

    if (lastAttendanceDate.HasValue && lastAttendanceDate.Value.Date == DateTime.Today)
    {
        return ErrorCode.AttendanceCheckFailAlreadyChecked;
    }

    // 트랜잭션 시작
    var result = await _gameDb.ExecuteTransaction(async transaction =>
    {
        return await UpdateAttendanceInfoAndGiveReward(playerUid, transaction);
    });

    if (!result)
    {
        return ErrorCode.AttendanceCheckFailException;
    }

    return ErrorCode.None;
}

private async Task<bool> UpdateAttendanceInfoAndGiveReward(long playerUid, MySqlTransaction transaction)
{
    var updateResult = await _gameDb.UpdateAttendanceInfo(playerUid, transaction);
    if (!updateResult)
    {
        return false;
    }

    var attendanceCount = await _gameDb.GetTodayAttendanceCount(playerUid, transaction);
    if (attendanceCount == -1)
    {
        return false;
    }

    var rewardResult = await _gameDb.AddAttendanceRewardToMailbox(playerUid, attendanceCount, transaction);
    if (!rewardResult)
    {
        return false;
    }

    return true;
}



classs GameDb
{
	MySqlConnection _connection;
	readonly QueryFactory _queryFactory;


	public async Task<bool> ExecuteTransaction(Func<MySqlTransaction, Task<bool>> operation)
	{
		using var transaction = await _connection.BeginTransactionAsync();
		try
		{
			var result = await operation(transaction);
			if (result)
			{
				await transaction.CommitAsync();
				return true;
			}
			else
			{
				await transaction.RollbackAsync();
				return false;
			}
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			_logger.LogError(ex, "Transaction failed");
			return false;
		}
	}

	public async Task<bool> UpdateAttendanceInfo(long playerUid, MySqlTransaction transaction)
	{
		var updateCountResult = await _queryFactory.Query("attendance")
		.Where("player_uid", playerUid)
		.IncrementAsync("attendance_cnt", 1, transaction);

		var updateDateResult = await _queryFactory.Query("attendance")
			.Where("player_uid", playerUid)
			.UpdateAsync(new
			{
				recent_attendance_dt = DateTime.Now
			}, transaction);

		return updateCountResult > 0 && updateDateResult > 0;
	}
}
```

```
class GameDb
{
	MySqlConnection _connection;

	public async Task<PlayerInfo> CreatePlayerInfoDataAndStartItems(string playerId)
	{
		using var transaction = await _connection.BeginTransactionAsync();
		try
		{
			var newPlayerInfo = new PlayerInfo
			{
				PlayerId = playerId,
				NickName = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 27),
				Exp = 0,
				Level = 1,
				Win = 0,
				Lose = 0,
				Draw = 0
			};

			var insertId = await _queryFactory.Query("player_info").InsertGetIdAsync<int>(new
			{
				player_id = newPlayerInfo.PlayerId,
				nickname = newPlayerInfo.NickName,
				exp = newPlayerInfo.Exp,
				level = newPlayerInfo.Level,
				win = newPlayerInfo.Win,
				lose = newPlayerInfo.Lose,
				draw = newPlayerInfo.Draw
			}, transaction);

			newPlayerInfo.PlayerUid = insertId;

			var addItemsResult = await AddFirstItemsForPlayer(newPlayerInfo.PlayerUid, transaction);
			if (addItemsResult != ErrorCode.None)
			{
				await transaction.RollbackAsync();
				return null;
			}

			var attendanceResult = await CreatePlayerAttendanceInfo(newPlayerInfo.PlayerUid, transaction);
			if (!attendanceResult)
			{
				await transaction.RollbackAsync();
				return null;
			}

			await transaction.CommitAsync();
			return newPlayerInfo;
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			_logger.LogError(ex, "Error creating player info for playerId: {PlayerId}", playerId);
			return null;
		}
	}
}
```   
  
  
# 수동으로 트랜잭션 기능 구현하기
DB에서 제공하는 트랜잭션 기능을 사용하지 않고, 직접 코드로 롤백 기능을 구현한다   
```
public async Task<ErrorCode> Attend(long userId, AttendanceModel attendData, bool usingPass)
{
    var rollbackQueries = new List<SqlKata.Query>();

    // 누적 출석일 수 계산
    var newAttendCount = CalcAttendanceCount(attendData);

    // 최종 출석일 갱신
    var attendResult = await UpdateAttendanceData(userId, newAttendCount, attendData, rollbackQueries);
    if (!Successed(attendResult))
    {
        _logger.ZLogDebugWithPayload(EventIdGenerator.Create(attendResult),
                                     new { UserId = userId, NewAttendCount = newAttendCount }, "Failed");

        return ErrorCode.AttendanceService_UpdateAttendanceData;
    }

    // 보상 아이템 우편으로 지급
    var rewardResult = await SendRewardMail(userId, newAttendCount, usingPass, rollbackQueries);
    if (!Successed(rewardResult))
    {
		// 실패가 발생하면 롤백한다
        await Rollback(rewardResult, rollbackQueries);

        _logger.ZLogDebugWithPayload(EventIdGenerator.Create(rewardResult),
                                     new { UserID = userId, NewAttendCount = newAttendCount }, "Failed");

        return ErrorCode.AttendanceService_SendAttendanceReward;
    }

    return ErrorCode.None;
}

async Task<ErrorCode> UpdateAttendanceData(Int64 userId, Int32 newAttendCount, AttendanceModel attendData, List<SqlKata.Query> queries)
{
    try
    {
        // 누적 출석일수와 최종 출석일 갱신
        var affectedRow = await _gameDb.UpdateAttendanceData(userId, newAttendCount);
        if (!ValidateAffectedRow(affectedRow, 1))
        {
            return ErrorCode.AttendanceService_UpdateAttendanceData_AffectedRowOutOfRange;
        }

        // 성공 시 롤백 쿼리 추가
        var query = _gameDb.GetQuery("user_attendance").Where("UserId", userId)
                           .AsUpdate(new { LastAttendance = attendData.LastAttendance,
                                           AttendanceCount = attendData.AttendanceCount });
        queries.Add(query);

        return ErrorCode.None;
    }
    catch (Exception ex)
    {
        var errorCode = ErrorCode.AttendanceService_UpdateAttendanceData_Fail;

        _logger.ZLogErrorWithPayload(EventIdGenerator.Create(errorCode), ex,
                                     new { UserId = userId,
                                           NewAttendanceCount = newAttendCount },
                                     "Failed");

        return errorCode;
    }
}
```



================================================
File: guides/integration_testing.md
================================================
# 통합테스트
from: ChatGPT  
  
통합테스트(integration test)는 **애플리케이션의 여러 컴포넌트를 실제로 연결해서 전체 흐름이 제대로 동작하는지 검증하는 테스트**다.
즉, 유닛 테스트가 “각각의 단위 로직”을 검증한다면, 통합 테스트는 다음을 확인한다:

* 실제 DB와 잘 통신하는가
* Repository + Service + DB + ORM 흐름이 전체적으로 맞는가
* 트랜잭션이 제대로 동작하는가
* 쿼리가 실제 MySQL에서 의도대로 실행되는가
* 스키마 제약(Unique, FK, 인덱스)이 제대로 적용되는가

즉, **“현실 환경과 최대한 비슷하게 돌려서 전체 기능이 진짜로 돌아가는지”를 확인하는 테스트**다.

---

# 1. 통합테스트가 왜 필요한가?

예를 들어 이런 코드가 있다고 하자:

```csharp
var user = _db.Users.FirstOrDefault(u => u.Name == name);
```

유닛 테스트에서는 Mock으로 Users 리스트를 만들어서 테스트할 수 있다.
하지만 현실에서는 다음 문제들이 발생할 수 있다:

* 실제 MySQL에서는 문자열 비교가 대소문자 구분됨
* 인덱스가 없어서 성능이 느려짐
* 컬럼 타입이 다르면 EF 매핑에서 예외가 발생
* 트랜잭션 스코프가 실제 DB에서 의도와 다르게 동작

이런 문제는 **유닛 테스트로 절대 못 잡고, 통합 테스트로만 잡을 수 있다**.

---

# 2. 통합테스트 구현 방법(ASP.NET Core + xUnit 기준)

ASP.NET Core에서는 기본적으로 다음 두 가지 방식이 많이 사용된다.

---

## 2-1. **실제 DB(MySQL)와 연결해서 테스트**

가장 현실적인 방식이다.

테스트 전용 DB를 하나 만든 뒤:

1. 테스트 시작할 때 DB 초기화
2. 테스트 데이터 Insert
3. 실제 Repository/Service 로직 호출
4. DB에 반영된 상태 또는 반환값 검증
5. 테스트 끝나면 데이터 삭제(또는 테스트용 DB 자체를 초기화)

예: xUnit + EF Core + MySQL

```csharp
public class UserRepositoryTests
{
    private readonly AppDbContext _db;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=test;User=root;Password=1234;",
                      new MySqlServerVersion(new Version(8,0,25)))
            .Options;

        _db = new AppDbContext(options);
    }

    [Fact]
    public async Task AddUser_ShouldInsertIntoDatabase()
    {
        var repo = new UserRepository(_db);

        var user = new User { Name = "test" };
        await repo.Add(user);

        var savedUser = await _db.Users.FirstOrDefaultAsync(u => u.Name == "test");

        Assert.NotNull(savedUser);
    }
}
```

장점

* 실제 환경과 가장 가까워서 신뢰도 높음

단점

* DB 초기화가 필요 → 느림
* 테스트 실행 환경에서 MySQL이 필요

그래서 다음 방법이 자주 쓰인다:

---

## 2-2. **Docker TestContainer 사용**

테스트 실행 시 Docker로 MySQL 컨테이너를 자동 실행하고
테스트 끝나면 자동으로 삭제하는 방식이다.

→ 개발 PC, CI/CD 환경에서 항상 동일한 DB 환경으로 테스트 가능.

간단 예(테스트 컨테이너 라이브러리 사용):

```csharp
public class MySqlIntegrationTest : IAsyncLifetime
{
    private MySqlContainer _container;
    public AppDbContext Db { get; private set; }

    public async Task InitializeAsync()
    {
        _container = new MySqlBuilder()
            .WithImage("mysql:8.0")
            .WithUsername("root")
            .WithPassword("1234")
            .Build();

        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(_container.GetConnectionString(),
                    new MySqlServerVersion("8.0"))
                .Options;

        Db = new AppDbContext(options);
        await Db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

이 방식은 널리 사용되고 유지보수도 편하다.

---

# 3. ASP.NET Core 전체 API 통합 테스트도 가능

Repository 레벨이 아니라 API 전체 흐름도 테스트할 수 있다.

`WebApplicationFactory<TEntryPoint>` 사용:

```csharp
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_users_id_ReturnsOk()
    {
        var res = await _client.GetAsync("/users/1");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

이건 **Controller → Service → Repository → DB** 전체 플로우를 검증한다.

---

# 4. 통합 테스트 구성 요약

| 테스트 타입               | 특징                           | DB 사용            |
| -------------------- | ---------------------------- | ---------------- |
| **유닛 테스트**           | 로직 단위 검증                     | ❌ 없음 (Mock/Fake) |
| **통합 테스트**           | Service/Repository/DB 함께 테스트 | ⭕ 있음             |
| **엔드 투 엔드 테스트(E2E)** | API 전체 흐름 테스트                | ⭕ 있음             |

---

# 5. 실제 프로젝트에서는 보통 이렇게 구성한다

* **유닛 테스트**: 빠르고 로직 중심 (70%)
* **통합 테스트**: 느리지만 정확한 DB 기반 테스트 (20%)
* **E2E 테스트**: 실제 API 검증 (10%)

특히 DB 기반 서비스라면 통합 테스트 비율이 더 높아지는 것도 일반적이다.

---

</br>  
</br>  
  
  
## 1. 실제 MySQL + EF Core 통합 테스트 예제

### 1-1. 예제 도메인

아주 단순하게 **유저가 아이템을 구매하는** 도메인이라고 하자.

```csharp
// Domain
public class User
{
    public long Id { get; set; }
    public int Cash { get; set; }
    public List<UserItem> Items { get; set; } = new();
}

public class UserItem
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public int ItemId { get; set; }
}

public class ShopItem
{
    public int Id { get; set; }
    public int Price { get; set; }
}
```

DbContext는 이렇게 둔다.

```csharp
public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<UserItem> UserItems { get; set; }
    public DbSet<ShopItem> ShopItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasMany(u => u.Items)
            .WithOne()
            .HasForeignKey(i => i.UserId);
    }
}
```

Repository 예제:

```csharp
public interface IUserRepository
{
    Task<User?> GetAsync(long id);
    Task SaveAsync(User user);
}

public class UserRepository : IUserRepository
{
    private readonly GameDbContext _db;

    public UserRepository(GameDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetAsync(long id)
    {
        return _db.Users
            .Include(u => u.Items)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task SaveAsync(User user)
    {
        _db.Update(user);
        await _db.SaveChangesAsync();
    }
}
```

Service 예제:

```csharp
public class PurchaseService
{
    private readonly GameDbContext _db;
    private readonly IUserRepository _users;

    public PurchaseService(GameDbContext db, IUserRepository users)
    {
        _db = db;
        _users = users;
    }

    public async Task<bool> PurchaseAsync(long userId, int itemId)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        var user = await _users.GetAsync(userId);
        var item = await _db.ShopItems.FindAsync(itemId);

        if (user == null || item == null)
            return false;

        if (user.Cash < item.Price)
            return false;

        user.Cash -= item.Price;
        user.Items.Add(new UserItem { ItemId = item.Id });

        await _users.SaveAsync(user);
        await tx.CommitAsync();
        return true;
    }
}
```

---

### 1-2. 실제 MySQL 통합 테스트 환경 구성

전제: `test_game` 같은 **테스트 전용 DB**를 만들어둔다.

`appsettings.Test.json` 같은 데에 연결 문자열을 분리해 두거나, 테스트 코드에서 직접 설정해도 된다.

```csharp
// Test의 공통 Fixture
public class MySqlFixture : IAsyncLifetime
{
    public GameDbContext Db { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseMySql(
                "Server=localhost;Port=3306;Database=test_game;User=root;Password=1234;",
                new MySqlServerVersion(new Version(8, 0, 25)))
            .Options;

        Db = new GameDbContext(options);

        // 스키마 없으면 생성
        await Db.Database.EnsureCreatedAsync();

        // 매 테스트 전 초기화가 필요하면 여기서 TRUNCATE 등의 작업을 수행해도 된다
    }

    public Task DisposeAsync()
    {
        Db.Dispose();
        return Task.CompletedTask;
    }
}
```

테스트 클래스:

```csharp
public class PurchaseServiceIntegrationTests : IClassFixture<MySqlFixture>
{
    private readonly GameDbContext _db;

    public PurchaseServiceIntegrationTests(MySqlFixture fixture)
    {
        _db = fixture.Db;
    }

    [Fact]
    public async Task Purchase_Succeeds_And_Persists_To_Database()
    {
        // Arrange: 깨끗한 상태를 위해 기존 데이터 삭제
        _db.Users.RemoveRange(_db.Users);
        _db.ShopItems.RemoveRange(_db.ShopItems);
        await _db.SaveChangesAsync();

        var user = new User { Cash = 1000 };
        var item = new ShopItem { Id = 1, Price = 500 };

        await _db.Users.AddAsync(user);
        await _db.ShopItems.AddAsync(item);
        await _db.SaveChangesAsync();

        var repo = new UserRepository(_db);
        var service = new PurchaseService(_db, repo);

        // Act
        var result = await service.PurchaseAsync(user.Id, item.Id);

        // Assert
        result.Should().BeTrue();

        var saved = await _db.Users
            .Include(u => u.Items)
            .FirstAsync(u => u.Id == user.Id);

        saved.Cash.Should().Be(500);
        saved.Items.Should().Contain(i => i.ItemId == item.Id);
    }
}
```

이렇게 하면 **실제 MySQL + 실제 EF 쿼리 + 실제 트랜잭션**까지 다 검증하는 통합 테스트가 된다.

---

## 2. Docker + Testcontainers 기반 추천 구조

로컬 MySQL 설치 안 하고, 테스트 돌릴 때마다 컨테이너를 띄워서 테스트하는 방식이다.

### 2-1. 필요한 NuGet 패키지

```plaintext
dotnet add package Testcontainers
dotnet add package Testcontainers.MySql
```

### 2-2. MySql 컨테이너 Fixture

```csharp
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

public class MySqlContainerFixture : IAsyncLifetime
{
    public MySqlContainer Container { get; private set; } = null!;
    public GameDbContext Db { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Container = new TestcontainersBuilder<MySqlContainer>()
            .WithDatabase(new MySqlTestcontainerConfiguration
            {
                Database = "test_game",
                Username = "root",
                Password = "1234"
            })
            .WithImage("mysql:8.0")
            .WithCleanUp(true)
            .Build();

        await Container.StartAsync();

        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseMySql(
                Container.GetConnectionString(),
                new MySqlServerVersion(new Version(8, 0, 25)))
            .Options;

        Db = new GameDbContext(options);
        await Db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }
}
```

### 2-3. 통합 테스트

```csharp
public class PurchaseServiceWithContainerTests : IClassFixture<MySqlContainerFixture>
{
    private readonly GameDbContext _db;

    public PurchaseServiceWithContainerTests(MySqlContainerFixture fixture)
    {
        _db = fixture.Db;
    }

    [Fact]
    public async Task Purchase_Works_On_Real_MySql_In_Container()
    {
        _db.Users.RemoveRange(_db.Users);
        _db.ShopItems.RemoveRange(_db.ShopItems);
        await _db.SaveChangesAsync();

        var user = new User { Cash = 1000 };
        var item = new ShopItem { Id = 1, Price = 500 };

        await _db.Users.AddAsync(user);
        await _db.ShopItems.AddAsync(item);
        await _db.SaveChangesAsync();

        var repo = new UserRepository(_db);
        var service = new PurchaseService(_db, repo);

        var result = await service.PurchaseAsync(user.Id, item.Id);

        result.Should().BeTrue();

        var saved = await _db.Users
            .Include(u => u.Items)
            .FirstAsync(u => u.Id == user.Id);

        saved.Cash.Should().Be(500);
        saved.Items.Should().Contain(i => i.ItemId == item.Id);
    }
}
```

이 구조의 장점은 다음과 같다.

* 로컬/CI 어디에서도 **동일한 DB 버전**으로 테스트 가능하다
* DB를 따로 설치/관리할 필요가 없다
* 테스트 종료 시 컨테이너와 데이터가 모두 삭제된다

---

## 3. 게임 서버에서 레포지토리/서비스 통합 테스트를 어떻게 나누나

게임 서버 기준으로 대략 이렇게 레벨을 나누는 게 좋다.

### 3-1. Repository 통합 테스트

목표: **쿼리와 매핑이 제대로 되는지** 확인하는 테스트다.

* `UserRepository.GetByNickname`이 실제로 올바른 WHERE/JOIN을 쓰는지
* `InventoryRepository.GetEquipments`가 실제 DB 구조와 맞는지
* 샤딩 키, 파티션 키가 의도대로 적용되는지
* Lazy/Eager Loading 전략으로 N+1 문제를 피하고 있는지 등

예:

```csharp
public class UserRepositoryIntegrationTests : IClassFixture<MySqlContainerFixture>
{
    private readonly GameDbContext _db;
    private readonly IUserRepository _repo;

    public UserRepositoryIntegrationTests(MySqlContainerFixture fixture)
    {
        _db = fixture.Db;
        _repo = new UserRepository(_db);
    }

    [Fact]
    public async Task GetAsync_Returns_User_With_Items()
    {
        _db.Users.RemoveRange(_db.Users);
        _db.UserItems.RemoveRange(_db.UserItems);
        await _db.SaveChangesAsync();

        var user = new User { Cash = 1000 };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        await _db.UserItems.AddAsync(new UserItem { UserId = user.Id, ItemId = 10 });
        await _db.SaveChangesAsync();

        var result = await _repo.GetAsync(user.Id);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().ItemId.Should().Be(10);
    }
}
```

여기서는 **Repository + DbContext + MySQL**만 테스트한다.
Service, Controller는 개입하지 않는다.

---

### 3-2. Service 통합 테스트

목표: **비즈니스 흐름(트랜잭션, 여러 Repository 사용, 상태 변경)을 검증**하는 테스트다.

* 여러 Repository/도메인 객체를 조합하는 Purchase, Matchmaking, Reward 지급 등
* 트랜잭션 안에서 여러 엔티티가 같이 바뀌는 로직
* 외부 시스템(메시지 큐, Redis, gRPC)과 상호작용하는 부분

패턴은 보통 이렇게 나눈다.

* DB는 실제 사용
* 외부 시스템(예: Kafka, Redis)은 Mock 또는 In-memory 구현 사용
* 진짜로 외부까지 붙이는 건 e2e 테스트에서 소량만 수행

Service 통합 테스트 예:

```csharp
public class PurchaseServiceIntegrationTests2 : IClassFixture<MySqlContainerFixture>
{
    private readonly GameDbContext _db;
    private readonly PurchaseService _service;

    public PurchaseServiceIntegrationTests2(MySqlContainerFixture fixture)
    {
        _db = fixture.Db;

        var userRepo = new UserRepository(_db);
        _service = new PurchaseService(_db, userRepo);
    }

    [Fact]
    public async Task Purchase_Withdraws_Cash_And_Adds_Item_In_Same_Transaction()
    {
        _db.Users.RemoveRange(_db.Users);
        _db.UserItems.RemoveRange(_db.UserItems);
        _db.ShopItems.RemoveRange(_db.ShopItems);
        await _db.SaveChangesAsync();

        var user = new User { Cash = 500 };
        var item = new ShopItem { Id = 1, Price = 500 };

        await _db.Users.AddAsync(user);
        await _db.ShopItems.AddAsync(item);
        await _db.SaveChangesAsync();

        var result = await _service.PurchaseAsync(user.Id, item.Id);

        result.Should().BeTrue();

        var saved = await _db.Users
            .Include(u => u.Items)
            .FirstAsync(u => u.Id == user.Id);

        saved.Cash.Should().Be(0);
        saved.Items.Should().Contain(i => i.ItemId == item.Id);
    }
}
```

---

## 4. 통합 테스트에서 트랜잭션 롤백 처리하는 기술

통합 테스트의 큰 문제는 **데이터가 계속 쌓이면서 서로 영향을 주는 것**이다.
대표적인 해결 패턴은 세 가지 정도가 있다.

### 4-1. 테스트마다 트랜잭션 열고 끝나면 롤백

각 테스트가 **자기 트랜잭션 안에서만 연산**하고, 테스트 끝날 때 롤백하는 방식이다.

xUnit에서 Fixture + IAsyncLifetime을 써서 구현할 수 있다.

```csharp
public class TransactionalTestBase : IAsyncLifetime
{
    protected readonly GameDbContext Db;
    private IDbContextTransaction _tx = null!;

    public TransactionalTestBase()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseMySql("Server=localhost;Database=test_game;User=root;Password=1234;",
                new MySqlServerVersion(new Version(8, 0, 25)))
            .Options;

        Db = new GameDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await Db.Database.OpenConnectionAsync();
        _tx = await Db.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        await _tx.RollbackAsync();
        await Db.DisposeAsync();
    }
}
```

그리고 테스트 클래스는 이렇게 상속한다.

```csharp
public class PurchaseServiceTxTests : TransactionalTestBase
{
    [Fact]
    public async Task Purchase_Rollsback_After_Test()
    {
        var user = new User { Cash = 1000 };
        var item = new ShopItem { Id = 1, Price = 500 };
        await Db.Users.AddAsync(user);
        await Db.ShopItems.AddAsync(item);
        await Db.SaveChangesAsync();

        var repo = new UserRepository(Db);
        var service = new PurchaseService(Db, repo);

        var result = await service.PurchaseAsync(user.Id, item.Id);

        result.Should().BeTrue();
        // 여기까지는 DB 안에 실제로 들어가 있지만,
        // 테스트가 끝나면 트랜잭션 롤백으로 깨끗해진다.
    }
}
```

주의할 점은 다음과 같다.

* Service 내부에서 별도의 트랜잭션을 열면(**Nested Transaction**) 약간 꼬일 수 있다
  → 가능하면 “테스트 코드에서 트랜잭션을 관리”하고, Service는 DbContext의 Transaction을 그대로 사용하도록 설계하는 것이 깔끔하다.

---

### 4-2. 테스트마다 DB 초기화 (TRUNCATE, Respawn 등)

테스트 앞/뒤로 모든 테이블을 TRUNCATE하거나, `Respawn` 같은 라이브러리로 DB 상태를 초기화하는 방식이다.

장점

* 트랜잭션/Isolation 걱정이 적다
* 테스트 코드에서 트랜잭션을 크게 신경 쓸 필요가 없다

단점

* 테이블 수가 많으면 초기화가 느려질 수 있다

단순 TRUNCATE 패턴:

```csharp
public static class DbTestHelper
{
    public static async Task ResetDatabaseAsync(GameDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;");
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE UserItems;");
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Users;");
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE ShopItems;");
        await db.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;");
    }
}
```

테스트에서:

```csharp
[Fact]
public async Task Purchase_Works_With_Clean_Db()
{
    await DbTestHelper.ResetDatabaseAsync(Db);

    // Arrange, Act, Assert...
}
```

---

### 4-3. 테스트 전용 DB를 아예 매번 새로 만들고, 끝나면 삭제

* Testcontainers + MySQL에서 **컨테이너 하나 = 테스트 세션 하나**
* 각 테스트 클래스마다 새로운 DB 컨테이너를 띄워서 사용하고, 끝나면 날리는 방식이다

이 방식은 “테스트 격리”가 가장 확실하다.
단, 성능 비용이 크기 때문에 보통은 **테스트 클래스 단위로 컨테이너 하나**를 공유하게 만든다.

---

## 마무리 정리

* **실제 MySQL + EF Core 통합 테스트**

  * 테스트 전용 DB를 만들고, DbContext를 실제 MySQL에 붙인다
  * Repository/Service를 그대로 사용해 CRUD + 트랜잭션 동작을 검증한다

* **Docker + Testcontainers**

  * 테스트 시작 시 MySQL 컨테이너를 띄우고, 끝나면 제거한다
  * 환경 의존성을 줄이고 CI에도 동일한 환경을 제공한다

* **게임 서버에서 레이어별 통합 테스트**

  * Repository 통합 테스트: 쿼리/매핑/샤딩/스키마 검증
  * Service 통합 테스트: 트랜잭션, 여러 Repository/도메인 조합, 비즈니스 흐름 검증

* **트랜잭션 롤백 패턴**

  * 테스트마다 트랜잭션 시작 → 테스트 끝날 때 롤백
  * TRUNCATE/Respawn 등으로 DB를 매 테스트마다 초기화
  * 컨테이너/테스트 DB를 테스트 세션마다 새로 만들고 삭제
  
---   

</br>   
</br>   
  

## 유닛테스트와 통합테스트 프로젝트를 각각 분리해야 하는가?
  
1. **유닛테스트 / 통합테스트를 반드시 다른 프로젝트로 분리해야 하는 건 아니다**
2. 하지만 **규모가 조금만 커져도 분리하는 편이 훨씬 편하다**
3. **통합테스트 실행 시점은 “비용 대비”로 나눠서**

   * 유닛 테스트: *매 빌드, 매 커밋, 매 PR*
   * 통합 테스트: *PR 단계 또는 merge 이후 / 주기적(CI 파이프라인)*
     정도로 가져가는 게 보통 좋다.

아래에서 좀 더 구체적으로 설명한다.
  

## 1. 프로젝트를 분리해야 하는가?

### 1-1. 선택지 2개

일반적으로 두 가지 패턴이 있다.

1. **테스트 프로젝트 하나에 유닛 + 통합을 다 넣는 패턴**

   * `MyApp.Tests`

     * `Unit/…`
     * `Integration/…`

2. **테스트 프로젝트를 타입별로 분리하는 패턴**

   * `MyApp.UnitTests`
   * `MyApp.IntegrationTests`

두 방식 모두 가능하다. 프레임워크에서 강제하는 건 없다.

---

### 1-2. “하나로 쓰는” 경우 장단점

**장점**

* 솔루션 구조가 단순하다
* 공용 테스트 유틸(빌더, 헬퍼 등) 공유가 쉽다
* 작은/중간 규모 프로젝트는 이 정도로도 충분하다

**단점**

* 유닛/통합이 물리적으로 섞여 있어서

  * 어떤 테스트가 느리고, 어떤 테스트가 빠른지 구분이 흐려진다
  * `dotnet test` 한 번 돌리면 느린 통합테스트까지 같이 돌아간다
* 실행 필터링을 Trait/Category로 해야 해서 설정이 약간 귀찮다

이 방식이면 보통 xUnit 기준으로 이렇게 태깅한다.

```csharp
public class PurchaseServiceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Some_unit_test() { ... }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Some_integration_test() { ... }
}
```

그리고 실행할 때:

* 유닛 테스트만:
  `dotnet test --filter "Category=Unit"`
* 통합 테스트만:
  `dotnet test --filter "Category=Integration"`

---

### 1-3. “프로젝트를 분리하는” 경우 장단점

**장점**

1. **실행 전략 분리가 쉬움**

   * CI에서 `MyApp.UnitTests`는 항상, `MyApp.IntegrationTests`는 선택적으로 실행
   * 로컬에서도 솔루션 탐색기에서 프로젝트 단위로 바로 실행 가능

2. **참조/의존성 분리가 깔끔함**

   * 통합 테스트 프로젝트는

     * `WebApplicationFactory<Program>` 사용
     * Docker / Testcontainers, 실제 DB 연결 문자열, appsettings.Integration.json 등
   * 유닛 테스트 프로젝트는

     * Mock 라이브러리(Moq, NSubstitute 등) 위주만 참조

3. **환경 설정이 다름**

   * 통합 테스트는 보통 별도 `appsettings.Integration.json`,
     실제 DB 주소, 테스트용 Redis/Kafka 등 필요
   * 이걸 유닛테스트와 같이 쓰면 config가 꼬이기 쉽다

**단점**

* 프로젝트/솔루션이 늘어난다
* 공용 테스트 유틸을 다른 프로젝트에 공유하려면

  * 공용 테스트 라이브러리(`MyApp.TestCommon`)를 하나 더 만들 수도 있다

**ASP.NET Core에서는**
`MyApp.Api` (실제 웹 프로젝트)
`MyApp.UnitTests`
`MyApp.IntegrationTests`
이 구조가 꽤 흔한 패턴이다.

---

## 2. 통합 테스트는 언제 실행하는 게 좋은가?

핵심은 “느리고, 외부 의존성이 있으니까” **유닛만큼 자주 돌리기 힘들다**는 점이다. 보통 이렇게 나눈다.

### 2-1. 유닛 테스트

* 실행 비용: 매우 빠름 (ms~수십 ms 단위)
* 의존성: 없음 (DB, 네트워크 X)
* 역할: 개발자가 안심하고 리팩토링할 수 있게 해주는 최소 안전망

**실행 타이밍**

* 로컬:

  * 큰 변경 전에 한번, 커밋 전에 한번 돌리는 걸 추천
* CI:

  * *모든 PR / 모든 push*에서 항상 실행

---

### 2-2. 통합 테스트

* 실행 비용: 상대적으로 느림 (초 단위, DB 초기화 포함)
* 의존성: 실제 DB, Docker, 외부 시스템 등
* 역할:

  * ORM + DB 스키마 + 트랜잭션 + 실제 HTTP 파이프라인이 잘 붙는지 검증
  * “로직은 맞는데, 실 서버에서는 왜 깨지냐?”를 방지

**실행 전략 몇 가지 케이스**

1. **규모가 작은 팀 / 프로젝트 초기**

   * CI에서 PR마다 유닛 + 통합 테스트 모두 실행해도 된다
   * 테스트 수가 적고 DB도 가벼워서 시간 부담이 크지 않다

2. **규모가 커질 때(통합테스트가 느려지는 시점)**
   많이 쓰는 패턴은:

   * PR 생성/업데이트 시:

     * 유닛 테스트 **항상**
     * 통합 테스트는

       * 특정 브랜치만(`main`, `develop`) 실행
       * 혹은, label을 달았을 때만 (예: `run-integration-tests`)
   * 일정 주기:

     * 밤마다 / 몇 시간마다 전체 통합 테스트 실행 (스케줄 CI)

3. **로컬 개발자 워크플로우**

   * 유닛 테스트:

     * 뭔가 로직을 만졌으면 자주 돌리는 편이 좋다
   * 통합 테스트:

     * 인프라나 DB 쪽을 건드린 날, PR 올리기 전 등 *중요한 변경 전*에 한 번 돌리는 정도로도 충분하다

---

## 3. 정리: 어떻게 하는 걸 추천하나?

현실적인 추천안을 적어 본다.

### 3-1. 솔루션 구조 추천

프로젝트가 이제 막 시작이거나 중간 규모라면:

* `MyGame.Api` (ASP.NET Core 프로젝트)
* `MyGame.UnitTests`
* `MyGame.IntegrationTests`

이렇게 **분리하는 쪽**을 추천한다.

이유:

* 통합 테스트 쪽은 반드시

  * 실제 DB 연결
  * Docker / Testcontainers
  * `appsettings.Integration.json`
    같은 걸 잡게 되는데,
    이걸 유닛테스트와 같은 프로젝트에 섞어두면 나중에 반드시 꼬인다.

(이미 `MyGame.Tests` 하나만 있는 상태라면, 당장 갈아엎지는 말고, 시간이 될 때 분리하는 방식으로 진행해도 된다)

---

### 3-2. 실행 정책 추천

1. **유닛 테스트**

   * 로컬: 자주, 커밋 전에 실행
   * CI: 모든 PR / 모든 push에서 실행 (필수 통과로 설정)

2. **통합 테스트**

   * CI:

     * 기본: `develop` / `main` 브랜치에 push 시 실행
     * 또는 모든 PR에서 실행하되, 특정 job으로 분리해서 느리면 나중에 결과를 보게 할 수도 있다
   * 로컬:

     * DB, 레포지터리, 컨트롤러 레벨에 영향을 주는 큰 변경을 했을 때
     * PR 올리기 전 “한 번은 돌려본다” 정도의 문화로 가져가면 좋다

이렇게 하면:

* 유닛 테스트 → 빠르고 자주, 개발자 리듬을 해치지 않게
* 통합 테스트 → 느린 대신, 인프라/실제 환경과의 호환성을 보장하는 안전망으로 사용  


================================================
File: guides/mysql_sqlkata.md
================================================
# MySQL + SqlKata 사용 가이드

본 저장소의 게임 서버 프로젝트에서 MySQL을 사용하는 방법을 설명한다.
EF Core(Entity Framework)를 사용하지 않고, **MySqlConnector + SqlKata** 조합으로 직접 쿼리를 작성한다.

---

## 기술 스택

```
┌─────────────────────────────┐
│         SqlKata              │  ← C# 코드로 SQL 쿼리를 빌드 (Fluent API)
├─────────────────────────────┤
│       MySqlConnector         │  ← MySQL 데이터베이스 드라이버 (.NET용)
├─────────────────────────────┤
│          MySQL               │  ← 데이터베이스 서버
└─────────────────────────────┘
```

| 라이브러리 | NuGet 패키지 | 역할 |
|:---|:---|:---|
| **MySqlConnector** | `MySqlConnector` | MySQL 연결, 쿼리 실행 |
| **SqlKata** | `SqlKata` + `SqlKata.Execution` | SQL 쿼리 빌더 (문자열 SQL 대신 C# 메서드로 조합) |

### 왜 EF Core를 사용하지 않는가?

| | EF Core | MySqlConnector + SqlKata |
|:---|:---|:---|
| **장점** | 자동 마이그레이션, LINQ, 코드 퍼스트 | 직접 쿼리 제어, 성능 예측 가능, 가벼움 |
| **단점** | 복잡한 쿼리 어려움, 성능 오버헤드 | 마이그레이션 수동 관리 |
| **게임 서버** | 복잡한 조인, 벌크 연산이 많아 불리 | **직접 SQL 제어가 유리** |

---

## 1단계: 설정

### NuGet 패키지 설치

```bash
dotnet add package MySqlConnector
dotnet add package SqlKata
dotnet add package SqlKata.Execution
```

### appsettings.json에 접속 정보 추가

```json
{
  "DbConfig": {
    "GameDb": "Server=localhost;Port=3306;User=root;Password=123qwe;Database=game_db;Pooling=true;Min Pool Size=0;Max Pool Size=40;AllowUserVariables=True;"
  }
}
```

### Program.cs에서 DI 등록

```csharp
builder.Services.Configure<DbConfig>(configuration.GetSection(nameof(DbConfig)));
builder.Services.AddTransient<IGameDb, GameDb>();
```

---

## 2단계: Repository 클래스 구성

### 연결 + QueryFactory 생성

```csharp
using MySqlConnector;
using SqlKata.Execution;

public partial class GameDb : IGameDb
{
    readonly IOptions<DbConfig> _dbConfig;
    readonly ILogger<GameDb> _logger;

    MySqlConnection _dbConn;
    SqlKata.Compilers.MySqlCompiler _compiler;
    QueryFactory _queryFactory;

    public GameDb(ILogger<GameDb> logger, IOptions<DbConfig> dbConfig)
    {
        _dbConfig = dbConfig;
        _logger = logger;

        // MySQL 연결 생성
        _dbConn = new MySqlConnection(dbConfig.Value.GameDb);
        _dbConn.Open();

        // SqlKata QueryFactory 생성 (MySqlCompiler 사용)
        _compiler = new SqlKata.Compilers.MySqlCompiler();
        _queryFactory = new QueryFactory(_dbConn, _compiler);
    }
}
```

> **참고 코드:** `codes/GameAPIServer_Template/Repository/GameDB_User.cs`

---

## 3단계: CRUD 쿼리

### 조회 (SELECT)

```csharp
// 단건 조회: WHERE uid = {uid}
var account = await _queryFactory.Query("account")
    .Where("uid", uid)
    .FirstOrDefaultAsync<Account>();

// 조건부 조회
var user = await _queryFactory.Query("account")
    .Where("user_id", userId)
    .Where("pw", hashedPassword)
    .FirstOrDefaultAsync<Account>();

// 목록 조회
var friends = await _queryFactory.Query("friend")
    .Where("uid", uid)
    .Where("accept_yn", true)
    .GetAsync<Friend>();

// 특정 컬럼만 조회
var rankings = await _queryFactory.Query("user")
    .Select("uid", "total_bestscore")
    .GetAsync<RdbUserScoreData>();
```

생성되는 SQL:
```sql
SELECT * FROM `account` WHERE `uid` = @p0
SELECT * FROM `account` WHERE `user_id` = @p0 AND `pw` = @p1
SELECT * FROM `friend` WHERE `uid` = @p0 AND `accept_yn` = @p1
SELECT `uid`, `total_bestscore` FROM `user`
```

### 삽입 (INSERT)

```csharp
// 단건 삽입
var insertedId = await _queryFactory.Query("account").InsertGetIdAsync<long>(new
{
    user_id = userId,
    pw = hashedPassword,
    salt = salt,
    create_dt = DateTime.Now
});

// 여러 컬럼 삽입
await _queryFactory.Query("user_money").InsertAsync(new
{
    uid = uid,
    jewelry = 0,
    money = 1000
});
```

### 수정 (UPDATE)

```csharp
// 특정 행 수정
var affectedRows = await _queryFactory.Query("user")
    .Where("uid", uid)
    .UpdateAsync(new
    {
        main_char = characterId
    });

// 값 증감
await _queryFactory.Query("user_money")
    .Where("uid", uid)
    .IncrementAsync("jewelry", amount);
```

### 삭제 (DELETE)

```csharp
var deleted = await _queryFactory.Query("mailbox")
    .Where("mail_seq", mailSeq)
    .DeleteAsync();
```

---

## 4단계: 트랜잭션

여러 DB 조작을 하나의 단위로 묶어야 할 때 트랜잭션을 사용한다.

```csharp
using var transaction = await _dbConn.BeginTransactionAsync();
try
{
    // 아이템 지급
    await _queryFactory.Query("user_item").InsertAsync(new { uid, item_code = itemCode });

    // 재화 차감
    await _queryFactory.Query("user_money").Where("uid", uid)
        .DecrementAsync("money", price);

    await transaction.CommitAsync();   // 모두 성공 시 확정
}
catch
{
    await transaction.RollbackAsync(); // 하나라도 실패 시 전체 취소
    throw;
}
```

> **상세 가이드:** `docs/guides/how_to_db_transaction.md`

---

## SqlKata vs 문자열 SQL 비교

```csharp
// ❌ 문자열 SQL — SQL 인젝션 위험, 오타 발견 어려움
var sql = $"SELECT * FROM account WHERE user_id = '{userId}'";

// ✅ SqlKata — 파라미터 바인딩 자동, 컴파일 타임 검증
var result = await _queryFactory.Query("account")
    .Where("user_id", userId)
    .FirstOrDefaultAsync<Account>();
```

| | 문자열 SQL | SqlKata |
|:---|:---|:---|
| **SQL 인젝션** | 취약 (직접 방어 필요) | 안전 (자동 파라미터 바인딩) |
| **가독성** | 복잡한 쿼리일수록 떨어짐 | 메서드 체이닝으로 명확 |
| **오타 감지** | 런타임에서만 발견 | IDE 자동완성 지원 |
| **DB 교체** | SQL 방언 직접 수정 | Compiler만 교체 (MySql → Postgres 등) |

---

## 참고

- SqlKata 라이브러리 상세: `docs/references/sqlkata.md`
- DB 트랜잭션 가이드: `docs/guides/how_to_db_transaction.md`
- 실제 Repository 코드: `codes/GameAPIServer_Template/Repository/`



================================================
File: guides/project_structure.md
================================================
# 게임 API 서버 프로젝트 구조 가이드

## 왜 계층을 나누는가?

모든 코드를 컨트롤러 하나에 넣으면 처음에는 간단하지만, 기능이 늘어날수록 문제가 생긴다.

```csharp
// ❌ 컨트롤러에 모든 로직이 있는 경우
[HttpPost]
public async Task<LoginResponse> Login(LoginRequest request)
{
    // 1) DB에서 계정 조회 (SQL 쿼리)
    // 2) 비밀번호 검증 (해시 비교)
    // 3) Redis에 토큰 저장
    // 4) 마스터 데이터 확인
    // 5) 응답 생성
    // → 200줄짜리 메서드가 됨
}
```

**문제점:**
- 컨트롤러가 비대해짐 (수천 줄)
- 같은 로직을 다른 컨트롤러에서 재사용 불가
- DB를 바꾸면 컨트롤러를 전부 수정해야 함
- 테스트 작성 불가능 (DB가 있어야 테스트 가능)

---

## Controller → Service → Repository 패턴

본 저장소의 모든 프로젝트는 **3계층 구조**를 따른다.

```
요청 → Controller → Service → Repository → DB/Redis
                                                ↓
응답 ← Controller ← Service ← Repository ← 결과
```

| 계층 | 역할 | 알아야 하는 것 | 모르는 것 |
|:---|:---|:---|:---|
| **Controller** | 요청 수신, 응답 반환 | HTTP, DTO | SQL, Redis 명령 |
| **Service** | 비즈니스 로직 | 게임 규칙, 검증 | SQL, HTTP |
| **Repository** | DB/Redis 접근 | SQL, Redis 명령 | HTTP, 게임 규칙 |

### 각 계층의 책임

#### Controller — "무엇을 요청했는지" 처리

```csharp
// Controllers/LoginController.cs
[HttpPost]
public async Task<LoginResponse> Login(LoginRequest request)
{
    var response = new LoginResponse();

    // Service에 위임 (구현 세부사항을 모름)
    var (errorCode, uid, token) = await _authService.Login(request.UserID, request.Password);

    response.Result = errorCode;
    response.AuthToken = token;
    return response;
}
```

**규칙:** 컨트롤러에 비즈니스 로직을 넣지 않는다. Service 메서드를 호출하고 결과를 응답에 담을 뿐이다.

#### Service — "어떤 규칙으로 처리하는지" 구현

```csharp
// Services/AuthService.cs
public async Task<(ErrorCode, long, string)> Login(string userId, string password)
{
    // 1) DB에서 계정 조회 (Repository에 위임)
    var account = await _gameDb.GetAccountByUserId(userId);
    if (account == null) return (ErrorCode.AccountNotFound, 0, "");

    // 2) 비밀번호 검증 (비즈니스 로직)
    if (Security.HashPassword(password, account.Salt) != account.PasswordHash)
        return (ErrorCode.InvalidPassword, 0, "");

    // 3) 토큰 생성 및 Redis 저장 (Repository에 위임)
    var token = Security.CreateAuthToken();
    await _memoryDb.SetAuthToken(account.Uid, token);

    return (ErrorCode.None, account.Uid, token);
}
```

**규칙:** SQL 쿼리나 Redis 명령을 직접 실행하지 않는다. Repository를 통해 데이터에 접근한다.

#### Repository — "어떻게 저장/조회하는지" 구현

```csharp
// Repository/GameDb.cs
public async Task<Account?> GetAccountByUserId(string userId)
{
    using var connection = await GetConnection();
    return await connection.QueryFactory()
        .Query("account")
        .Where("user_id", userId)
        .FirstOrDefaultAsync<Account>();
}
```

**규칙:** 비즈니스 규칙을 판단하지 않는다. 데이터를 저장/조회/삭제하는 것만 담당한다.

---

## 실제 프로젝트 디렉토리 구조

```
GameAPIServer/
├── Program.cs                 # 앱 진입점 (DI 등록, 미들웨어 설정)
├── appsettings.json           # DB 접속 정보, 서버 포트 등 설정
├── ErrorCode.cs               # 에러코드 enum (클라이언트와 약속된 코드)
│
├── Controllers/               # [1계층] 요청 수신 → Service 호출 → 응답 반환
│   ├── LoginController.cs
│   ├── MailListController.cs
│   └── ...
│
├── Services/                  # [2계층] 비즈니스 로직 (게임 규칙, 검증)
│   ├── Interfaces/            #   인터페이스 (DI를 위해 분리)
│   │   ├── IAuthService.cs
│   │   └── ...
│   ├── AuthService.cs
│   └── ...
│
├── Repository/                # [3계층] DB/Redis 접근 (SQL 쿼리, Redis 명령)
│   ├── Interfaces/
│   │   ├── IGameDb.cs         #   MySQL 게임 DB
│   │   └── IMemoryDb.cs       #   Redis 캐시
│   ├── GameDb.cs
│   └── MemoryDb.cs
│
├── Models/                    # 데이터 모델
│   ├── DAO/                   #   DB 테이블과 1:1 매핑되는 모델
│   │   ├── Account.cs
│   │   └── ...
│   └── DTO/                   #   클라이언트 요청/응답 모델
│       ├── Login.cs           #   LoginRequest, LoginResponse
│       └── ...
│
├── Middleware/                 # 미들웨어 (요청 전처리)
│   ├── VersionCheck.cs
│   └── CheckUserAuth.cs
│
└── DB_Schema.md               # DB 테이블 스키마 문서
```

### DAO vs DTO

| 구분 | DAO (Data Access Object) | DTO (Data Transfer Object) |
|:---|:---|:---|
| **위치** | `Models/DAO/` | `Models/DTO/` |
| **용도** | DB 테이블의 행(row)을 C# 객체로 매핑 | 클라이언트와 주고받는 요청/응답 데이터 |
| **예시** | `Account { Uid, UserId, Salt, PwHash }` | `LoginRequest { UserID, Password }` |
| **규칙** | DB 컬럼과 1:1 대응, 비밀번호 해시 포함 가능 | 클라이언트에 노출해도 되는 데이터만 포함 |

---

## 왜 Interface를 사용하는가?

```
Services/
├── Interfaces/
│   └── IAuthService.cs    ← 인터페이스 (계약)
└── AuthService.cs          ← 구현체
```

**이유:** 테스트 시 실제 DB 없이 FakeGameDb로 교체할 수 있다.

```csharp
// 실제 운영
builder.Services.AddTransient<IGameDb, GameDb>();       // MySQL 사용

// 테스트/개발 (DB 없이)
builder.Services.AddTransient<IGameDb, FakeGameDb>();   // Mock 사용
```

> **참고 코드:** `codes/GameAPIServer_Template/Repository/FakeGameDb.cs`는 모든 메서드가 성공을 반환하는 Mock 구현이다. DB 없이도 서버를 실행할 수 있게 해준다.

---

## 참고

- 디렉토리 구조 예시: `docs/references/APIServer_Directory.md`
- DI 패턴 설명: `docs/patterns/why_di.md`
- ASP.NET Core 기초: `docs/guides/aspnetcore_basics.md`



================================================
File: patterns/Cache-Aside_pattern.md
================================================
# 게임 서버에서 Redis를 이용한 Cache-Aside 패턴
- 클라이언트 요청이 오면 RDB가 아닌 Redis에서 먼저 데이터를 읽고, 없으면 RDB에서 가져온다.
- 유저의 데이트를 저장할 때는 Redis에 먼저 저장하고, DB에도 저장한다.  
  
-----   
## Cache-Aside 패턴의 동작 방식
읽기: Redis 캐시 확인 → 없으면 DB 조회 → Redis에 저장  
쓰기: DB에 먼저 저장 → Redis 캐시 갱신  

### 제네릭 구조의 장점
CacheService<T> 하나로 Player, Item 등 모든 엔티티 처리  
새로운 엔티티 추가 시 DatabaseRepository만 구현하면 됨  
  

### 성능 최적화
Player 데이터: 30분 캐시 (자주 조회)  
Item 데이터: 1시간 캐시 (상대적으로 변경 적음)  
  
이런 구조로 게임 서버의 응답 속도를 크게 개선하면서도 데이터 정합성을 보장할 수 있다  
  
![](../../images/001.png)  
![](../../images/002.png)  
![](../../images/003.png)  
![](../../images/004.png)  
  
![](../../images/005.png)  
  

## ASP.NET Core Redis Cache 구조코드   

```
// Models/Player.cs
public class Player
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Level { get; set; }
    public long Experience { get; set; }
    public DateTime LastLoginTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Repositories/Interfaces/IRepository.cs
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
}

// Repositories/Interfaces/ICacheRepository.cs
public interface ICacheRepository<T> : IRepository<T> where T : class
{
    Task<bool> ExistsAsync(int id);
    Task SetAsync(int id, T entity, TimeSpan? expiry = null);
    Task RemoveAsync(int id);
}

// Repositories/Interfaces/IDatabaseRepository.cs
public interface IDatabaseRepository<T> : IRepository<T> where T : class
{
}

// Repositories/RedisCacheRepository.cs
public class RedisCacheRepository<T> : ICacheRepository<T> where T : class
{
    private readonly IDatabase _database;
    private readonly string _keyPrefix;
    private readonly TimeSpan _defaultExpiry;

    public RedisCacheRepository(IConnectionMultiplexer redis, string keyPrefix = "", TimeSpan? defaultExpiry = null)
    {
        _database = redis.GetDatabase();
        _keyPrefix = keyPrefix;
        _defaultExpiry = defaultExpiry ?? TimeSpan.FromMinutes(30);
    }

    private string GetKey(int id) => $"{_keyPrefix}:{typeof(T).Name}:{id}";

    public async Task<T?> GetByIdAsync(int id)
    {
        var json = await _database.StringGetAsync(GetKey(id));
        return json.HasValue ? JsonSerializer.Deserialize<T>(json!) : null;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _database.KeyExistsAsync(GetKey(id));
    }

    public async Task SetAsync(int id, T entity, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(entity);
        await _database.StringSetAsync(GetKey(id), json, expiry ?? _defaultExpiry);
    }

    public async Task<T> CreateAsync(T entity)
    {
        throw new NotSupportedException("Cache repository는 Create 작업을 지원하지 않는다");
    }

    public async Task<T> UpdateAsync(T entity)
    {
        throw new NotSupportedException("Cache repository는 Update 작업을 지원하지 않는다");
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _database.KeyDeleteAsync(GetKey(id));
    }

    public async Task RemoveAsync(int id)
    {
        await _database.KeyDeleteAsync(GetKey(id));
    }
}

// Repositories/PlayerDatabaseRepository.cs
public class PlayerDatabaseRepository : IDatabaseRepository<Player>
{
    private readonly string _connectionString;

    public PlayerDatabaseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("Connection string이 설정되지 않았다");
    }

    public async Task<Player?> GetByIdAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, Username, Level, Experience, LastLoginTime, CreatedAt, UpdatedAt 
            FROM Players 
            WHERE Id = @Id";
        
        return await connection.QueryFirstOrDefaultAsync<Player>(sql, new { Id = id });
    }

    public async Task<Player> CreateAsync(Player player)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO Players (Username, Level, Experience, LastLoginTime, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.*
            VALUES (@Username, @Level, @Experience, @LastLoginTime, @CreatedAt, @UpdatedAt)";

        player.CreatedAt = DateTime.UtcNow;
        player.UpdatedAt = DateTime.UtcNow;
        
        return await connection.QuerySingleAsync<Player>(sql, player);
    }

    public async Task<Player> UpdateAsync(Player player)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            UPDATE Players 
            SET Username = @Username, Level = @Level, Experience = @Experience, 
                LastLoginTime = @LastLoginTime, UpdatedAt = @UpdatedAt
            OUTPUT INSERTED.*
            WHERE Id = @Id";

        player.UpdatedAt = DateTime.UtcNow;
        
        return await connection.QuerySingleAsync<Player>(sql, player);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = "DELETE FROM Players WHERE Id = @Id";
        
        var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
        return affectedRows > 0;
    }
}

// Services/Interfaces/ICacheService.cs
public interface ICacheService<T> where T : class
{
    Task<T?> GetAsync(int id);
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
}

// Services/CacheService.cs
public class CacheService<T> : ICacheService<T> where T : class
{
    private readonly ICacheRepository<T> _cacheRepository;
    private readonly IDatabaseRepository<T> _databaseRepository;
    private readonly ILogger<CacheService<T>> _logger;

    public CacheService(
        ICacheRepository<T> cacheRepository, 
        IDatabaseRepository<T> databaseRepository,
        ILogger<CacheService<T>> logger)
    {
        _cacheRepository = cacheRepository;
        _databaseRepository = databaseRepository;
        _logger = logger;
    }

    public async Task<T?> GetAsync(int id)
    {
        try
        {
            // 1. Redis에서 먼저 확인
            var cachedEntity = await _cacheRepository.GetByIdAsync(id);
            if (cachedEntity != null)
            {
                _logger.LogDebug("캐시에서 {EntityType} ID {Id}를 찾았다", typeof(T).Name, id);
                return cachedEntity;
            }

            // 2. DB에서 조회
            var dbEntity = await _databaseRepository.GetByIdAsync(id);
            if (dbEntity != null)
            {
                // 3. Redis에 저장
                await _cacheRepository.SetAsync(id, dbEntity);
                _logger.LogDebug("DB에서 {EntityType} ID {Id}를 찾아 캐시에 저장했다", typeof(T).Name, id);
            }

            return dbEntity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "데이터 조회 중 오류가 발생했다. ID: {Id}", id);
            throw;
        }
    }

    public async Task<T> CreateAsync(T entity)
    {
        try
        {
            // 1. DB에 저장
            var createdEntity = await _databaseRepository.CreateAsync(entity);
            
            // 2. Redis에 저장 (ID가 있다고 가정)
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty != null)
            {
                var id = (int)idProperty.GetValue(createdEntity)!;
                await _cacheRepository.SetAsync(id, createdEntity);
                _logger.LogDebug("새로운 {EntityType} ID {Id}를 생성하고 캐시에 저장했다", typeof(T).Name, id);
            }

            return createdEntity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "데이터 생성 중 오류가 발생했다");
            throw;
        }
    }

    public async Task<T> UpdateAsync(T entity)
    {
        try
        {
            // 1. DB 업데이트
            var updatedEntity = await _databaseRepository.UpdateAsync(entity);
            
            // 2. Redis 업데이트
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty != null)
            {
                var id = (int)idProperty.GetValue(updatedEntity)!;
                await _cacheRepository.SetAsync(id, updatedEntity);
                _logger.LogDebug("{EntityType} ID {Id}를 업데이트하고 캐시를 갱신했다", typeof(T).Name, id);
            }

            return updatedEntity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "데이터 업데이트 중 오류가 발생했다");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            // 1. DB에서 삭제
            var deleted = await _databaseRepository.DeleteAsync(id);
            
            if (deleted)
            {
                // 2. Redis에서 삭제
                await _cacheRepository.RemoveAsync(id);
                _logger.LogDebug("{EntityType} ID {Id}를 삭제하고 캐시에서 제거했다", typeof(T).Name, id);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "데이터 삭제 중 오류가 발생했다. ID: {Id}", id);
            throw;
        }
    }
}

// Controllers/PlayerController.cs
[ApiController]
[Route("api/[controller]")]
public class PlayerController : ControllerBase
{
    private readonly ICacheService<Player> _playerService;

    public PlayerController(ICacheService<Player> playerService)
    {
        _playerService = playerService;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Player>> GetPlayer(int id)
    {
        var player = await _playerService.GetAsync(id);
        return player == null ? NotFound() : Ok(player);
    }

    [HttpPost]
    public async Task<ActionResult<Player>> CreatePlayer([FromBody] CreatePlayerRequest request)
    {
        var player = new Player
        {
            Username = request.Username,
            Level = 1,
            Experience = 0,
            LastLoginTime = DateTime.UtcNow
        };

        var createdPlayer = await _playerService.CreateAsync(player);
        return CreatedAtAction(nameof(GetPlayer), new { id = createdPlayer.Id }, createdPlayer);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Player>> UpdatePlayer(int id, [FromBody] UpdatePlayerRequest request)
    {
        var existingPlayer = await _playerService.GetAsync(id);
        if (existingPlayer == null)
            return NotFound();

        existingPlayer.Username = request.Username ?? existingPlayer.Username;
        existingPlayer.Level = request.Level ?? existingPlayer.Level;
        existingPlayer.Experience = request.Experience ?? existingPlayer.Experience;
        existingPlayer.LastLoginTime = DateTime.UtcNow;

        var updatedPlayer = await _playerService.UpdateAsync(existingPlayer);
        return Ok(updatedPlayer);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePlayer(int id)
    {
        var deleted = await _playerService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}

// DTOs/PlayerRequests.cs
public class CreatePlayerRequest
{
    public string Username { get; set; } = string.Empty;
}

public class UpdatePlayerRequest
{
    public string? Username { get; set; }
    public int? Level { get; set; }
    public long? Experience { get; set; }
}

// Program.cs에서 DI 설정
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Redis 설정
        builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var configuration = provider.GetService<IConfiguration>();
            var connectionString = configuration!.GetConnectionString("Redis");
            return ConnectionMultiplexer.Connect(connectionString!);
        });

        // Repository 등록
        builder.Services.AddScoped<ICacheRepository<Player>>(provider =>
        {
            var redis = provider.GetService<IConnectionMultiplexer>();
            return new RedisCacheRepository<Player>(redis!, "game", TimeSpan.FromMinutes(30));
        });
        
        builder.Services.AddScoped<IDatabaseRepository<Player>, PlayerDatabaseRepository>();
        
        // Service 등록
        builder.Services.AddScoped<ICacheService<Player>, CacheService<Player>>();

        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseRouting();
        app.MapControllers();

        app.Run();
    }
}
```  
  

## 구조 설명

**1. Repository 계층 분리**
- `ICacheRepository<T>`: Redis 캐시 전용 인터페이스
- `IDatabaseRepository<T>`: DB 전용 인터페이스
- 제네릭을 사용해서 다른 엔티티(아이템, 길드 등)에도 쉽게 적용 가능하다

**2. CacheService 핵심 로직**
- **읽기**: Redis → DB → Redis 저장 순서로 처리
- **쓰기**: DB 저장 → Redis 저장 순서로 처리
- 에러 발생 시에도 데이터 정합성을 보장한다

**3. 사용하기 쉬운 구조**
- Controller에서는 Service만 호출하면 된다
- 캐시 로직은 Service에서 자동으로 처리된다
  

다음은 설정 파일이다:  
```
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GameDB;Trusted_Connection=true;TrustServerCertificate=true;",
    "Redis": "localhost:6379"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "CacheService": "Debug"
    }
  },
  "AllowedHosts": "*"
}
```


## 다른 엔티티 추가하는 방법
아이템 시스템을 추가하고 싶다면 이렇게 하면 된다:

```
// Models/Item.cs
public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public int Rarity { get; set; }
    public long Price { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Repositories/ItemDatabaseRepository.cs
public class ItemDatabaseRepository : IDatabaseRepository<Item>
{
    private readonly string _connectionString;

    public ItemDatabaseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<Item?> GetByIdAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = "SELECT * FROM Items WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Item>(sql, new { Id = id });
    }

    public async Task<Item> CreateAsync(Item item)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO Items (Name, Type, Rarity, Price, CreatedAt)
            OUTPUT INSERTED.*
            VALUES (@Name, @Type, @Rarity, @Price, @CreatedAt)";

        item.CreatedAt = DateTime.UtcNow;
        return await connection.QuerySingleAsync<Item>(sql, item);
    }

    public async Task<Item> UpdateAsync(Item item)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            UPDATE Items 
            SET Name = @Name, Type = @Type, Rarity = @Rarity, Price = @Price
            OUTPUT INSERTED.*
            WHERE Id = @Id";

        return await connection.QuerySingleAsync<Item>(sql, item);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = "DELETE FROM Items WHERE Id = @Id";
        var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
        return affectedRows > 0;
    }
}

// Controllers/ItemController.cs
[ApiController]
[Route("api/[controller]")]
public class ItemController : ControllerBase
{
    private readonly ICacheService<Item> _itemService;

    public ItemController(ICacheService<Item> itemService)
    {
        _itemService = itemService;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Item>> GetItem(int id)
    {
        var item = await _itemService.GetAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Item>> CreateItem([FromBody] CreateItemRequest request)
    {
        var item = new Item
        {
            Name = request.Name,
            Type = request.Type,
            Rarity = request.Rarity,
            Price = request.Price
        };

        var createdItem = await _itemService.CreateAsync(item);
        return CreatedAtAction(nameof(GetItem), new { id = createdItem.Id }, createdItem);
    }
}

// Program.cs에 추가할 DI 설정
// Repository 등록
builder.Services.AddScoped<ICacheRepository<Item>>(provider =>
{
    var redis = provider.GetService<IConnectionMultiplexer>();
    return new RedisCacheRepository<Item>(redis!, "game", TimeSpan.FromHours(1)); // 아이템은 1시간 캐시
});

builder.Services.AddScoped<IDatabaseRepository<Item>, ItemDatabaseRepository>();

// Service 등록
builder.Services.AddScoped<ICacheService<Item>, CacheService<Item>>();
```  


## 이 구조의 장점

**1. 중복 코드 제거**
- 제네릭 CacheService 하나로 모든 엔티티 처리 가능
- Repository 패턴으로 데이터 액세스 로직 분리

**2. 사용하기 쉬움**
- Controller에서 `_service.GetAsync(id)` 호출만 하면 캐시 로직 자동 처리
- 새로운 엔티티 추가도 DatabaseRepository만 구현하면 됨

**3. 성능 최적화**
- Redis 먼저 확인하여 DB 부하 감소
- 캐시 만료 시간을 엔티티별로 다르게 설정 가능

**4. 데이터 정합성**
- 쓰기 작업 시 DB와 Redis 모두 업데이트
- 트랜잭션 실패 시에도 일관성 유지

이 구조를 사용하면 게임 서버의 플레이어 데이터, 아이템, 길드 정보 등을 효율적으로 캐시할 수 있다. Redis의 빠른 응답성과 DB의 안정성을 모두 활용하는 구조다.



================================================
File: patterns/error_code_design.md
================================================
# ErrorCode 설계 패턴

게임 API 서버에서 에러를 관리하는 ErrorCode enum의 설계 원칙을 설명한다.

---

## 왜 ErrorCode가 필요한가?

```csharp
// ❌ 문자열로 에러를 전달하면:
return new Response { Error = "로그인 실패: 비밀번호가 틀립니다" };
// → 클라이언트가 문자열을 파싱해야 함
// → 다국어 지원 불가
// → 오타로 인한 버그 위험

// ✅ 숫자 코드로 에러를 전달하면:
return new Response { Result = ErrorCode.LoginFailPwNotMatch }; // 2006
// → 클라이언트가 숫자로 분기 처리
// → 다국어는 클라이언트가 숫자에 맞는 메시지를 표시
// → 컴파일 타임에 오타 검출
```

---

## 기본 구조

```csharp
// ErrorCode.cs
public enum ErrorCode : UInt16  // 0 ~ 65,535 범위
{
    None = 0,                   // 성공 (에러 없음)

    // Common 1000 ~
    UnhandleException = 1001,
    RedisFailException = 1002,
    // ...

    // Auth 2000 ~
    LoginFailPwNotMatch = 2006,
    // ...

    // Item 3000 ~
    CharReceiveFailInsert = 3011,
    // ...
}
```

### 왜 UInt16인가?

| 타입 | 범위 | 크기 |
|:---|:---|:---|
| `UInt16` | 0 ~ 65,535 | 2바이트 |
| `int` (기본) | -2B ~ 2B | 4바이트 |

- 에러 코드는 음수가 필요 없으므로 `UInt16`을 사용
- 네트워크 전송 시 2바이트로 충분 (패킷 크기 절약)
- 65,535개의 코드를 정의할 수 있어 게임 서버에 충분

---

## 범위별 분류 규칙

에러 코드를 **기능 영역별로 범위를 나눠** 관리한다. 숫자만 보고도 어떤 기능에서 발생한 에러인지 즉시 파악할 수 있다.

| 범위 | 영역 | 예시 |
|:---|:---|:---|
| **0** | 성공 | `None = 0` |
| **1000 ~** | 공통 (인프라) | 네트워크 오류, Redis 장애, 요청 파싱 실패, 버전 불일치 |
| **2000 ~** | 인증/계정 | 로그인 실패, 토큰 만료, 계정 중복 |
| **2100 ~** | 친구 | 친구 요청 실패, 이미 친구, 자기 자신에게 요청 |
| **2200 ~** | 게임 플레이 | 게임 데이터 로드 실패, 잠긴 게임 |
| **3000 ~** | 아이템 | 아이템 수령 실패, 이미 보유, 재화 부족 |
| **4000 ~** | DB 연결 | MySQL 커넥션 실패 |
| **5000 ~** | 마스터 데이터 | 기획 데이터 로드 실패 |
| **6000 ~** | 유저 정보 | 유저 조회 실패, 유저 없음 |
| **8000 ~** | 메일 | 메일 조회/수령/삭제 실패 |
| **9000 ~** | 출석 | 출석 체크 실패, 이미 출석 |

### 하위 번호 규칙

같은 기능 내에서도 **실패 원인**을 구분한다.

```
2001 = CreateUserFailException      ← 예외 발생
2002 = CreateUserFailNoNickname     ← 닉네임 누락
2003 = CreateUserFailDuplicateNickname ← 닉네임 중복
```

일반적인 접미사 패턴:

| 접미사 | 의미 |
|:---|:---|
| `~Exception` | try-catch에서 잡힌 예외 |
| `~NotExist` / `~NotFound` | 조회 결과 없음 |
| `~AlreadyExist` / `~Duplicate` | 중복 데이터 |
| `~Insert` / `~Update` / `~Delete` | DB 조작 실패 |
| `~NotMatch` / `~Mismatch` | 값 불일치 (비밀번호, 토큰) |

---

## 클라이언트와의 약속

### 응답 구조

모든 API 응답에는 `Result` 필드가 포함된다.

```json
// 성공
{ "Result": 0, "AuthToken": "abc123..." }

// 실패
{ "Result": 2006 }
```

### 클라이언트 측 처리

```
if (response.Result == 0) {
    // 성공 처리
} else if (response.Result >= 1000 && response.Result < 2000) {
    // 공통 에러 → "서버 오류가 발생했습니다" 표시
} else if (response.Result >= 2000 && response.Result < 2100) {
    // 인증 에러 → 에러 코드별 메시지 표시
    // 2006: "비밀번호가 올바르지 않습니다"
} else {
    // 기타 에러
}
```

### 다국어 지원

에러 코드는 숫자이므로, 클라이언트가 언어별 메시지 테이블을 가지고 매핑한다.

```
// 한국어
2006 → "비밀번호가 올바르지 않습니다"

// 영어
2006 → "Password does not match"

// 일본어
2006 → "パスワードが一致しません"
```

---

## 참고

- 실제 ErrorCode 예시: `codes/GameAPIServer_Template/ErrorCode.cs`
- 프로젝트별 `ErrorCode.cs` 위치는 각 프로젝트 루트에 있음



================================================
File: patterns/why_di.md
================================================
# 왜 DI 패턴을 사용해야하나?
from: ChatGPT  
  
ASP.NET Core에서 DI(Dependency Injection) 패턴을 사용하는 이유는 **“DB가 바뀔 가능성” 하나 때문이 아니라, 애플리케이션 구조 전반의 품질을 높이기 위한 기본 설계 원칙을 지키기 위해서**다.
레포지터리가 MySQL에서 다른 DB로 바뀔 확률이 거의 없다 하더라도 DI는 여전히 의미가 크다.

아래에서 핵심 이유를 정리한다.

---

## 1. **결합도 감소 — 테스트하기 쉬운 구조를 만든다**

DI를 빼고 `new MySqlRepository()` 같은 방식으로 직접 객체를 생성하면, 해당 클래스는 MySQL 구현체에 **강하게 결합된다**.
그러면 다음 문제가 생긴다:

* 유닛 테스트 시 실제 DB 없이 테스트하기 어려움
* Mock Repository를 주입해 테스트하려면 코드 구조를 갈아엎어야 함
* 서비스 로직이 특정 저장소 구현체에 종속적이 되어 재사용성 감소

DI를 적용하면 테스트 시에는 Mock을, 런타임에서는 실제 Repository를 넣는 식으로 깔끔하게 처리할 수 있다.
**테스트 편의성 하나만으로도 DI의 가치는 충분하다**고 평가된다.

---

## 2. **관심사의 분리(SoC) — 서비스는 “무엇”을 하는지에만 집중한다**

DI는 서비스 레이어가 *레포지터리를 어떻게 생성하는지*에 대해 전혀 신경 쓰지 않도록 만들어준다.

예:

```csharp
public class UserService
{
    private readonly IUserRepository _repo;
    public UserService(IUserRepository repo)
    {
        _repo = repo;
    }

    public Task<User> GetUser(int id)
    {
        return _repo.Get(id);
    }
}
```

UserService는 **레포지터리가 MySQL인지, PostgreSQL인지, MongoDB인지 전혀 모른다**.
서비스는 오직 “유저를 가져온다”는 책임만 갖게 되고, 인프라와 비즈니스 로직이 분리된다.

---

## 3. **유연성 — DB가 안 바뀌어도 ‘구현체 변경’은 자주 일어난다**

DB 타입은 안 바뀔지 몰라도, 다음은 얼마든지 바뀔 수 있다:

* MySQL 드라이버 변경 (예: Dapper → EF Core → 자체 구현)
* 캐시 레이어 추가
  (예: Decorator 패턴으로 캐시 추가한 CacheRepository)
* 로깅/성능 측정을 위한 래퍼
* 분산 트랜잭션 적용 여부
* 샤딩/리딩-레플리카 구현 변경

이런 변경을 할 때 DI가 없으면 기존 코드가 죄다 영향을 받는데,
DI 구조에서는 구현체만 DI Container에서 바꾸면 끝이다.

즉, **DB 스펙이 바뀌지 않아도 구현 디테일은 자주 바뀐다**.

---

## 4. **ASP.NET Core 생태계 자체가 DI 중심으로 설계됨**

ASP.NET Core의 대부분 기능이 DI 컨테이너를 중심으로 동작한다:

* Logging
* Configuration
* Authentication/Authorization
* EF Core DbContext
* Middleware

따라서 DI를 쓰지 않으면 .NET Core 기본 철학과 맞지 않는 구조가 되고, 확장성도 떨어진다.

---

## 5. **테스트, 유지보수, 확장성 측면에서의 비용 대비 효과가 매우 큼**

DI 패턴은 코드량이 늘어서 비용이 있다고 여겨지지만, 장기 유지보수에서는 비용이 오히려 줄어든다.

* 버그 수정 시 영향 범위가 좁아짐
* 테스트 자동화가 쉬워짐
* 새 기능을 붙일 때 기존 서비스 코드 수정량이 줄어듦
* 의존성 구조가 명확해져서 팀원 간 협업이 쉬워짐

**“DB가 안 바뀌니까 DI가 필요 없다”는 단기적 관점이고,
DI는 장기 유지보수 비용을 줄이는 구조를 만든다는 게 핵심이다**.

---

## 결론

레포지터리 DB 타입이 바뀔 가능성이 낮아도 DI를 사용하는 이유는 다음과 같다:

* 테스트 용이성 확보
* 결합도 낮추기
* 관심사 분리
* 향후 구현체 변경 가능성 대응
* ASP.NET Core 생태계와의 통일성 확보

즉, **유연성과 유지보수성**을 위해 DI는 필수적인 설계 패턴이다.  


================================================
File: references/APIServer_Directory.md
================================================
# API 서버의 디렉토리 구성 예
- Controllers: 컨트롤러 클래스를 저장하는 곳
- Dto: 클라이언트와 데이터를 주고받을 때 모델 클래스를 저장하는 곳
- Infrastructure: DB, 메일, 기타 시스템 연동 등의 설정파일을 저장하는 곳
- Libraries: 프로젝트 내 공통 처리를 저장하는 곳
- Repositories: DB 접근 처리를 저장하는 곳
- Services : 비즈니스 로직을 저장하는 곳  

<pre>
│   ├── Controllers
│   │   └── TodoController.cs
│   ├── Dto
│   │   ├── CreateRequestDto.cs
│   │   └── CreateResponseDto.cs
│   ├── Infrastructure
│   │   ├── Configure
│   │   │   ├── DatabaseConfigure.cs
│   │   │   └── ServiceConfigure.cs
│   │   └── Database
│   │       └── TodoEntity.cs
│   ├── Libraries
│   │   ├── Exception
│   │   │   └── ValidationException.cs
│   │   └── Middleware
│   │       └── CustomExceptionMiddleware.cs
│   ├── Program.cs
│   ├── Properties
│   │   └── launchSettings.json
│   ├── Repositories
│   │   └── Todo
│   │       ├── ITodoRepository.cs
│   │       └── TodoRepository.cs
│   ├── Services
│   │   └── Todo
│   │       ├── ITodoService.cs
│   │       └── TodoService.cs
│   ├── TodoApi.csproj
│   ├── TodoApi.sln	
│   ├── TodoApi.http
│   ├── appsettings.Development.json
│   └── appsettings.json
├── Api.Tests
│   ├── Api.Tests.csproj
│   ├── Api.Tests.sln	
│   ├── GlobalUsings.cs
│   └── UnitTests
│       └── Services
│           └── TodoServiceTests.cs
└── dotnet-todo-web-api.sln	
</pre>   
    
---  
  
</br>   
</br>    

# 디렉토리 구조는 어떻게 구성하는 것이 좋은가? 
from: ChatGPT  

정석 하나가 딱 있는 건 아니지만, **ASP.NET Core Web API에서 많이 쓰이는 패턴**은 몇 가지로 거의 수렴한다.
특히 질문한 것처럼 `Models` 안에 `DTOs`를 넣느냐, `DTOs`를 `Models`와 동급으로 두느냐는 **“Models에 뭐가 들어가느냐”**에 따라 달라진다.

아래를 기준으로 보면 된다.

---

## 1. 먼저 개념부터 정리: Entity vs DTO

* **Entity / Domain Model**

  * DB와 매핑되거나 비즈니스 규칙을 담는 핵심 모델
  * 예: `User`, `Order`, `InventoryItem` 등
* **DTO (Data Transfer Object)**

  * API 입출력용, “전달용” 모델
  * Request DTO: `CreateUserRequest`, `UpdateUserRequest`
  * Response DTO: `UserResponse`, `PagedResult<T>` 등
  * 외부 계약(API 스펙)에 가깝고, 도메인 모델과는 독립적으로 변경될 수 있음

이 둘은 **책임과 라이프사이클이 다르다**
→ 폴더도 분리하는 게 보편적이다.

---

## 2. 단일 Web API 프로젝트에서의 흔한 구조

프로젝트 하나만 두는 비교적 단순한 구조라면, 보통 이렇게 많이 나눈다.

```text
MyApp
 ├─ Controllers
 ├─ Domain          // 도메인 모델, 엔티티
 │   ├─ Entities
 │   └─ ValueObjects
 ├─ Services        // 비즈니스 서비스 (Application 서비스)
 ├─ Dtos            // API용 DTO
 │   ├─ Requests
 │   └─ Responses
 ├─ Repositories    // 인터페이스 + 구현(간단한 경우)
 └─ Infrastructure  // DB 관련 구현, 외부 시스템 등
```

### 여기서 질문에 대한 답

* `Models`라는 폴더에 **Entity**를 두고 있다면:

  * `Models/DTOs` 로 넣는 것보다
  * `Dtos`를 **`Models`와 동급**으로 빼는 쪽이 더 일반적이다.
  * 이유: DTO는 “API 레벨 계약”, Entity는 “도메인 레벨 모델”이라 책임이 다르기 때문이다.

예를 들면 이렇게:

```text
 ├─ Models          // 도메인 엔티티
 │   ├─ User.cs
 │   └─ Item.cs
 ├─ Dtos            // API 입출력용
 │   ├─ User
 │   │   ├─ UserResponse.cs
 │   │   └─ CreateUserRequest.cs
 │   └─ Item
 │       └─ ItemResponse.cs
```

`Models/DTOs` 로 넣으면 “DTO도 모델의 한 종류”라는 느낌이 강해져서
나중에 **Entity와 DTO가 뒤섞이는 코드**가 되기 쉽다.

---

## 3. 멀티 프로젝트(계층형) 구조에서의 보편적인 패턴

조금 규모가 있으면 보통 이렇게 나눈다.

```text
src
 ├─ MyApp.Api             // Web API (Controller, API DTO)
 ├─ MyApp.Application     // 유스케이스/서비스, Application DTO
 ├─ MyApp.Domain          // Entity, ValueObject, Domain 서비스
 └─ MyApp.Infrastructure  // EF Core, Repository 구현, 외부 연동
```

각 프로젝트 내부는 대략 이렇게 간다.

### `MyApp.Domain`

```text
MyApp.Domain
 ├─ Entities
 ├─ ValueObjects
 └─ Services   // 도메인 서비스(규칙)
```

### `MyApp.Application`

```text
MyApp.Application
 ├─ Interfaces  // IUserRepository, IEmailSender 등
 ├─ Services    // UseCase(Service) 레벨
 └─ Dtos
     ├─ UserDto.cs
     └─ ...
```

### `MyApp.Api`

```text
MyApp.Api
 ├─ Controllers
 └─ Contracts or Dtos
     ├─ Requests
     └─ Responses
```

여기서도 DTO는 **Api/Contracts** 폴더에 두고,
Domain의 Entity와는 분리하는 것이 거의 표준에 가깝다.

---

## 4. 타입별이 아니라 기능별(Vertical Slice / Feature 폴더) 구조도 많이 쓴다

최근에는 폴더를 타입별이 아니라 “기능별”로 묶는 형태도 많이 쓴다.

```text
MyApp.Api
 ├─ Features
 │   ├─ Users
 │   │   ├─ UsersController.cs
 │   │   ├─ UserDto.cs
 │   │   ├─ CreateUserRequest.cs
 │   │   └─ CreateUserHandler.cs (또는 Service)
 │   └─ Items
 │       ├─ ItemsController.cs
 │       ├─ ItemDto.cs
 │       └─ ...
 └─ Program.cs
```

이 패턴의 특징은 다음과 같다.

* “User 관련 코드”가 한 폴더에 모여서

  * Controller
  * DTO
  * Handler / Service
    를 한 번에 보기 쉬움
* 규모 커질수록 **팀/기능 단위로 나누기 좋다**

이 방식에서도 DTO는 Entity와 폴더를 섞지 않고,
각 Feature 폴더 안에 `UserDto`, `CreateUserRequest` 등을 둔다.

---

## 5. 정리: DTO 폴더를 어떻게 두는 게 좋나
질문에 대한 결론만 다시 정리하면 다음과 같다.

1. **보편적인 패턴**

   * 도메인 Entity(Model)와 DTO를 **물리적으로 분리**한다.
   * DTO는 보통 `/Dtos`, `/Contracts`, `/Features/...` 등에 두고
     도메인 모델이 있는 `/Models` 또는 `/Domain`과는 별도로 관리한다.

2. **Models/DTOs처럼 Models 하위에 두는 경우**

   * 작은/단기 프로젝트에서 “일단 폴더 하나 더 만들기 귀찮다” 수준이면 할 수는 있지만
   * 책임이 섞이기 쉬워서 규모가 커지면 후회하기 쉽다.

3. **API 스펙 중심으로 생각하는 게 좋다**

   * DTO = 클라이언트와의 계약(API)
   * Entity = 내부 도메인 모델
     이 관점으로 보면 자연스럽게 폴더를 나눌 수 있다.

---
  
</br>    
</br>     
     
## 1. 왜 굳이 프로젝트를 나누는가?
솔직히 말해서 **초기에는 안 나눠도 된다**
그런데도 다들 어느 시점부터는 나누는 이유는 대략 다음 네 가지 때문이다.

### 1) 참조 방향을 강제로 고정할 수 있다
예를 들어 이렇게 나눈다고 하자.

* `Game.Api` (ASP.NET Core Web API)
* `Game.Application` (서비스, 유즈케이스)
* `Game.Domain` (엔티티, 도메인 로직)
* `Game.Infrastructure` (EF Core, Redis, 외부 시스템 구현 등)

이때 참조 방향을 이렇게만 허용한다.

* `Game.Api` → `Game.Application`
* `Game.Application` → `Game.Domain`
* `Game.Infrastructure` → `Game.Application`, `Game.Domain`
* **반대로는 참조 불가**

프로젝트를 나누면 csproj 참조 덕분에 **물리적으로 역참조를 막을 수 있다**
단일 프로젝트에서는:

* Controller에서 바로 DbContext 들고 와서 쿼리하고
* 도메인 모델이 HttpContext나 DTO를 알고
  이런 “편하지만 나중에 골치 아픈” 참조가 쉽게 생긴다.

즉, 나누는 이유 중 하나는 **“팀원이(나 포함) 막 짓지 못하게 레일을 깔아놓는 것”**이다.

---

### 2) 테스트 방식과 실행 환경을 나누기 쉽다

* `Game.Domain`, `Game.Application`은 별도 콘솔 없이도 테스트 가능
  → 순수 로직 테스트(유닛 테스트) 돌리기 좋다
* `Game.Api`는 실제 HTTP 파이프라인, 미들웨어까지 포함한 통합 테스트에 집중
* `Game.Infrastructure`는 진짜 DB/Redis 붙인 통합 테스트 전용으로 쓸 수 있다

한 프로젝트에 모든 걸 다 넣으면,
레퍼런스/설정/테스트가 다 섞이면서 점점 손 대기 싫어지는 코드가 된다.

---

### 3) 모듈/도메인 재사용성이 올라간다

예를 들어:

* 수집형 게임 A
* 차기 프로젝트 수집형 게임 B

둘 다 공통으로 쓸 수 있는 게 있을 수 있다.

* `Domain` 레벨: 돈/재화, 인벤토리, 우편, 보상 분배, 쿨타임, 기간제 아이템 등
* `Application` 레벨: “보상 지급 유즈케이스”, “출석 보상 처리”, “메일 보내기” 등

이걸 `Game.Common.Domain`, `Game.Common.Application` 같은 형태로 잘 분리해두면
**다음 프로젝트에서 그대로 가져다 쓸 수 있다**

단일 Web API 프로젝트에 섞여 있으면, 재사용하려고 할 때
컨트롤러, ASP.NET Core 의존성, DB 설정 등과 엉겨서 떼어내기 매우 귀찮다.

---

### 4) 팀 규모가 커질수록 “경계”가 필요하다
사람이 많아지면 구조가 느슨하면 느슨한 대로 다들 본인 스타일대로 건드리기 시작한다.

* 어떤 사람은 Controller에서 바로 DbContext 사용
* 어떤 사람은 Service를 만들지만, 그 안에서 또 HTTP 의존성 사용
* 어떤 사람은 도메인 엔티티 안에 로깅/캐시 코드 넣음

프로젝트를 나누고, 각 프로젝트의 역할을 정해두면:

* Domain 쪽 담당자는 HTTP/DB 모르고, 도메인 규칙에 집중
* Application 담당자는 유즈케이스, 트랜잭션, 여러 도메인 조합에 집중
* API 담당자는 라우팅, 인증/인가, 버전 관리에 집중

이렇게 **관심사/역할을 사람 단위로도 나누기 쉬워진다**.

---

## 2. 수집형(가챠, 캐릭터/카드 수집) 모바일 게임 서버 구조라면?

온라인 수집형 게임 서버 기준으로, 현실적인 구조를 하나 추천하겠다.
“처음부터 너무 잘게 쪼개기보단, 중간 정도 복잡도”를 목표로 둔다.

### 2-1. 솔루션 구조 예시

```text
src
 ├─ Game.Api               // ASP.NET Core Web API
 ├─ Game.Application       // 유즈케이스/서비스
 ├─ Game.Domain            // 도메인 모델/규칙
 └─ Game.Infrastructure    // DB, Redis, 외부 연동 구현
```

각 프로젝트 역할은 이렇게 잡는다.

---

### 2-2. Game.Domain – 게임 규칙의 심장

여기에 들어가는 것들이다.

* 엔티티

  * `Player`, `Inventory`, `Item`, `Hero`, `Stage`, `Quest`, `Mail`, `GachaPool` 등
* 값 객체(Value Object)

  * `Currency`, `DropRate`, `LevelInfo`, `Cooldown`, `Reward` 등
* 도메인 서비스

  * 가챠 롤 연산
  * 보상 계산 로직
  * 경험치/레벨업 계산
  * 스태미나 소모/회복 로직 등

여기서는 **ASP.NET Core, EF Core, HttpContext, ILogger** 같은 걸 절대 모르게 만든다.
“게임 규칙만 아는 순수한 C# 라이브러리” 느낌으로 유지하는 것이 중요하다.

---

### 2-3. Game.Application – 유즈케이스(플로우/오케스트레이션)

여기에는 이런 코드가 들어간다.

* 유즈케이스/서비스

  * `LoginService`
  * `GachaService`
  * `StageService`
  * `InventoryService`
  * `MailService`
* 인터페이스

  * `IPlayerRepository`, `IInventoryRepository`, `IGachaRepository`
  * `IMailSender`, `INotificationService`
  * `IRandomProvider` (테스트를 위한 RNG 추상화 등)
* Application 레벨 DTO (내부 이동용)

  * `PlayerDto`, `InventoryDto`, `RewardDto`

여기서 하는 일은 “한 요청에서 어떤 도메인 기능들을 조합해서 실행할지”이다.

예를 들어 `GachaService.RollAsync()`는:

1. Player 로드
2. 재화 부족 여부 확인
3. 재화 차감
4. 도메인 `GachaDomainService.Roll()` 호출하여 결과 생성
5. 인벤토리에 아이템/캐릭터 추가
6. 로그 작성/이벤트 발행
7. 트랜잭션 커밋

흐름은 Application이, 세부 규칙은 Domain이 맡는다.

---

### 2-4. Game.Infrastructure – 실제 구현체 모음
여기에는 인프라 의존 코드가 들어간다.

* EF Core DbContext 및 엔티티 매핑
* `IPlayerRepository`의 MySQL 구현
* Redis 캐시 구현
* Kafka, RabbitMQ, gRPC 클라이언트, 외부 결제 서버 연동 등

이 프로젝트만 `Microsoft.EntityFrameworkCore`, `StackExchange.Redis` 같은 라이브러리를 의존하도록 제한한다.

---

### 2-5. Game.Api – ASP.NET Core Web API (입출구)
여기에 들어갈 것들이다.

* Controllers (`PlayerController`, `GachaController`, `StageController` 등)
* API DTO (Request/Response)

  * `LoginRequest`, `LoginResponse`
  * `RollGachaRequest`, `RollGachaResponse`
* 필터, 미들웨어 (인증, 로깅, 에러 처리 등)
* DI 등록 (`IPlayerRepository -> PlayerRepository`, `IGachaService -> GachaService`)

컨트롤러는 최대한 얇게 만든다.

```csharp
[ApiController]
[Route("api/gacha")]
public class GachaController : ControllerBase
{
    private readonly IGachaService _gacha;

    public GachaController(IGachaService gacha)
    {
        _gacha = gacha;
    }

    [HttpPost("roll")]
    public async Task<ActionResult<RollGachaResponse>> Roll([FromBody] RollGachaRequest request)
    {
        var result = await _gacha.RollAsync(request.PlayerId, request.PoolId, request.Count);

        return Ok(new RollGachaResponse
        {
            Rewards = result.Rewards.Select(r => new RewardDto { ... }).ToList()
        });
    }
}
```

컨트롤러는 **HTTP → Application 호출 → HTTP 응답으로 변환** 정도에만 집중하고,
비즈니스 규칙/DB 접근/트랜잭션 등은 전부 아래 레이어에 위임한다.

---

## 3. 현실적인 조언
마지막으로 현실적인 쪽으로 요약하겠다.

1. **처음부터 무조건 4개 프로젝트로 쪼개라는 건 아니다**

   * 혼자 개발하거나 프로토타입이라면 `Game.Api` 하나에 다 넣고 시작해도 된다.
   * 다만 Entity, DTO, Service 정도만이라도 폴더로 구분해두는 걸 추천한다.

2. **어느 정도 규모가 보여갈 때 쯤 나누는게 좋다**

   * 기능이 많아지고, 팀원이 2~3명 이상이 되면
     Domain을 분리하고, Application/Infrastructure를 떼어내는 걸 고려할 만하다.

3. **수집형 모바일 게임 서버라면**

   * 도메인이 풍부하고 규칙이 복잡해지기 쉬운 장르라서,
   * Domain/Application/Infrastructure 분리가 가져다주는 이득이 크다.
   * 특히 가챠, 보상, 재화, 쿨타임 등은 재사용성과 규칙 검증의 가치가 높다.

---   
   
</br>     

## 예: 수집형 RPG 게임 서버  
**수집형 RPG 하나 만든다고 가정**하고 각 프로젝트에 *무엇을* 넣을지, 그리고 *하나의 유즈케이스가 어떻게 4개 프로젝트를 타고 흐르는지*까지 구체적으로 적어보겠다.

프로젝트 구조는 이것을 기준으로 한다.

```text
src
 ├─ MyApp.Api             // Web API (Controller, API DTO)
 ├─ MyApp.Application     // 유즈케이스/서비스, Application DTO
 ├─ MyApp.Domain          // Entity, ValueObject, Domain 서비스
 └─ MyApp.Infrastructure  // EF Core, Repository 구현, 외부 연동
```

예시는 대표적인 수집형 RPG 도메인들(플레이어, 영웅, 가챠, 인벤토리, 스테이지, 우편 등)을 기준으로 한다.

---

## 1. MyApp.Domain – 게임 규칙의 심장

**핵심 개념**

* “이 게임이 어떤 규칙으로 돌아가는가?”를 표현하는 레이어다.
* HTTP, DB, EF Core, 컨트롤러, 로깅 같은 건 전혀 몰라야 한다.
* 여긴 그냥 **순수 C# 라이브러리**라고 생각하면 된다.

### 1.1. 폴더 구조 예시

```text
MyApp.Domain
 ├─ Players
 │   ├─ Player.cs
 │   └─ PlayerLevelPolicy.cs
 ├─ Heroes
 │   ├─ Hero.cs
 │   └─ HeroRarity.cs
 ├─ Inventory
 │   ├─ Item.cs
 │   ├─ ItemInstance.cs
 │   └─ Inventory.cs
 ├─ Gacha
 │   ├─ GachaPool.cs
 │   ├─ GachaResult.cs
 │   └─ GachaDomainService.cs
 ├─ Stage
 │   ├─ Stage.cs
 │   ├─ StageResult.cs
 │   └─ StageDomainService.cs
 ├─ Mail
 │   ├─ Mail.cs
 │   └─ MailAttachment.cs
 ├─ Common
 │   ├─ Currency.cs          // ValueObject
 │   ├─ Reward.cs            // ValueObject
 │   ├─ TimeSpanRange.cs     // 기간제 아이템 등
 │   └─ IRandomGenerator.cs  // RNG 추상화(도메인에서 쓸 인터페이스)
 └─ DomainExceptions
     ├─ NotEnoughCurrencyException.cs
     └─ InvalidStateException.cs
```

### 1.2. 예시 코드

#### Player / Currency

```csharp
// Value Object
public readonly struct Currency
{
    public int Gems { get; }
    public int Gold { get; }

    public Currency(int gems, int gold)
    {
        if (gems < 0 || gold < 0)
            throw new ArgumentException("Currency cannot be negative.");

        Gems = gems;
        Gold = gold;
    }

    public Currency Add(Currency other)
        => new Currency(Gems + other.Gems, Gold + other.Gold);

    public Currency Subtract(Currency other)
    {
        if (Gems < other.Gems || Gold < other.Gold)
            throw new NotEnoughCurrencyException();
        return new Currency(Gems - other.Gems, Gold - other.Gold);
    }
}
```

#### Player 엔티티

```csharp
public class Player
{
    public long Id { get; private set; }
    public int Level { get; private set; }
    public Currency Currency { get; private set; }
    public int Stamina { get; private set; }
    public DateTime LastLoginAt { get; private set; }

    private readonly List<Hero> _heroes = new();
    public IReadOnlyCollection<Hero> Heroes => _heroes;

    protected Player() { } // ORM용 기본 생성자

    public Player(long id)
    {
        Id = id;
        Level = 1;
        Currency = new Currency(0, 0);
        Stamina = 100;
        LastLoginAt = DateTime.UtcNow;
    }

    public void AddCurrency(Currency delta) => Currency = Currency.Add(delta);

    public void SpendCurrency(Currency cost) => Currency = Currency.Subtract(cost);

    public void AddHero(Hero hero) => _heroes.Add(hero);

    public void ChangeLevel(int newLevel)
    {
        if (newLevel < Level)
            throw new InvalidOperationException("Level cannot decrease.");
        Level = newLevel;
    }
}
```

#### GachaDomainService

```csharp
public class GachaDomainService
{
    private readonly IRandomGenerator _random;

    public GachaDomainService(IRandomGenerator random)
    {
        _random = random;
    }

    public IReadOnlyList<Hero> Roll(GachaPool pool, int count)
    {
        var results = new List<Hero>(count);
        for (int i = 0; i < count; i++)
        {
            var entry = pool.Pick(_random.NextDouble());
            var hero = new Hero(entry.HeroId, entry.Rarity);
            results.Add(hero);
        }

        return results;
    }
}
```

여기서는 “주어진 풀 안에서 확률에 따라 영웅을 뽑는다”는 **규칙만** 표현하고,
DB 저장, 로그, HTTP 응답 같은 건 전혀 몰라야 한다.

---

## 2. MyApp.Application – 유즈케이스 / 서비스

**핵심 개념**

* “한 요청에서 어떤 도메인 기능을 조합해서 무엇을 할지”를 정의하는 레이어다.
* 트랜잭션, 여러 Aggregate 조합, 도메인 서비스 호출을 묶어서 처리한다.
* DB 구현은 모르는 대신, **Repository 인터페이스**만 알고 의존한다.

### 2.1. 폴더 구조 예시

```text
MyApp.Application
 ├─ Interfaces
 │   ├─ Repositories
 │   │   ├─ IPlayerRepository.cs
 │   │   ├─ IHeroRepository.cs
 │   │   ├─ IInventoryRepository.cs
 │   │   └─ IGachaRepository.cs
 │   ├─ Services
 │   │   ├─ ITimeProvider.cs
 │   │   └─ IUnitOfWork.cs
 │   └─ External
 │       ├─ IAuthService.cs
 │       └─ INotificationService.cs
 ├─ Dtos
 │   ├─ PlayerDto.cs
 │   ├─ HeroDto.cs
 │   └─ RewardDto.cs
 ├─ Gacha
 │   ├─ RollGachaCommand.cs
 │   ├─ RollGachaResult.cs
 │   └─ GachaService.cs
 ├─ Stage
 │   ├─ ClearStageCommand.cs
 │   ├─ ClearStageResult.cs
 │   └─ StageService.cs
 ├─ Player
 │   ├─ LoginCommand.cs
 │   └─ PlayerService.cs
 └─ Mail
     ├─ ReceiveMailCommand.cs
     └─ MailService.cs
```

### 2.2. Repository 인터페이스

```csharp
public interface IPlayerRepository
{
    Task<Player?> GetAsync(long id, CancellationToken ct = default);
    Task AddAsync(Player player, CancellationToken ct = default);
    Task UpdateAsync(Player player, CancellationToken ct = default);
}
```

### 2.3. Gacha 유즈케이스 예시

#### Command / Result DTO

```csharp
public class RollGachaCommand
{
    public long PlayerId { get; init; }
    public int PoolId { get; init; }
    public int Count { get; init; }
}

public class RollGachaResult
{
    public PlayerDto Player { get; init; } = null!;
    public IReadOnlyList<HeroDto> Heroes { get; init; } = Array.Empty<HeroDto>();
}
```

#### GachaService

```csharp
public class GachaService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IGachaRepository _gachaRepository;
    private readonly IUnitOfWork _uow;
    private readonly GachaDomainService _gachaDomainService;

    public GachaService(
        IPlayerRepository playerRepository,
        IGachaRepository gachaRepository,
        IUnitOfWork uow,
        GachaDomainService gachaDomainService)
    {
        _playerRepository = playerRepository;
        _gachaRepository = gachaRepository;
        _uow = uow;
        _gachaDomainService = gachaDomainService;
    }

    public async Task<RollGachaResult> RollAsync(RollGachaCommand command, CancellationToken ct = default)
    {
        // 1. 유저/가챠풀 로드
        var player = await _playerRepository.GetAsync(command.PlayerId, ct)
            ?? throw new InvalidOperationException("Player not found");
        var pool = await _gachaRepository.GetPoolAsync(command.PoolId, ct)
            ?? throw new InvalidOperationException("Gacha pool not found");

        // 2. 재화 차감 (도메인 규칙)
        var cost = pool.GetCostFor(command.Count);
        player.SpendCurrency(cost);

        // 3. 도메인 서비스로 뽑기
        var heroes = _gachaDomainService.Roll(pool, command.Count);
        foreach (var hero in heroes)
        {
            player.AddHero(hero);
        }

        // 4. 저장 + 트랜잭션 커밋
        await _playerRepository.UpdateAsync(player, ct);
        await _uow.CommitAsync(ct);

        // 5. 결과 DTO 매핑
        return new RollGachaResult
        {
            Player = PlayerDto.From(player),
            Heroes = heroes.Select(HeroDto.From).ToList()
        };
    }
}
```

여기서 Application은:

* 도메인 규칙을 호출하고
* 여러 Aggregate를 묶어 트랜잭션 처리하며
* 결과를 상위(API)가 쓰기 쉬운 DTO로 변환하는 역할을 한다.

---

## 3. MyApp.Infrastructure – EF Core / 외부 시스템 구현

**핵심 개념**

* Application에 정의된 인터페이스를 **실제로 MySQL/Redis/외부 서비스에 붙이는 구현체** 레이어다.
* 이 프로젝트만 EF Core, Redis, gRPC, HTTP 클라이언트 같은 라이브러리를 안다.

### 3.1. 폴더 구조 예시

```text
MyApp.Infrastructure
 ├─ Persistence
 │   ├─ GameDbContext.cs
 │   ├─ Configurations
 │   │   ├─ PlayerConfiguration.cs
 │   │   └─ HeroConfiguration.cs
 │   ├─ Repositories
 │   │   ├─ PlayerRepository.cs
 │   │   ├─ HeroRepository.cs
 │   │   └─ GachaRepository.cs
 │   └─ UnitOfWork.cs
 ├─ External
 │   ├─ AuthService.cs
 │   └─ NotificationService.cs
 └─ Random
     └─ DefaultRandomGenerator.cs
```

### 3.2. DbContext 예시

```csharp
public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<Hero> Heroes => Set<Hero>();
    public DbSet<GachaPool> GachaPools => Set<GachaPool>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PlayerConfiguration());
        modelBuilder.ApplyConfiguration(new HeroConfiguration());
        // ...
    }
}
```

### 3.3. Repository 구현

```csharp
public class PlayerRepository : IPlayerRepository
{
    private readonly GameDbContext _db;

    public PlayerRepository(GameDbContext db)
    {
        _db = db;
    }

    public Task<Player?> GetAsync(long id, CancellationToken ct = default)
    {
        return _db.Players
            .Include(p => p.Heroes)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public Task AddAsync(Player player, CancellationToken ct = default)
        => _db.Players.AddAsync(player, ct).AsTask();

    public Task UpdateAsync(Player player, CancellationToken ct = default)
    {
        _db.Players.Update(player);
        return Task.CompletedTask;
    }
}
```

### 3.4. UnitOfWork

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly GameDbContext _db;

    public UnitOfWork(GameDbContext db)
    {
        _db = db;
    }

    public Task<int> CommitAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
```

### 3.5. Domain에서 쓰는 Random 구현체

```csharp
public class DefaultRandomGenerator : IRandomGenerator
{
    private readonly Random _random = new();

    public double NextDouble() => _random.NextDouble();
}
```

---

## 4. MyApp.Api – Web API (컨트롤러 / API DTO)

**핵심 개념**

* “HTTP 요청 ↔ Application 호출 ↔ HTTP 응답”을 담당하는 최상위 레이어다.
* Swagger, 인증, 미들웨어, 버전 관리, 필터 등은 여기서 처리한다.
* 도메인 모델을 직접 노출하지 않고, **API 전용 DTO**를 쓰는 것이 일반적이다.

### 4.1. 폴더 구조 예시

```text
MyApp.Api
 ├─ Controllers
 │   ├─ PlayerController.cs
 │   ├─ GachaController.cs
 │   └─ StageController.cs
 ├─ Dtos
 │   ├─ Gacha
 │   │   ├─ RollGachaRequest.cs
 │   │   └─ RollGachaResponse.cs
 │   └─ Player
 │       ├─ PlayerResponse.cs
 │       └─ LoginRequest.cs
 ├─ Filters
 │   └─ ApiExceptionFilter.cs
 ├─ Startup (Program.cs / DI 설정)
 └─ Mapping
     └─ ApiMappingProfile.cs (필요하면 AutoMapper 등)
```

### 4.2. API Request/Response DTO

```csharp
public class RollGachaRequest
{
    public int PoolId { get; set; }
    public int Count { get; set; } = 1;
}

public class RollGachaResponse
{
    public PlayerResponse Player { get; set; } = null!;
    public List<HeroResponse> Heroes { get; set; } = new();
}

public class PlayerResponse
{
    public long Id { get; set; }
    public int Level { get; set; }
    public int Gems { get; set; }
    public int Gold { get; set; }
}

public class HeroResponse
{
    public long HeroId { get; set; }
    public string Rarity { get; set; } = "";
    public int Level { get; set; }
}
```

### 4.3. Controller 예시

```csharp
[ApiController]
[Route("api/gacha")]
public class GachaController : ControllerBase
{
    private readonly GachaService _gachaService;
    private readonly ICurrentPlayerProvider _currentPlayer; // 토큰에서 playerId 꺼내는 용도라고 가정

    public GachaController(GachaService gachaService, ICurrentPlayerProvider currentPlayer)
    {
        _gachaService = gachaService;
        _currentPlayer = currentPlayer;
    }

    [HttpPost("roll")]
    public async Task<ActionResult<RollGachaResponse>> Roll([FromBody] RollGachaRequest request)
    {
        var cmd = new RollGachaCommand
        {
            PlayerId = _currentPlayer.PlayerId,
            PoolId = request.PoolId,
            Count = request.Count
        };

        var result = await _gachaService.RollAsync(cmd);

        var response = new RollGachaResponse
        {
            Player = new PlayerResponse
            {
                Id = result.Player.Id,
                Level = result.Player.Level,
                Gems = result.Player.Gems,
                Gold = result.Player.Gold
            },
            Heroes = result.Heroes.Select(h => new HeroResponse
            {
                HeroId = h.HeroId,
                Rarity = h.Rarity.ToString(),
                Level = h.Level
            }).ToList()
        };

        return Ok(response);
    }
}
```

### 4.4. Program.cs / DI 구성

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Application
builder.Services.AddScoped<GachaService>();

// Domain
builder.Services.AddScoped<GachaDomainService>();
builder.Services.AddSingleton<IRandomGenerator, DefaultRandomGenerator>();

// Infrastructure
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("GameDb"),
        new MySqlServerVersion(new Version(8, 0, 25))));

builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IGachaRepository, GachaRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var app = builder.Build();
app.MapControllers();
app.Run();
```

---

## 5. 한 유즈케이스(가챠 롤)가 4개 프로젝트를 타는 흐름

1. **클라이언트 → API**

   * `/api/gacha/roll` POST + `{ poolId, count }`
   * `GachaController`가 `RollGachaRequest` 바인딩

2. **API → Application**

   * Controller가 `RollGachaCommand` 생성
   * `GachaService.RollAsync(cmd)` 호출

3. **Application → Domain + Repository 인터페이스 사용**

   * `IPlayerRepository.GetAsync`, `IGachaRepository.GetPoolAsync`로 도메인 엔티티 로드
   * `player.SpendCurrency`, `GachaDomainService.Roll` 호출
   * `player.AddHero(hero)` 등 도메인 메서드 호출
   * `IPlayerRepository.UpdateAsync`, `IUnitOfWork.CommitAsync` 호출
   * 결과를 `RollGachaResult`로 반환

4. **Infrastructure에서 실제 동작**

   * `PlayerRepository`가 EF Core로 MySQL에서 Player 도큐먼트/테이블 로드
   * `GameDbContext.SaveChangesAsync`로 실제 DB에 반영
   * `DefaultRandomGenerator`가 실제 랜덤값 생성

5. **Application → API → 클라이언트**

   * Application의 Result를 API의 Response DTO로 매핑
   * JSON으로 내려간다

---

## 요약

질문한 구조에서 **각 프로젝트에 구체적으로 들어가야 할 것**을 정리하면 다음과 같다.

* **MyApp.Domain**

  * Entity, ValueObject, Domain Service, 도메인 예외
  * 게임 규칙(가챠 확률, 보상 계산, 레벨업 규칙, 스태미나 처리 등)

* **MyApp.Application**

  * 유즈케이스/서비스 (`GachaService`, `StageService`, `MailService` 등)
  * Repository/외부 서비스 인터페이스(`IPlayerRepository`, `IGachaRepository` 등)
  * Application용 DTO / Command / Result
  * 트랜잭션 단위 정의

* **MyApp.Infrastructure**

  * EF Core DbContext, Entity 매핑
  * Repository 구현, UnitOfWork 구현
  * Redis, gRPC, 외부 결제/푸시 서버 연동 구현
  * Domain에서 쓰는 기술 구현체(`IRandomGenerator` 구현 등)

* **MyApp.Api**

  * Controllers, API Request/Response DTO
  * 필터, 미들웨어, 인증/인가, Swagger
  * DI 구성, 앱 시작/라우팅 설정

이 정도 구조면 수집형 RPG 서버에서

* 로직 복잡도
* 기능 확장
* 테스트(유닛/통합)
* 차기 프로젝트 재사용
  을 모두 고려했을 때 꽤 현실적인 밸런스를 가져가는 형태가 된다.

---   
  
</br>  


# 유즈케이스  

## 공통 전제: 프로젝트 구조

```text
src
 ├─ MyApp.Api             // Web API (Controller, API DTO)
 ├─ MyApp.Application     // 유즈케이스/서비스, Application DTO
 ├─ MyApp.Domain          // Entity, ValueObject, Domain 서비스
 └─ MyApp.Infrastructure  // EF Core, Repository 구현, 외부 연동
```


# 1. 유즈케이스 1: 스테이지 클리어

## 1-1. Domain 레이어 (MyApp.Domain)

### 1) 엔티티/값 객체

```csharp
// 스테이지 자체 정의
public class Stage
{
    public int Id { get; private set; }
    public int RequiredStamina { get; private set; }
    public Reward BaseReward { get; private set; }  // 골드/재화, 아이템 등
    public int BaseExp { get; private set; }

    protected Stage() { }

    public Stage(int id, int requiredStamina, Reward baseReward, int baseExp)
    {
        Id = id;
        RequiredStamina = requiredStamina;
        BaseReward = baseReward;
        BaseExp = baseExp;
    }
}

// 클리어 결과
public class StageClearResult
{
    public Reward Reward { get; }
    public int GainedExp { get; }

    public StageClearResult(Reward reward, int gainedExp)
    {
        Reward = reward;
        GainedExp = gainedExp;
    }
}
```

### 2) 도메인 서비스

```csharp
public class StageDomainService
{
    public StageClearResult Clear(Player player, Stage stage, bool isFirstClear)
    {
        // 스태미나 소모는 Player가 책임짐
        player.ConsumeStamina(stage.RequiredStamina);

        var reward = stage.BaseReward;

        if (isFirstClear)
        {
            // 첫 클리어 보너스가 있다면 합산
            reward = reward.Merge(Reward.FirstClearBonus());
        }

        player.AddReward(reward);
        player.AddExperience(stage.BaseExp);

        return new StageClearResult(reward, stage.BaseExp);
    }
}
```

여기까지는 **HTTP도 DB도 모르고, 오직 규칙만** 표현한다.

---

## 1-2. Application 레이어 (MyApp.Application)

### 1) Command / Result DTO

```csharp
public class ClearStageCommand
{
    public long PlayerId { get; init; }
    public int StageId { get; init; }
    public bool IsSuccess { get; init; }
}

public class ClearStageResultDto
{
    public PlayerDto Player { get; init; } = null!;
    public RewardDto Reward { get; init; } = null!;
}
```

### 2) 레포지토리/서비스 인터페이스

```csharp
public interface IPlayerRepository
{
    Task<Player?> GetAsync(long id, CancellationToken ct = default);
    Task UpdateAsync(Player player, CancellationToken ct = default);
}

public interface IStageRepository
{
    Task<Stage?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> HasClearedAsync(long playerId, int stageId, CancellationToken ct = default);
    Task MarkClearedAsync(long playerId, int stageId, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken ct = default);
}
```

### 3) StageService 유즈케이스

```csharp
public class StageService
{
    private readonly IPlayerRepository _players;
    private readonly IStageRepository _stages;
    private readonly StageDomainService _stageDomainService;
    private readonly IUnitOfWork _uow;

    public StageService(
        IPlayerRepository players,
        IStageRepository stages,
        StageDomainService stageDomainService,
        IUnitOfWork uow)
    {
        _players = players;
        _stages = stages;
        _stageDomainService = stageDomainService;
        _uow = uow;
    }

    public async Task<ClearStageResultDto> ClearAsync(ClearStageCommand command, CancellationToken ct = default)
    {
        if (!command.IsSuccess)
            throw new InvalidOperationException("Stage not cleared");

        var player = await _players.GetAsync(command.PlayerId, ct)
            ?? throw new InvalidOperationException("Player not found");

        var stage = await _stages.GetAsync(command.StageId, ct)
            ?? throw new InvalidOperationException("Stage not found");

        var isFirstClear = !await _stages.HasClearedAsync(command.PlayerId, command.StageId, ct);

        var result = _stageDomainService.Clear(player, stage, isFirstClear);

        await _players.UpdateAsync(player, ct);
        if (isFirstClear)
            await _stages.MarkClearedAsync(player.Id, stage.Id, ct);

        await _uow.CommitAsync(ct);

        return new ClearStageResultDto
        {
            Player = PlayerDto.From(player),
            Reward = RewardDto.From(result.Reward)
        };
    }
}
```

---

## 1-3. Infrastructure 레이어 (MyApp.Infrastructure)

### 1) DbContext & 매핑

```csharp
public class GameDbContext : DbContext
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Stage> Stages => Set<Stage>();
    public DbSet<PlayerStageClear> PlayerStageClears => Set<PlayerStageClear>();

    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }
}

public class PlayerStageClear
{
    public long PlayerId { get; set; }
    public int StageId { get; set; }
    public DateTime ClearedAt { get; set; }
}
```

### 2) Repository 구현

```csharp
public class StageRepository : IStageRepository
{
    private readonly GameDbContext _db;

    public StageRepository(GameDbContext db) => _db = db;

    public Task<Stage?> GetAsync(int id, CancellationToken ct = default)
        => _db.Stages.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> HasClearedAsync(long playerId, int stageId, CancellationToken ct = default)
        => _db.PlayerStageClears
              .AnyAsync(x => x.PlayerId == playerId && x.StageId == stageId, ct);

    public async Task MarkClearedAsync(long playerId, int stageId, CancellationToken ct = default)
    {
        _db.PlayerStageClears.Add(new PlayerStageClear
        {
            PlayerId = playerId,
            StageId = stageId,
            ClearedAt = DateTime.UtcNow
        });
        await Task.CompletedTask;
    }
}
```

`PlayerRepository`, `UnitOfWork`는 이전에 보여준 것과 동일한 패턴으로 구현하면 된다.

---

## 1-4. Api 레이어 (MyApp.Api)

### 1) Request / Response DTO

```csharp
public class ClearStageRequest
{
    public int StageId { get; set; }
    public bool IsSuccess { get; set; }
}

public class ClearStageResponse
{
    public PlayerResponse Player { get; set; } = null!;
    public RewardResponse Reward { get; set; } = null!;
}
```

### 2) Controller

```csharp
[ApiController]
[Route("api/stage")]
public class StageController : ControllerBase
{
    private readonly StageService _stageService;
    private readonly ICurrentPlayerProvider _currentPlayer;

    public StageController(StageService stageService, ICurrentPlayerProvider currentPlayer)
    {
        _stageService = stageService;
        _currentPlayer = currentPlayer;
    }

    [HttpPost("clear")]
    public async Task<ActionResult<ClearStageResponse>> Clear([FromBody] ClearStageRequest request)
    {
        var cmd = new ClearStageCommand
        {
            PlayerId = _currentPlayer.PlayerId,
            StageId = request.StageId,
            IsSuccess = request.IsSuccess
        };

        var result = await _stageService.ClearAsync(cmd);

        var response = new ClearStageResponse
        {
            Player = PlayerResponse.From(result.Player),
            Reward = RewardResponse.From(result.Reward)
        };

        return Ok(response);
    }
}
```

---

## 1-5. 스테이지 클리어 전체 흐름 요약

1. 클라이언트 → `POST /api/stage/clear` (`ClearStageRequest`)
2. Api → `StageService.ClearAsync(ClearStageCommand)` 호출
3. Application → Player/Stage 로드, `StageDomainService.Clear` 호출, 트랜잭션 커밋
4. Domain → 규칙대로 스태미나 차감, 보상 계산, 경험치 증가
5. Infrastructure → 실제 DB 읽기/쓰기
6. Application → `ClearStageResultDto` 반환
7. Api → `ClearStageResponse` JSON으로 응답

---

# 2. 유즈케이스 2: 우편 받기(보상 수령)

## 2-1. Domain 레이어

### 1) Mail 엔티티

```csharp
public class Mail
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public string Title { get; private set; } = "";
    public string Body { get; private set; } = "";
    public Reward Reward { get; private set; }
    public bool IsClaimed { get; private set; }
    public DateTime ExpireAt { get; private set; }

    protected Mail() { }

    public Mail(long playerId, string title, string body, Reward reward, DateTime expireAt)
    {
        PlayerId = playerId;
        Title = title;
        Body = body;
        Reward = reward;
        ExpireAt = expireAt;
        IsClaimed = false;
    }

    public Reward Claim(DateTime now)
    {
        if (IsClaimed)
            throw new InvalidOperationException("Mail already claimed.");
        if (now > ExpireAt)
            throw new InvalidOperationException("Mail expired.");

        IsClaimed = true;
        return Reward;
    }
}
```

---

## 2-2. Application 레이어

### 1) 인터페이스

```csharp
public interface IMailRepository
{
    Task<Mail?> GetAsync(long mailId, CancellationToken ct = default);
    Task UpdateAsync(Mail mail, CancellationToken ct = default);
}
```

### 2) Command / Result DTO

```csharp
public class ClaimMailCommand
{
    public long PlayerId { get; init; }
    public long MailId { get; init; }
}

public class ClaimMailResult
{
    public PlayerDto Player { get; init; } = null!;
    public RewardDto Reward { get; init; } = null!;
}
```

### 3) MailService

```csharp
public class MailService
{
    private readonly IMailRepository _mails;
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _uow;
    private readonly ITimeProvider _time;

    public MailService(
        IMailRepository mails,
        IPlayerRepository players,
        IUnitOfWork uow,
        ITimeProvider time)
    {
        _mails = mails;
        _players = players;
        _uow = uow;
        _time = time;
    }

    public async Task<ClaimMailResult> ClaimAsync(ClaimMailCommand cmd, CancellationToken ct = default)
    {
        var player = await _players.GetAsync(cmd.PlayerId, ct)
            ?? throw new InvalidOperationException("Player not found");

        var mail = await _mails.GetAsync(cmd.MailId, ct)
            ?? throw new InvalidOperationException("Mail not found");

        if (mail.PlayerId != player.Id)
            throw new InvalidOperationException("Mail does not belong to player");

        var reward = mail.Claim(_time.UtcNow); // Domain 로직 호출
        player.AddReward(reward);

        await _mails.UpdateAsync(mail, ct);
        await _players.UpdateAsync(player, ct);
        await _uow.CommitAsync(ct);

        return new ClaimMailResult
        {
            Player = PlayerDto.From(player),
            Reward = RewardDto.From(reward)
        };
    }
}
```

---

## 2-3. Infrastructure 레이어

```csharp
public class MailRepository : IMailRepository
{
    private readonly GameDbContext _db;

    public MailRepository(GameDbContext db) => _db = db;

    public Task<Mail?> GetAsync(long mailId, CancellationToken ct = default)
        => _db.Set<Mail>().FirstOrDefaultAsync(m => m.Id == mailId, ct);

    public Task UpdateAsync(Mail mail, CancellationToken ct = default)
    {
        _db.Update(mail);
        return Task.CompletedTask;
    }
}
```

`ITimeProvider`는 여기서도 구현해줄 수 있다.

---

## 2-4. Api 레이어

### 1) DTO

```csharp
public class ClaimMailRequest
{
    public long MailId { get; set; }
}

public class ClaimMailResponse
{
    public PlayerResponse Player { get; set; } = null!;
    public RewardResponse Reward { get; set; } = null!;
}
```

### 2) Controller

```csharp
[ApiController]
[Route("api/mail")]
public class MailController : ControllerBase
{
    private readonly MailService _mailService;
    private readonly ICurrentPlayerProvider _currentPlayer;

    public MailController(MailService mailService, ICurrentPlayerProvider currentPlayer)
    {
        _mailService = mailService;
        _currentPlayer = currentPlayer;
    }

    [HttpPost("claim")]
    public async Task<ActionResult<ClaimMailResponse>> Claim([FromBody] ClaimMailRequest request)
    {
        var cmd = new ClaimMailCommand
        {
            PlayerId = _currentPlayer.PlayerId,
            MailId = request.MailId
        };

        var result = await _mailService.ClaimAsync(cmd);

        var response = new ClaimMailResponse
        {
            Player = PlayerResponse.From(result.Player),
            Reward = RewardResponse.From(result.Reward)
        };

        return Ok(response);
    }
}
```

---

## 2-5. 우편 받기 전체 흐름 요약

1. 클라이언트 → `POST /api/mail/claim` (`MailId`)
2. Api → `MailService.ClaimAsync(ClaimMailCommand)` 호출
3. Application → Player/Mail 로드, Mail.Claim(now), Player.AddReward, 커밋
4. Domain → 중복 수령, 만료 체크, Reward 반환
5. Infrastructure → Mail/Player 상태 업데이트 DB 반영
6. Application → ClaimMailResult 반환
7. Api → ClaimMailResponse JSON 응답

---

# 3. 유즈케이스 3: 출석체크 보상 받기

## 3-1. Domain 레이어

### 1) AttendanceInfo 엔티티

```csharp
public class AttendanceInfo
{
    public long PlayerId { get; private set; }
    public int CurrentDay { get; private set; }  // 1-based
    public DateTime LastCheckedAt { get; private set; }

    protected AttendanceInfo() { }

    public AttendanceInfo(long playerId)
    {
        PlayerId = playerId;
        CurrentDay = 0;
        LastCheckedAt = DateTime.MinValue;
    }

    public int GetTodayDay(DateTime now)
    {
        // 예시: 매일 00:00 UTC 기준으로 다음 날로 침
        // 실제 구현에서는 기간/이벤트별 로직을 넣을 수 있다
        // 여기서는 단순하게 CurrentDay + 1 로 간다고 가정
        return CurrentDay + 1;
    }

    public void MarkChecked(DateTime now, int day)
    {
        if (day != CurrentDay + 1)
            throw new InvalidOperationException("Invalid attendance day");

        CurrentDay = day;
        LastCheckedAt = now;
    }
}
```

### 2) AttendanceRewardPolicy 도메인 서비스

```csharp
public class AttendanceRewardPolicy
{
    public Reward GetRewardForDay(int day)
    {
        // 예시: 간단한 switch
        return day switch
        {
            1 => Reward.Gold(1000),
            2 => Reward.Gems(10),
            3 => Reward.Item(1001, 1),
            _ => Reward.Gold(500)
        };
    }
}
```

---

## 3-2. Application 레이어

### 1) 인터페이스

```csharp
public interface IAttendanceRepository
{
    Task<AttendanceInfo?> GetAsync(long playerId, CancellationToken ct = default);
    Task AddAsync(AttendanceInfo attendance, CancellationToken ct = default);
    Task UpdateAsync(AttendanceInfo attendance, CancellationToken ct = default);
}
```

### 2) Command / Result DTO

```csharp
public class ClaimAttendanceRewardCommand
{
    public long PlayerId { get; init; }
}

public class ClaimAttendanceRewardResult
{
    public PlayerDto Player { get; init; } = null!;
    public RewardDto Reward { get; init; } = null!;
    public int Day { get; init; }
}
```

### 3) AttendanceService

```csharp
public class AttendanceService
{
    private readonly IPlayerRepository _players;
    private readonly IAttendanceRepository _attendances;
    private readonly AttendanceRewardPolicy _rewardPolicy;
    private readonly ITimeProvider _time;
    private readonly IUnitOfWork _uow;

    public AttendanceService(
        IPlayerRepository players,
        IAttendanceRepository attendances,
        AttendanceRewardPolicy rewardPolicy,
        ITimeProvider time,
        IUnitOfWork uow)
    {
        _players = players;
        _attendances = attendances;
        _rewardPolicy = rewardPolicy;
        _time = time;
        _uow = uow;
    }

    public async Task<ClaimAttendanceRewardResult> ClaimAsync(ClaimAttendanceRewardCommand cmd, CancellationToken ct = default)
    {
        var now = _time.UtcNow;

        var player = await _players.GetAsync(cmd.PlayerId, ct)
            ?? throw new InvalidOperationException("Player not found");

        var attendance = await _attendances.GetAsync(cmd.PlayerId, ct)
                        ?? new AttendanceInfo(cmd.PlayerId);

        var todayDay = attendance.GetTodayDay(now);
        var reward = _rewardPolicy.GetRewardForDay(todayDay);

        attendance.MarkChecked(now, todayDay);
        player.AddReward(reward);

        if (attendance.CurrentDay == 1 && attendance.LastCheckedAt == now)
            await _attendances.AddAsync(attendance, ct);
        else
            await _attendances.UpdateAsync(attendance, ct);

        await _players.UpdateAsync(player, ct);
        await _uow.CommitAsync(ct);

        return new ClaimAttendanceRewardResult
        {
            Player = PlayerDto.From(player),
            Reward = RewardDto.From(reward),
            Day = todayDay
        };
    }
}
```

---

## 3-3. Infrastructure 레이어

간단 예시만 적는다.

```csharp
public class AttendanceRepository : IAttendanceRepository
{
    private readonly GameDbContext _db;

    public AttendanceRepository(GameDbContext db) => _db = db;

    public Task<AttendanceInfo?> GetAsync(long playerId, CancellationToken ct = default)
        => _db.Set<AttendanceInfo>().FirstOrDefaultAsync(a => a.PlayerId == playerId, ct);

    public Task AddAsync(AttendanceInfo attendance, CancellationToken ct = default)
        => _db.Set<AttendanceInfo>().AddAsync(attendance, ct).AsTask();

    public Task UpdateAsync(AttendanceInfo attendance, CancellationToken ct = default)
    {
        _db.Update(attendance);
        return Task.CompletedTask;
    }
}
```

---

## 3-4. Api 레이어

### 1) DTO

```csharp
public class ClaimAttendanceRequest
{
    // 보통 요청 바디는 비어있고 인증 정보로 Player를 식별한다
}

public class ClaimAttendanceResponse
{
    public PlayerResponse Player { get; set; } = null!;
    public RewardResponse Reward { get; set; } = null!;
    public int Day { get; set; }
}
```

### 2) Controller

```csharp
[ApiController]
[Route("api/attendance")]
public class AttendanceController : ControllerBase
{
    private readonly AttendanceService _attendanceService;
    private readonly ICurrentPlayerProvider _currentPlayer;

    public AttendanceController(AttendanceService attendanceService, ICurrentPlayerProvider currentPlayer)
    {
        _attendanceService = attendanceService;
        _currentPlayer = currentPlayer;
    }

    [HttpPost("claim")]
    public async Task<ActionResult<ClaimAttendanceResponse>> Claim([FromBody] ClaimAttendanceRequest request)
    {
        var cmd = new ClaimAttendanceRewardCommand
        {
            PlayerId = _currentPlayer.PlayerId
        };

        var result = await _attendanceService.ClaimAsync(cmd);

        var response = new ClaimAttendanceResponse
        {
            Player = PlayerResponse.From(result.Player),
            Reward = RewardResponse.From(result.Reward),
            Day = result.Day
        };

        return Ok(response);
    }
}
```

---

## 3-5. 출석체크 전체 흐름 요약

1. 클라이언트 → `POST /api/attendance/claim`
2. Api → `AttendanceService.ClaimAsync(ClaimAttendanceRewardCommand)` 호출
3. Application → Player/Attendance 로드, 도메인 로직 호출, 보상/날짜 계산, 커밋
4. Domain → 오늘 출석 가능 여부, Day 증가, Reward 결정(Policy)
5. Infrastructure → Attendance/Player 상태 DB 반영
6. Application → ClaimAttendanceRewardResult 반환
7. Api → ClaimAttendanceResponse JSON 응답

---

## 전체적으로 보면

세 유즈케이스 모두 공통 패턴을 따른다.

* **Domain**: 규칙만 알고, 환경을 모른다
* **Application**: 여러 도메인 객체를 조합해 유즈케이스를 완성하고, 트랜잭션을 관리한다
* **Infrastructure**: Repository/UnitOfWork로 실제 DB와 외부 시스템에 붙는다
* **Api**: HTTP ↔ Application 변환만 한다

이 구조를 유지하면:

* 테스트하기 좋고
* 규칙/플로우/인프라가 잘 분리되고
* 수집형 RPG에서 자주 나오는 유즈케이스(가챠, 우편, 출석, 미션, 업적 등)를 계속 추가해도 구조가 크게 흐트러지지 않는다.

원하면 여기에서 하나 골라서 **테스트 코드(유닛/통합)까지 포함한 예제**를 이어서 만들어 줄 수 있다.
  
  

# 4개 유즈케이스(가챠, 스테이지 클리어, 우편 수령, 출석체크 보상) 전체 구현

* 도메인 유닛 테스트
* 애플리케이션 유닛 테스트
* Dapper 기반 통합 테스트(간단 버전)

까지 한 번에 정리하겠다.

> 전제: 아래 코드는 “패턴 참고용” 예제라서
> 실제 프로젝트에서는 네임스페이스/파일 분리해서 쓰는 게 좋다.

---

## 0. 전제 인터페이스/도메인 간단 정의

### 0-1. 공통 도메인

```csharp
// Value Object
public readonly struct Currency
{
    public int Gems { get; }
    public int Gold { get; }

    public Currency(int gems, int gold)
    {
        if (gems < 0 || gold <  0)
            throw new ArgumentException();

        Gems = gems;
        Gold = gold;
    }

    public Currency Add(Currency other) => new Currency(Gems + other.Gems, Gold + other.Gold);

    public Currency Subtract(Currency other)
    {
        if (Gems < other.Gems || Gold < other.Gold)
            throw new InvalidOperationException("Not enough currency");
        return new Currency(Gems - other.Gems, Gold - other.Gold);
    }
}

public class Reward
{
    public int Gold { get; }
    public int Gems { get; }

    public Reward(int gold, int gems)
    {
        Gold = gold;
        Gems = gems;
    }

    public Reward Merge(Reward other) => new Reward(Gold + other.Gold, Gems + other.Gems);

    public static Reward GoldOnly(int gold) => new Reward(gold, 0);
    public static Reward GemsOnly(int gems) => new Reward(0, gems);
}
```

### 0-2. Player 도메인 (공통)

```csharp
public class Player
{
    public long Id { get; private set; }
    public int Level { get; private set; }
    public Currency Currency { get; private set; }
    public int Stamina { get; private set; }

    private readonly List<Hero> _heroes = new();
    public IReadOnlyCollection<Hero> Heroes => _heroes;

    protected Player() { }

    public Player(long id)
    {
        Id = id;
        Level = 1;
        Currency = new Currency(0, 0);
        Stamina = 100;
    }

    public void AddReward(Reward reward)
    {
        Currency = Currency.Add(new Currency(reward.Gems, reward.Gold));
    }

    public void ConsumeStamina(int cost)
    {
        if (Stamina < cost)
            throw new InvalidOperationException("Not enough stamina");

        Stamina -= cost;
    }

    public void AddHero(Hero hero) => _heroes.Add(hero);
}

public class Hero
{
    public long Id { get; private set; }
    public string Rarity { get; private set; }
    public int Level { get; private set; }

    protected Hero() { }

    public Hero(long id, string rarity, int level = 1)
    {
        Id = id;
        Rarity = rarity;
        Level = level;
    }
}
```

---

## 1. 도메인 유닛 테스트 (Domain Layer Tests)

xUnit 기준으로 작성한다.

### 1-1. 가챠 도메인

#### 도메인

```csharp
public interface IRandomGenerator
{
    double NextDouble();
}

public class GachaEntry
{
    public long HeroId { get; }
    public string Rarity { get; }
    public double Weight { get; }

    public GachaEntry(long heroId, string rarity, double weight)
    {
        HeroId = heroId;
        Rarity = rarity;
        Weight = weight;
    }
}

public class GachaPool
{
    private readonly List<GachaEntry> _entries;

    public GachaPool(IEnumerable<GachaEntry> entries)
    {
        _entries = entries.ToList();
    }

    public GachaEntry Pick(double r)
    {
        var total = _entries.Sum(e => e.Weight);
        var target = r * total;
        double acc = 0;
        foreach (var e in _entries)
        {
            acc += e.Weight;
            if (target <= acc)
                return e;
        }
        return _entries.Last();
    }
}

public class GachaDomainService
{
    private readonly IRandomGenerator _random;

    public GachaDomainService(IRandomGenerator random) => _random = random;

    public IReadOnlyList<Hero> Roll(GachaPool pool, int count)
    {
        var result = new List<Hero>(count);
        for (int i = 0; i < count; i++)
        {
            var r = _random.NextDouble();
            var entry = pool.Pick(r);
            result.Add(new Hero(entry.HeroId, entry.Rarity));
        }
        return result;
    }
}
```

#### 테스트

```csharp
public class FixedRandom : IRandomGenerator
{
    private readonly Queue<double> _values;

    public FixedRandom(IEnumerable<double> values)
    {
        _values = new Queue<double>(values);
    }

    public double NextDouble() => _values.Dequeue();
}

public class GachaDomainTests
{
    [Fact]
    public void Roll_GivesHeroesCountEqualToRequested()
    {
        var pool = new GachaPool(new[]
        {
            new GachaEntry(1, "R", 1),
            new GachaEntry(2, "SR", 1)
        });

        var random = new FixedRandom(new[] { 0.1, 0.9 });
        var service = new GachaDomainService(random);

        var result = service.Roll(pool, 2);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, h => h.Id == 1);
        Assert.Contains(result, h => h.Id == 2);
    }
}
```

---

### 1-2. 스테이지 클리어 도메인

```csharp
public class Stage
{
    public int Id { get; private set; }
    public int RequiredStamina { get; private set; }
    public Reward BaseReward { get; private set; }
    public int BaseExp { get; private set; }

    protected Stage() { }

    public Stage(int id, int requiredStamina, Reward baseReward, int baseExp)
    {
        Id = id;
        RequiredStamina = requiredStamina;
        BaseReward = baseReward;
        BaseExp = baseExp;
    }
}

public class StageClearResult
{
    public Reward Reward { get; }
    public int GainedExp { get; }

    public StageClearResult(Reward reward, int gainedExp)
    {
        Reward = reward;
        GainedExp = gainedExp;
    }
}

public class StageDomainService
{
    public StageClearResult Clear(Player player, Stage stage, bool isFirstClear)
    {
        player.ConsumeStamina(stage.RequiredStamina);

        var reward = stage.BaseReward;
        if (isFirstClear)
            reward = reward.Merge(Reward.GemsOnly(10)); // 임의 첫 클리어 보너스

        player.AddReward(reward);

        return new StageClearResult(reward, stage.BaseExp);
    }
}
```

#### 테스트

```csharp
public class StageDomainTests
{
    [Fact]
    public void Clear_FirstClear_GivesBonusRewardAndConsumesStamina()
    {
        var player = new Player(1);
        var stage = new Stage(1, requiredStamina: 10, baseReward: Reward.GoldOnly(100), baseExp: 50);
        var service = new StageDomainService();

        var result = service.Clear(player, stage, isFirstClear: true);

        Assert.Equal(90, player.Stamina); // 100 - 10
        Assert.Equal(100, result.Reward.Gold);
        Assert.Equal(10, result.Reward.Gems); // 첫 클리어 보너스
    }
}
```

---

### 1-3. 우편 도메인

```csharp
public class Mail
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public Reward Reward { get; private set; }
    public bool IsClaimed { get; private set; }
    public DateTime ExpireAt { get; private set; }

    protected Mail() { }

    public Mail(long playerId, Reward reward, DateTime expireAt)
    {
        PlayerId = playerId;
        Reward = reward;
        ExpireAt = expireAt;
        IsClaimed = false;
    }

    public Reward Claim(DateTime now)
    {
        if (IsClaimed)
            throw new InvalidOperationException("Already claimed");
        if (now > ExpireAt)
            throw new InvalidOperationException("Mail expired");

        IsClaimed = true;
        return Reward;
    }
}
```

#### 테스트

```csharp
public class MailDomainTests
{
    [Fact]
    public void Claim_FirstTime_ReturnsRewardAndMarksClaimed()
    {
        var mail = new Mail(1, Reward.GoldOnly(1000), DateTime.UtcNow.AddDays(1));

        var reward = mail.Claim(DateTime.UtcNow);

        Assert.True(mail.IsClaimed);
        Assert.Equal(1000, reward.Gold);
    }

    [Fact]
    public void Claim_Twice_Throws()
    {
        var mail = new Mail(1, Reward.GoldOnly(1000), DateTime.UtcNow.AddDays(1));
        mail.Claim(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => mail.Claim(DateTime.UtcNow));
    }
}
```

---

### 1-4. 출석체크 도메인

```csharp
public class AttendanceInfo
{
    public long PlayerId { get; private set; }
    public int CurrentDay { get; private set; }
    public DateTime LastCheckedAt { get; private set; }

    protected AttendanceInfo() { }

    public AttendanceInfo(long playerId)
    {
        PlayerId = playerId;
        CurrentDay = 0;
        LastCheckedAt = DateTime.MinValue;
    }

    public int GetNextDay() => CurrentDay + 1;

    public void MarkChecked(DateTime now, int day)
    {
        if (day != CurrentDay + 1)
            throw new InvalidOperationException("Invalid day");

        CurrentDay = day;
        LastCheckedAt = now;
    }
}

public class AttendanceRewardPolicy
{
    public Reward GetRewardForDay(int day) =>
        day switch
        {
            1 => Reward.GoldOnly(1000),
            2 => Reward.GemsOnly(10),
            _ => Reward.GoldOnly(500)
        };
}
```

#### 테스트

```csharp
public class AttendanceDomainTests
{
    [Fact]
    public void MarkChecked_IncrementsDay()
    {
        var attendance = new AttendanceInfo(1);

        var nextDay = attendance.GetNextDay();
        attendance.MarkChecked(DateTime.UtcNow, nextDay);

        Assert.Equal(1, attendance.CurrentDay);
    }

    [Fact]
    public void GetRewardForDay_ReturnsDifferentRewards()
    {
        var policy = new AttendanceRewardPolicy();

        var r1 = policy.GetRewardForDay(1);
        var r2 = policy.GetRewardForDay(2);

        Assert.Equal(1000, r1.Gold);
        Assert.Equal(10, r2.Gems);
    }
}
```

---

## 2. 애플리케이션 유닛 테스트 (Application Layer Tests)

Moq를 사용해 레포지토리/UnitOfWork만 Mock 한다.

### 공통 인터페이스

```csharp
public interface IPlayerRepository
{
    Task<Player?> GetAsync(long id, CancellationToken ct = default);
    Task UpdateAsync(Player player, CancellationToken ct = default);
}

public interface IGachaRepository
{
    Task<GachaPool?> GetPoolAsync(int poolId, CancellationToken ct = default);
}

public interface IStageRepository
{
    Task<Stage?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> HasClearedAsync(long playerId, int stageId, CancellationToken ct = default);
    Task MarkClearedAsync(long playerId, int stageId, CancellationToken ct = default);
}

public interface IMailRepository
{
    Task<Mail?> GetAsync(long mailId, CancellationToken ct = default);
    Task UpdateAsync(Mail mail, CancellationToken ct = default);
}

public interface IAttendanceRepository
{
    Task<AttendanceInfo?> GetAsync(long playerId, CancellationToken ct = default);
    Task AddAsync(AttendanceInfo attendance, CancellationToken ct = default);
    Task UpdateAsync(AttendanceInfo attendance, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken ct = default);
}

public interface ITimeProvider
{
    DateTime UtcNow { get; }
}
```

DTO는 간단하게 만든다.

```csharp
public class PlayerDto
{
    public long Id { get; set; }
    public int Level { get; set; }
    public int Gems { get; set; }
    public int Gold { get; set; }

    public static PlayerDto From(Player p) =>
        new PlayerDto { Id = p.Id, Level = p.Level, Gems = p.Currency.Gems, Gold = p.Currency.Gold };
}

public class HeroDto
{
    public long HeroId { get; set; }
    public string Rarity { get; set; } = "";
    public int Level { get; set; }

    public static HeroDto From(Hero h) =>
        new HeroDto { HeroId = h.Id, Rarity = h.Rarity, Level = h.Level };
}

public class RewardDto
{
    public int Gold { get; set; }
    public int Gems { get; set; }

    public static RewardDto From(Reward r) => new RewardDto { Gold = r.Gold, Gems = r.Gems };
}
```

---

### 2-1. 가챠 Application

```csharp
public class RollGachaCommand
{
    public long PlayerId { get; init; }
    public int PoolId { get; init; }
    public int Count { get; init; }
}

public class RollGachaResult
{
    public PlayerDto Player { get; init; } = null!;
    public IReadOnlyList<HeroDto> Heroes { get; init; } = Array.Empty<HeroDto>();
}

public class GachaService
{
    private readonly IPlayerRepository _players;
    private readonly IGachaRepository _gacha;
    private readonly IUnitOfWork _uow;
    private readonly GachaDomainService _domain;

    public GachaService(IPlayerRepository players, IGachaRepository gacha, IUnitOfWork uow, GachaDomainService domain)
    {
        _players = players;
        _gacha = gacha;
        _uow = uow;
        _domain = domain;
    }

    public async Task<RollGachaResult> RollAsync(RollGachaCommand cmd, CancellationToken ct = default)
    {
        var player = await _players.GetAsync(cmd.PlayerId, ct) ?? throw new InvalidOperationException("Player not found");
        var pool = await _gacha.GetPoolAsync(cmd.PoolId, ct) ?? throw new InvalidOperationException("Pool not found");

        // 비용은 간단히 Count * 300 Gems 라고 가정
        var cost = new Currency(cmd.Count * 300, 0);
        player.Currency = player.Currency.Subtract(cost);

        var heroes = _domain.Roll(pool, cmd.Count);
        foreach (var h in heroes)
            player.AddHero(h);

        await _players.UpdateAsync(player, ct);
        await _uow.CommitAsync(ct);

        return new RollGachaResult
        {
            Player = PlayerDto.From(player),
            Heroes = heroes.Select(HeroDto.From).ToList()
        };
    }
}
```

#### 테스트

```csharp
public class GachaServiceTests
{
    [Fact]
    public async Task RollAsync_SubtractsCurrency_AndAddsHeroes()
    {
        var player = new Player(1);
        player.Currency = new Currency(1000, 0);

        var pool = new GachaPool(new[]
        {
            new GachaEntry(1, "R", 1)
        });

        var playerRepo = new Mock<IPlayerRepository>();
        playerRepo.Setup(r => r.GetAsync(1, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(player);

        var gachaRepo = new Mock<IGachaRepository>();
        gachaRepo.Setup(r => r.GetPoolAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(pool);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
           .ReturnsAsync(1);

        var random = new FixedRandom(new[] { 0.1 });
        var domain = new GachaDomainService(random);

        var service = new GachaService(playerRepo.Object, gachaRepo.Object, uow.Object, domain);

        var result = await service.RollAsync(new RollGachaCommand
        {
            PlayerId = 1,
            PoolId = 1,
            Count = 1
        });

        Assert.Single(result.Heroes);
        Assert.Equal(700, result.Player.Gems); // 1000 - 300
        playerRepo.Verify(r => r.UpdateAsync(player, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

---

### 2-2. 스테이지 Application

앞에서 만든 StageDomainService 재사용.

```csharp
public class ClearStageCommand
{
    public long PlayerId { get; init; }
    public int StageId { get; init; }
    public bool IsSuccess { get; init; }
}

public class ClearStageResultDto
{
    public PlayerDto Player { get; init; } = null!;
    public RewardDto Reward { get; init; } = null!;
}

public class StageService
{
    private readonly IPlayerRepository _players;
    private readonly IStageRepository _stages;
    private readonly StageDomainService _domain;
    private readonly IUnitOfWork _uow;

    public StageService(IPlayerRepository players, IStageRepository stages, StageDomainService domain, IUnitOfWork uow)
    {
        _players = players;
        _stages = stages;
        _domain = domain;
        _uow = uow;
    }

    public async Task<ClearStageResultDto> ClearAsync(ClearStageCommand cmd, CancellationToken ct = default)
    {
        if (!cmd.IsSuccess)
            throw new InvalidOperationException("Stage not cleared");

        var player = await _players.GetAsync(cmd.PlayerId, ct) ?? throw new InvalidOperationException("Player not found");
        var stage = await _stages.GetAsync(cmd.StageId, ct) ?? throw new InvalidOperationException("Stage not found");

        var firstClear = !await _stages.HasClearedAsync(cmd.PlayerId, cmd.StageId, ct);

        var result = _domain.Clear(player, stage, firstClear);

        await _players.UpdateAsync(player, ct);
        if (firstClear)
            await _stages.MarkClearedAsync(player.Id, stage.Id, ct);

        await _uow.CommitAsync(ct);

        return new ClearStageResultDto
        {
            Player = PlayerDto.From(player),
            Reward = RewardDto.From(result.Reward)
        };
    }
}
```

#### 테스트

```csharp
public class StageServiceTests
{
    [Fact]
    public async Task ClearAsync_FirstClear_MarksClearedAndUpdatesPlayer()
    {
        var player = new Player(1);
        var stage = new Stage(1, 10, Reward.GoldOnly(100), 50);

        var playerRepo = new Mock<IPlayerRepository>();
        playerRepo.Setup(r => r.GetAsync(1, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(player);

        var stageRepo = new Mock<IStageRepository>();
        stageRepo.Setup(r => r.GetAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(stage);
        stageRepo.Setup(r => r.HasClearedAsync(1, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
           .ReturnsAsync(1);

        var domain = new StageDomainService();
        var service = new StageService(playerRepo.Object, stageRepo.Object, domain, uow.Object);

        var result = await service.ClearAsync(new ClearStageCommand
        {
            PlayerId = 1,
            StageId = 1,
            IsSuccess = true
        });

        Assert.Equal(90, result.Player.Stamina);
        Assert.Equal(100, result.Reward.Gold);
        stageRepo.Verify(r => r.MarkClearedAsync(1, 1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

---

### 2-3. 우편 Application

```csharp
public class ClaimMailCommand
{
    public long PlayerId { get; init; }
    public long MailId { get; init; }
}

public class ClaimMailResult
{
    public PlayerDto Player { get; init; } = null!;
    public RewardDto Reward { get; init; } = null!;
}

public class MailService
{
    private readonly IMailRepository _mails;
    private readonly IPlayerRepository _players;
    private readonly ITimeProvider _time;
    private readonly IUnitOfWork _uow;

    public MailService(IMailRepository mails, IPlayerRepository players, ITimeProvider time, IUnitOfWork uow)
    {
        _mails = mails;
        _players = players;
        _time = time;
        _uow = uow;
    }

    public async Task<ClaimMailResult> ClaimAsync(ClaimMailCommand cmd, CancellationToken ct = default)
    {
        var player = await _players.GetAsync(cmd.PlayerId, ct) ?? throw new InvalidOperationException("Player not found");
        var mail = await _mails.GetAsync(cmd.MailId, ct) ?? throw new InvalidOperationException("Mail not found");

        if (mail.PlayerId != player.Id)
            throw new InvalidOperationException("Mail does not belong to player");

        var reward = mail.Claim(_time.UtcNow);
        player.AddReward(reward);

        await _mails.UpdateAsync(mail, ct);
        await _players.UpdateAsync(player, ct);
        await _uow.CommitAsync(ct);

        return new ClaimMailResult
        {
            Player = PlayerDto.From(player),
            Reward = RewardDto.From(reward)
        };
    }
}
```

#### 테스트

```csharp
public class MailServiceTests
{
    [Fact]
    public async Task ClaimAsync_AddsRewardAndMarksMailClaimed()
    {
        var player = new Player(1);
        var mail = new Mail(1, Reward.GemsOnly(10), DateTime.UtcNow.AddDays(1));

        var playerRepo = new Mock<IPlayerRepository>();
        playerRepo.Setup(r => r.GetAsync(1, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(player);

        var mailRepo = new Mock<IMailRepository>();
        mailRepo.Setup(r => r.GetAsync(100, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mail);

        var time = new Mock<ITimeProvider>();
        time.SetupGet(t => t.UtcNow).Returns(DateTime.UtcNow);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
           .ReturnsAsync(1);

        var service = new MailService(mailRepo.Object, playerRepo.Object, time.Object, uow.Object);

        var result = await service.ClaimAsync(new ClaimMailCommand { PlayerId = 1, MailId = 100 });

        Assert.Equal(10, result.Reward.Gems);
        Assert.True(mail.IsClaimed);
    }
}
```

---

### 2-4. 출석체크 Application

```csharp
public class ClaimAttendanceRewardCommand
{
    public long PlayerId { get; init; }
}

public class ClaimAttendanceRewardResult
{
    public PlayerDto Player { get; init; } = null!;
    public RewardDto Reward { get; init; } = null!;
    public int Day { get; init; }
}

public class AttendanceService
{
    private readonly IPlayerRepository _players;
    private readonly IAttendanceRepository _attendances;
    private readonly AttendanceRewardPolicy _policy;
    private readonly ITimeProvider _time;
    private readonly IUnitOfWork _uow;

    public AttendanceService(
        IPlayerRepository players,
        IAttendanceRepository attendances,
        AttendanceRewardPolicy policy,
        ITimeProvider time,
        IUnitOfWork uow)
    {
        _players = players;
        _attendances = attendances;
        _policy = policy;
        _time = time;
        _uow = uow;
    }

    public async Task<ClaimAttendanceRewardResult> ClaimAsync(ClaimAttendanceRewardCommand cmd, CancellationToken ct = default)
    {
        var now = _time.UtcNow;

        var player = await _players.GetAsync(cmd.PlayerId, ct) ?? throw new InvalidOperationException("Player not found");
        var attendance = await _attendances.GetAsync(cmd.PlayerId, ct) ?? new AttendanceInfo(cmd.PlayerId);

        var day = attendance.GetNextDay();
        var reward = _policy.GetRewardForDay(day);

        attendance.MarkChecked(now, day);
        player.AddReward(reward);

        if (day == 1 && attendance.LastCheckedAt == now)
            await _attendances.AddAsync(attendance, ct);
        else
            await _attendances.UpdateAsync(attendance, ct);

        await _players.UpdateAsync(player, ct);
        await _uow.CommitAsync(ct);

        return new ClaimAttendanceRewardResult
        {
            Player = PlayerDto.From(player),
            Reward = RewardDto.From(reward),
            Day = day
        };
    }
}
```

#### 테스트

```csharp
public class AttendanceServiceTests
{
    [Fact]
    public async Task ClaimAsync_FirstDay_AddsRewardAndCreatesAttendance()
    {
        var player = new Player(1);

        var playerRepo = new Mock<IPlayerRepository>();
        playerRepo.Setup(r => r.GetAsync(1, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(player);

        var attRepo = new Mock<IAttendanceRepository>();
        attRepo.Setup(r => r.GetAsync(1, It.IsAny<CancellationToken>()))
               .ReturnsAsync((AttendanceInfo?)null);

        var policy = new AttendanceRewardPolicy();

        var time = new Mock<ITimeProvider>();
        time.SetupGet(t => t.UtcNow).Returns(DateTime.UtcNow);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
           .ReturnsAsync(1);

        var service = new AttendanceService(playerRepo.Object, attRepo.Object, policy, time.Object, uow.Object);

        var result = await service.ClaimAsync(new ClaimAttendanceRewardCommand { PlayerId = 1 });

        Assert.Equal(1, result.Day);
        Assert.Equal(1000, result.Reward.Gold); // 정책상 1일차 1000 골드
        attRepo.Verify(r => r.AddAsync(It.IsAny<AttendanceInfo>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

---

## 3. Dapper 기반 통합 테스트 (Integration Tests with Dapper)

실제 DB 대신 **SQLite In-Memory**를 사용해 Dapper로 통합 테스트 예제를 보여준다.
(실제 MySQL로 바꾸려면 연결 문자열과 DDL만 바꾸면 된다.)

### 3-1. Dapper용 Repository 예시 (Player + Stage)

```csharp
using System.Data;
using Dapper;

public class DapperPlayerRepository : IPlayerRepository
{
    private readonly IDbConnection _conn;

    public DapperPlayerRepository(IDbConnection conn) => _conn = conn;

    public async Task<Player?> GetAsync(long id, CancellationToken ct = default)
    {
        var row = await _conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT Id, Level, Gems, Gold, Stamina FROM Players WHERE Id = @Id",
            new { Id = id });

        if (row == null) return null;

        var p = new Player((long)row.Id);
        p.GetType().GetProperty("Level")!.SetValue(p, (int)row.Level);
        p.GetType().GetProperty("Currency")!.SetValue(p, new Currency((int)row.Gems, (int)row.Gold));
        p.GetType().GetProperty("Stamina")!.SetValue(p, (int)row.Stamina);
        return p;
    }

    public async Task UpdateAsync(Player player, CancellationToken ct = default)
    {
        await _conn.ExecuteAsync(
            "UPDATE Players SET Level = @Level, Gems = @Gems, Gold = @Gold, Stamina = @Stamina WHERE Id = @Id",
            new
            {
                Id = player.Id,
                Level = player.Level,
                Gems = player.Currency.Gems,
                Gold = player.Currency.Gold,
                Stamina = player.Stamina
            });
    }
}

public class DapperStageRepository : IStageRepository
{
    private readonly IDbConnection _conn;

    public DapperStageRepository(IDbConnection conn) => _conn = conn;

    public async Task<Stage?> GetAsync(int id, CancellationToken ct = default)
    {
        var row = await _conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT Id, RequiredStamina, RewardGold, RewardGems, BaseExp FROM Stages WHERE Id = @Id",
            new { Id = id });

        if (row == null) return null;

        var reward = new Reward((int)row.RewardGold, (int)row.RewardGems);
        return new Stage((int)row.Id, (int)row.RequiredStamina, reward, (int)row.BaseExp);
    }

    public async Task<bool> HasClearedAsync(long playerId, int stageId, CancellationToken ct = default)
    {
        var count = await _conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM PlayerStageClears WHERE PlayerId = @PlayerId AND StageId = @StageId",
            new { PlayerId = playerId, StageId = stageId });
        return count > 0;
    }

    public async Task MarkClearedAsync(long playerId, int stageId, CancellationToken ct = default)
    {
        await _conn.ExecuteAsync(
            "INSERT INTO PlayerStageClears(PlayerId, StageId, ClearedAt) VALUES(@PlayerId, @StageId, @ClearedAt)",
            new { PlayerId = playerId, StageId = stageId, ClearedAt = DateTime.UtcNow });
    }
}

public class DapperUnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _conn;
    private readonly IDbTransaction _tx;

    public DapperUnitOfWork(IDbConnection conn, IDbTransaction tx)
    {
        _conn = conn;
        _tx = tx;
    }

    public Task<int> CommitAsync(CancellationToken ct = default)
    {
        _tx.Commit();
        return Task.FromResult(0);
    }
}
```

> 통합 테스트에서는 보통 트랜잭션을 테스트마다 열고 끝에 롤백하는 패턴을 많이 쓴다.
> 여기서는 단순하게 Commit만 보이도록 만들었다.

---

### 3-2. 스테이지 클리어 통합 테스트 예시 (Dapper + SQLite InMemory)

```csharp
using System.Data;
using Microsoft.Data.Sqlite;
using Dapper;
using Xunit;

public class StageIntegrationTests : IAsyncLifetime
{
    private IDbConnection _conn = null!;
    private IDbTransaction _tx = null!;
    private StageService _service = null!;

    public async Task InitializeAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        _conn = connection;

        // 스키마 생성
        await _conn.ExecuteAsync(@"
                                    CREATE TABLE Players(
                                        Id INTEGER PRIMARY KEY,
                                        Level INTEGER NOT NULL,
                                        Gems INTEGER NOT NULL,
                                        Gold INTEGER NOT NULL,
                                        Stamina INTEGER NOT NULL
                                    );
                                    CREATE TABLE Stages(
                                        Id INTEGER PRIMARY KEY,
                                        RequiredStamina INTEGER NOT NULL,
                                        RewardGold INTEGER NOT NULL,
                                        RewardGems INTEGER NOT NULL,
                                        BaseExp INTEGER NOT NULL
                                    );
                                    CREATE TABLE PlayerStageClears(
                                        PlayerId INTEGER NOT NULL,
                                        StageId INTEGER NOT NULL,
                                        ClearedAt TEXT NOT NULL
                                    );");

        // 초기 데이터
        await _conn.ExecuteAsync(
            "INSERT INTO Players(Id, Level, Gems, Gold, Stamina) VALUES(1, 1, 0, 0, 100);");
        await _conn.ExecuteAsync(
            "INSERT INTO Stages(Id, RequiredStamina, RewardGold, RewardGems, BaseExp) VALUES(1, 10, 100, 0, 50);");

        _tx = _conn.BeginTransaction();

        var playerRepo = new DapperPlayerRepository(_conn);
        var stageRepo = new DapperStageRepository(_conn);
        var uow = new DapperUnitOfWork(_conn, _tx);
        var domain = new StageDomainService();

        _service = new StageService(playerRepo, stageRepo, domain, uow);
    }

    public Task DisposeAsync()
    {
        _tx.Dispose();
        _conn.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ClearStage_UpdatesPlayerAndInsertsClearRecord()
    {
        var result = await _service.ClearAsync(new ClearStageCommand
        {
            PlayerId = 1,
            StageId = 1,
            IsSuccess = true
        });

        // 플레이어 상태 확인
        var playerRow = await _conn.QuerySingleAsync<dynamic>("SELECT * FROM Players WHERE Id = 1");
        Assert.Equal(90, (int)playerRow.Stamina);
        Assert.Equal(100, (int)playerRow.Gold);

        // 클리어 기록 확인
        var clearCount = await _conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM PlayerStageClears WHERE PlayerId = 1 AND StageId = 1");
        Assert.Equal(1, clearCount);
    }
}
```

---

### 3-3. 나머지 유즈케이스 통합 테스트 패턴

가챠 / 우편 / 출석체크도 패턴은 같다.

1. SQLite InMemory + Dapper로 테이블/데이터 준비
2. Dapper 기반 Repository 구현 (Mail, Attendance, GachaPool 등)
3. UnitOfWork/트랜잭션 시작
4. Application 서비스 호출
5. DB 상태를 직접 SELECT해서 검증

예를 들어 우편 수령 통합 테스트는:

* `Mails` 테이블, `Players` 테이블 생성
* `Mail` 행에 `IsClaimed = 0`, `RewardGold/Gems` 등 세팅
* `MailService.ClaimAsync` 호출
* `Mails`에서 `IsClaimed`가 1로 바뀌었는지, `Players`의 골드/젬이 증가했는지 확인

출석체크도:

* `AttendanceInfos` 테이블 준비
* 기존 레코드 없는 상태에서 호출 → 1일차 추가 + 보상 지급
* 기존 레코드 있는 상태에서 호출 → 일 수 증가 + 보상 지급

이런 식으로 Dapper Repository + 실제 DB 상태 검증으로 가져가면 된다.

---

여기까지가

* **4개 유즈케이스(가챠, 스테이지, 우편, 출석)**에 대한

  * 도메인 유닛 테스트
  * 애플리케이션 유닛 테스트
  * Dapper 기반 통합 테스트 패턴 예시

전체 흐름이다.
  


# 우편 수령 + 출석체크 보상 통합테스트 예제
전제는 앞에서 만든 도메인/애플리케이션 레이어(Mail, AttendanceInfo, MailService, AttendanceService, IPlayerRepository 등)가 이미 정의되어 있다는 것으로 두겠다.

여기서는 **Infrastructure(Dapper Repo) + 통합 테스트 코드**만 집중해서 보여준다.
DB는 SQLite In-Memory + Dapper를 사용한다.

---

## 1. 공통: Player용 Dapper Repository & UnitOfWork

우편/출석 둘 다 Player 재화를 갱신해야 하므로 공통으로 쓸 수 있는 Dapper 구현을 먼저 둔다.

```csharp
using System.Data;
using Dapper;

public class DapperPlayerRepository : IPlayerRepository
{
    private readonly IDbConnection _conn;

    public DapperPlayerRepository(IDbConnection conn)
    {
        _conn = conn;
    }

    public async Task<Player?> GetAsync(long id, CancellationToken ct = default)
    {
        var row = await _conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT Id, Level, Gems, Gold, Stamina FROM Players WHERE Id = @Id",
            new { Id = id });

        if (row == null) return null;

        var p = new Player((long)row.Id);
        p.GetType().GetProperty("Level")!.SetValue(p, (int)row.Level);
        p.GetType().GetProperty("Currency")!.SetValue(p, new Currency((int)row.Gems, (int)row.Gold));
        p.GetType().GetProperty("Stamina")!.SetValue(p, (int)row.Stamina);

        return p;
    }

    public async Task UpdateAsync(Player player, CancellationToken ct = default)
    {
        await _conn.ExecuteAsync(
            @"UPDATE Players
              SET Level = @Level,
                  Gems  = @Gems,
                  Gold  = @Gold,
                  Stamina = @Stamina
              WHERE Id = @Id",
            new
            {
                Id = player.Id,
                Level = player.Level,
                Gems = player.Currency.Gems,
                Gold = player.Currency.Gold,
                Stamina = player.Stamina
            });
    }
}

public class DapperUnitOfWork : IUnitOfWork
{
    private readonly IDbTransaction _tx;

    public DapperUnitOfWork(IDbTransaction tx)
    {
        _tx = tx;
    }

    public Task<int> CommitAsync(CancellationToken ct = default)
    {
        _tx.Commit();
        return Task.FromResult(0);
    }
}
```

테스트마다 트랜잭션을 열고, 끝나면 `Dispose`에서 롤백하는 패턴을 써도 되고, 여기처럼 `Commit`만 해도 된다(예제 단순화를 위해 Commit만 둔다).

---

## 2. 우편 수령: Dapper Repository + 통합 테스트

### 2.1. 우편 테이블 스키마

SQLite In-Memory에서 사용할 간단한 스키마다.

```sql
CREATE TABLE Players(
    Id      INTEGER PRIMARY KEY,
    Level   INTEGER NOT NULL,
    Gems    INTEGER NOT NULL,
    Gold    INTEGER NOT NULL,
    Stamina INTEGER NOT NULL
);

CREATE TABLE Mails(
    Id          INTEGER PRIMARY KEY,
    PlayerId    INTEGER NOT NULL,
    RewardGold  INTEGER NOT NULL,
    RewardGems  INTEGER NOT NULL,
    IsClaimed   INTEGER NOT NULL,
    ExpireAt    TEXT    NOT NULL
);
```

* `IsClaimed`는 0/1 정수로 두고, Dapper에서 bool처럼 다룬다
* `ExpireAt`는 TEXT(ISO8601)로 저장한다

### 2.2. DapperMailRepository 구현

앞에서 만든 `Mail` 도메인 클래스를 DB와 연결하는 Repository다.

```csharp
public class DapperMailRepository : IMailRepository
{
    private readonly IDbConnection _conn;

    public DapperMailRepository(IDbConnection conn)
    {
        _conn = conn;
    }

    public async Task<Mail?> GetAsync(long mailId, CancellationToken ct = default)
    {
        var row = await _conn.QuerySingleOrDefaultAsync<dynamic>(
            @"SELECT Id, PlayerId, RewardGold, RewardGems, IsClaimed, ExpireAt
              FROM Mails
              WHERE Id = @Id",
            new { Id = mailId });

        if (row == null) return null;

        var reward = new Reward((int)row.RewardGold, (int)row.RewardGems);
        var mail = new Mail((long)row.PlayerId, reward, DateTime.Parse((string)row.ExpireAt));
        mail.GetType().GetProperty("Id")!.SetValue(mail, (long)row.Id);
        mail.GetType().GetProperty("IsClaimed")!.SetValue(mail, ((long)row.IsClaimed) != 0);

        return mail;
    }

    public async Task UpdateAsync(Mail mail, CancellationToken ct = default)
    {
        await _conn.ExecuteAsync(
            @"UPDATE Mails
              SET IsClaimed = @IsClaimed
              WHERE Id = @Id",
            new
            {
                Id = mail.Id,
                IsClaimed = mail.IsClaimed ? 1 : 0
            });
    }
}
```

필요하면 RewardGold/RewardGems도 업데이트하지만, 여기서는 수령 후 reward 자체는 안 바뀐다고 보고 `IsClaimed`만 갱신한다.

### 2.3. TimeProvider 구현 (테스트용)

```csharp
public class FixedTimeProvider : ITimeProvider
{
    public DateTime UtcNow { get; }

    public FixedTimeProvider(DateTime utcNow)
    {
        UtcNow = utcNow;
    }
}
```

### 2.4. 우편 수령 통합 테스트

```csharp
using System.Data;
using Microsoft.Data.Sqlite;
using Dapper;
using Xunit;

public class MailIntegrationTests : IAsyncLifetime
{
    private IDbConnection _conn = null!;
    private IDbTransaction _tx = null!;
    private MailService _service = null!;

    public async Task InitializeAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        _conn = connection;

        // 스키마 생성
        await _conn.ExecuteAsync(@"
                                    CREATE TABLE Players(
                                        Id      INTEGER PRIMARY KEY,
                                        Level   INTEGER NOT NULL,
                                        Gems    INTEGER NOT NULL,
                                        Gold    INTEGER NOT NULL,
                                        Stamina INTEGER NOT NULL
                                    );
                                    CREATE TABLE Mails(
                                        Id          INTEGER PRIMARY KEY,
                                        PlayerId    INTEGER NOT NULL,
                                        RewardGold  INTEGER NOT NULL,
                                        RewardGems  INTEGER NOT NULL,
                                        IsClaimed   INTEGER NOT NULL,
                                        ExpireAt    TEXT    NOT NULL
                                    );");

        // 초기 데이터
        await _conn.ExecuteAsync(
            "INSERT INTO Players(Id, Level, Gems, Gold, Stamina) VALUES(1, 1, 0, 0, 100);");

        var expireAt = DateTime.UtcNow.AddDays(1).ToString("O");
        await _conn.ExecuteAsync(
            @"INSERT INTO Mails(Id, PlayerId, RewardGold, RewardGems, IsClaimed, ExpireAt)
              VALUES(100, 1, 500, 10, 0, @ExpireAt);",
            new { ExpireAt = expireAt });

        _tx = _conn.BeginTransaction();

        var playerRepo = new DapperPlayerRepository(_conn);
        var mailRepo = new DapperMailRepository(_conn);
        var time = new FixedTimeProvider(DateTime.UtcNow);
        var uow = new DapperUnitOfWork(_tx);

        _service = new MailService(mailRepo, playerRepo, time, uow);
    }

    public Task DisposeAsync()
    {
        _tx.Dispose();
        _conn.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ClaimMail_UpdatesMailAndPlayer()
    {
        var result = await _service.ClaimAsync(new ClaimMailCommand
        {
            PlayerId = 1,
            MailId = 100
        });

        // 플레이어 재화 검증
        var playerRow = await _conn.QuerySingleAsync<dynamic>(
            "SELECT Gems, Gold FROM Players WHERE Id = 1");
        Assert.Equal(10, (int)playerRow.Gems);
        Assert.Equal(500, (int)playerRow.Gold);

        // 우편 상태 검증
        var mailRow = await _conn.QuerySingleAsync<dynamic>(
            "SELECT IsClaimed FROM Mails WHERE Id = 100");
        Assert.Equal(1L, (long)mailRow.IsClaimed);
    }
}
```

이 테스트는 실제 흐름이 이렇게 된다.

* Dapper로 DB에 Player/Mail 입력
* `MailService.ClaimAsync` 호출
* Mail 도메인의 `Claim` 호출 + Player.AddReward
* DapperMailRepository/DapperPlayerRepository가 DB 상태 갱신
* 마지막에 DB에서 SELECT 해서 변경 결과 검증

---

## 3. 출석체크: Dapper Repository + 통합 테스트

### 3.1. 출석 테이블 스키마

```sql
CREATE TABLE Players(
    Id      INTEGER PRIMARY KEY,
    Level   INTEGER NOT NULL,
    Gems    INTEGER NOT NULL,
    Gold    INTEGER NOT NULL,
    Stamina INTEGER NOT NULL
);

CREATE TABLE AttendanceInfos(
    PlayerId      INTEGER PRIMARY KEY,
    CurrentDay    INTEGER NOT NULL,
    LastCheckedAt TEXT    NOT NULL
);
```

### 3.2. DapperAttendanceRepository 구현

```csharp
public class DapperAttendanceRepository : IAttendanceRepository
{
    private readonly IDbConnection _conn;

    public DapperAttendanceRepository(IDbConnection conn)
    {
        _conn = conn;
    }

    public async Task<AttendanceInfo?> GetAsync(long playerId, CancellationToken ct = default)
    {
        var row = await _conn.QuerySingleOrDefaultAsync<dynamic>(
            @"SELECT PlayerId, CurrentDay, LastCheckedAt
              FROM AttendanceInfos
              WHERE PlayerId = @PlayerId",
            new { PlayerId = playerId });

        if (row == null) return null;

        var info = new AttendanceInfo((long)row.PlayerId);
        info.GetType().GetProperty("CurrentDay")!.SetValue(info, (int)row.CurrentDay);
        info.GetType().GetProperty("LastCheckedAt")!.SetValue(info, DateTime.Parse((string)row.LastCheckedAt));

        return info;
    }

    public async Task AddAsync(AttendanceInfo attendance, CancellationToken ct = default)
    {
        await _conn.ExecuteAsync(
            @"INSERT INTO AttendanceInfos(PlayerId, CurrentDay, LastCheckedAt)
              VALUES(@PlayerId, @CurrentDay, @LastCheckedAt)",
            new
            {
                PlayerId = attendance.PlayerId,
                CurrentDay = attendance.CurrentDay,
                LastCheckedAt = attendance.LastCheckedAt.ToString("O")
            });
    }

    public async Task UpdateAsync(AttendanceInfo attendance, CancellationToken ct = default)
    {
        await _conn.ExecuteAsync(
            @"UPDATE AttendanceInfos
              SET CurrentDay = @CurrentDay,
                  LastCheckedAt = @LastCheckedAt
              WHERE PlayerId = @PlayerId",
            new
            {
                PlayerId = attendance.PlayerId,
                CurrentDay = attendance.CurrentDay,
                LastCheckedAt = attendance.LastCheckedAt.ToString("O")
            });
    }
}
```

### 3.3. 출석체크 보상 통합 테스트

```csharp
using System.Data;
using Microsoft.Data.Sqlite;
using Dapper;
using Xunit;

public class AttendanceIntegrationTests : IAsyncLifetime
{
    private IDbConnection _conn = null!;
    private IDbTransaction _tx = null!;
    private AttendanceService _service = null!;

    public async Task InitializeAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        _conn = connection;

        await _conn.ExecuteAsync(@"
CREATE TABLE Players(
    Id      INTEGER PRIMARY KEY,
    Level   INTEGER NOT NULL,
    Gems    INTEGER NOT NULL,
    Gold    INTEGER NOT NULL,
    Stamina INTEGER NOT NULL
);
CREATE TABLE AttendanceInfos(
    PlayerId      INTEGER PRIMARY KEY,
    CurrentDay    INTEGER NOT NULL,
    LastCheckedAt TEXT    NOT NULL
);");

        await _conn.ExecuteAsync(
            "INSERT INTO Players(Id, Level, Gems, Gold, Stamina) VALUES(1, 1, 0, 0, 100);");

        _tx = _conn.BeginTransaction();

        var playerRepo = new DapperPlayerRepository(_conn);
        var attRepo = new DapperAttendanceRepository(_conn);
        var policy = new AttendanceRewardPolicy();
        var time = new FixedTimeProvider(DateTime.UtcNow);
        var uow = new DapperUnitOfWork(_tx);

        _service = new AttendanceService(playerRepo, attRepo, policy, time, uow);
    }

    public Task DisposeAsync()
    {
        _tx.Dispose();
        _conn.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ClaimAttendance_FirstDay_InsertsRecordAndGivesReward()
    {
        var result = await _service.ClaimAsync(new ClaimAttendanceRewardCommand
        {
            PlayerId = 1
        });

        // Player 재화 확인 (1일차 = 1000 골드라고 가정)
        var playerRow = await _conn.QuerySingleAsync<dynamic>(
            "SELECT Gems, Gold FROM Players WHERE Id = 1");
        Assert.Equal(0, (int)playerRow.Gems);
        Assert.Equal(1000, (int)playerRow.Gold);

        // Attendance 레코드 확인
        var attRow = await _conn.QuerySingleAsync<dynamic>(
            "SELECT CurrentDay FROM AttendanceInfos WHERE PlayerId = 1");
        Assert.Equal(1, (int)attRow.CurrentDay);
    }

    [Fact]
    public async Task ClaimAttendance_SecondDay_UpdatesRecordAndGivesReward()
    {
        // 1일차 선지급
        await _service.ClaimAsync(new ClaimAttendanceRewardCommand { PlayerId = 1 });

        // 트랜잭션 안에서 같은 커넥션을 쓰고 있으니 AttendanceInfos에 이미 1일차가 들어가 있다
        var result = await _service.ClaimAsync(new ClaimAttendanceRewardCommand { PlayerId = 1 });

        // 2일차 = 10 Gems라고 가정
        var playerRow = await _conn.QuerySingleAsync<dynamic>(
            "SELECT Gems, Gold FROM Players WHERE Id = 1");
        Assert.Equal(10, (int)playerRow.Gems);
        Assert.Equal(1000 + 0, (int)playerRow.Gold); // 1일차 골드 + 2일차 젬

        var attRow = await _conn.QuerySingleAsync<dynamic>(
            "SELECT CurrentDay FROM AttendanceInfos WHERE PlayerId = 1");
        Assert.Equal(2, (int)attRow.CurrentDay);
    }
}
```

---

## 4. 정리

요청한 것처럼 **우편 수령 / 출석체크 보상** 유즈케이스에 대해:

* **Dapper 기반 Repository 구현**

  * `DapperMailRepository`
  * `DapperAttendanceRepository`
* **공통 Player용 DapperRepository + UnitOfWork**
* **SQLite In-Memory + Dapper를 사용하는 통합 테스트 코드**

까지 한 세트로 만들었다.

이 패턴을 그대로 복사해서
가챠, 스테이지, 기타 유즈케이스(예: 미션, 업적, 이벤트 보상)도 같은 방식으로 확장하면 된다.

---  
  

# 1. 미션 (Mission) 유즈케이스
이미 앞에서 `Player`, `Reward`, `Currency`, `IPlayerRepository`, `IUnitOfWork`, `ITimeProvider` 등은 정의되어 있다고 가정한다
(없다면 이전 답변 코드와 그대로 이어서 보면 된다).
  
## 1-1. 도메인 (MyApp.Domain)

### Mission 엔티티

* 특정 미션은 “진행도(Progress)”를 올리고, 완료되면 보상을 받을 수 있다고 가정한다
* 미션 보상은 한 번만 수령 가능하다고 본다

```csharp
public enum MissionType
{
    Daily,
    Weekly,
    Normal
}

public class Mission
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public MissionType Type { get; private set; }
    public int TargetCount { get; private set; }
    public int CurrentCount { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsClaimed { get; private set; }
    public Reward Reward { get; private set; }

    protected Mission() { }

    public Mission(long playerId, MissionType type, int targetCount, Reward reward)
    {
        PlayerId = playerId;
        Type = type;
        TargetCount = targetCount;
        Reward = reward;
        CurrentCount = 0;
        IsCompleted = false;
        IsClaimed = false;
    }

    public void AddProgress(int amount)
    {
        if (IsCompleted)
            return;

        CurrentCount += amount;
        if (CurrentCount >= TargetCount)
            IsCompleted = true;
    }

    public Reward Claim()
    {
        if (!IsCompleted)
            throw new InvalidOperationException("Mission not completed");

        if (IsClaimed)
            throw new InvalidOperationException("Mission already claimed");

        IsClaimed = true;
        return Reward;
    }
}
```

---

## 1-2. 애플리케이션 (MyApp.Application)

### 인터페이스

```csharp
public interface IMissionRepository
{
    Task<Mission?> GetAsync(long missionId, CancellationToken ct = default);
    Task UpdateAsync(Mission mission, CancellationToken ct = default);
}
```

### Command / Result DTO

```csharp
public class ClaimMissionRewardCommand
{
    public long PlayerId { get; init; }
    public long MissionId { get; init; }
}

public class ClaimMissionRewardResult
{
    public PlayerDto Player { get; init; } = null!;
    public RewardDto Reward { get; init; } = null!;
}
```

### MissionService

```csharp
public class MissionService
{
    private readonly IPlayerRepository _players;
    private readonly IMissionRepository _missions;
    private readonly IUnitOfWork _uow;

    public MissionService(IPlayerRepository players, IMissionRepository missions, IUnitOfWork uow)
    {
        _players = players;
        _missions = missions;
        _uow = uow;
    }

    public async Task<ClaimMissionRewardResult> ClaimAsync(ClaimMissionRewardCommand cmd, CancellationToken ct = default)
    {
        var player = await _players.GetAsync(cmd.PlayerId, ct)
            ?? throw new InvalidOperationException("Player not found");

        var mission = await _missions.GetAsync(cmd.MissionId, ct)
            ?? throw new InvalidOperationException("Mission not found");

        if (mission.PlayerId != player.Id)
            throw new InvalidOperationException("Mission does not belong to player");

        var reward = mission.Claim();
        player.AddReward(reward);

        await _missions.UpdateAsync(mission, ct);
        await _players.UpdateAsync(player, ct);
        await _uow.CommitAsync(ct);

        return new ClaimMissionRewardResult
        {
            Player = PlayerDto.From(player),
            Reward = RewardDto.From(reward)
        };
    }
}
```

---

## 1-3. Infrastructure – Dapper Repository

### 테이블 스키마 예시

```sql
CREATE TABLE Missions(
    Id           INTEGER PRIMARY KEY,
    PlayerId     INTEGER NOT NULL,
    Type         INTEGER NOT NULL,
    TargetCount  INTEGER NOT NULL,
    CurrentCount INTEGER NOT NULL,
    IsCompleted  INTEGER NOT NULL,
    IsClaimed    INTEGER NOT NULL,
    RewardGold   INTEGER NOT NULL,
    RewardGems   INTEGER NOT NULL
);
```

### DapperMissionRepository

```csharp
using System.Data;
using Dapper;

public class DapperMissionRepository : IMissionRepository
{
    private readonly IDbConnection _conn;

    public DapperMissionRepository(IDbConnection conn)
    {
        _conn = conn;
    }

    public async Task<Mission?> GetAsync(long missionId, CancellationToken ct = default)
    {
        var row = await _conn.QuerySingleOrDefaultAsync<dynamic>(
            @"SELECT Id, PlayerId, Type, TargetCount, CurrentCount,
                     IsCompleted, IsClaimed, RewardGold, RewardGems
              FROM Missions
              WHERE Id = @Id",
            new { Id = missionId });

        if (row == null)
            return null;

        var reward = new Reward((int)row.RewardGold, (int)row.RewardGems);
        var mission = new Mission((long)row.PlayerId, (MissionType)(int)row.Type, (int)row.TargetCount, reward);

        mission.GetType().GetProperty("Id")!.SetValue(mission, (long)row.Id);
        mission.GetType().GetProperty("CurrentCount")!.SetValue(mission, (int)row.CurrentCount);
        mission.GetType().GetProperty("IsCompleted")!.SetValue(mission, (long)row.IsCompleted != 0);
        mission.GetType().GetProperty("IsClaimed")!.SetValue(mission, (long)row.IsClaimed != 0);

        return mission;
    }

    public async Task UpdateAsync(Mission mission, CancellationToken ct = default)
    {
        await _conn.ExecuteAsync(
            @"UPDATE Missions
              SET CurrentCount = @CurrentCount,
                  IsCompleted  = @IsCompleted,
                  IsClaimed    = @IsClaimed
              WHERE Id = @Id",
            new
            {
                Id = mission.Id,
                CurrentCount = mission.CurrentCount,
                IsCompleted = mission.IsCompleted ? 1 : 0,
                IsClaimed = mission.IsClaimed ? 1 : 0
            });
    }
}
```

---

## 1-4. 미션 보상 통합 테스트 예시 (SQLite + Dapper)

```csharp
using System.Data;
using Microsoft.Data.Sqlite;
using Dapper;
using Xunit;

public class MissionIntegrationTests : IAsyncLifetime
{
    private IDbConnection _conn = null!;
    private IDbTransaction _tx = null!;
    private MissionService _service = null!;

    public async Task InitializeAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        _conn = connection;

        await _conn.ExecuteAsync(@"
CREATE TABLE Players(
    Id      INTEGER PRIMARY KEY,
    Level   INTEGER NOT NULL,
    Gems    INTEGER NOT NULL,
    Gold    INTEGER NOT NULL,
    Stamina INTEGER NOT NULL
);
CREATE TABLE Missions(
    Id           INTEGER PRIMARY KEY,
    PlayerId     INTEGER NOT NULL,
    Type         INTEGER NOT NULL,
    TargetCount  INTEGER NOT NULL,
    CurrentCount INTEGER NOT NULL,
    IsCompleted  INTEGER NOT NULL,
    IsClaimed    INTEGER NOT NULL,
    RewardGold   INTEGER NOT NULL,
    RewardGems   INTEGER NOT NULL
);");

        await _conn.ExecuteAsync(
            "INSERT INTO Players(Id, Level, Gems, Gold, Stamina) VALUES(1, 1, 0, 0, 100);");

        // 이미 완료된 미션이라고 가정 (CurrentCount >= TargetCount, IsCompleted=1)
        await _conn.ExecuteAsync(
            @"INSERT INTO Missions(Id, PlayerId, Type, TargetCount, CurrentCount, IsCompleted, IsClaimed, RewardGold, RewardGems)
              VALUES(100, 1, 0, 10, 10, 1, 0, 1000, 5);");

        _tx = _conn.BeginTransaction();

        var playerRepo = new DapperPlayerRepository(_conn);
        var missionRepo = new DapperMissionRepository(_conn);
        var uow = new DapperUnitOfWork(_tx);

        _service = new MissionService(playerRepo, missionRepo, uow);
    }

    public Task DisposeAsync()
    {
        _tx.Dispose();
        _conn.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ClaimMissionReward_UpdatesMissionAndPlayer()
    {
        var result = await _service.ClaimAsync(new ClaimMissionRewardCommand
        {
            PlayerId = 1,
            MissionId = 100
        });

        var playerRow = await _conn.QuerySingleAsync<dynamic>(
            "SELECT Gold, Gems FROM Players WHERE Id = 1");
        Assert.Equal(1000, (int)playerRow.Gold);
        Assert.Equal(5, (int)playerRow.Gems);

        var missionRow = await _conn.QuerySingleAsync<dynamic>(
            "SELECT IsClaimed FROM Missions WHERE Id = 100");
        Assert.Equal(1L, (long)missionRow.IsClaimed);
    }
}
```

---

# 2. 업적 (Achievement) 유즈케이스

업적은 **한 번 달성되면 언제든 보상 수령 가능**하다고 가정한다.

## 2-1. 도메인

```csharp
public class Achievement
{
    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public Reward Reward { get; private set; }

    protected Achievement() { }

    public Achievement(int id, string name, Reward reward)
    {
        Id = id;
        Name = name;
        Reward = reward;
    }
}

// 플레이어별 업적 상태
public class PlayerAchievement
{
    public long PlayerId { get; private set; }
    public int AchievementId { get; private set; }
    public bool IsUnlocked { get; private set; }
    public bool IsClaimed { get; private set; }

    protected PlayerAchievement() { }

    public PlayerAchievement(long playerId, int achievementId)
    {
        PlayerId = playerId;
        AchievementId = achievementId;
        IsUnlocked = false;
        IsClaimed = false;
    }

    public void Unlock()
    {
        if (!IsUnlocked)
            IsUnlocked = true;
    }

    public void MarkClaimed()
    {
        if (!IsUnlocked)
            throw new InvalidOperationException("Achievement not unlocked");
        if (IsClaimed)
            throw new InvalidOperationException("Already claimed");
        IsClaimed = true;
    }
}
```

실제로는 “언제 Unlock 되는지”는 다른 도메인 로직(예: 누적 킬 수, 스테이지 클리어 수 등)이 결정하고,
여기서는 **이미 Unlock 된 상태에서 보상만 받는 유즈케이스**만 다룬다.

---

## 2-2. 애플리케이션

### 인터페이스

```csharp
public interface IAchievementRepository
{
    Task<Achievement?> GetAsync(int achievementId, CancellationToken ct = default);
}

public interface IPlayerAchievementRepository
{
    Task<PlayerAchievement?> GetAsync(long playerId, int achievementId, CancellationToken ct = default);
    Task AddAsync(PlayerAchievement pa, CancellationToken ct = default);
    Task UpdateAsync(PlayerAchievement pa, CancellationToken ct = default);
}
```

### Command / Result DTO

```csharp
public class ClaimAchievementRewardCommand
{
    public long PlayerId { get; init; }
    public int AchievementId { get; init; }
}

public class ClaimAchievementRewardResult
{
    public PlayerDto Player { get; init; } = null!;
    public RewardDto Reward { get; init; } = null!;
}
```

### AchievementService

```csharp
public class AchievementService
{
    private readonly IPlayerRepository _players;
    private readonly IAchievementRepository _achievements;
    private readonly IPlayerAchievementRepository _playerAchievements;
    private readonly IUnitOfWork _uow;

    public AchievementService(
        IPlayerRepository players,
        IAchievementRepository achievements,
        IPlayerAchievementRepository playerAchievements,
        IUnitOfWork uow)
    {
        _players = players;
        _achievements = achievements;
        _playerAchievements = playerAchievements;
        _uow = uow;
    }

    public async Task<ClaimAchievementRewardResult> ClaimAsync(ClaimAchievementRewardCommand cmd, CancellationToken ct = default)
    {
        var player = await _players.GetAsync(cmd.PlayerId, ct)
            ?? throw new InvalidOperationException("Player not found");

        var achievement = await _achievements.GetAsync(cmd.AchievementId, ct)
            ?? throw new InvalidOperationException("Achievement not found");

        var pa = await _playerAchievements.GetAsync(cmd.PlayerId, cmd.AchievementId, ct)
                 ?? throw new InvalidOperationException("Player has not unlocked this achievement");

        if (!pa.IsUnlocked)
            throw new InvalidOperationException("Achievement not unlocked");

        pa.MarkClaimed();
        player.AddReward(achievement.Reward);

        await _playerAchievements.UpdateAsync(pa, ct);
        await _players.UpdateAsync(player, ct);
        await _uow.CommitAsync(ct);

        return new ClaimAchievementRewardResult
        {
            Player = PlayerDto.From(player),
            Reward = RewardDto.From(achievement.Reward)
        };
    }
}
```

---

## 2-3. Infrastructure – Dapper Repositories

### 테이블 스키마 예시

```sql
CREATE TABLE Achievements(
    Id         INTEGER PRIMARY KEY,
    Name       TEXT    NOT NULL,
    RewardGold INTEGER NOT NULL,
    RewardGems INTEGER NOT NULL
);

CREATE TABLE PlayerAchievements(
    PlayerId       INTEGER NOT NULL,
    AchievementId  INTEGER NOT NULL,
    IsUnlocked     INTEGER NOT NULL,
    IsClaimed      INTEGER NOT NULL,
    PRIMARY KEY(PlayerId, AchievementId)
);
```

### Dapper 구현

```csharp
public class DapperAchievementRepository : IAchievementRepository
{
    private readonly IDbConnection _conn;

    public DapperAchievementRepository(IDbConnection conn)
    {
        _conn = conn;
    }

    public async Task<Achievement?> GetAsync(int achievementId, CancellationToken ct = default)
    {
        var row = await _conn.QuerySingleOrDefaultAsync<dynamic>(
            @"SELECT Id, Name, RewardGold, RewardGems
              FROM Achievements
              WHERE Id = @Id",
            new { Id = achievementId });

        if (row == null) return null;

        var reward = new Reward((int)row.RewardGold, (int)row.RewardGems);
        return new Achievement((int)row.Id, (string)row.Name, reward);
    }
}

public class DapperPlayerAchievementRepository : IPlayerAchievementRepository
{
    private readonly IDbConnection _conn;

    public DapperPlayerAchievementRepository(IDbConnection conn)
    {
        _conn = conn;
    }

    public async Task<PlayerAchievement?> GetAsync(long playerId, int achievementId, CancellationToken ct = default)
    {
        var row = await _conn.QuerySingleOrDefaultAsync<dynamic>(
            @"SELECT PlayerId, AchievementId, IsUnlocked, IsClaimed
              FROM PlayerAchievements
              WHERE PlayerId = @PlayerId AND AchievementId = @AchievementId",
            new { PlayerId = playerId, AchievementId = achievementId });

        if (row == null) return null;

        var pa = new PlayerAchievement((long)row.PlayerId, (int)row.AchievementId);
        pa.GetType().GetProperty("IsUnlocked")!.SetValue(pa, (long)row.IsUnlocked != 0);
        pa.GetType().GetProperty("IsClaimed")!.SetValue(pa, (long)row.IsClaimed != 0);

        return pa;
    }

    public async Task AddAsync(PlayerAchievement pa, CancellationToken ct = default)
    {
        await _conn.ExecuteAsync(
            @"INSERT INTO PlayerAchievements(PlayerId, AchievementId, IsUnlocked, IsClaimed)
              VALUES(@PlayerId, @AchievementId, @IsUnlocked, @IsClaimed)",
            new
            {
                PlayerId = pa.PlayerId,
                AchievementId = pa.AchievementId,
                IsUnlocked = pa.IsUnlocked ? 1 : 0,
                IsClaimed = pa.IsClaimed ? 1 : 0
            });
    }

    public async Task UpdateAsync(PlayerAchievement pa, CancellationToken ct = default)
    {
        await _conn.ExecuteAsync(
            @"UPDATE PlayerAchievements
              SET IsUnlocked = @IsUnlocked,
                  IsClaimed  = @IsClaimed
              WHERE PlayerId = @PlayerId AND AchievementId = @AchievementId",
            new
            {
                PlayerId = pa.PlayerId,
                AchievementId = pa.AchievementId,
                IsUnlocked = pa.IsUnlocked ? 1 : 0,
                IsClaimed = pa.IsClaimed ? 1 : 0
            });
    }
}
```

통합 테스트는 미션/우편과 거의 동일 패턴으로 만들 수 있다

* Achievements/PlayerAchievements/Players 테이블 생성
* 데이터 insert
* `AchievementService.ClaimAsync` 호출
* Players/PlayerAchievements 상태 SELECT 후 검증

---

# 3. 이벤트 보상 (Event Reward) 유즈케이스
이벤트는 “기간 제한 + 1회성 보상 수령” 정도로 단순하게 가정한다
(실제로는 누적 포인트, 누적 접속일 등 여러 유형이 있지만 패턴은 동일하다).

## 3-1. 도메인

```csharp
public class EventDefinition
{
    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public Reward Reward { get; private set; }

    protected EventDefinition() { }

    public EventDefinition(int id, string name, DateTime startAt, DateTime endAt, Reward reward)
    {
        Id = id;
        Name = name;
        StartAt = startAt;
        EndAt = endAt;
        Reward = reward;
    }

    public bool IsActive(DateTime now) => now >= StartAt && now <= EndAt;
}

public class PlayerEventReward
{
    public long PlayerId { get; private set; }
    public int EventId { get; private set; }
    public bool IsClaimed { get; private set; }

    protected PlayerEventReward() { }

    public PlayerEventReward(long playerId, int eventId)
    {
        PlayerId = playerId;
        EventId = eventId;
        IsClaimed = false;
    }

    public void MarkClaimed()
    {
        if (IsClaimed)
            throw new InvalidOperationException("Event reward already claimed");
        IsClaimed = true;
    }
}
```

---

## 3-2. 애플리케이션

### 인터페이스

```csharp
public interface IEventDefinitionRepository
{
    Task<EventDefinition?> GetAsync(int eventId, CancellationToken ct = default);
}

public interface IPlayerEventRewardRepository
{
    Task<PlayerEventReward?> GetAsync(long playerId, int eventId, CancellationToken ct = default);
    Task AddAsync(PlayerEventReward per, CancellationToken ct = default);
    Task UpdateAsync(PlayerEventReward per, CancellationToken ct = default);
}
```

### Command / Result DTO

```csharp
public class ClaimEventRewardCommand
{
    public long PlayerId { get; init; }
    public int EventId { get; init; }
}

public class ClaimEventRewardResult
{
    public PlayerDto Player { get; init; } = null!;
    public RewardDto Reward { get; init; } = null!;
}
```

### EventRewardService

```csharp
public class EventRewardService
{
    private readonly IPlayerRepository _players;
    private readonly IEventDefinitionRepository _events;
    private readonly IPlayerEventRewardRepository _playerEvents;
    private readonly ITimeProvider _time;
    private readonly IUnitOfWork _uow;

    public EventRewardService(
        IPlayerRepository players,
        IEventDefinitionRepository events,
        IPlayerEventRewardRepository playerEvents,
        ITimeProvider time,
        IUnitOfWork uow)
    {
        _players = players;
        _events = events;
        _playerEvents = playerEvents;
        _time = time;
        _uow = uow;
    }

    public async Task<ClaimEventRewardResult> ClaimAsync(ClaimEventRewardCommand cmd, CancellationToken ct = default)
    {
        var now = _time.UtcNow;

        var player = await _players.GetAsync(cmd.PlayerId, ct)
            ?? throw new InvalidOperationException("Player not found");

        var ev = await _events.GetAsync(cmd.EventId, ct)
            ?? throw new InvalidOperationException("Event not found");

        if (!ev.IsActive(now))
            throw new InvalidOperationException("Event is not active");

        var per = await _playerEvents.GetAsync(cmd.PlayerId, cmd.EventId, ct)
                  ?? new PlayerEventReward(cmd.PlayerId, cmd.EventId);

        if (per.IsClaimed)
            throw new InvalidOperationException("Already claimed");

        per.MarkClaimed();
        player.AddReward(ev.Reward);

        if (per.IsClaimed && per.PlayerId == cmd.PlayerId && per.EventId == cmd.EventId)
        {
            // 신규면 Add, 기존이면 Update라고 가정하고 간단히 처리한다
            await _playerEvents.AddAsync(per, ct); // 실제로는 존재 여부 보고 분기하는게 안전하다
        }

        await _players.UpdateAsync(player, ct);
        await _uow.CommitAsync(ct);

        return new ClaimEventRewardResult
        {
            Player = PlayerDto.From(player),
            Reward = RewardDto.From(ev.Reward)
        };
    }
}
```

실제 구현에서는 `GetAsync` 결과 null / not null에 따라 Add/Update를 나눠야 한다
여기서는 예제를 단순하게 유지하기 위해 새로 생성한 경우만 Add 한다고 보았다.

---

## 3-3. Infrastructure – Dapper Repositories

### 테이블 스키마

```sql
CREATE TABLE EventDefinitions(
    Id         INTEGER PRIMARY KEY,
    Name       TEXT    NOT NULL,
    StartAt    TEXT    NOT NULL,
    EndAt      TEXT    NOT NULL,
    RewardGold INTEGER NOT NULL,
    RewardGems INTEGER NOT NULL
);

CREATE TABLE PlayerEventRewards(
    PlayerId  INTEGER NOT NULL,
    EventId   INTEGER NOT NULL,
    IsClaimed INTEGER NOT NULL,
    PRIMARY KEY(PlayerId, EventId)
);
```

### Dapper 구현

```csharp
public class DapperEventDefinitionRepository : IEventDefinitionRepository
{
    private readonly IDbConnection _conn;

    public DapperEventDefinitionRepository(IDbConnection conn)
    {
        _conn = conn;
    }

    public async Task<EventDefinition?> GetAsync(int eventId, CancellationToken ct = default)
    {
        var row = await _conn.QuerySingleOrDefaultAsync<dynamic>(
            @"SELECT Id, Name, StartAt, EndAt, RewardGold, RewardGems
              FROM EventDefinitions
              WHERE Id = @Id",
            new { Id = eventId });

        if (row == null) return null;

        var reward = new Reward((int)row.RewardGold, (int)row.RewardGems);
        return new EventDefinition(
            (int)row.Id,
            (string)row.Name,
            DateTime.Parse((string)row.StartAt),
            DateTime.Parse((string)row.EndAt),
            reward);
    }
}

public class DapperPlayerEventRewardRepository : IPlayerEventRewardRepository
{
    private readonly IDbConnection _conn;

    public DapperPlayerEventRewardRepository(IDbConnection conn)
    {
        _conn = conn;
    }

    public async Task<PlayerEventReward?> GetAsync(long playerId, int eventId, CancellationToken ct = default)
    {
        var row = await _conn.QuerySingleOrDefaultAsync<dynamic>(
            @"SELECT PlayerId, EventId, IsClaimed
              FROM PlayerEventRewards
              WHERE PlayerId = @PlayerId AND EventId = @EventId",
            new { PlayerId = playerId, EventId = eventId });

        if (row == null) return null;

        var per = new PlayerEventReward((long)row.PlayerId, (int)row.EventId);
        per.GetType().GetProperty("IsClaimed")!.SetValue(per, (long)row.IsClaimed != 0);

        return per;
    }

    public async Task AddAsync(PlayerEventReward per, CancellationToken ct = default)
    {
        await _conn.ExecuteAsync(
            @"INSERT INTO PlayerEventRewards(PlayerId, EventId, IsClaimed)
              VALUES(@PlayerId, @EventId, @IsClaimed)",
            new
            {
                PlayerId = per.PlayerId,
                EventId = per.EventId,
                IsClaimed = per.IsClaimed ? 1 : 0
            });
    }

    public async Task UpdateAsync(PlayerEventReward per, CancellationToken ct = default)
    {
        await _conn.ExecuteAsync(
            @"UPDATE PlayerEventRewards
              SET IsClaimed = @IsClaimed
              WHERE PlayerId = @PlayerId AND EventId = @EventId",
            new
            {
                PlayerId = per.PlayerId,
                EventId = per.EventId,
                IsClaimed = per.IsClaimed ? 1 : 0
            });
    }
}
```

통합 테스트는 다른 것들과 동일한 패턴으로 작성하면 된다

* Players, EventDefinitions, PlayerEventRewards 테이블 생성
* 활성 상태 이벤트를 insert
* Player 1 생성
* `EventRewardService.ClaimAsync` 호출
* Player 재화와 PlayerEventRewards의 IsClaimed를 SELECT해서 검증

---

## 마무리

지금까지

* **미션**: 도메인 `Mission`, `MissionService`, `DapperMissionRepository`, 통합 테스트
* **업적**: 도메인 `Achievement`, `PlayerAchievement`, `AchievementService`, Dapper Repo
* **이벤트 보상**: 도메인 `EventDefinition`, `PlayerEventReward`, `EventRewardService`, Dapper Repo

까지 다 구성했으니, 앞에서 만든

* 가챠
* 스테이지 클리어
* 우편
* 출석체크

까지 합치면 웬만한 수집형 RPG의 기본 보상/진행 유즈케이스는 거의 다 커버되는 셈이다.
  
  
     
# 도메인/유즈케이스들을 실제 솔루션 구조와 DI 구성까지 한 번에 정리
아래 기준으로 설명하겠다.

```text
src
 ├─ MyApp.Api             // Web API (Controller, API DTO)
 ├─ MyApp.Application     // 유즈케이스/서비스, Application DTO, 인터페이스
 ├─ MyApp.Domain          // Entity, ValueObject, Domain 서비스
 └─ MyApp.Infrastructure  // Dapper, Repository 구현, 외부 연동
```

---

## 1. 프로젝트 참조 구조

의존성 방향을 이렇게 고정한다고 보면 된다.

* `MyApp.Domain`

  * 아무 것도 참조하지 않음 (순수 C#)

* `MyApp.Application`

  * `MyApp.Domain`만 참조

* `MyApp.Infrastructure`

  * `MyApp.Domain`
  * `MyApp.Application`

* `MyApp.Api`

  * `MyApp.Application`
  * `MyApp.Infrastructure`

의도는:

* Domain은 제일 안쪽이라 아무 것도 모름
* Application은 규칙/유즈케이스만 알고 인프라를 인터페이스로 추상화함
* Infrastructure는 실제 구현(Dapper, DB 연결)을 제공함
* Api는 유즈케이스를 HTTP에 노출하고, DI만 구성함

---

## 2. 각 .csproj 예시

### 2.1. MyApp.Domain.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- 외부 패키지 의존성 없음: 순수 도메인 코드만 둔다 -->

</Project>
```

---

### 2.2. MyApp.Application.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyApp.Domain\MyApp.Domain.csproj" />
  </ItemGroup>

  <!-- 필요하다면 MediatR 등 추가 가능하지만,
       예제에서는 순수 C# + Domain 의존만 둔다 -->

</Project>
```

---

### 2.3. MyApp.Infrastructure.csproj

Dapper + DB Provider를 쓰는 프로젝트다.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyApp.Domain\MyApp.Domain.csproj" />
    <ProjectReference Include="..\MyApp.Application\MyApp.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Dapper" Version="2.1.35" />
    <PackageReference Include="MySqlConnector" Version="2.3.7" />
    <!-- 개발/테스트용으로 SQLite를 쓴다면
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" /> -->
  </ItemGroup>

</Project>
```

여기에는:

* `DapperPlayerRepository`, `DapperStageRepository`,
  `DapperMailRepository`, `DapperAttendanceRepository`,
  `DapperMissionRepository`, `DapperAchievementRepository`,
  `DapperEventDefinitionRepository`, `DapperPlayerEventRewardRepository`
* `DapperUnitOfWork`, `FixedTimeProvider`(테스트용 말고 실제용 TimeProvider 구현)
* DB Connection/Transaction을 사용하는 각종 구현체

들을 둔다고 보면 된다.

---

### 2.4. MyApp.Api.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyApp.Application\MyApp.Application.csproj" />
    <ProjectReference Include="..\MyApp.Infrastructure\MyApp.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  </ItemGroup>

</Project>
```

여기에는:

* `GachaController`, `StageController`, `MailController`, `AttendanceController`,
  `MissionController`, `AchievementController`, `EventController` 등
* API DTO (Request/Response)
* Program.cs / DI 구성, 미들웨어, Swagger 설정

이 들어간다고 보면 된다.

---

## 3. Infrastructure에 DI 확장 메서드 만들기

`MyApp.Infrastructure` 프로젝트에 DI 확장 메서드를 만들어두면 Api 쪽 Program.cs가 깔끔해진다.

예시로 `DependencyInjection.cs` 파일을 만든다고 하겠다.

```csharp
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using MyApp.Application;
using MyApp.Domain;

namespace MyApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("GameDb");

        // IDbConnection 등록 (Request 스코프)
        services.AddScoped<IDbConnection>(sp =>
        {
            var conn = new MySqlConnection(connectionString);
            conn.Open();
            return conn;
        });

        // IDbTransaction + IUnitOfWork 등록 (Request 스코프)
        services.AddScoped<IDbTransaction>(sp =>
        {
            var conn = sp.GetRequiredService<IDbConnection>();
            return conn.BeginTransaction();
        });

        services.AddScoped<IUnitOfWork>(sp =>
        {
            var tx = sp.GetRequiredService<IDbTransaction>();
            return new DapperUnitOfWork(tx);
        });

        // TimeProvider
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();

        // Dapper Repository들 등록
        services.AddScoped<IPlayerRepository, DapperPlayerRepository>();
        services.AddScoped<IStageRepository, DapperStageRepository>();
        services.AddScoped<IMailRepository, DapperMailRepository>();
        services.AddScoped<IAttendanceRepository, DapperAttendanceRepository>();
        services.AddScoped<IMissionRepository, DapperMissionRepository>();
        services.AddScoped<IAchievementRepository, DapperAchievementRepository>();
        services.AddScoped<IPlayerAchievementRepository, DapperPlayerAchievementRepository>();
        services.AddScoped<IEventDefinitionRepository, DapperEventDefinitionRepository>();
        services.AddScoped<IPlayerEventRewardRepository, DapperPlayerEventRewardRepository>();

        return services;
    }
}

// 실제 시간 제공용 구현
public class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
```

> 주의: Dapper Repository 구현에서 `IDbConnection`만 쓰고 `IDbTransaction`을 안 넘겨주면
> 트랜잭션이 적용되지 않을 수 있으니, 실제 구현에서는 생성자에 `IDbTransaction`도 주입하거나,
> Dapper 호출 시 `transaction: _tx`를 넘기는 식으로 통일하는 편이 좋다.

---

## 4. Application 서비스 DI 등록

Application 프로젝트에 있는 서비스들도 DI로 등록해야 한다.

보통은 `MyApp.Api` 쪽에서 직접 등록하거나,
`MyApp.Application`에 `DependencyInjection` 확장 메서드를 하나 더 두고 거기서 묶는다.

여기서는 Application쪽에 `DependencyInjection`을 둔다고 가정하겠다.

### MyApp.Application.DependencyInjection.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using MyApp.Domain;

namespace MyApp.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 도메인 서비스 (도메인에 있지만 DI가 필요하므로 여기서 등록)
        services.AddScoped<GachaDomainService>();
        services.AddScoped<StageDomainService>();
        services.AddSingleton<AttendanceRewardPolicy>();

        // Application 서비스 (유즈케이스)
        services.AddScoped<GachaService>();
        services.AddScoped<StageService>();
        services.AddScoped<MailService>();
        services.AddScoped<AttendanceService>();
        services.AddScoped<MissionService>();
        services.AddScoped<AchievementService>();
        services.AddScoped<EventRewardService>();

        return services;
    }
}
```

이렇게 해두면 Api 쪽 Program.cs에서 한 줄로 Application 서비스들을 등록할 수 있다.

---

## 5. Program.cs / DI 구성 전체 예시

`MyApp.Api` 프로젝트의 `Program.cs`를 다음처럼 구성할 수 있다.

```csharp
using MyApp.Application;
using MyApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Controller + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Application 레이어 등록
builder.Services.AddApplication();

// Infrastructure(Dapper, Repositories, UnitOfWork, TimeProvider) 등록
builder.Services.AddInfrastructure(builder.Configuration);

// 인증/인가, 기타 미들웨어 설정도 여기서 추가한다
// builder.Services.AddAuthentication(...);
// builder.Services.AddAuthorization(...);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

app.Run();
```

이 상태에서 Controller들은 전부 DI로 서비스들을 받을 수 있다.

예를 들어 `MailController`는 이렇게 생긴다.

```csharp
[ApiController]
[Route("api/mail")]
public class MailController : ControllerBase
{
    private readonly MailService _mailService;
    private readonly ICurrentPlayerProvider _currentPlayer;

    public MailController(MailService mailService, ICurrentPlayerProvider currentPlayer)
    {
        _mailService = mailService;
        _currentPlayer = currentPlayer;
    }

    [HttpPost("claim")]
    public async Task<ActionResult<ClaimMailResponse>> Claim([FromBody] ClaimMailRequest request)
    {
        var cmd = new ClaimMailCommand
        {
            PlayerId = _currentPlayer.PlayerId,
            MailId = request.MailId
        };

        var result = await _mailService.ClaimAsync(cmd);

        var response = new ClaimMailResponse
        {
            Player = PlayerResponse.From(result.Player),
            Reward  = RewardResponse.From(result.Reward)
        };

        return Ok(response);
    }
}
```

다른 컨트롤러들도 마찬가지로:

* `GachaController` → `GachaService`
* `StageController` → `StageService`
* `AttendanceController` → `AttendanceService`
* `MissionController` → `MissionService`
* `AchievementController` → `AchievementService`
* `EventController` → `EventRewardService`

를 주입받아서 사용하면 된다.

---

## 6. 요약

정리하면:

1. **프로젝트 구조**

   * `MyApp.Domain`
   * `MyApp.Application` (Domain 참조)
   * `MyApp.Infrastructure` (Domain + Application 참조, Dapper/DB 구현)
   * `MyApp.Api` (Application + Infrastructure 참조, Controller/Program)

2. **DI 분리 전략**

   * Application에 `AddApplication` 확장 메서드 → 도메인 서비스 + 유즈케이스 서비스 등록
   * Infrastructure에 `AddInfrastructure` 확장 메서드 → Dapper Repos + UnitOfWork + IDbConnection + TimeProvider 등록
   * Api의 Program.cs에서는 `AddApplication()`, `AddInfrastructure()`만 호출

3. **유즈케이스들**

   * 가챠, 스테이지 클리어, 우편 수령, 출석체크, 미션 보상, 업적 보상, 이벤트 보상
   * 전부 동일한 패턴으로

     * Domain: 규칙
     * Application: 흐름/트랜잭션
     * Infrastructure: DB/Dapper 구현
     * Api: HTTP 입출력

까지 연결된다.

지금 단계에서 실제로 구현을 시작하고 싶은 클래스(예: MissionController, EventController)의 구체적인 코드 골격이 필요하면, 그 부분만 골라서 컨트롤러 + DTO까지 쭉 만들어줄 수 있다.

  


# 우편함(메일 목록 조회) 기능*을 4레이어 전체로 추가
이미 있는 전제들 그대로 쓴다

* 레이어 구조

  * `MyApp.Domain`
  * `MyApp.Application`
  * `MyApp.Infrastructure` (Dapper)
  * `MyApp.Api`

* 도메인 `Mail`은 이미 있고, `MailService.ClaimAsync` 같은 “수령” 유즈케이스도 구현돼 있다고 가정한다.

여기서는 **“우편함 목록 조회 + 단건 조회”** 기준으로 만든다:

* 우편함 목록: `/api/mail`

  * 페이지네이션
  * 정렬(보통 최신순)
  * 기본 필터: 아직 만료 안 된 메일
  * 옵션: 수령 완료/미수령 필터
* 단건 조회: `/api/mail/{id}`

---

## 1. Domain: Mail에 약간만 정보 추가

우편함 구현 자체는 거의 애플리케이션/인프라 레이어 일이기 때문에
도메인은 크게 바꿀 필요는 없고, **CreatedAt** 정도만 추가하면 충분하다.

```csharp
// MyApp.Domain.Mail

public class Mail
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public string Title { get; private set; } = "";
    public string Body { get; private set; } = "";
    public Reward Reward { get; private set; }
    public bool IsClaimed { get; private set; }
    public DateTime ExpireAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected Mail() { }

    public Mail(long playerId, string title, string body, Reward reward, DateTime expireAt)
    {
        PlayerId = playerId;
        Title = title;
        Body = body;
        Reward = reward;
        ExpireAt = expireAt;
        CreatedAt = DateTime.UtcNow;
        IsClaimed = false;
    }

    public bool IsExpired(DateTime now) => now > ExpireAt;

    public Reward Claim(DateTime now)
    {
        if (IsClaimed)
            throw new InvalidOperationException("Mail already claimed.");
        if (IsExpired(now))
            throw new InvalidOperationException("Mail expired.");

        IsClaimed = true;
        return Reward;
    }
}
```

우편함 자체는 별 Aggregate를 만들 필요 없고,
“플레이어의 Mail 리스트”가 곧 우편함이라 보고 Repository에서 목록을 가져오면 된다.

---

## 2. Application: Mailbox 유즈케이스 설계

### 2.1. 쿼리/DTO 정의

우편함은 보통 목록용 DTO가 필요하니 **요약용 + 상세용**으로 나눈다.

```csharp
// MyApp.Application.Dtos.MailDto들

public class MailSummaryDto
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsClaimed { get; set; }
    public bool IsExpired { get; set; }
    public DateTime ExpireAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public static MailSummaryDto From(Mail mail, DateTime now) => new()
    {
        Id = mail.Id,
        Title = mail.Title,
        IsClaimed = mail.IsClaimed,
        IsExpired = mail.IsExpired(now),
        ExpireAt = mail.ExpireAt,
        CreatedAt = mail.CreatedAt
    };
}

public class MailDetailDto
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsClaimed { get; set; }
    public bool IsExpired { get; set; }
    public DateTime ExpireAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public RewardDto Reward { get; set; } = null!;

    public static MailDetailDto From(Mail mail, DateTime now) => new()
    {
        Id = mail.Id,
        Title = mail.Title,
        Body = mail.Body,
        IsClaimed = mail.IsClaimed,
        IsExpired = mail.IsExpired(now),
        ExpireAt = mail.ExpireAt,
        CreatedAt = mail.CreatedAt,
        Reward = RewardDto.From(mail.Reward)
    };
}

public class PagedMailResultDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<MailSummaryDto> Items { get; set; } = Array.Empty<MailSummaryDto>();
}
```

### 2.2. Repository 인터페이스 확장

기존 `IMailRepository`에 “목록 조회” 기능을 추가한다.

```csharp
// MyApp.Application.Interfaces.Repositories

public interface IMailRepository
{
    Task<Mail?> GetAsync(long mailId, CancellationToken ct = default);
    Task UpdateAsync(Mail mail, CancellationToken ct = default);

    // 우편함 목록 조회
    Task<IReadOnlyList<Mail>> GetListByPlayerAsync(
        long playerId,
        int skip,
        int take,
        bool includeClaimed,
        bool includeExpired,
        DateTime now,
        CancellationToken ct = default);

    Task<int> CountByPlayerAsync(
        long playerId,
        bool includeClaimed,
        bool includeExpired,
        DateTime now,
        CancellationToken ct = default);
}
```

필터 기준은 이렇게 두었다.

* `includeClaimed`: true면 수령한 메일도 포함, false면 미수령만
* `includeExpired`: true면 만료된 메일도 포함, false면 `ExpireAt > now` 만

### 2.3. MailboxService 유즈케이스

이건 “우편함 조회 전용” 서비스라고 생각하면 된다.

```csharp
// MyApp.Application.Mailbox.MailboxService

public class MailboxQuery
{
    public long PlayerId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool IncludeClaimed { get; init; } = false;
    public bool IncludeExpired { get; init; } = false;
}

public class MailboxService
{
    private readonly IMailRepository _mails;
    private readonly ITimeProvider _time;

    public MailboxService(IMailRepository mails, ITimeProvider time)
    {
        _mails = mails;
        _time = time;
    }

    public async Task<PagedMailResultDto> GetMailboxAsync(MailboxQuery query, CancellationToken ct = default)
    {
        var now = _time.UtcNow;
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

        var skip = (page - 1) * pageSize;
        var take = pageSize;

        var mails = await _mails.GetListByPlayerAsync(
            query.PlayerId,
            skip,
            take,
            query.IncludeClaimed,
            query.IncludeExpired,
            now,
            ct);

        var total = await _mails.CountByPlayerAsync(
            query.PlayerId,
            query.IncludeClaimed,
            query.IncludeExpired,
            now,
            ct);

        var items = mails.Select(m => MailSummaryDto.From(m, now)).ToList();

        return new PagedMailResultDto
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
    }

    public async Task<MailDetailDto> GetMailDetailAsync(long playerId, long mailId, CancellationToken ct = default)
    {
        var now = _time.UtcNow;

        var mail = await _mails.GetAsync(mailId, ct)
            ?? throw new InvalidOperationException("Mail not found");

        if (mail.PlayerId != playerId)
            throw new InvalidOperationException("Mail does not belong to player");

        return MailDetailDto.From(mail, now);
    }
}
```

---

## 3. Infrastructure: Dapper 기반 MailRepository 구현

### 3.1. 테이블 스키마 상정

앞에서 썼던 것에 컬럼 조금 확장:

```sql
CREATE TABLE Mails(
    Id          BIGINT PRIMARY KEY,
    PlayerId    BIGINT NOT NULL,
    Title       TEXT   NOT NULL,
    Body        TEXT   NOT NULL,
    RewardGold  INT    NOT NULL,
    RewardGems  INT    NOT NULL,
    IsClaimed   INT    NOT NULL,
    ExpireAt    TEXT   NOT NULL,
    CreatedAt   TEXT   NOT NULL
);

CREATE INDEX IX_Mails_PlayerId_CreatedAt
    ON Mails(PlayerId, CreatedAt DESC);
```

### 3.2. DapperMailRepository 구현

```csharp
// MyApp.Infrastructure.Persistence.DapperMailRepository

using System.Data;
using Dapper;
using MyApp.Application;
using MyApp.Domain;

public class DapperMailRepository : IMailRepository
{
    private readonly IDbConnection _conn;

    public DapperMailRepository(IDbConnection conn)
    {
        _conn = conn;
    }

    public async Task<Mail?> GetAsync(long mailId, CancellationToken ct = default)
    {
        var row = await _conn.QuerySingleOrDefaultAsync<dynamic>(
            @"SELECT Id, PlayerId, Title, Body,
                     RewardGold, RewardGems,
                     IsClaimed, ExpireAt, CreatedAt
              FROM Mails
              WHERE Id = @Id",
            new { Id = mailId });

        if (row == null)
            return null;

        return MapToMail(row);
    }

    public async Task UpdateAsync(Mail mail, CancellationToken ct = default)
    {
        await _conn.ExecuteAsync(
            @"UPDATE Mails
              SET IsClaimed = @IsClaimed
              WHERE Id = @Id",
            new
            {
                Id = mail.Id,
                IsClaimed = mail.IsClaimed ? 1 : 0
            });
    }

    public async Task<IReadOnlyList<Mail>> GetListByPlayerAsync(
        long playerId,
        int skip,
        int take,
        bool includeClaimed,
        bool includeExpired,
        DateTime now,
        CancellationToken ct = default)
    {
        // WHERE 조건 구성
        var sql = @"
SELECT Id, PlayerId, Title, Body,
       RewardGold, RewardGems,
       IsClaimed, ExpireAt, CreatedAt
FROM Mails
WHERE PlayerId = @PlayerId";

        if (!includeClaimed)
            sql += " AND IsClaimed = 0";

        if (!includeExpired)
            sql += " AND ExpireAt > @Now";

        sql += @"
ORDER BY CreatedAt DESC
LIMIT @Take OFFSET @Skip;";

        var rows = await _conn.QueryAsync<dynamic>(
            sql,
            new
            {
                PlayerId = playerId,
                Now = now.ToString("O"),
                Skip = skip,
                Take = take
            });

        var list = rows.Select(MapToMail).ToList();
        return list;
    }

    public async Task<int> CountByPlayerAsync(
        long playerId,
        bool includeClaimed,
        bool includeExpired,
        DateTime now,
        CancellationToken ct = default)
    {
        var sql = @"
SELECT COUNT(*)
FROM Mails
WHERE PlayerId = @PlayerId";

        if (!includeClaimed)
            sql += " AND IsClaimed = 0";

        if (!includeExpired)
            sql += " AND ExpireAt > @Now";

        var count = await _conn.ExecuteScalarAsync<long>(
            sql,
            new
            {
                PlayerId = playerId,
                Now = now.ToString("O")
            });

        return (int)count;
    }

    private Mail MapToMail(dynamic row)
    {
        var reward = new Reward((int)row.RewardGold, (int)row.RewardGems);

        var mail = new Mail(
            (long)row.PlayerId,
            (string)row.Title,
            (string)row.Body,
            reward,
            DateTime.Parse((string)row.ExpireAt));

        mail.GetType().GetProperty("Id")!.SetValue(mail, (long)row.Id);
        mail.GetType().GetProperty("IsClaimed")!.SetValue(mail, (long)row.IsClaimed != 0);
        mail.GetType().GetProperty("CreatedAt")!.SetValue(mail, DateTime.Parse((string)row.CreatedAt));

        return mail;
    }
}
```

---

## 4. Api: 우편함용 Controller & DTO

### 4.1. API DTO

```csharp
// MyApp.Api.Dtos.Mailbox

public class MailboxRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeClaimed { get; set; } = false;
    public bool IncludeExpired { get; set; } = false;
}

public class MailSummaryResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsClaimed { get; set; }
    public bool IsExpired { get; set; }
    public DateTime ExpireAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public static MailSummaryResponse From(MailSummaryDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        IsClaimed = dto.IsClaimed,
        IsExpired = dto.IsExpired,
        ExpireAt = dto.ExpireAt,
        CreatedAt = dto.CreatedAt
    };
}

public class PagedMailboxResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<MailSummaryResponse> Items { get; set; } = Array.Empty<MailSummaryResponse>();
}

public class MailDetailResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsClaimed { get; set; }
    public bool IsExpired { get; set; }
    public DateTime ExpireAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public RewardResponse Reward { get; set; } = null!;

    public static MailDetailResponse From(MailDetailDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Body = dto.Body,
        IsClaimed = dto.IsClaimed,
        IsExpired = dto.IsExpired,
        ExpireAt = dto.ExpireAt,
        CreatedAt = dto.CreatedAt,
        Reward = RewardResponse.From(dto.Reward)
    };
}
```

### 4.2. Controller

기존 `MailController`에 우편함 API를 추가한다고 하겠다.

```csharp
// MyApp.Api.Controllers.MailController

[ApiController]
[Route("api/mail")]
public class MailController : ControllerBase
{
    private readonly MailService _mailService;       // 수령용 기존 서비스
    private readonly MailboxService _mailboxService; // 새로 만든 우편함 서비스
    private readonly ICurrentPlayerProvider _currentPlayer;

    public MailController(
        MailService mailService,
        MailboxService mailboxService,
        ICurrentPlayerProvider currentPlayer)
    {
        _mailService = mailService;
        _mailboxService = mailboxService;
        _currentPlayer = currentPlayer;
    }

    // 우편함 목록
    [HttpGet]
    public async Task<ActionResult<PagedMailboxResponse>> GetMailbox([FromQuery] MailboxRequest request)
    {
        var query = new MailboxQuery
        {
            PlayerId = _currentPlayer.PlayerId,
            Page = request.Page,
            PageSize = request.PageSize,
            IncludeClaimed = request.IncludeClaimed,
            IncludeExpired = request.IncludeExpired
        };

        var result = await _mailboxService.GetMailboxAsync(query);

        var response = new PagedMailboxResponse
        {
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            Items = result.Items.Select(MailSummaryResponse.From).ToList()
        };

        return Ok(response);
    }

    // 우편 상세
    [HttpGet("{mailId:long}")]
    public async Task<ActionResult<MailDetailResponse>> GetMail(long mailId)
    {
        var playerId = _currentPlayer.PlayerId;

        var dto = await _mailboxService.GetMailDetailAsync(playerId, mailId);

        return Ok(MailDetailResponse.From(dto));
    }

    // 이미 만들어 둔 우편 수령 API (예시)
    [HttpPost("claim")]
    public async Task<ActionResult<ClaimMailResponse>> Claim([FromBody] ClaimMailRequest request)
    {
        var cmd = new ClaimMailCommand
        {
            PlayerId = _currentPlayer.PlayerId,
            MailId = request.MailId
        };

        var result = await _mailService.ClaimAsync(cmd);

        return Ok(new ClaimMailResponse
        {
            Player = PlayerResponse.From(result.Player),
            Reward = RewardResponse.From(result.Reward)
        });
    }
}
```

---

## 5. Application / Infrastructure DI에 우편함 서비스 추가

### 5.1. Application DI

```csharp
// MyApp.Application.DependencyInjection

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 도메인 서비스
        services.AddScoped<GachaDomainService>();
        services.AddScoped<StageDomainService>();
        services.AddSingleton<AttendanceRewardPolicy>();

        // Application 서비스
        services.AddScoped<GachaService>();
        services.AddScoped<StageService>();
        services.AddScoped<MailService>();
        services.AddScoped<MailboxService>();      // ← 여기
        services.AddScoped<AttendanceService>();
        services.AddScoped<MissionService>();
        services.AddScoped<AchievementService>();
        services.AddScoped<EventRewardService>();

        return services;
    }
}
```

### 5.2. Infrastructure DI는 이미 `IMailRepository -> DapperMailRepository`를 등록하고 있으니 그대로 사용하면 된다.

---

## 6. 요약

지금까지 내용대로라면 **우편함 기능**은 이렇게 구성된다.

* **Domain**

  * `Mail` 엔티티에 `CreatedAt`, `IsExpired(now)` 등 최소 정보/행동 추가

* **Application**

  * `MailSummaryDto`, `MailDetailDto`, `PagedMailResultDto`
  * `MailboxQuery`
  * `MailboxService`
  * `IMailRepository`에 `GetListByPlayerAsync`, `CountByPlayerAsync` 추가

* **Infrastructure(Dapper)**

  * `Mails` 테이블에 Title/Body/Reward/IsClaimed/ExpireAt/CreatedAt 저장
  * `DapperMailRepository`에서 목록/카운트 쿼리 구현

* **Api**

  * `GET /api/mail` → 우편함 목록 (페이지, 수령/만료 필터 가능)
  * `GET /api/mail/{mailId}` → 우편 상세
  * 기존 `POST /api/mail/claim` → 메일 수령

이 구조대로 구현하면, 클라이언트 쪽에서는

* 우편함 리스트 요청 → 목록/페이지 정보 수신
* 아이템 선택 시 상세 요청 → 내용 + 보상 확인
* “받기” 버튼 → 수령 API 호출 → 인벤 업데이트

이렇게 깔끔하게 흐름을 구성할 수 있다  .




================================================
File: references/Serilog.md
================================================
# Serilog 사용 가이드

Serilog는 .NET 애플리케이션에서 구조화된 로깅을 지원하는 강력한 라이브러리입니다.
다양한 기능을 통해 개발자는 로그를 효율적으로 관리하고 분석할 수 있습니다.
본 저장소는 Serilog의 주요 기능과 각 기능의 사용 예시를 코드와 함께 설명합니다.

## 목차

- [Serilog 소개](#serilog-소개)
- [기본 활용법](#기본-활용법)
  - [설치](#설치)
  - [구성](#구성)
    - [기본 구성](#1-기본적인-설정과-콘솔-로깅)
      - [설정 파일 구성](#2-appsettingsjson-파일-기반-구성)
      - [싱크(Sink)추가 및 동시 출력](#3-로그-동시-출력-및-sink-추가)
    - [출력 구성](#출력-구성)
      - [Output Template으로 포맷팅 설정](#1-output-template을-통한-포맷팅-설정)
      - [JSON 형식으로 출력하기](#2-json형식으로-출력하기)
        - [Serilog.Formatting.Json.JsonFormatter (기본 제공)](#serilogformattingjsonjsonformatter)
        - [Serilog.Formatting.Compact.CompactJsonFormatter (압축형)](#serilogformattingcompactcompactjsonformatter)
      - [Enrich로 추가 정보 설정](#3-enrich를-활용한-추가-정보-설정)
      - [Filter로 조건부 로깅](#4-filter를-이용한-조건부-로깅)
    - [Sink 구성](#sink-구성)
      - [파일 싱크 (Serilog.Sinks.File)](#serilogsinksfile)
      - [콘솔 싱크 (Serilog.Sinks.Console)](#serilogsinksconsole)
      - [디버그 싱크 (Serilog.Sinks.Debug)](#serilogsinksdebug)
      - [비동기 싱크 (Serilog.Sinks.Async)](#serilogsinksasync)
      - [Elasticsearch 싱크 (Serilog.Sinks.Elasticsearch)](#serilogsinkselasticsearch)
      - [HTTP 싱크 (Serilog.Sinks.Http)](#serilogsinkshttp)
      - [SQL Server 싱크 (Serilog.Sinks.MSSqlServer)](#serilogsinksmssqlserver)
      - [SQLite 싱크 (Serilog.Sinks.SQLite)](#serilogsinkssqlite)
      - [MongoDB 싱크 (Serilog.Sinks.MongoDB)](#serilogsinksmongodb)
- [로그 구조화](#로그-구조화)
  - [기본 직렬화 방식](#기본-동작)
  - [콜렉션 처리](#콜렉션-처리)
  - [객체 처리](#객체-처리)
  - [메시지 템플릿 (Message Template)](#message-template)
- [활용 예제]()
  - [ASP .NET Core 9 Web API Server](#asp-net-core-9-web-api-server)
  - [.NET 9 Socket Server using SuperSocket](#net-9-socket-server-using-supersocket)

## Serilog 소개

[Serilog](https://serilog.net/)는 .NET 플랫폼을 위한 구조화된 로깅 라이브러리로, 로그 데이터를 구조화된 형식으로 기록하여 효율적인 검색과 분석을 가능하게 합니다.

다양한 싱크([Sink](https://github.com/serilog/serilog/wiki/Provided-Sinks)) 통해 콘솔, 파일, 데이터베이스 등 여러 출력 대상으로 로그를 전송할 수 있습니다.

# 기본 활용법

## 설치

Serilog를 사용하려면 NuGet 패키지를 통해 필요한 라이브러리를 설치해야 합니다.

```
Using Serilog;
```

기본적으로 Serilog와 원하는 싱크 패키지를 설치합니다.
각 싱크 패키지의 자세한 정보는 [Sink 구성](#sink-구성) 항목에서 확인 하세요.

## 구성

Serilog의 로거는 `LoggerConfiguration` 클래스를 사용하여 구성한뒤

`CreateLogger()` 실행을 통해 로거를 생성합니다.

다음과 같이 최소 로그 레벨, 출력 형식, 싱크 등을 설정할 수 있습니다.

### 1. 기본적인 설정과 콘솔 로깅:

```
$ dotnet add package Serilog.Sinks.Console
```

콘솔에 `Debug` 수준의 로그를 출력하기 위에서 위 Sink를 설치합니다.

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("Some log message");

Log.CloseAndFlush();
```

Logger는 애플리케이션 초기화 시 한 번만 생성하며,

`CloseAndFlush()`는 애플리케이션 종료 시 호출합니다.

#### Sink 별 로그 레벨 설정

특정 Sink에 대해서만 더 높은 수준의 로그만 출력하도록 제한하고 싶은 경우,

`restrictedToMinimumLevel` 파라미터를 사용하여 개별 Sink마다 최소 로그 레벨을 별도로 설정할 수 있습니다.

예를 들어 아래와 같이 구성할 수 있습니다:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File("log.txt")
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();
```

이 구성에서는:

- `Debug`, `Information`, `Warning` 등 모든 로그가 `log.txt` 파일에 기록됩니다.
- `Information` 이상(`Warning`, `Error`, `Fatal`)만 콘솔에 출력됩니다.

> 💡 **Logger vs. Sink 레벨의 차이점**  
> 전체 Logger의 `MinimumLevel`은 로그 이벤트가 생성될지 여부를 결정하며,
> Sink의 `restrictedToMinimumLevel`은 이미 생성된 이벤트 중 어떤 것을 해당 Sink에 출력할지를 결정합니다.  
> 따라서 Logger 수준보다 낮은 레벨을 Sink에 지정해도 출력되지 않습니다.

#### 로거 등록하기

```
$ dotnet add package Serilog.AspNetCore
```

.NET 6+부터는 다음과 같이 `Serilog.AspNetCore`패키지 또는 `Serilog.Extensions.Hosting`패키지를 활용하여

Program.cs에서 Serilog를 직접 Host에 연결해 사용하는 방식이 일반적입니다.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
{
	loggerConfiguration
		.ReadFrom.Configuration(hostingContext.Configuration)
		.Enrich.FromLogContext()
		.WriteTo.Console();
});
```

(Serilog를 애플리케이션 전체의 기본 로깅 시스템으로 완전히 대체합니다)

또는

```csharp
builder.Logging.AddSerilog(
	new LoggerConfiguration()
		.ReadFrom.Configuration(configuration)
		.CreateLogger()
	);
```

(Serilog를 로그 제공자로 등록합니다.)

### 2. `appsettings.json` 파일 기반 구성

```
$ dotnet add package Serilog.Settings.Configuration
```

위는 `appsettings.json` 파일 설정을 지원하는 패키지 입니다.

구성을 코드에 하드코딩하지 않고 `JSON` 설정으로 관리할 수 있습니다.

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(_config)
    .CreateLogger();
```

.NET DI 기반의 애플리케이션에 가장 적합한 형태로,

다양한 환경에 따라 다른 로깅 설정을 구별 할 수 있는 장점이 있습니다.

**appsettings.json 구성 예**:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"],
    "Filter": [
      {
        "Name": "ByExcluding",
        "Args": {
          "expression": "RequestPath like '/ping'"
        }
      }
    ]
  }
}
```

#### 구성 항목

| Configuration Key               | 설명                                                                                                            |
| :------------------------------ | :-------------------------------------------------------------------------------------------------------------- |
| [MinimumLevel](#minimumlevel)   | 전체 로깅 수준과 네임스페이스별 최소 로그 레벨을 설정합니다. `Default`와 `Override`를 지원합니다.               |
| [WriteTo](#writeto)             | 로그를 기록할 하나 이상의 싱크(sink)를 정의합니다. `Name`과 `Args`를 통해 파라미터 설정이 가능합니다.           |
| [Enrich](#enrich)               | 로그 이벤트에 추가적인 정보를 붙이기 위한 enricher를 지정합니다. 예: `FromLogContext`, `WithMachineName` 등     |
| [Destructure](#destructure)     | 복잡한 객체를 로그로 출력하기 위해 사용자 정의 구조 해석 규칙(destructuring policy)을 지정합니다.               |
| [Filter](#filter)               | 특정 조건의 로그를 포함하거나 제외하는 필터를 지정합니다. `ByIncludingOnly`, `ByExcluding` 등을 사용합니다.     |
| Using                           | 설정에서 사용되는 sink, enricher, 기타 구성 요소가 포함된 어셈블리를 지정합니다.                                |
| [AuditTo](#auditto)             | 중요한 감사(audit) 로그를 기록할 sink를 정의합니다. `WriteTo`와 유사하나 로그 손실이 없어야 할 경우 사용합니다. |
| [Properties](#properties)       | 모든 로그 이벤트에 자동으로 포함될 글로벌 속성(key-value 쌍)을 설정합니다.                                      |
| [LevelSwitches](#levelswitches) | 런타임에 동적으로 제어 가능한 로그 수준을 선언합니다. 다른 설정에서 참조할 수 있습니다.                         |
| [Theme](#theme)                 | 콘솔 출력 시 사용할 테마를 지정합니다 (예: `"Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code"`).      |
| [Extensions](#extensions)       | 사용자 정의 확장 기능을 구성할 수 있는 키입니다 (드물게 사용됨).                                                |

##### :MinimumLevel

제공되는 로그 레벨

| Log Level     | 설명                                                                                                                    |
| :------------ | :---------------------------------------------------------------------------------------------------------------------- |
| `Verbose`     | 가장 많은 정보를 담는 수준으로, 운영 환경에서는 거의 (또는 전혀) 활성화되지 않습니다.                                   |
| `Debug`       | 외부에서는 반드시 관찰되지 않지만, 어떤 일이 발생했는지 파악하는 데 유용한 내부 시스템 이벤트에 사용됩니다.             |
| `Information` | 시스템의 책임과 기능에 해당하는 동작을 설명하는 이벤트입니다. 일반적으로 시스템이 수행할 수 있는 관찰 가능한 동작입니다 |
| `Warning`     | 서비스가 저하되었거나 예상된 결과를 벗어난 경우에 사용됩니다.                                                           |
| `Error`       | 기능을 사용할 수 없거나 기대한 동작이 깨진 경우에 사용됩니다.                                                           |
| `Fatal`       | 시스템 전체에 영향을 줄 수 있는 치명적인 오류를 나타냅니다. 즉각적인 대응이 요구됩니다.                                 |

##### :WriteTo

`WriteTo` 키에는 로그를 출력할 Sink들을 배열로 정의합니다.

```json
"Serilog": {
  "MinimumLevel": "Information",
  "WriteTo": [
    { "Name": "Console" },
    {
      "Name": "File",
      "Args": {
        "path": "Logs/log-.txt",
        "rollingInterval": "Day"
      }
    }
  ]
}
```

각 Sink의 세부 설정은 [Sink 구성](#sink-구성)에서 확인할 수 있습니다.

##### :Enrich

`Enrich`는 로그 이벤트에 머신 이름, 스레드 ID 등 추가 정보를 포함시키기 위한 설정입니다.

```json
"Serilog": {
  "Enrich": [
    "FromLogContext",
    "WithMachineName",
    "WithThreadId",
    {
      "Name": "WithProperty",
      "Args": {
        "name": "Application",
        "value": "MyApp"
      }
    }
  ]
}
```

지원하는 enricher 목록은 [추가 정보 설정](#2-enrich를-활용한-추가-정보-설정)에서 확인할 수 있습니다.

##### :Destructure

`Destructure`는 복잡한 객체를 로깅할 때 커스터마이징할 수 있는 정책을 설정합니다.

사용자 정의 구조 해석(destructuring) 정책을 통해 로그 표현을 제어합니다.

```json
"Serilog": {
  "Destructure": [
    {
      "Name": "With",
      "Args": {
        "policy": "MyNamespace.CustomPolicy, MyAssembly"
      }
    }
  ]
}
```

##### :Filter

`Filter`는 로그를 조건에 따라 포함 또는 제외할 수 있게 합니다.

```json
"Serilog": {
  "Filter": [
    {
      "Name": "ByExcluding",
      "Args": {
        "expression": "RequestPath like '/health%'"
      }
    }
  ]
}
```

위 설정은 `/health` 경로와 일치하는 로그를 필터링합니다.

Filter 의 추가적인 구성은 [조건부 로깅 설정](#3-filter를-이용한-조건부-로깅),

Expression 문법에 대한 정보는 [공식 저장소](https://github.com/serilog/serilog-expressions)에서 확인할 수 있습니다.

##### :AuditTo

`AuditTo`는 일반 로그(`WriteTo`)와는 별도로 항상 기록되어야 할 이벤트에 사용됩니다.

```json
"Serilog": {
  "AuditTo": [
    {
      "Name": "File",
      "Args": {
        "path": "Logs/audit-.txt",
        "rollingInterval": "Day"
      }
    }
  ]
}
```

##### :Properties

전역 속성을 설정하여 모든 로그 이벤트에 자동으로 포함시킬 수 있습니다.

```json
"Serilog": {
  "Properties": {
    "Application": "MyApp",
    "Environment": "Production"
  }
}
```

##### :LevelSwitches

`LevelSwitches`는 런타임에 로그 수준을 동적으로 조절할 수 있게 해주는 스위치를 선언합니다.

다른 항목에서 참조(`$switch`)할 수 있습니다.

```json
"Serilog": {
  "LevelSwitches": {
    "$controlSwitch": "Information"
  },
  "MinimumLevel": {
    "ControlledBy": "$controlSwitch"
  }
}
```

##### :Theme

콘솔 출력에 사용할 테마를 지정합니다. 정적 속성 형식으로 입력되어야 하며, `Serilog.Sinks.Console`에서 제공됩니다.

```json
"Serilog": {
  "WriteTo": [
    {
      "Name": "Console",
      "Args": {
        "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code"
      }
    }
  ]
}
```

##### :Extensions

사용자 정의 확장을 구성할 수 있습니다.

```json
"Serilog": {
  "Extensions": [
    {
      "Name": "UseMyCustomLogger",
      "Args": {
        "setting": "value"
      }
    }
  ]
}
```

### 3. 로그 동시 출력 및 Sink 추가

```
$ dotnet add package Serilog.Sinks.File
```

출력된 로그를 파일 형태로 저장하기 위해서 `Serilog.Sinks.File` 패키지를 설치합니다.

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

설치 후, `WriteTo` 항목을 반복하여 여러 Sink를 자유롭게 추가할 수 있습니다.

각 Sink에 대한 자세한 설정 방법은 [Sink 구성](#sink-구성) 문서를 참고하세요.

## 출력 구성

### 1. `Output Template`을 통한 포맷팅 설정

텍스트 기반 sink (콘솔, 파일 등)는 `outputTemplate` 파라미터로 로그 포맷을 제어할 수 있습니다.

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("log.txt",
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();
```

| 템플릿 코드      | 설명                                            |
| :--------------- | :---------------------------------------------- |
| `{Timestamp}`    | 로그 시간                                       |
| `{Level:u3}`     | 로그 레벨 (세 글자 대문자: INF, ERR 등)         |
| `{Message:lj}`   | 메시지 (내장 객체는 JSON, 문자열은 그대로 출력) |
| `{Properties:j}` | 컨텍스트 정보 (Enricher로 추가된 값들 포함)     |
| `{Exception}`    | 예외 스택 출력 (있는 경우)                      |

### 2. `JSON`형식으로 출력하기

텍스트 기반 sink 는 기본적으로 고정된 텍스트 형식으로 로그를 기록합니다.

로그를 JSON 형식으로 기록하려면 `outputTemplate` 대신 ITextFormatter를 첫 번째 인자로 전달해야 합니다.

```csharp
// Serilog.Formatting.Compact 설치 필요
.WriteTo.File(new CompactJsonFormatter(), "log.txt")
```

#### Serilog.Formatting.Json.JsonFormatter

Serilog 기본 패키지에서 제공하는 기본 JSON 포매터입니다.

**appsettings.json 설정 예시:**

```json
{
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.json",
          "formatter": "Serilog.Formatting.Json.JsonFormatter, Serilog"
        }
      }
    ]
  }
}
```

로그 이벤트에 Timestamp, Level, MessageTemplate, Properties, Exception 등의 전체 메타데이터가 포함됩니다.

**출력 예시:**

```json
{
  "Timestamp": "2025-04-02T12:34:56.789Z",
  "Level": "Information",
  "MessageTemplate": "Hello {Name}",
  "RenderedMessage": "Hello Alice",
  "Properties": {
    "Name": "Alice"
  }
}
```

#### Serilog.Formatting.Compact.CompactJsonFormatter

`Serilog.Formatting.Compact` 패키지에서 제공하는 포매터로,

로그 파일 크기를 줄이고 로그 수집 도구와의 연동을 최적화하기 위해 설계되었습니다.

**appsettings.json 설정 예시:**

```json
{
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.json",
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ]
  }
}
```

줄 바꿈으로 구분된 JSON (`NDJSON`) 형식이며 매우 압축되어 있습니다.

Seq, Elasticsearch, Datadog 같은 로그 분석 도구와의 연동에 적합합니다.

짧은 속성명을 사용하고 불필요한 필드는 생략합니다.

**출력 예시:**

```json
{
  "@t": "2025-04-02T12:34:56.789Z",
  "@mt": "Hello {Name}",
  "Name": "Alice",
  "@l": "Information"
}
```

### 3. `Enrich`를 활용한 추가 정보 설정

`Enrich` 기능은 로그 메시지에 추가적인 컨텍스트 정보(예: 머신 이름, 스레드 ID, 사용자 정보 등)를 자동으로 포함시켜,

데이터 분석에 적합한 내용으로 가공할 수 있도록 도와줍니다.

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("AppName", "SampleLoggerApp")
    .WriteTo.Console()
    .CreateLogger();
```

위 예제에서는 로그에 다음과 같은 정보가 자동으로 추가됩니다:

- MachineName: 로그가 생성된 머신의 이름
- ThreadId: 로그를 생성한 스레드 ID
- AppName: 사용자 정의 속성

아래는 자주 사용하는 Enricher 목록입니다.

| Enricher 이름                | 설명                                                        |
| :--------------------------- | :---------------------------------------------------------- |
| `.WithMachineName()`         | 현재 머신의 이름을 포함                                     |
| `.WithThreadId()`            | 현재 스레드 ID 포함                                         |
| `.WithProcessId()`           | 프로세스 ID 포함                                            |
| `.WithEnvironmentUserName()` | 실행 중인 OS 계정명 포함                                    |
| `.WithProperty(key, value)`  | 임의의 커스텀 속성 추가                                     |
| `.WithCorrelationId()`       | 분산 트레이싱을 위한 Correlation ID 포함 (추가 패키지 필요) |
| `.FromLogContext()`          | LogContext.PushProperty()에서 설정된 정보 포함              |

> 일부 Enricher는 별도의 NuGet 패키지를 통해 제공됩니다:
>
> - Serilog.Enrichers.Thread
> - Serilog.Enrichers.Process
> - Serilog.Enrichers.Environment

### 4. `Filter`를 이용한 조건부 로깅

`Filter` 기능은 특정 조건에 따라 로그 메시지를 필터링하는 역할을 합니다.

```csharp
Log.Logger = new LoggerConfiguration()
    .Filter.ByExcluding(logEvent =>
        logEvent.Level == LogEventLevel.Debug)
    .WriteTo.Console()
    .CreateLogger();

Log.Debug("이 메시지는 필터에 의해 기록되지 않습니다.");
Log.Information("이 메시지는 출력됩니다.");
```

| Filter 이름                 | 설명                                |
| :-------------------------- | :---------------------------------- |
| `.Filter.ByIncludingOnly()` | 조건을 만족하는 로그만 포함         |
| `.Filter.ByExcluding()`     | 조건을 만족하는 로그는 제외         |
| `.Filter.With()`            | 커스텀 필터 구현체를 사용할 수 있음 |

## Sink 구성

Serilog는 다양한 `Sink`를 통해 로그를 여러 출력 대상으로 전송할 수 있습니다.

### Serilog.Sinks.File

```
$ dotnet add package Serilog.Sinks.File
```

로그 이벤트를 로컬 파일에 `JSON` 또는 `TEXT`형식으로 기록합니다.

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

#### 날짜/크기 기준 롤링

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
        path: "Logs/log-.txt",                // 파일 이름에 날짜 형식 포함
        rollingInterval: RollingInterval.Day, // 일 단위로 로그 분리
        retainedFileCountLimit: 7,            // 최근 7일치만 보관
        rollOnFileSizeLimit: true,           // 크기로 분할 활성화
        fileSizeLimitBytes: 10_000_000,      // 10MB
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();
```

**구성 요소 설명:**
| 옵션| 설명 |
| :-------------------------- | :---------------------------------- |
| `path` | 로그 파일 경로이며 - 기호 뒤에 날짜 포맷이 붙음 (log-20250401.txt) |
| `rollingInterval` | `Day`, `Hour`, `Minute`, `Month`, `Infinite` 중 하나로 날짜 단위 분할 |
| `retainedFileCountLimit` | 보관할 최대 파일 수. 초과 시 오래된 파일 자동 삭제 (null은 무제한) |
| `fileSizeLimitBytes` |파일 크기 기준 분할 (바이트 단위). 기본값: 1GB |
| `rollOnFileSizeLimit` | true일 경우 `fileSizeLimitBytes` 초과 시 새 파일 생성 |
| `outputTemplate` | 로그 출력 형식. 로깅 포맷 일관성 유지 가능|

**appsettings.json 구성 예제:**

```json
"Serilog": {
  "MinimumLevel": "Information",
  "WriteTo": [
    {
        "Name": "File",
        "Args": {
            "path": "Logs/log-.json",
            "restrictedToMinimumLevel": "Information",
            "rollingInterval": "Day",
            "retainedFileCountLimit": 7,
            "rollOnFileSizeLimit": true,
            "formatter": "Serilog.Formatting.Json.JsonFormatter, Serilog",
            "fileSizeLimitBytes": 10000000
        }
    }
  ]
}
```

#### 공유 로그 파일 설정

여러 프로세스에서 동일한 로그 파일에 접근하도록 허용하려면 `shared` 옵션을 `true`로 설정합니다:

**Program.cs 설정 예시:**

```csharp
var logger = new LoggerConfiguration()
	.WriteTo.File("Logs/log-.txt", shared: true)
	.CreateLogger();
```

**appsettings.json 설정 예시:**

```json
"Serilog": {
  "MinimumLevel": "Information",
  "WriteTo": [
    {
      "Name": "File",
      "Args": {
        "path": "Logs/log-.txt",
        "shared": true
      }
    }
  ]
}
```

#### FileLifecycleHooks

Serilog.Sinks.File은 `FileLifecycleHooks` 클래스를 통해 로그 파일의 생명주기 이벤트에 대한 훅을 제공합니다.

이를 통해 로그 파일이 열리거나 삭제되기 전에 사용자 정의 로직을 삽입할 수 있습니다.

- `OnFileOpened`: 로그 파일이 열릴 때 호출되며, 스트림에 헤더를 추가하거나 스트림을 래핑하여 버퍼링, 압축, 암호화 등을 적용할 수 있습니다.​
- `OnFileDeleting`: 오래된 롤링 로그 파일이 삭제되기 전에 호출되며, 해당 파일을 다른 위치에 아카이브하는 등의 작업을 수행할 수 있습니다.

**사용 예시:**

> 로그 파일의 시작 부분에 헤더를 추가하는 커스텀 훅 구현

```csharp
public class CustomFileLifecycleHooks : FileLifecycleHooks
{
	public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
	{
		// 스트림에 헤더를 작성
		var writer = new StreamWriter(underlyingStream, encoding);
		writer.WriteLine("Hello This is Custom File Message!");
		writer.Flush();

		// 원본 스트림 반환
		return underlyingStream;
	}
}
```

> 설정 적용, Program.cs:

```csharp
var logger = new LoggerConfiguration()
    .WriteTo.File(
        path: "Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        hooks: new HeaderWriterHooks() // 커스텀 훅 적용
    )
    .CreateLogger();
```

**출력된 로그 파일:**

```
Hello This is Custom File Message!
2025-04-02 15:54:20.272 +09:00 [INF] Now listening on: http://[::]:8000
2025-04-02 15:54:20.274 +09:00 [DBG] Loaded hosting startup assembly APIServer
```

### Serilog.Sinks.Console

```
$ dotnet add package Serilog.Sinks.Console
```

로그 메시지를 콘솔에 출력합니다.

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
```

> 참고: 콘솔 싱크는 개발 환경에서 주로 사용되며, 프로덕션 환경에서는 성능 이슈로 인해 다른 싱크를 사용하는 것이 권장됩니다.

### Serilog.Sinks.Debug

```
$ dotnet add package Serilog.Sinks.Debug
```

로그 이벤트를 디버그 출력 창(예: Visual Studio의 출력 창)에 전송합니다. 디버깅 시 유용합니다

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Debug()
    .CreateLogger();
```

### Serilog.Sinks.Async

비동기용 래퍼(Wrapper)로, 다른 Serilog 싱크(Sink)를 감쌉니다.

이 싱크를 사용하면 로깅 호출의 오버헤드를 줄이고, 작업을 백그라운드 스레드에 위임함으로써 성능을 향상시킬 수 있습니다.

특히 I/O 병목 현상의 영향을 받을 수 있는 File 및 RollingFile과 같은 비배치(Non-batching) 싱크에 적합합니다.

> 참고: CouchDB, Elasticsearch, MongoDB, Seq, Splunk 등의 네트워크 기반 싱크들은 이미 자체적으로 비동기 배치 처리를 지원하므로, 이 Sink를 사용해도 추가적인 이점이 없습니다.

### Serilog.Sinks.Http

```
$ dotnet add package Serilog.Sinks.Http
```

로그 이벤트를 HTTP 프로토콜을 통해 원격 서버로 전송할 수 있도록 하는 싱크입니다.

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Http("http://your-log-server.com")
    .CreateLogger();
```

### Serilog.Sinks.Elasticsearch

```
$ dotnet add package Serilog.Sinks.Elasticsearch
```

로그 이벤트를 Elasticsearch 클러스터에 전송합니다.

```csharp
Log.Logger =  new LoggerConfiguration()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
    {
        AutoRegisterTemplate = true,
    })
    .CreateLogger();
```

### Serilog.Sinks.MSSqlServer

```
$ dotnet add package Serilog.Sinks.MSSqlServer
```

로그 이벤트를 SQL Server 데이터베이스에 저장합니다.

### Serilog.Sinks.SQLite

```
$ dotnet add package Serilog.Sinks.SQLite
```

이 싱크는 내부적으로 로그를 버퍼링한 후, 전용 스레드를 통해 배치로 SQLite 데이터베이스에 플러시합니다.
이를 통해 성능을 향상시키고 I/O 병목 현상을 최소화합니다.

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.SQLite(@"Logs\log.db")
    .CreateLogger();
```

**appsettings.json 설정 예시:**

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "SQLite",
        "Args": {
          "sqliteDbPath": "Logs/logs.db",
          "tableName": "Logs"
        }
      }
    ]
  }
}
```

위 설정으로 로그 출력시 `Logs/` 경로에 다음과 같이 저장됩니다

**저장된 .db 파일:**

![](Images/serilog-sqlite.png)

### Serilog.Sinks.MongoDB

```
$ dotnet add package Serilog.Sinks.MongoDB
```

로그 이벤트를 MongoDB에 문서 형태로 저장하는 싱크입니다.

MongoDB의 컬렉션에 개별 문서로 삽입됩니다.

```csharp
var logger = new LoggerConfiguration()
    .WriteTo.MongoDB("mongodb://localhost/logs")
    .CreateLogger();
```

TLS 및 인증등의 고급 설정은 다음과 같이 가능합니다:

```csharp
var log = new LoggerConfiguration()
    .WriteTo.MongoDBBson(cfg =>
    {
        var mongoDbSettings = new MongoClientSettings
        {
            UseTls = true,
            AllowInsecureTls = true,
            Credential = MongoCredential.CreateCredential("databaseName", "username", "password"),
            Server = new MongoServerAddress("127.0.0.1")
        };

        var mongoDbInstance = new MongoClient(mongoDbSettings).GetDatabase("serilog");

        cfg.SetMongoDatabase(mongoDbInstance);
        cfg.SetRollingInternal(RollingInterval.Month);
    })
    .CreateLogger();
```

**appsettings.json 설정 예시:**

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Error",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "MongoDBBson",
        "Args": {
          "databaseUrl": "mongodb://username:password@ip:port/dbName?authSource=admin",
          "collectionName": "logs",
          "cappedMaxSizeMb": "1024",
          "cappedMaxDocuments": "50000",
          "rollingInterval": "Month"
        }
      }
    ]
  }
}
```

- `databaseUrl`: MongoDB 접속 URL

- `collectionName`: 로그를 저장할 컬렉션 이름

- `cappedMaxSizeMb`: 컬렉션의 최대 크기 (MB) 설정

- `cappedMaxDocuments`: 컬렉션 내 최대 문서 수

(JSON 설정의 키는 대소문자를 구분하지 않습니다.)

**저장된 로그 예시:**

```mongodb
{
  _id: ObjectId('67eba632615965a169662f6d'),
  Level: 'Information',
  UtcTimeStamp: ISODate('2025-04-01T08:39:14.009Z'),
  MessageTemplate: {
    Text: 'chat completion succeeded',
    Tokens: [ { _t: 'TextToken', Text: 'chat completion succeeded' } ]
  },
  RenderedMessage: 'chat completion succeeded',
  Properties: {},
  Exception: { _csharpnull: true },
  TraceId: 'ea82f36d6af746a03bbe67d8386c5a82',
  SpanId: 'b8cee52faa67327c'
}
```

# 로그 구조화

### 기본 동작

Serilog는 로그에 속성이 전달되면 적절한 표현 방식(문자열, 구조화 등)을 자동으로 선택하려고 시도합니다.

```csharp
var count = 456;
Log.Information("Retrieved {Count} records", count);
```

위의 로그는 JSON 형태로 출력시 다음과 같습니다.

```json
{ "Count": 456 }
```

| 기본 스칼라 | 인식되는 데이터 목록                                                |
| :---------- | :------------------------------------------------------------------ |
| `Boolean`   | bool                                                                |
| `Numerics`  | byte, short, ushort, int, uint, long, ulong, float, double, decimal |
| `Strings`   | string, byte[]                                                      |
| `Temporals` | DateTime, DateTimeOffset, TimeSpan                                  |
| `Others`    | Guid, Uri                                                           |
| `Nullables` | 위 데이터 타입 모두                                                 |

### 콜렉션 처리

객체가 `IEnumerable` 형태로 전달될 경우 콜렉션으로 간주합니다.

```csharp
var fruit = new[] { "Apple", "Pear", "Orange" };
Log.Information("In my bowl I have {Fruit}", fruit);
```

이경우 출력되는 JSON 형태는 다음과 같습니다

```json
{ "Fruit": ["Apple", "Pear", "Orange"] }
```

또한, `Dictionary<TKey,TValue>`의 형태 에서 Key의 데이터 타입이 앞서 언급된 데이터 목록 중 하나 일경우,

자동으로 직렬화가 가능합니다.

(단, `IDictionary<TKey,TValue>`등의 인터페이스를 구현한 객체의 경우 모호성 문제때문에 직렬화 되지 않습니다.)

### 객체 처리

#### 복잡한 객체

이외 Serilog가 인식하지 못하는 사용자 정의 타입을 전달하면 기본적으로 ToString()이 호출됩니다.

```csharp
SqlConnection conn = ...;
Log.Information("Connected to {Connection}", conn);
```

위 와같은 경우, 아래와 같이 문자열로 출력됩니다.

```
"System.Data.SqlClient.SqlConnection"
```

#### 객체 구조 보존

객체 내부 속성을 구조화된 형태로 기록하고 싶을 경우, `@` 연산자를 사용합니다:

```csharp
var sensorInput = new { Latitude = 25, Longitude = 134 };
Log.Information("Processing {@SensorInput}", sensorInput);
```

**JSON:**

```json
{ "SensorInput": { "Latitude": 25, "Longitude": 134 } }
```

#### 구조 분해 사용자 지정

특정 속성만 로깅하고 싶은 경우 `Destructure.ByTransforming<T>()`을 사용합니다.

```csharp
Log.Logger = new LoggerConfiguration()
    .Destructure.ByTransforming<HttpRequest>(r => new {
        RawUrl = r.RawUrl,
        Method = r.Method
    })
    .WriteTo...
```

\*변환 함수는 반드시 다른 타입을 반환해야 합니다. 그렇지 않으면 재귀 호출되어 예외가 발생할 수 있습니다.

**Destructure 관련 확장 기능들:**

| 설정정                             | 설명                                                       |
| :--------------------------------- | :--------------------------------------------------------- |
| `.Destructure.ByTransforming<T>()` | 특정 타입의 객체를 변형하여 구조화                         |
| `.Destructure.With<Policy>()`      | 커스텀 구조화 정책 적용                                    |
| `.Destructure.JsonNetTypes()`      | Newtonsoft.Json 특성에 따른 구조화 지원 (별도 패키지 필요) |
| `.Destructure.ToMaximumDepth()`    | 깊은 중첩 객체 구조화 시 최대 깊이 제한                    |
| `.Destructure.AsScalar<T>()`       | 특정 타입을 단일 값으로 처리하도록 지정                    |

#### JSON.NET 연동

복잡한 JSON 직렬화 로직이 필요한 경우, Serilog는 JSON.NET과의 연동도 지원합니다.

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithExceptionDetails()
    .Destructure.JsonNetTypes()
    .WriteTo.Console()
    .CreateLogger();
```

이 설정을 통해 [JsonIgnore], [JsonProperty] 등의 속성을 활용한 구조화 로깅이 가능해집니다.

#### 문자열화

객체 타입이 불확실하거나 ToString 결과만 기록하고 싶을 경우 $ 연산자를 사용합니다:

```csharp
var unknown = new[] { 1, 2, 3 };
Log.Information("Received {$Data}", unknown);
```

**출력 결과:**

```
"System.Int32[]"
```

### Message Template

메시지 템플릿(`Message Template`)은 .NET의 string.Format()에서 사용하는 형식 문자열을 포함하는 상위 개념으로,

`string.Format()`에서 유효한 모든 포맷 문자열은 Serilog에서도 정상적으로 처리됩니다.

메시지 템플릿을 사용하여 다음과 같이 로그 메시지에 변수를 포함할 수 있습니다.

```csharp
var userName = "shana";
var items = 3;
var totalPrice = 99.99;

Log.Information("{UserName}님이 {Items}개의 아이템을 총 {TotalPrice}원에 구매했습니다.", userName, items, totalPrice);
```

위 로그는 아래와 같이 출력됩니다

**Result:**

```
"shana"님이 3개의 아이템을 총 99.99원에 구매했습니다.
```

Serilog는 데이터 타입을 명확하게 구분하기 위해 로그 메시지에서 문자열(`string`) 값을 큰따옴표(`""`)로 감싸서 출력합니다.

#### 속성 구조화 활용

위처럼 메시지 템플릿에 포함된 각 속성은 별도의 필드로 분리됩니다.

**사용 예시:**

```csharp
public static void Log(string message, [CallerMemberName] string? caller = null)
{
	Serilog.Log.Information("[{Caller}]: {Message}", caller, message);
}
```

이 방식은 caller와 message를 Serilog의 메시지 템플릿 안에서 `{속성명}`으로 명시적으로 지정하여,

`JSON` 형태로 출력 시 로그 내부의 `Properties` 섹션에 다음과 같이 출력합니다.

**출력 결과:**

```json
{
  "Timestamp": "2025-04-02T16:22:21.8118034+09:00",
  "Level": "Information",
  "MessageTemplate": "[{caller}] {message}",
  "TraceId": "02e30ce35de9c3bc6b9daca7160325f8",
  "SpanId": "286b6a8552d56885",
  "Properties": {
    "Caller": "Chat",
    "Message": "chat completion succeeded"
  }
}
```

> ❌ 참고: 문자열 보간(string interpolation)을 사용할 경우 구조화된 속성으로 인식되지 않습니다

```csharp
public static void Log(string message, [CallerMemberName] string caller = "")
{
	Serilog.Log.Information($"[{caller}] {message}");
}
```

**출력 결과:**

```json
{
  "Timestamp": "2025-04-02T16:19:35.9413610+09:00",
  "Level": "Information",
  "MessageTemplate": "[Chat] chat completion succeeded",
  "TraceId": "a69199d37a6f811eec4c265ca79bd173",
  "SpanId": "be51b4ebb5c23cf8"
}
```

#### 문법 규칙

- 속성 이름은 중괄호(`{}`) 안에 작성합니다

```csharp
Log.Information("User {UserId} logged in", userId);
```

- 속성 이름은 유효한 C# 식별자여야 합니다.

```
  - (`O`) FooBar는 유효
  - (`X`) Foo.Bar 또는 Foo-Bar는 유효하지 않음
```

- 중괄호를 이스케이프(`escape`)하려면 두 번 중복해서 작성합니다. (`{{`는 `{`로 렌더링됩니다.)
- 숫자 인덱스를 사용하는 포맷 (`{0}`, `{1}` 등)은 string.Format()과 동일하게 파라미터 순서에 따라 바인딩됩니다.

```csharp
Log.Information("Item {0} at index {1}", item, index); // {0}, {1} → item, index에 대응
```

- 속성 이름 중 하나라도 숫자가 아닌 이름이라면, 모든 속성 이름은 왼쪽에서 오른쪽 순서대로 파라미터에 매칭됩니다.

```csharp
Log.Information("User {Name} (ID: {Id})", name, id); // 이름 기준으로 순서 매칭
```

- 속성 이름 앞에 @ 또는 $를 붙이면 직렬화 방식을 제어할 수 있습니다.

  - **@Property**: 객체 전체를 구조화된 형태로 로깅
  - **$Property**: 객체의 ToString() 값을 사용하여 문자열로 로깅

- 속성 이름 뒤에 :000 등 포맷 문자열을 붙이면 렌더링 형식을 제어할 수 있습니다.

  - 이는 `string.Format()`에서 사용하는 포맷 문자열과 동일하게 동작합니다.

  ```csharp
  Log.Information("Order total: {Total:0.00}", total); // 소수점 두 자리로 출력
  ```

# 활용 예제

## ASP .NET Core 9 Web API Server

웹 API 서버에서 Serilog를 활용한 예시 입니다.

[📁 프로젝트 바로가기](APIServer/)

#### 목차

- [Serilog 설정 및 구성](#로그-시스템을-serilog로-설정하기)
- [Logger 래퍼 클래스 구현](#supersocket에-serilog-구성하기)
- [OpenTelemetry Sink 추가](#opentelemetry-sink-추가)

### Serilog 설정 및 구성

아래는 appsettings.json에서 Serilog 다중 출력 구성 예시입니다.

아래 예제에서는 콘솔, JSON 파일, SQLite, MongoDB에 로그를 동시에 기록합니다.

```json
"Serilog": {
  "MinimumLevel": "Debug",
  "WriteTo": [
    { "Name": "Console" },
    {
      "Name": "File",
      "Args": {
        "path": "Logs/log-.json",
        "restrictedToMinimumLevel": "Information",
        "rollingInterval": "Day",
        "retainedFileCountLimit": 7,
        "rollOnFileSizeLimit": true,
        "formatter": "Serilog.Formatting.Json.JsonFormatter, Serilog",
        "fileSizeLimitBytes": 10000000
      }
    },
    {
      "Name": "SQLite",
      "Args": {
        "restrictedToMinimumLevel": "Information",
        "sqliteDbPath": "Logs/logs.db",
        "tableName": "Logs"
      }
    },
    {
      "Name": "MongoDBBson",
      "Args": {
        "restrictedToMinimumLevel": "Error",
        "databaseUrl": "mongodb://shanabunny:comsooyoung!1@localhost:27017/serilog?authSource=admin",
        "collectionName": "logs",
        "cappedMaxSizeMb": "100"
      }
    }
  ]
}
```

### Logger 래퍼 클래스 구현

메서드 이름 자동 추적 기능을 포함하는 유틸리티 클래스를 작성합니다.

> [Logger.cs](APIServer/Logger.cs)

```csharp
public static class Logger
{
	public static void Log(string message, [CallerMemberName] string? caller = null)
	{
		Serilog.Log.Information("{Caller} {Message}", caller, message);
	}

	public static void LogError(string message)
	{
		Serilog.Log.Error(message);
	}

	public static void LogError(ResultCode resultCode, string message, [CallerMemberName] string? caller = null)
	{
		Serilog.Log.Error("{Caller} {ResultCode} {Message}", caller, resultCode, message);
	}

	public static void LogError(Exception e, string message)
	{
		Serilog.Log.Error(e, message);
	}
}
```

### Controller에서 Serilog 활용

클라이언트 요청 처리 흐름에서 다음과 같이 로그를 남깁니다.

> [AIController.cs](APIServer/Controllers/AIController.cs)

```csharp
[HttpPost("chat")]
public async Task<ChatResponse> Chat([FromBody] ChatRequest request)
{
	var response = new ChatResponse();
	(response.Result, response.Completion) = await _aiService.CompleteChatAsync(request);
	if (response.Result != ResultCode.Success)
	{
		Logger.LogError(response.Result, "chat completion failed");
	}
	else
	{
		Logger.Log("chat completion succeeded");
	}
	return response;
}
```

기능 단위로 성공/실패 여부를 명확히 구분하여 로그 출력합니다.

요청에 성공할 경우(`Logger.Log()`실행) 저장된 Serilog 로그는 다음과 같습니다:

```json
{
  "Timestamp": "2025-04-02T17:07:38.1636638+09:00",
  "Level": "Information",
  "MessageTemplate": "{Caller} {Message}",
  "TraceId": "1edad387457dd601435b2dc323c353e4",
  "SpanId": "f8aad533cc823c42",
  "Properties": { "Caller": "Chat", "Message": "chat completion succeeded" }
}
```

## .NET 9 Socket Server using SuperSocket

소켓서버에서 Serilog를 활용한 예시 입니다.

[📁 프로젝트 바로가기](SocketServer/)

#### 목차

- [앱 로그 시스템을 Serilog로 설정하기](#로그-시스템을-serilog로-설정하기)
- [SuperSocket에 Serilog 구성하기](#supersocket에-serilog-구성하기)+
- [SuperSocket에 Serilog 구성하기](#supersocket에-serilog-구성하기)

### 로그 시스템을 Serilog로 설정하기

```
$ dotnet add package Serilog.Extensions.Hosting
```

위 패키지를 사용하여 .NET Host 환경에서 Serilog를 다음과 같이 메인 애플리케이션 로거로 설정합니다.

```csharp
var host = new HostBuilder()
	.ConfigureAppConfiguration((context, config) =>
	{
		var env = context.HostingEnvironment;
		config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
	})
	.UseSerilog((hostingContext, loggerConfiguration) =>
	{
		loggerConfiguration
			.ReadFrom.Configuration(hostingContext.Configuration);
	})
	// ...
	.Build();
```

이 설정만으로는 SuperSocket 내부의 로그 (base.Logger, ILog)에는 영향을 주지 않으며,

따로 Serilog를 SuperSocket에 연동해야 합니다.

### SuperSocket에 Serilog 구성하기

SuperSocket은 자체 로깅 인터페이스인 ILog와 ILogFactory를 사용하므로,

Serilog를 연결하기 위해서는 커스텀 어댑터 및 팩토리 클래스를 구현해야 합니다.

#### 1. SerilogAdaptor 클래스 생성

> [SerilogAdaptor](SocketServer/SerilogAdaptor.cs): SuperSocket의 ILog를 Serilog에 연결하는 어댑터

```csharp
public class SerilogAdaptor : ILog
{
	private readonly ILogger _logger;

	public SerilogAdaptor(ILogger logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public bool IsDebugEnabled => _logger.IsEnabled(LogEventLevel.Debug);
	public bool IsErrorEnabled => _logger.IsEnabled(LogEventLevel.Error);
	public bool IsFatalEnabled => _logger.IsEnabled(LogEventLevel.Fatal);
	public bool IsInfoEnabled => _logger.IsEnabled(LogEventLevel.Information);
	public bool IsWarnEnabled => _logger.IsEnabled(LogEventLevel.Warning);

	public void Debug(string message) => _logger.Debug(message);
	public void Error(string message) => _logger.Error(message);
	public void Error(string message, Exception exception) => _logger.Error(exception, message);
	public void Fatal(string message) => _logger.Fatal(message);
	public void Fatal(string message, Exception exception) => _logger.Fatal(exception, message);
	public void Info(string message) => _logger.Information(message);
	public void Warn(string message) => _logger.Warning(message);
}
```

#### 2. SerilogFactory 구현

SuperSocket은 ILogFactory 팩토리 패턴을 채택하여 각 구성 요소에 이름 기반(Contextual) 로거를 제공합니다.

Serilog의 `ForContext()`를 사용해 name 값을 로그 출처로 지정하여,

SuperSocket 내부의 각 컴포넌트가 고유한 `SourceContext`를 가진 Serilog 로거를 사용하도록 합니다.

> [SerilogFactory](SocketServer/SerilogFactory.cs): SuperSocket의 LogFactoryBase 구현체

```csharp
public class SerilogFactory : LogFactoryBase
{
	public SerilogFactory(string configPath = "appsettings.json", bool isSharedConfig = false)
		: base(configPath)
	{
		// 메인 애플리케이션에서 Program.cs에서 UseSerilog()를 통해
		// Serilog의 전역 로거(Log.Logger)를 이미 설정한 경우
		if (isSharedConfig)
		{
		}
		// Supersocket만 별도로 구성
		else
		{
			Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(new ConfigurationBuilder().AddJsonFile(configPath).Build())
				.CreateLogger();
		}
	}

	public override ILog GetLog(string name)
	{
		var logger = Log.Logger.ForContext("SourceContext", name);
		return new SerilogAdaptor(logger);
	}
}
```

본 예제와 같이 해당 Factory 외부에서 Serilog를 이미 초기화한 경우,

Serilog의 `Log.Logger`를 다시 설정하면 기존 전역 로거 구성이 덮어쓰기되어

로그가 이중 설정되거나, 일부 로그가 유실될 수 있습니다.

이 경우 `isShared`를 true로 설정하여 사전에 등록된 전역 로거를 참조만 하도록 구성합니다.

##### 💡 Serilog를 SuperSocket에서만 단독 실행하는 경우

만약 Serilog를 SuperSocket에서만 단독 실행하는 경우이고,

외부에서 `Serilog.Log.Logger`가 초기화되지 않은 상태라면,

Serilog를 `SerilogFactory` 생성자에서 직접 초기화하도록 `isShared`를 false 로 전달합니다.

구현한 SerilogFactory는 SuperSocket의 Setup 메서드 호출 시 아래와 같이 적용합니다:

```csharp
bool bResult = Setup(new RootConfig(), _networkConfig, logFactory: new SerilogFactory(isSharedConfig: true));
```

서버가 정상적으로 기동되면, Serilog를 통해 다음과 같은 구조화된 로그가 출력됩니다:

```json
{
  "Timestamp": "2025-04-02T19:46:39.9345125+09:00",
  "Level": "Debug",
  "MessageTemplate": "Listener (0.0.0.0:9000) was started",
  "Properties": {
    "SourceContext": "SocketServer",
    "MachineName": "\"SHANABUNNY\"",
    "ThreadId": 1
  }
}
```

### OpenTelemetry Sink 추가

.NET 애플리케이션에서 OpenTelemetry Collector로 로그를 전송하려면
Serilog에 `Serilog.Sinks.OpenTelemetry` 패키지를 사용합니다.

appsettings.json 구성에 다음 항목을 추가합니다:

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "OpenTelemetry",
        "Args": {
          "EndPoint": "http://127.0.0.1:4317",
          "ResourceAttributes": {
            "service.name": "SocketServer"
          }
        }
      }
    ]
  }
}
```

OpenTelemetry용 Serilog 설정의

- 기본 EndPoint는 `http://localhost:4317`이며,
- 기본 Protocol은 `OtlpProtocol.Grpc`입니다.

Protocol 설정은 필요에 따라 OtlpProtocol.HttpProtobuf로 변경할 수 있으며,

이 경우 OpenTelemetry 로그는 HTTP + Protobuf 형식으로 전송됩니다.

Protocol을 명시적으로 설정하고 싶을 경우 protocol 옵션에 원하는 값을 지정하면 됩니다.

추가적으로, OpenTelemetry 로그에는 로그가 속한 서비스나 환경 정보를 포함하는 `ResourceAttributes`를 설정할 수 있습니다.

아래는 Collector가 gRPC로 수신한 로그를 debug exporter를 통해 출력한 예시입니다:

```
otel-collector-1  | Trace ID:
otel-collector-1  | Span ID:
otel-collector-1  | Flags: 0
otel-collector-1  | LogRecord #1
otel-collector-1  | ObservedTimestamp: 2025-04-03 00:47:58.3004766 +0000 UTC
otel-collector-1  | Timestamp: 2025-04-03 00:47:58.3004766 +0000 UTC
otel-collector-1  | SeverityText: Information
otel-collector-1  | SeverityNumber: Info(9)
otel-collector-1  | Body: Str(서버 생성 성공)
otel-collector-1  | Attributes:
otel-collector-1  |      -> MachineName: Str("SHANABUNNY")
otel-collector-1  |      -> ThreadId: Int(1)
otel-collector-1  |      -> message_template.text: Str(서버 생성 성공)
```



================================================
File: references/aspnet_core_tips.md
================================================
# API 게임 서버 
    
## Poly를 사용한 재 요청 기능
시스템의 안정성과 보안을 보장하기 위해 타사 서비스를 호출할 때 재시도 및 회로 차단기를 추가할 수 있다.   
재시도는 한 번의 호출이 실패한 후 다시 시도하여 다운스트림 서비스의 일시적인 단절로 인해 모든 프로세스가 종료되는 것을 방지한다.   
회로 차단기는 과도한 무효 액세스를 방지하고 시스템에서 알 수 없는 예외가 발생하는 것을 방지하기 위한 것이다.  
Polly는 독립적인 재시도 메커니즘의 서드파티 라이브러리이다.   
  
아래 코드는 httpclient를 사용하여 하류 API에 대한 요청 시 재시도 및 회로 차단기에 대해서만 다룬다.  
NuGet 패키지 Microsoft.Extensions.Http.Polly를 가져와야 한다.  
  
```
using Polly;
var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddHttpClient("RetryClient", httpclient =>
    {
        httpclient.BaseAddress = new Uri("http://localhost:5258");
    })
    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.RetryAsync(3));

var app = builder.Build();

// httpclient를 호출한다
app.MapGet("/test", async (IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var httpClient = httpClientFactory.CreateClient("RetryClient");
        var content = await httpClient.GetStringAsync("other-api");
        Console.WriteLine(content);
        return "ok";
    }
    catch (Exception exc)
    {
        if (!Count.Time.HasValue)
        {
            Count.Time = DateTime.Now;
        }
        return $"{exc.Message}    【횟수：{Count.I++}】  【{Count.Time.Value.ToString("yyyy-MM-dd HH:mm:ss.fffffff")}】";
    }
});

// 상태 코드 500을 돌려준다
app.MapGet("/other-api", (ILogger<Program> logger) =>
{
    logger.LogInformation($"실패:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff")}");
    return Results.StatusCode(500);
});
app.Run();

static class Count
{
    public static int I = 1;
    public static DateTime? Time;
}
```  
   
`.AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.RetryAsync(3))` 에 의해 요청에 대해 3번의 재시도가 이루어지며, 첫 번째 재시도까지 총 4번의 재시도가 이루어진다.   
다운스트림 서비스에 장애가 발생하면 이렇게 짧은 시간 내에 자동으로 복구되지 않을 수 있다. 더 좋은 방법은 재시도 횟수에 따라 요청 간 시간을 연장(무작위 또는 자체 지연 알고리즘을 구축)하는 것이다.    
```
.AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(3, retryNumber =>
        {
            switch (retryNumber)
            {
                case 1:
                    return TimeSpan.FromMilliseconds(500);
                case 2:
                    return TimeSpan.FromMilliseconds(1000);
                case 3:
                    return TimeSpan.FromMilliseconds(1500);
                default:
                    return TimeSpan.FromMilliseconds(100);
            }
        }));
```  
  
또 다른 재시도 전략을 소개한다.  
```
// 뮤한으로 재시도 한다
.AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.RetryForeverAsync());
// 2초 마다 재시도 한다
.AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryForeverAsync(retryNumber =>
{
    Console.WriteLine(retryNumber);
    return TimeSpan.FromSeconds(2);
}));
// 5초간 4회 요청이 있고, 50%가 실패한 경우 10초간 서킷브레이커가 동작한다
.AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.AdvancedCircuitBreakerAsync(0.5d, TimeSpan.FromSeconds(5), 4, TimeSpan.FromSeconds(10)));
```  
  
서킷브레이커는 서비스를 보호하는 수단이다. 이 예에서 구체적인 사용 방법은 아래와 같다.  
```
builder.Services
    .AddHttpClient("RetryClient", httpclient =>
    {
        httpclient.BaseAddress = new Uri("http://localhost:5258");
    })
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(3, retryNumber =>
        {
            switch (retryNumber)
            {
                case 1:
                    return TimeSpan.FromMilliseconds(500);
                case 2:
                    return TimeSpan.FromMilliseconds(1000);
                case 3:
                    return TimeSpan.FromMilliseconds(1500);
                default:
                    return TimeSpan.FromMilliseconds(100);
            }
        }))
    // 서킷브레이커
    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.CircuitBreakerAsync(6, TimeSpan.FromSeconds(30)));
```    
CircuitBreaker는 6번의 실패한 요청이 있을 경우, 30초 동안 일시 정지를 제어한다.   
  
     
   
## RateLimit  
RateLimit은 네트워크의 기반 설비에서 설정하여 구현할 수도 있고, 게이트웨이에서 RateLimit을 할 수도 있다. 하지만 서비스 자체의 RateLimit도 빼놓을 수 없다.   
복수의 레플리카가 있는 경우 하나의 레플리카가 장애가 발생하면 다른 레플리카에 대한 트래픽이 증가하게 되고, 이것이 감당할 수 있는 요청량을 초과하면 서비스가 연쇄적으로 크래시될 수 있기 때문이다. 따라서 개별 서비스 자체적으로도 RateLimit을 구현하는 것이 바람직하다.  
  
ASP.NET Core 프로젝트에서는 AspNetCoreRateLimit을 도입하여 RateLimit 처리가 가능하다.  
아래와 같은 방법으로 NuGet 패키지를 도입할 수 있다.   
```
Install-Package AspNetCoreRateLimit
```  
  
클라이언트 RateLimit 설정:    
```
using AspNetCoreRateLimit;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

// ClientRateLimiting 설정 파일을 읽는다
builder.Services.Configure<ClientRateLimitOptions>(builder.Configuration.GetSection("ClientRateLimiting"));

// ClientRateLimitPolicies 설정 파일을 읽는다
builder.Services.Configure<ClientRateLimitPolicies>(builder.Configuration.GetSection("ClientRateLimitPolicies"));

// RateLimit 메모리캐시 서비스를 도입
builder.Services.AddInMemoryRateLimiting();

// RateLimit 설정 파일 서비스를 도입
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

// ClientRateLimitPolicies 를 유효화
var clientPolicyStore = app.Services.GetRequiredService<IClientPolicyStore>();
await clientPolicyStore.SeedAsync();

// 클라이언트 RateLimit 미들웨를 사용
app.UseClientRateLimiting();

app.MapGet("/test00", () => "get test00 ok");
app.MapGet("/test01", () => "get test01 ok");
app.MapGet("/test02", () => "get test02 ok");
app.MapPost("/test02", () => "post test02 ok");

app.Run();
```    
     
appsetings.json     
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ClientRateLimiting": {
    "EnableEndpointRateLimiting": false,
    "StackBlockedRequests": false,
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "EndpointWhitelist": [ "get:/test00", "*:/test01" ],
    "ClientWhitelist": [ "dev-id-1", "dev-id-2" ],
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "5s",
        "Limit": 2
      },
      {
        "Endpoint": "*",
        "Period": "10s",
        "Limit": 3
      }
    ]  
  },   
  "ClientRateLimitPolicies": {
    "ClientRules": [
      {
        "ClientId": "client-id-1",
        "Rules": [
          {
            "Endpoint": "*",
            "Period": "5s",
            "Limit": 1
          },
          {
            "Endpoint": "*",
            "Period": "15m",
            "Limit": 200
          }
        ]
      },
      {
        "ClientId": "client-id-2",
        "Rules": [
          {
            "Endpoint": "*",
            "Period": "1s",
            "Limit": 5
          },
          {
            "Endpoint": "*",
            "Period": "15m",
            "Limit": 150
          },
          {
            "Endpoint": "*",
            "Period": "12h",
            "Limit": 500
          }
        ]
      }
    ]
  }
}
```  
    
설정 설명:   
- EnableEndpointRateLimiting이 false인 경우 모든 요청의 총 수가 임계값을 초과하면 속도 제한을 적용하고, true인 경우 각 요청이 임계 값을 초과하면 속도 제한을 적용한다.
- StackBlockedRequests가 false인 경우 이전 5초 동안 2번 성공하고 1번 실패한 경우 6초 후에 한 번 더 성공할 수 있으며, true인 경우 6초 후의 요청은 성공하지 못한다.
- ClientIdHeader는 속도 제한의 블랙/화이트 리스트를 처리하기 위해 헤더 키 X-ClientId를 지정한다.
- ClientWhitelist는 dev-id-1, dev-id-2이며, 헤더 내 X-ClientId가 이 값이면 통과시킨다.
- EndpointWhitelist는 속도 제한에 포함되지 않는 엔드포인트이다.
- HttpStatusCode는 속도 제한 후 반환되는 상태 코드이다.
- GeneralRules는 일반적인 속도 제한 규칙이다.
- ClientRateLimitPolicies 설정은 서로 다른 X-ClientId에 대해 서로 다른 속도 제한을 설정하기 위한 것으로 클라이언트 ID의 그레이리스트를 의미한다.     
    
또한, ClientID에 의한 속도 제한뿐만 아니라 클라이언트의 요청 IP에 대해서도 속도 제한을 할 수 있으며, 설정 방법은 동일하다.    
     
	 
IP Rate Limit 설정: IP Rate Limit 설정	  
```
using AspNetCoreRateLimit;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

// IPRateLimiting 설정 파일을 읽는다
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));

// IPRateLimitPolicies 설정 파일을 읽는다
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));

// RateLimit 메모리캐시 서비스를 주입
builder.Services.AddInMemoryRateLimiting();

// RateLimit 설정 파일 서비스를 주입
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

// IPRateLimitPolicies 를 유효화
var ipPolicyStore = app.Services.GetRequiredService<IIpPolicyStore>();
await ipPolicyStore.SeedAsync();

// IP RateLimit 미들웨어를 사용
app.UseIpRateLimiting();

app.MapGet("/test00", () => "get test00 ok");
app.MapGet("/test01", () => "get test01 ok");
app.MapGet("/test02", () => "get test02 ok");
app.MapPost("/test02", () => "post test02 ok");
app.MapGet("/test03", () => "get test01 ok");

app.Run();
```  
       
appsettings.json  
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": false,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "IpWhitelist": [ "127.0.0.1"],
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "EndpointWhitelist": [ "get:/test00", "*:/test01" ],
    "ClientWhitelist": [ "dev-id-1", "dev-id-2" ],
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "5s",
        "Limit": 2
      },
      {
        "Endpoint": "*",
        "Period": "10s",
        "Limit": 3
      }
    ]  
  },
  "IpRateLimitPolicies": {
    "IpRules": [
      {
        "Ip": "127.0.0.2",
        "Rules": [
          {
            "Endpoint": "*",
            "Period": "4s",
            "Limit": 1
          },
          {
            "Endpoint": "*",
            "Period": "15m",
            "Limit": 200
          }
        ]
      }
    ]  
  }
}   
```     
   
   
##  FluentValidation: 엔티티 검증
API POST로 전송되는 데이터의 유효성을 검증하기 위해 FluentValidation(자세한 내용은 공식 웹사이트 https://fluentvalidation.net  참조)을 도입할 수 있으며, asp.net mvc에서는 모델의 유효성 검사를 사용하여, 엔티티 클래스 상에 속성을 추가하여 검증 효과를 얻고 있다.   
FluentValidation의 원리는 AbstractValidator의 구현을 통해 T 엔티티 클래스의 검증을 수행하는 것으로, T의 속성을 다양한 규칙을 통해 검증한다(더 많은 검증 규칙은 공식 웹사이트를 참조). 아래 구현을 참고한다:   
```
public class Person{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Tel { get; set; }
    public string Email { get; set; }
    public DateTime Birthday { get; set; }
    public string IDCard { get; set; }
    public PersonAddress Address { get; set; }
}
public class PersonAddress{
    public string Country { get; set; }
    public string Province { get; set; }
    public string City { get; set; }
    public string County { get; set; }
    public string Address { get; set; }
    public string Postcode { get; set; }
}

/// <summary>
/// Person 검증
/// </summary>
public class PersonValidator : AbstractValidator<Person>{
    public PersonValidator(IPersonService personService)
    {
        RuleFor(p => p.Name).NotNull().NotEmpty();
        RuleFor(p => p.Email).NotNull().EmailAddress();
        RuleFor(p => p.Birthday).NotNull();
        RuleFor(p => p.IDCard)
            .NotNull()
            .NotEmpty()
            .Length(18)
            .When(p => (DateTime.Now > p.Birthday.AddYears(1)))
            .WithMessage(p => $"出生日期为{p.Birthday}，现在时间为{DateTime.Now},大于一岁，CardID值必填！");
        RuleFor(p => p.Tel).NotNull().Matches(@"^(\d{3,4}-)?\d{6,8}$|^[1]+[3,4,5,8]+\d{9}$").WithMessage("电话格式为：0000-0000000或13000000000");
        RuleFor(p => p.Address).NotNull();
        RuleFor(p => p.Address).SetValidator(new PersonAddressValidator());
        //외부 메소드를 호출하여 검증한다
        RuleFor(p => p.Id).Must(id => personService.IsExist(id)).WithMessage(p => $"不存在id={p.Id}の用户");
    }
}
/// <summary>
/// Person Address 검증
/// </summary>
public class PersonAddressValidator : AbstractValidator<PersonAddress>{
    public PersonAddressValidator()
    {
        RuleFor(a => a.Country).NotNull().NotEmpty();
        RuleFor(a => a.Province).NotNull().NotEmpty();
        RuleFor(a => a.City).NotNull().NotEmpty();
        RuleFor(a => a.County).NotNull().NotEmpty();
        RuleFor(a => a.Address).NotNull().NotEmpty();
        RuleFor(a => a.Postcode).NotNull().NotEmpty().Length(6);
    }
}
```  
  
FluentValidation을 도입하는 것도 쉬운데, IValidator를 주입하여 구현할 수도 있고, AddFluentValidation으로 주입한 후 IValidatorFactory를 사용하여 Validator를 가져와 검증을 할 수도 있다. 코드는 아래와 같다:    
```
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFluentValidation();
builder.Services.AddScoped<IValidator<Person>, PersonValidator>();
builder.Services.AddScoped<IPersonService, PersonService>();
var app = builder.Build();

app.MapPost("/person", async (IValidator<Person> validator, Person person) => {
     var result = await validator.ValidateAsync(person);
     if (!result.IsValid)
     {
         var errors = new StringBuilder();
         foreach (var valid in result.Errors)
         {
             errors.AppendLine(valid.ErrorMessage);
         }
         return errors.ToString();
     }
     return "OK";
});
app.MapPost("/person1", async (IValidatorFactory validatorFactory, Person person) => {
    var result = await validatorFactory.GetValidator<Person>().ValidateAsync(person);
    if (!result.IsValid)
    {
        var errors = new StringBuilder();
        foreach (var valid in result.Errors)
        {
            errors.AppendLine(valid.ErrorMessage);
        }
        return errors.ToString();
    }
    return "OK";
});
app.MapPost("/person2", async (IValidatorFactory validatorFactory, Person person) => {
    var result = await validatorFactory.GetValidator(typeof(Person)).ValidateAsync(new ValidationContext<Person>(person));
    if (!result.IsValid)
    {
        var errors = new StringBuilder();
        foreach (var valid in result.Errors)
        {
            errors.AppendLine(valid.ErrorMessage);
        }
        return errors.ToString();
    }
    return "OK";
});
app.Run();

public interface IPersonService{
    public bool IsExist(int id);
}
public class PersonService : IPersonService{
    public bool IsExist(int id)
    {
        if (DateTime.Now.Second % 2 == 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}
```     


================================================
File: references/dotnet_build.md
================================================
출처:  https://zenn.dev/takumi_machino/articles/how-dotnet-work  
  
# .NET 애플리케이션은 어떻게 작동하는가?

## 시작하며
C# 또는 .NET 애플리케이션을 Visual Studio 등에서 "▶ 실행"했을 때, 내부적으로 어떤 처리가 이루어져 애플리케이션이 실행되는지 생각해 본 적이 있는가?  
이 글에서는 .NET의 빌드부터 실행까지의 흐름을 초보자도 이해할 수 있도록 설명한다.  
  
  
## 빌드란 무엇인가
빌드란 C# 등의 **소스코드를 컴퓨터가 실행할 수 있는 형태로 변환하는 작업**이다.  
단, .NET의 경우 한 번 **중간 언어(CIL)** 형식으로 변환한 후, 실행 시에 필요한 부분만 진짜 **기계어(네이티브 코드)**로 변환한다.  
  
## 빌드 결과물의 위치
프로젝트를 빌드하면 다음과 같은 디렉토리 구조가 된다.  
  
```
MyApp/
└── bin/
    └── Debug/
        └── net8.0/
            ├── MyApp.dll
            ├── {추가한패키지명}.Json.dll
            |　               ︙
            ├── MyApp.pdb
            ├── MyApp.runtimeconfig.json
            └── MyApp.deps.json
```
  
**MyApp.dll**: 실행 대상 어셈블리(CIL 형식)    
**MyApp.pdb**: 디버그 정보    
**{추가한패키지명}.Json.dll**: 추가한 패키지의 어셈블리  
**runtimeconfig.json**: 실행 시 필요한 런타임 지정  
**deps.json**: 의존 관계 정보  
  
이 **MyApp.dll**을 **dotnet** 명령으로 실행함으로써 애플리케이션이 시작된다.  
"▶ 실행"했을 때는 내부적으로 **dotnet** 명령을 실행하고 있다.  
  
```
dotnet bin/Debug/net8.0/MyApp.dll
```
  
애플리케이션 실행 시에는 CIL 형식의 **.dll**이 .NET 런타임(CLR)에 의해 로드된다.  
필요한 부분은 JIT(Just-In-Time) 컴파일로 네이티브 코드로 변환된 후 실행된다.  
  
```
[MyApp.dll] --로드--> [CLR (.NET 런타임)]
           --JIT변환--> [네이티브 코드로 실행]
```
  
  
## .exe는 어디서 만들어지는가
일반적인 .NET 6/7/8 프로젝트에서는 **.dll**이 생성되며, **.exe**는 출력되지 않는다.  
단, 다음 경우에는 **.exe**가 생성된다.  
  
- **.NET Framework** 프로젝트(Windows 전용) 
- 다음과 같이 자체 완결형으로 빌드한 경우(Windows 전용)
  ```
  dotnet publish -r win-x64 --self-contained -c Release
  ```
  
이를 통해 다음과 같은 출력을 얻을 수 있다.
  
```
bin/Release/net6.0/win-x64/publish/
├── MyApp.exe
├── MyApp.dll
├── 기타 DLL 및 설정 파일
```
  
**.exe**는 단독으로 실행 가능한 애플리케이션이 된다.
  
  
## 참고
https://dotnet.microsoft.com/ko-kr/learn/dotnet/what-is-dotnet-framework  


================================================
File: references/infographic.md
================================================
# 인포 그래픽 
   
## ASP.NET Core 앱에서 URL을 설정하는 5가지 방법
[문서](https://docs.google.com/document/d/1x3ZJQtGt2uNW5_xRT6QHrOmZkfbNU2KQ23N5RCLi4cE/edit?usp=sharing )   
![](../../images/5-ways-to-set-the-urls-for-an-aspnetcore-app.png)     


================================================
File: references/sqlkata.md
================================================
# SqlKata  
https://sqlkata.com/docs/ 여기의 글을 번역 정리하였다.  
  

## 소개
우아한 쿼리 빌더 및 실행기는 우아하고 예측 가능한 방식으로 SQL 쿼리를 처리할 수 있도록 도와줍니다.  
모두가 좋아하는 언어인 C#으로 작성되었으며, 소스 코드는 [Github의 SqlKata](https://github.com/sqlkata/querybuilder )에서 확인할 수 있습니다.  
  
매개 변수 바인딩 기술을 사용하여 SQL 인젝션 공격으로부터 애플리케이션을 보호합니다.  
바인딩으로 전달되는 문자열을 정리할 필요가 없습니다.    
  
이 기술은 SQL 인젝션 공격으로부터 보호할 뿐 아니라 매개변수가 변경되더라도 SQL 엔진이 동일한 쿼리 계획을 캐시하고 재사용하도록 하여 쿼리 실행 속도를 높여줍니다.  
  
```
IEnumerable<Post> posts = await db.Query("Posts")
    .Where("Likes", ">", 10)
    .WhereIn("Lang", new [] {"en", "fr"})
    .WhereNotNull("AuthorId")
    .OrderByDesc("Date")
    .Select("Id", "Title")
    .GetAsync<Post>();
```  
  
```  
SELECT [Id], [Title] FROM [Posts] WHERE
  [Likes] > @p1 AND
  [Lang] IN ( @p2, @p3 ) AND
  [AuthorId] IS NOT NULL
ORDER BY [Date] DESC
```  
     
  
## 설치
Nuget으로 설치한다.   
```
dotnet add package SqlKata
dotnet add package SqlKata.Execution
```
   
  
  
## 시작  
```
using SqlKata;
using SqlKata.Execution;
using System.Data.SqlClient; // Sql Server Connection Namespace

// Setup the connection and compiler
var connection = new SqlConnection("Data Source=MyDb;User Id=User;Password=TopSecret");
var compiler = new SqlServerCompiler();

var db = new QueryFactory(connection, compiler);

// You can register the QueryFactory in the IoC container

var user = db.Query("Users").Where("Id", 1).Where("Status", "Active").First();
```  
  
Sql output  
```
SELECT TOP(1) * FROM [Users] WHERE [Id] = @p0 AND [Status] = @p1
```  
where @p0, @p1 are equivalent to 1, "Active" respectively.
  
  
  
## 컴파일만 하는 예제 
쿼리를 실행할 필요가 없는 경우 SqlKata를 사용하여 쿼리를 바인딩 배열이 있는 SQL 문자열로 빌드하고 컴파일할 수 있습니다. 물론 여기에는 연결 인스턴스가 필요하지 않습니다. 시작하는 가장 간단한 방법은 테이블 이름을 전달하여 쿼리 객체의 새 인스턴스를 만드는 것입니다.  
```
using SqlKata;
using SqlKata.Compilers;

// Create an instance of SQLServer
var compiler = new SqlServerCompiler();

var query = new Query("Users").Where("Id", 1).Where("Status", "Active");

SqlResult result = compiler.Compile(query);

string sql = result.Sql;
List<object> bindings = result.Bindings; // [ 1, "Active" ]
```  
다음 SQL 문자열이 생성됩니다.  
```
SELECT * FROM [Users] WHERE [Id] = @p0 AND [Status] = @p1
```   
  

<br>   

## Compilers
컴파일러는 쿼리 인스턴스를 데이터베이스 엔진에서 직접 실행할 수 있는 SQL 문자열로 변환하는 컴포넌트입니다. 
  
### 지원되는 컴파일러 
현재 SqlKata 쿼리 빌더는 기본적으로 다음 컴파일러를 지원합니다. Sql Server, SQLite, MySql, PostgreSql, Oracle 및 Firebird.  
    
### 몇 가지 눈에 띄는 차이점 
이론적으로 다른 컴파일러의 출력은 유사해야 하며, 이는 80%의 경우에 해당되지만 일부 에지 케이스에서는 출력이 매우 다를 수 있습니다. 예를 들어 각 컴파일러에서 Limit 및 Offset 절이 어떻게 컴파일되는지 살펴보십시오.    
  
```  
new Query("Posts").Limit(10).Offset(20); 
```
  
Sql Server  
```
SELECT * FROM [Posts] ORDER BY (SELECT 0) OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY
```  
  
Legacy Sql Server (< 2012)  
```
SELECT * FROM (
    SELECT *, ROW_NUMBER() OVER (ORDER BY (SELECT 0)) AS [row_num] FROM [Posts]
) WHERE [row_num] BETWEEN 21 AND 30
```  
  
MySql   
```
SELECT * FROM `Posts` LIMIT 10 OFFSET 20
```  
   
PostgreSql  
```
SELECT * FROM "Posts" LIMIT 10 OFFSET 20 
```  
  
이 문서에서는 출력이 동일하지 않은 쿼리를 제외하고 SqlServer 컴파일러에 의해 컴파일된 쿼리만 표시합니다.  
  
  

## Select 
   
### Column
하나 또는 여러 개의 열을 선택  
```
new Query("Posts").Select("Id", "Title", "CreatedAt as Date"); 
```  
```  
SELECT [Id], [Title], [CreatedAt] AS [Date] FROM [Posts]
```  
  
### 하위 쿼리
하위 쿼리에서 선택  
```
var countQuery = new Query("Comments").WhereColumns("Comments.PostId", "=", "Posts.Id").AsCount();
var query = new Query("Posts").Select("Id").Select(countQuery, "CommentsCount");try 
```  
```
SELECT [Id], (SELECT COUNT(*) AS [count] FROM [Comments] WHERE [Comments].[PostId] = [Posts].[Id]) AS [CommentsCount] FROM [Posts]
```  
  
### Raw
자유롭게 기술하고 싶을 때   
```
new Query("Posts").Select("Id").SelectRaw("count(1) over(partition by AuthorId) as PostsByAuthor") 
```  
```
SELECT [Id], count(1) over(partition by AuthorId) as PostsByAuthor FROM [Posts]
```  
   
### Raw 내부의 열과 테이블 식별
식별자를 [ 및 ]로 감싸서 SqlKata에서 식별자로 인식하도록 할 수 있으므로 위의 동일한 예제를 다음과 같이 다시 작성할 수 있습니다.  
```
new Query("Posts").Select("Id").SelectRaw("count(1) over(partition by [AuthorId]) as [PostsByAuthor]")
```  
  
이제 AuthorId 및 PostsByAuthor가 컴파일러 식별자로 래핑되므로 특히 대소문자를 구분하는 PostgreSql과 같은 엔진에 유용합니다.
  
SQLServer 에서  
```
SELECT [Id], count(1) over(partition by [AuthorId]) as [PostsByAuthor] FROM [Posts]
```
  
Postgres 에서 
```
SELECT "Id", count(1) over(partition by "AuthorId") as "PostsByAuthor" FROM "Posts"
```  
  
MySQL 에서  
```
SELECT `Id`, count(1) over(partition by `AuthorId`) as `PostsByAuthor` FROM `Posts`
```  
   

### 열 확장 표현식(중괄호 확장)
v1.1.2 부터 Braces Expansions 기능을 사용하여 동시에 여러 열을 선택할 수 있습니다. 이를 통해 동일한 쿼리를 더 간결하게 작성할 수 있습니다.   
```
new Query("Users")
    .Join("Profiles", "Profiles.UserId", "Users.Id")
    .Select(
        "Users.{Id, Name, LastName}",
        "Profiles.{GithubUrl, Website, Stars}"
    )
```  	 
  
동일하게 기술한 것   
```
new Query("Users")
    .Join("Profiles", "Profiles.UserId", "Users.Id")
    .Select(
        "Users.Id",
        "Users.Name",
        "Users.LastName",
        "Profiles.GithubUrl",
        "Profiles.Website",
        "Profiles.Stars"
    )
```  
```	 
SELECT
  [Users].[Id],
  [Users].[Name],
  [Users].[LastName],
  [Profiles].[GithubUrl],
  [Profiles].[Website],
  [Profiles].[Stars]
FROM
  [Users]
  INNER JOIN [Profiles] ON [Profiles].[UserId] = [Users].[Id] 
```  
     

## From  
  
### 테이블이나 뷰에서
```
new Query("Posts");
```  
or   
```
new Query().From("Posts");
```  
  
```  
SELECT * FROM [Posts]
```  
  
   
### 별명(Alias)
테이블에 별칭을 지정하려면 as 구문을 사용해야 합니다.  
```
new Query("Posts as p")
```  
   
```   
SELECT * FROM [Posts] AS [p]
```  
  

### From a Sub Query
쿼리 인스턴스를 From 메서드에 전달하여 하위 쿼리에서 선택하거나 람다 함수 오버로드를 사용할 수 있습니다.  
```
var fewMonthsAgo = DateTime.UtcNow.AddMonths(-6);
var oldPostsQuery = new Query("Posts").Where("Date", "<", fewMonthsAgo).As("old");

var query = new Query().From(oldPostsQuery).OrderByDesc("Date");
```  
  
```  
SELECT * FROM (SELECT * FROM [Posts] WHERE [Date] < '2017-06-01 6:31:26') AS [old] ORDER BY [Date] DESC
```  
  

### From a Raw expression
이 FromRaw 방법을 사용하면 원시 표현식을 작성할 수 있습니다.
  
예를 들어 SqlServer에서는 TABLESAMPLE을 사용하여 주석 테이블의 전체 행 중 10% 샘플을 가져올 수 있습니다.  
```
var query = new Query().FromRaw("Comments TABLESAMPLE SYSTEM (10 PERCENT)")
```
  
```  
SELECT * FROM Comments TABLESAMPLE SYSTEM (10 PERCENT)
```  
  
  
## Where
SqlKata는 Where 조건을 쉽게 작성할 수 있는 많은 유용한 메서드를 제공합니다.  
이러한 모든 메서드에는 NOT 및 OR 연산자에 대한 오버로드가 함께 제공됩니다.  
따라서 부울 OR 연산자를 적용하려면 WhereNotNull 또는 OrWhereNotNull를 사용하고 조건을 무효화하려면 OrWhereNull를 사용할 수 있습니다.  
  
### 기본 Where
where 메서드의 두 번째 매개변수는 선택 사항이며 생략하면 기본적으로 `=`가 되므로 이 두 문은 완전히 동일합니다.  
```
new Query("Posts").Where("Id", 10); 

// `=`는 기본 연산자이므로 
new Query("Posts").Where("Id", "=", 10);
```  
```  
new Query("Posts").WhereFalse("IsPublished").Where("Score", ">", 10);
```  
```
SELECT * FROM [Posts] WHERE [IsPublished] = 0 AND [Score] > 10
```  
  
주: WhereNot, OrWhere 및 OrWhereNot에도 동일하게 적용됩니다.   
  

### 여러 필드
여러 필드에 대해 쿼리를 필터링하려면 col/value을 나타내는 객체를 전달합니다.  
```
var query = new Query("Posts").Where(new {
    Year = 2017 ,
    CategoryId = 198 ,
    IsPublished = true,
});
```   
```
SELECT * FROM [Posts] WHERE [Year] = 2017 AND [CategoryId] = 198 AND [IsPublished] = True
```  
  
  
### WhereNull, WhereTrue 및 WhereFalse
NULL, true 및 false 값에 대해 필터링하려면 다음을 수행합니다.   
```
db.Query("Users").WhereFalse("IsActive").OrWhereNull("LastActivityDate");
```  
```
SELECT * FROM [Users] WHERE [IsActive] = cast(0 as bit) OR [LastActivityDate] IS NULL
```   
  

### 하위 쿼리(Sub Query)
Query 인스턴스를 전달하여 열을 하위 쿼리와 비교할 수 있습니다.  
```
var averageQuery = new Query("Posts").AsAverage("score");

var query = new Query("Posts").Where("Score", ">", averageQuery);
```   
```
SELECT * FROM [Posts] WHERE [Score] > (SELECT AVG([score]) AS [avg] FROM [Posts])
```  
  
주: 하위 쿼리는 비교할 스칼라 셀 하나를 반환해야 하므로 필요한 경우 Limit(1)를 설정하고 하나의 열을 선택해야 할 수 있습니다.  
   

### 중첩된 조건 및 그룹화
조건을 그룹화하려면 조건을 다른 Where 블록 안에 래핑하면 됩니다.  
```
new Query("Posts").Where(q =>
    q.WhereFalse("IsPublished").OrWhere("CommentsCount", 0)
);
```  
``` 
SELECT * FROM [Posts] WHERE ([IsPublished] = 0 OR [CommentsCount] = 0)
```  
    

### Comparing two columns
두 열을 함께 비교하려는 경우 이 방법을 사용합니다.  
```
new Query("Posts").WhereColumns("Upvotes", ">", "Downvotes");
```  
```
SELECT * FROM [Posts] WHERE [Upvotes] > [Downvotes]
```  
  

### Where In
IEnumerable<T>을 전달하여 SQL WHERE IN 조건을 적용합니다.  
```
new Query("Posts").WhereNotIn("AuthorId", new []{1, 2, 3, 4, 5});
```  
``` 
SELECT * FROM [Posts] WHERE [AuthorId] NOT IN (1), 2, 3, 4, 5)
```  
  
하위 쿼리에 대해 필터링하기 위해 Query 인스턴스를 전달할 수 있습니다.  
```
var blocked = new Query("Authors").Where("Status", "blocked").Select("Id");

new Query("Posts").WhereNotIn("AuthorId", blocked);
```   
``` 
SELECT * FROM [Posts] WHERE [AuthorId] NOT IN (SELECT [Id] FROM [Authors] WHERE [Status] = 'blocked')
```    
      
주: 하위 쿼리는 하나의 열을 반환해야 합니다.  
  

### Where Exists
댓글이 하나 이상 있는 모든 글을 선택하려면 다음과 같이 하세요.   
```
new Query("Posts").WhereExists(q => q.From("Comments").WhereColumns("Comments.PostId", "=", "Posts.Id") ); 
```  
    
Sql Server에서  
```
SELECT * FROM [Posts] WHERE EXISTS (SELECT TOP (1) 1 FROM [댓글] WHERE [Id] = [Posts].[Id])
```  
  
PostgreSql에서   
```
SELECT * FROM "Posts" WHERE EXISTS (SELECT 1 FROM "Comments" WHERE "Id" = "Posts"."Id" LIMIT 1)
```  
  
SqlKata는 모든 데이터베이스 엔진에서 일관된 동작을 제공하기 위해 선택한 열을 무시하고 결과를 EXISTS로 제한하여 1로 쿼리를 최적화하려고 시도합니다.  
  

### Where Raw
WhereRaw 메서드를 사용하면 위의 메서드에서 지원되지 않는 모든 것을 작성할 수 있으므로 최대한의 유연성을 얻을 수 있습니다.
```
new Query("Posts").WhereRaw("lower(Title) = ?", "sql");
```   
```
SELECT * FROM [Posts] WHERE lower(Title) = 'sql'
```  
  
때때로 테이블/열을 엔진 식별자로 감싸는 것이 유용할 때가 있는데, 이는 PostgreSql에서처럼 데이터베이스가 대소문자를 구분하는 경우에 유용하며, 이렇게 하려면 문자열을 [ 및 ]로 감싸면 SqlKata가 해당 식별자를 넣습니다.  
```
new Query("Posts").WhereRaw("lower([Title]) = ?", "sql"); 
```   

Sql Server에서   
```
SELECT * FROM [Posts] WHERE lower([Title]) = 'sql'
```   
  
PostgreSql에서   
```
SELECT * FROM "Posts" WHERE lower("Title") = 'sql'
```   
  

## String Operations  
https://sqlkata.com/docs/where-string   



## Date Operations       
https://sqlkata.com/docs/where-date  
   


## Limit and Offset  
Limit 및 Offset를 사용하면 데이터베이스에서 반환되는 결과 수를 제한할 수 있으며, 이 방법은 OrderBy 및 OrderByDesc 방법과 높은 상관 관계가 있습니다.   
```
// 최신 게시물 
var query = new Query("Posts").OrderByDesc("Date").Limit(10)
```   
   
Sql Server에서  
```
SELECT TOP (10) * FROM [Posts] ORDER BY [Date] DESC
```    
  
PostgreSql에서  
```
SELECT * FROM "Posts" ORDER BY "Date" DESC LIMIT 10
```    
  
MySql에서   
```
SELECT * FROM `Posts` ORDER BY `Date` DESC LIMIT 10
```    
    
    
### 레코드 건너뛰기(오프셋)
일부 레코드를 건너뛰려면 오프셋 방법을 사용합니다.  
  
```
// latest posts
var query = new Query("Posts").OrderByDesc("Date").Limit(10).Offset(5); 
```  
       
In Sql Server  
```
SELECT * FROM [Posts] ORDER BY [Date] DESC OFFSET 5 ROWS FETCH NEXT 10 ROWS
```  
In Legacy Sql Server (< 2012)  
```
SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY [Date] DESC) AS [row_num] FROM [Posts]) AS [subquery] WHERE [row_num] BETWEEN 6 AND 15
```   
In PostgreSql  
```
SELECT * FROM "Posts" ORDER BY "Date" DESC LIMIT 10 OFFSET 5
```  
In MySql    
```
SELECT * FROM `Posts` ORDER BY `Date` DESC LIMIT 10 OFFSET 5
```   
  

### 데이터 페이지 매김
ForPage 메서드를 사용하여 데이터를 쉽게 페이지 매김할 수 있습니다.   
   
```
var posts = new Query("Posts").OrderByDesc("Date").ForPage(2);
```
   
기본적으로 이 메서드는 페이지당 15 행을 반환하며, 두 번째 매개변수로 정수를 전달하여 이 값을 재정의할 수 있습니다.  
  
주: ForPage는 1 기반이므로 첫 번째 페이지에 1을 전달합니다.  
  
```
var posts = new Query("Posts").OrderByDesc("Date").ForPage(3, 50);
```  


### Skip & Take   
Linq 배경에서 오신다면 여기에 보너스가 있습니다. 스킵 및 테이크 메서드를 오프셋 및 리밋의 별칭으로 사용할 수 있으니 즐겨보세요 :)

  
   
## Join
https://sqlkata.com/docs/join  
  
   

## Group
https://sqlkata.com/docs/group  
  
   

## Order 
https://sqlkata.com/docs/order  



## Having
https://sqlkata.com/docs/having  



## 여러 쿼리 결합하기  
  
### Union / Except / Intersect
SqlKata를 사용하면 Union, UnionAll, Intersect, IntersectAll, Except 및 ExceptAll 메서드를 제공하여 사용 가능한 연산자 union, intersect 및 except 중 하나를 사용하여 여러 쿼리를 결합할 수 있습니다.  
  
위의 메서드는 Query 인스턴스 또는 labmda 식을 허용합니다.  
```
var phones = new Query("Phones");
var laptops = new Query("Laptops");

var mobiles = laptops.Union(phones);
```   

```    
(SELECT * FROM [Laptops]) UNION (SELECT * FROM [Phones])
```  
  
또는 랩엠다 오버로드를 사용하여
```  
var mobiles = new Query("Laptops").ExceptAll(q => q.From("OldLaptops"));
```   
   
```   
(SELECT * FROM [노트북]) EXCEPT ALL (SELECT * FROM [OldLaptops])
```   
  
  
### 원시 표현식 결합
언제든지 CombineRaw 메서드를 사용하여 원시 식을 추가할 수 있습니다.  
```
var mobiles = new Query("Laptops").CombineRaw("union all select * from OldLaptops");
```  
   
```
SELECT * FROM [Laptops] union all select * from OldLaptops 
```     
  
물론 테이블 식별자 문자 [ 및 ]를 사용하여 테이블/열 키워드를 래핑하도록 SqlKata에 지시할 수 있습니다.   
```
var mobiles = new Query("Laptops").CombineRaw("[OldLaptops]에서 * 모두 선택");
```  
    
```
SELECT * FROM [Laptops] union all select * from [OldLaptops]
```   
     
    
## Common Table Expression
https://sqlkata.com/docs/cte  


   
## Advanced methods
https://sqlkata.com/docs/advanced  
  


## Insert, Update and Delete  
참고: 현재 삽입, 업데이트 및 삭제 문에서는 다음과 같은 절이 완전히 무시됩니다: 사용, 주문 기준, 그룹화 기준, 가지고, 조인, 제한, 오프셋 및 구별.  
  
### Insert  
```
var query = new Query("Books").AsInsert(new {
    Title = "Toyota Kata",
    CreatedAt = new DateTime(2009, 8, 4),
    Author = "Mike Rother"
});
```  

```   
INSERT INTO [Books] ([Title], [CreatedAt], [Author]) VALUES ('Toyota Kata', '2009-08-04 00:00:00', 'Mike Rother')
```  
  
Note: 쿼리를 실행하는 동안 InsertGetId() 메서드를 사용하여 삽입된 ID를 가져올 수 있습니다.   
  

### Insert Many
you can use the insert many overload to insert multiple records  
  
```
var cols = new [] {"Name", "Price"};

var data = new [] {
    new object[] { "A", 1000 },
    new object[] { "B", 2000 },
    new object[] { "C", 3000 },
};

var query = new Query("Products")
    .AsInsert(cols, data);try 
```  
   
```
INSERT INTO [Products] ([Name], [Price]) VALUES ("A", 1000), ("B", 2000), ("C", 3000)
```  
  

### Insert from Query
다른 선택 쿼리 결과에 대한 레코드를 삽입할 수도 있습니다.  
```
var cols = new [] { "Id", "Name", "Address" };
new Query("ActiveUsers").AsInsert(cols, new Query("Users").Where("Active", 1));
```    
   
```
INSERT INTO [ActiveUsers] ([Id], [Name], [Address]) SELECT * FROM [Users] WHERE [Active] = 1
```  
   

### Update
```
var query = new Query("Posts").WhereNull("AuthorId").AsUpdate(new {
    AuthorId = 10
});
```    
   
```
UPDATE [Posts] SET [AuthorId] = 10 WHERE [AuthorId] IS NULL
```  
   

### Delete  
```
var query = new Query("Posts").Where("Date", ">", DateTime.UtcNow.AddDays(-30)).AsDelete();
```   
  
```
DELETE FROM [Posts] WHERE [Date] > ?  
```   
  
   
   
## Update Data   
SqlKata는 데이터베이스에 대한 업데이트/삽입/삭제에 도움이 되는 다음과 같은 메서드를 제공합니다:   
- Update()
- Insert()
- InsertGetId()
- Delete()  
   
```
var db = new QueryFactory(connection, new SqlServerCompiler());
```  
  

### Update Existing Data
```
int affected = db.Query("Books").Where("Id", 1).Update(new {
    Price = 18,
    Status = "active",
});
```  
   

### Insert One Record
```
int affected = db.Query("Books").Insert(new {
    Title = "Introduction to C#",
    Price = 18,
    Status = "active",
});
```  
  

### Insert One Record and get the Inserted Id
  
```
var id = db.Query("Books").InsertGetId<int>(new {
    Title = "Introduction to Dart",
    Price = 0,
    Status = "active"
});
```  
  
Note: 현재 이 메서드는 단일 삽입 문에 대한 ID를 가져올 수 있습니다. 다중 레코드는 아직 지원되지 않습니다.  
  

### Insert Multiple Record
```
var cols = new [] {"Name", "Price"};

var data = new [] {
    new object[] { "A", 1000 },
    new object[] { "B", 2000 },
    new object[] { "C", 3000 },
};

db.Query("Products").Insert(cols, data);
```  
   

### Insert From Existing Query
```
var articlesQuery = new Query("Articles").Where("Type", "Book").Limit(100);
var columns = new [] { "Title", "Price", "Status" };

int affected = db.Query("Books").Insert(columns, articlesQuery);
```  
  

### Delete
```
int affected = db.Query("Books").Where("Status", "inactive").Delete();   
```  
   

## Fetching Records   
SqlKata는 쿼리 실행을 돕기 위해 다음과 같은 메서드를 제공합니다:  
- Get()
- First()
- FirstOrDefault()
- Paginate()
- Chunk()  
   

### 레코드 검색
기본적으로 Get 메서드를 호출하면 `IEnumerable<dynamic>` 이 반환되므로 최대한의 유연성을 제공합니다.  
```
var db = new QueryFactory(connection, new SqlServerCompiler()); 
IEnumerable<dynamic> users = db.Query("Users").Get();
```  
  
그러나 강력한 유형을 선호하는 경우 일반 오버로드를 대신 사용할 수 있습니다.  
```
IEnumerable<User< users = db.Query("Users").Get<User>();
```  
  

### 하나의 레코드 가져오기
쿼리의 첫 번째 레코드를 가져오려면 First 또는 FirstOrDefault를 사용합니다.  
```
var book = db.Query("Books").Where("Id", 1).First<Book>();
```  

주: First 및 FirstOrDefault는 Limit(1) 절을 쿼리에 암시적으로 추가하므로 직접 추가할 필요가 없습니다.  
  


### 데이터 페이지 매기기
데이터 페이지 매김을 하려면 Get 대신 Paginate(pageNumber, perPage?) 메서드를 사용하세요.  
  
Paginate 메서드는 페이지 번호(1 기준)와 기본값이 25인 선택적 페이지당의 두 매개 변수를 허용하고 PaginationResult 형식의 인스턴스를 반환합니다.  
  
PaginationResult는 반환된 데이터를 안전하게 반복할 수 있도록 Enumerable 인터페이스를 구현하는 Each 속성을 노출합니다.  
```
var users = query.Paginate(1, 10);

foreach(var user in users.Each)
{
    Console.WriteLine($"Id: {user.Id}, Name: {user.Name}");
}
```  
  

### 다음 및 이전
다음 및 이전 메서드를 호출하여 각각 다음/이전 페이지를 가져올 수 있습니다.  
```
var page1 = query.Paginate(1);

foreach(var item in page1.Each)
{
    // print items in the first page
}

var page2 = page1.Next(); // same as query.Paginate(2)

foreach(var item in page2.Each)
{
    // print items in the 2nd page
}
```  
     
   
### 다음 및 이전 쿼리
때로는 다음 및 이전 메서드에 대한 기본 쿼리에 액세스해야 할 수 있습니다. 이 경우 각각 다음 쿼리 및 이전 쿼리를 사용합니다.  

쿼리에 액세스하는 것이 추가적인 제약 조건을 추가하는 등 더 많은 제어를 원하는 경우 더 유용할 수 있습니다.  
```
var currentPage = query.Paginate(1, 10);

foreach(var item in currentPage.Each)
{
    // print all books in the first page
}

var publishedInPage2 = currentPage.NextQuery().WhereTrue("IsPublished").Get();

foreach(var item in publishedInPage2.Each)
{
    // print published books only in page 2
}
```  
  

### 모든 레코드 반복 예제
이 예제는 실제 사례에서는 사용되지 않을 수 있으며, 이러한 기능이 필요한 경우 대신 Chunk 메서드를 사용하십시오.  
```
var currentPage = db.Query("Books").OrderBy("Date").Paginate(1);

while(currentPage.HasNext)
{
    Console.WriteLine($"Looping over the page: {currentPage.Page}");

    foreach(var book in currentPage.Each)
    {
        // process book
    }

    currentPage = currentPage.Next();
}
```   
   

### 데이터 청크
전체 테이블이 메모리에 한 번 로드되는 것을 방지하기 위해 데이터를 청크로 검색하고 싶을 때가 있는데, 이 경우 청크 메서드를 사용할 수 있습니다.
  
이 방법은 수천 개의 레코드가 있는 상황에서 유용합니다.   
```
query.Chunk(100, (rows, page) => {

    Console.WriteLine($"Fetching page: {page}");

    foreach(var row in rows)
    {
        // do something with row
    }

});
```  
   
청크 검색을 중지하려면 `Chunk(int chunkSize, Func<IEnumerable<dynamic>, int, bool> func)` 오버로드를 사용하고 호출된 액션에서 false를 반환하기만 하면 됩니다.    
```
query.Chunk(100, (rows, page) => {

    // process rows

    if(page == 3) {

        // stop retrieving other chunks
        return false;

    }

    // return true to continue
    return true;

```  
  
  
### Execute Raw Statements
QueryFactory.Select 및 QueryFactory.Statement 메서드를 사용합니다.  
```
var users = db.Select("exec sp_get_users_by_date @date", new {date = DateTime.UtcNow}); 
```  
      
QueryFactory.Statement를 사용하면 truncate table, create database 등과 같은 임의의 문을 실행할 수 있습니다.   
```
db.Statement("truncate table Users");
```  


