Directory structure:
└── GameAPIServer_Template/
    ├── README.md
    ├── DB_Schema.md
    ├── ErrorCode.cs
    ├── GameAPIServer.csproj
    ├── GameAPIServer.sln
    ├── Program.cs
    ├── Security.cs
    ├── apiTest.http
    ├── appsettings.Development.json
    ├── appsettings.json
    ├── Controllers/
    │   ├── AttendanceCheckController.cs
    │   ├── AttendanceInfoController.cs
    │   ├── CreateAccountController.cs
    │   ├── LoginController.cs
    │   ├── LogoutController.cs
    │   ├── MailDeleteController.cs
    │   ├── MailListController.cs
    │   ├── MailReceiveController.cs
    │   ├── TopRankingController.cs
    │   ├── UserDataLoadController.cs
    │   └── UserRankController.cs
    ├── DTOs/
    │   ├── AttendanceCheck.cs
    │   ├── AttendanceInfo.cs
    │   ├── CreateAccount.cs
    │   ├── ErrorCode.cs
    │   ├── FreindDelete.cs
    │   ├── FriendAccept.cs
    │   ├── FriendAdd.cs
    │   ├── FriendList.cs
    │   ├── GameDataLoad.cs
    │   ├── Header.cs
    │   ├── Login.cs
    │   ├── Logout.cs
    │   ├── MailDelete.cs
    │   ├── MailList.cs
    │   ├── MailReceive.cs
    │   ├── OtherUserInfo.cs
    │   ├── Ranking.cs
    │   ├── SocialDataLoad.cs
    │   ├── UserDataLoad.cs
    │   ├── UserRank.cs
    │   └── UserSetMainChar.cs
    ├── Middleware/
    │   ├── CheckUserAuth.cs
    │   └── VersionCheck.cs
    ├── Models/
    │   ├── Account.cs
    │   ├── Attendance.cs
    │   ├── Friend.cs
    │   ├── Game.cs
    │   ├── Item.cs
    │   ├── Mailbox.cs
    │   ├── MasterDB.cs
    │   ├── RedisDB.cs
    │   └── User.cs
    ├── Properties/
    │   └── launchSettings.json
    ├── Repository/
    │   ├── GameDB_Attendance.cs
    │   ├── GameDB_Game.cs
    │   ├── GameDB_Mail.cs
    │   ├── GameDB_User.cs
    │   ├── MasterDb.cs
    │   ├── MemoryDBDefine.cs
    │   ├── MemoryDb.cs
    │   ├── MemoryDbKeyMaker.cs
    │   └── Interfaces/
    │       ├── IGameDb.cs
    │       ├── IMasterDb.cs
    │       └── IMemoryDb.cs
    └── Services/
        ├── AttendanceService.cs
        ├── AuthService.cs
        ├── DataLoadService.cs
        ├── GameService.cs
        ├── ItemService.cs
        ├── MailService.cs
        ├── UserService.cs
        └── Interfaces/
            ├── IAttendanceService.cs
            ├── IAuthService.cs
            ├── IDataLoadService.cs
            ├── IGameService.cs
            ├── IItemService.cs
            ├── IMailService.cs
            └── IUserService.cs

================================================
File: README.md
================================================
# GameAPIServer Template

ASP.NET Core 기반 게임 API 서버 템플릿. 이 코드를 참고하여 새 프로젝트를 만들되, 더 좋은 코드를 작성하는 것을 목표로 한다.

## 기술 스택

- .NET 9.0 / ASP.NET Core Web API
- MySQL (MySqlConnector + SqlKata)
- Redis (CloudStructures)
- ZLogger (구조화 로깅)

## 프로젝트 구조

```
GameAPIServer_Template/
├── Controllers/          # API 엔드포인트 (9개)
├── DTOs/                 # 요청/응답 데이터 모델
├── Middleware/            # 미들웨어 (버전 체크, 인증)
├── Models/               # DB 엔티티 모델
├── Repository/           # DB 접근 계층 (GameDb, MasterDb, MemoryDb)
│   └── Interfaces/
├── Services/             # 비즈니스 로직
│   └── Interfaces/
├── ErrorCode.cs          # 에러 코드 정의 (UInt16 enum)
├── Security.cs           # 보안 유틸리티
├── Program.cs            # 앱 진입점, DI 설정
├── appsettings.json      # 운영 설정
├── appsettings.Development.json  # 개발 설정
├── DB_Schema.md          # DB 스키마 정의
└── apiTest.http          # API 테스트 파일
```

## API 엔드포인트

모든 엔드포인트는 **POST** 메서드를 사용한다. `Login`, `CreateAccount`를 제외한 모든 요청에는 헤더에 `uid`, `token`, `AppVersion`, `MasterDataVersion`이 필요하다.

| 엔드포인트 | 기능 |
|-----------|------|
| `/CreateAccount` | 계정 생성 |
| `/Login` | 로그인 (인증 토큰 발급) |
| `/Logout` | 로그아웃 |
| `/UserDataLoad` | 유저 데이터 로드 |
| `/AttendanceCheck` | 출석 체크 |
| `/AttendanceInfo` | 출석 정보 조회 |
| `/MailList` | 우편함 목록 |
| `/MailReceive` | 우편 수령 |
| `/MailDelete` | 우편 삭제 |

## 미들웨어

1. **VersionCheck** — `AppVersion`, `MasterDataVersion` 헤더를 검증. 버전 불일치 시 HTTP 426 반환.
2. **CheckUserAuthAndLoadUserData** — Redis에서 토큰 검증, 유저 요청 락 처리. `Login`/`CreateAccount`는 건너뜀.

## 서비스 (DI 등록)

| 서비스 | Lifetime | 역할 |
|--------|----------|------|
| IAuthService | Transient | 인증, 계정 관리 |
| IGameService | Transient | 게임 초기화 |
| IItemService | Transient | 아이템 관리 |
| IMailService | Transient | 우편 시스템 |
| IUserService | Transient | 유저 프로필 |
| IAttendanceService | Transient | 출석 체크 |
| IDataLoadService | Transient | 데이터 로드 |
| IGameDb | Transient | 게임 DB 접근 |
| IMemoryDb | Singleton | Redis 접근 |
| IMasterDb | Singleton | 마스터 데이터 |

## 설정 (appsettings.json)

```json
{
  "ServerAddress": "http://localhost:11500",
  "logdir": "./log/",
  "DbConfig": {
    "Redis": "localhost",
    "GameDb": "Server=localhost;Port=3306;user=root;Password=<비밀번호>;Database=game_db;...",
    "MasterDb": "Server=localhost;Port=3306;user=root;Password=<비밀번호>;Database=master_db;..."
  }
}
```

## 에러 코드 범위

| 범위 | 카테고리 |
|------|---------|
| 1000~1999 | 공통 (Redis, 토큰, 버전) |
| 2000~2999 | 인증, 친구 |
| 3000~3999 | 아이템 |
| 4000~4999 | 게임 DB |
| 5000~5999 | 마스터 DB |
| 6000~6999 | 유저 관리 |
| 8000~8999 | 우편 |
| 9000~9999 | 출석 |

## DB 설정

`DB_Schema.md`를 참고하여 MySQL에 `game_db`, `master_db` 데이터베이스와 테이블을 생성한다.

## 빌드 및 실행

```bash
dotnet build
dotnet run
# 서버 주소: http://localhost:11500
```



================================================
File: DB_Schema.md
================================================
아래 사용 예를 참고하여 만드는 게임에 맞게 DB 스키마 정보를 만들도록 한다.  
사용하지 않는 것은 삭제한다.  
  
   
# game DB
데이터베이스 이름: `game_db`  
  
## account_info 테이블
하이브 계정 정보를 가지고 있는 테이블
```sql
-- 테이블 생성 SQL - account
CREATE TABLE account
(
    `uid`         BIGINT          NOT NULL    AUTO_INCREMENT COMMENT '유니크 유저 번호',
    `user_id`           VARCHAR(50)     NOT NULL    COMMENT '유저아이디.
    `salt_value`        VARCHAR(100)    NOT NULL    COMMENT '암호화 값',
    `pw`                VARCHAR(100)    NOT NULL    COMMENT '해싱된 비밀번호',
    `create_dt`         DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP COMMENT '생성 일시',
     PRIMARY KEY (uid),
     UNIQUE KEY (user_id)
)
```


## user_money 테이블
유저의 재화 정보를 가지고 있는 테이블
```sql
-- 테이블 생성 SQL - user_money
CREATE TABLE user_money
(
    `uid`         BIGINT    NOT NULL    COMMENT '유저아이디', 
    `jewelry`     INT    NOT NULL    DEFAULT 0 COMMENT '보석', 
    `gold_medal`  INT    NOT NULL    DEFAULT 0 COMMENT '금 메달', 
    `cash`        INT    NOT NULL    DEFAULT 0 COMMENT '현금', 
    `sunchip`     INT    NOT NULL    DEFAULT 0 COMMENT '썬칩', 
     PRIMARY KEY (uid)
);
-- Foreign Key 설정 SQL - user_money(uid) -> user(uid)
ALTER TABLE user_money
    ADD CONSTRAINT FK_user_money_uid_user_uid FOREIGN KEY (uid)
        REFERENCES user (uid) ON DELETE RESTRICT ON UPDATE RESTRICT;
```
  
    
## mailbox 테이블
우편함 정보를 가지고 있는 테이블
```sql
-- 테이블 생성 SQL - mailbox
CREATE TABLE mailbox
(
    `mail_seq`    INT             NOT NULL    AUTO_INCREMENT COMMENT '우편 일련번호', 
    `uid`         BIGINT             NOT NULL    COMMENT '유저아이디', 
    `mail_title`  VARCHAR(100)    NOT NULL    COMMENT '우편 제목', 
    `create_dt`   DATETIME        NOT NULL    COMMENT '생성 일시', 
    `expire_dt`   DATETIME        NOT NULL    COMMENT '만료 일시', 
    `receive_dt`  DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP COMMENT '수령 일시', 
    `receive_yn`  TINYINT         NOT NULL    DEFAULT 0 COMMENT '수령 유무',
     PRIMARY KEY (mail_seq)
);
-- Foreign Key 설정 SQL - mailbox(uid) -> user(uid)
ALTER TABLE mailbox
    ADD CONSTRAINT FK_mailbox_uid_user_uid FOREIGN KEY (uid)
        REFERENCES user (uid) ON DELETE RESTRICT ON UPDATE RESTRICT;
```

## mailbox_reward 테이블
우편함의 보상정보를 가지고 있는 테이블
```sql
-- 테이블 생성 SQL - mailbox_reward
CREATE TABLE mailbox_reward
(
    `mail_seq`     INT            NOT NULL    COMMENT '우편 일련번호', 
    `reward_key`   INT            NOT NULL    COMMENT '보상 키', 
    `reward_qty`   INT            NOT NULL    COMMENT '보상 수', 
    `reward_type`  VARCHAR(20)    NOT NULL    COMMENT '보상 타입', 
     PRIMARY KEY (mail_seq, reward_key)
);

-- Foreign Key 설정 SQL - mailbox_reward(mail_seq) -> mailbox(mail_seq)
ALTER TABLE mailbox_reward
    ADD CONSTRAINT FK_mailbox_reward_mail_seq_mailbox_mail_seq FOREIGN KEY (mail_seq)
        REFERENCES mailbox (mail_seq) ON DELETE RESTRICT ON UPDATE RESTRICT;
```

## user_attendance 테이블
유저의 출석 현황을 가지고 있는 테이블
```sql
-- 테이블 생성 SQL - user_attendance
CREATE TABLE user_attendance
(
    `uid`                   BIGINT         NOT NULL    COMMENT '유저아이디', 
    `attendance_cnt`        INT         NOT NULL    COMMENT '출석 횟수', 
    `recent_attendance_dt`  DATETIME    NOT NULL    COMMENT '최근 출석 일시', 
     PRIMARY KEY (uid)
);
-- Foreign Key 설정 SQL - user_attendance(uid) -> user(uid)
ALTER TABLE user_attendance
    ADD CONSTRAINT FK_user_attendance_uid_user_uid FOREIGN KEY (uid)
        REFERENCES user (uid) ON DELETE RESTRICT ON UPDATE RESTRICT;
```
    
    


<br>
<br>

# master DB
## version 테이블
앱버전과 데이터 버전을 가지고 있는 테이블
```sql
-- 테이블 생성 SQL - version
CREATE TABLE version
(
    `app_version`            INT         NOT NULL    COMMENT '앱 버전', 
    `master_data_version`    INT         NOT NULL    COMMENT '마스터 데이터 버전', 
);
```
      

## master_gacha_reward 테이블
가챠 보상의 확률 정보와 뽑는 수량을 가지고 있는 테이블
```sql
-- 테이블 생성 SQL - master_gacha_reward
CREATE TABLE master_gacha_reward
(
    `gacha_reward_key`        INT            NOT NULL    COMMENT '가챠 보상 키', 
    `gacha_reward_name`       VARCHAR(50)    NOT NULL    COMMENT '가챠 보상 이름', 
    `char_prob_percent`       INT            NOT NULL    COMMENT '캐릭터 확률 퍼센트', 
    `skin_prob_percent`       INT            NOT NULL    COMMENT '스킨 확률 퍼센트', 
    `costume_prob_percent`    INT            NOT NULL    COMMENT '코스튬 확률 퍼센트', 
    `food_prob_percent`       INT            NOT NULL    COMMENT '푸드 확률 퍼센트', 
    `food_gear_prob_percent`  INT            NOT NULL    COMMENT '푸드 기어 확률 퍼센트', 
    `gacha_count`             INT            NOT NULL    COMMENT '가챠 개수', 
    `create_dt`               DATETIME       NOT NULL    COMMENT '생성 일시', 
     PRIMARY KEY (gacha_reward_key)
);
```

## master_gacha_reward_list 테이블
가챠 보상에 포함되는 보상들의 정보를 가지고 있는 테이블
```sql
-- 테이블 생성 SQL - master_gacha_reward_list
CREATE TABLE master_gacha_reward_list
(
    `gacha_reward_key`  INT            NOT NULL    COMMENT '가챠 보상 키', 
    `reward_key`        INT            NOT NULL    COMMENT '보상 키', 
    `reward_type`       VARCHAR(20)    NOT NULL    COMMENT '보상 종류',
    `reward_qty`        INT             NOT NULL   DEFAULT 1 COMMENT '보상 수',
    `create_dt`         DATETIME       NOT NULL    COMMENT '생성 일시', 
     PRIMARY KEY (gacha_reward_key, reward_key)
);
-- Foreign Key 설정 SQL - master_gacha_reward_list(gacha_reward_key) -> master_gacha_reward(gacha_reward_key)
ALTER TABLE master_gacha_reward_list
    ADD CONSTRAINT FK_master_gacha_reward_list_gacha_reward_key_master_gacha_reward FOREIGN KEY (gacha_reward_key)
        REFERENCES master_gacha_reward (gacha_reward_key) ON DELETE RESTRICT ON UPDATE RESTRICT;
```

## master_attendance_reward 테이블
출석 보상 정보를 가지고 있는 테이블
```sql
-- 테이블 생성 SQL - master_attendance_reward
CREATE TABLE master_attendance_reward
(
    `day_seq`     INT         NOT NULL    COMMENT '날짜 번호', 
    `reward_key`  INT         NOT NULL    COMMENT '보상 키', 
    `reward_qty`  INT         NOT NULL    DEFAULT 0 COMMENT '보상 수',
    `reward_type` VARCHAR(20) NOT NULL    COMMENT '보상 종류',
    `create_dt`   DATETIME    NOT NULL    COMMENT '생성 일시', 
     PRIMARY KEY (day_seq)
);
```

## master_item_level 테이블
아이템(캐릭터, 코스튬, 푸드) 레벨업을 위한 개수의 정보를 가지고 있는 테이블
```sql
-- 테이블 생성 SQL - master_item_level
CREATE TABLE master_item_level
(
    `level`     INT            NOT NULL    COMMENT '레벨', 
    `item_cnt`  VARCHAR(50)    NOT NULL    DEFAULT 1 COMMENT '아이템 개수', 
     PRIMARY KEY (level)
);
```  
  


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
    TokenDoesNotExist = 1004,
    UidDoesNotExist = 1005,
    AuthTokenFailWrongAuthToken = 1006,
    Hive_Fail_InvalidResponse = 1010,
    InValidAppVersion = 1011,
    InvalidMasterDataVersion = 1012,

    // Auth 2000 ~
    CreateUserFailException = 2001,
    CreateUserFailNoNickname = 2002,
    CreateUserFailDuplicateNickname = 2003,
    LoginFailException = 2004,
    LoginFailUserNotExist = 2005,
    LoginFailPwNotMatch = 2006,
    LoginFailSetAuthToken = 2007,
    LoginUpdateRecentLoginFail = 2008,
    LoginUpdateRecentLoginFailException = 2009,
    AuthTokenMismatch = 2010,
    AuthTokenKeyNotFound = 2011,
    AuthTokenFailWrongKeyword = 2012,
    AuthTokenFailSetNx = 2013,
    AccountIdMismatch = 2014,
    DuplicatedLogin = 2015,
    CreateUserFailInsert = 2016,
    LoginFailAddRedis = 2017,
    CheckAuthFailNotExist = 2018,
    CheckAuthFailNotMatch = 2019,
    CheckAuthFailException = 2020,
    LogoutRedisDelFail = 2021,
    LogoutRedisDelFailException= 2022,
    DeleteAccountFail = 2023,
    DeleteAccountFailException = 2024,
    InitNewUserGameDataFailException = 2025,
    InitNewUserGameDataFailCharacter = 2026,
    InitNewUserGameDataFailGameList = 2027,
    InitNewUserGameDataFailMoney = 2028,
    InitNewUserGameDataFailAttendance = 2029,
    CreateAccountFailInsert = 2051,
    CreateAccountFailException = 2052,

    // Friend 2100
    FriendSendReqFailUserNotExist = 2101,
    FriendSendReqFailInsert = 2102,
    FriendSendReqFailException = 2103,
    FriendSendReqFailAlreadyExist = 2104,
    SendFriendReqFailSameUid = 2105,
    FriendGetListFailOrderby = 2106,
    FriendGetListFailException = 2107,
    FriendGetRequestListFailException = 2108,
    FriendDeleteFailNotFriend = 2109,
    FriendDeleteFailDelete = 2110,
    FriendDeleteFailException = 2111,
    FriendDeleteFailSameUid = 2112,
    FriendDeleteReqFailNotFriend = 2113,
    FriendDeleteReqFailDelete = 2114,
    FriendDeleteReqFailException = 2115,
    FriendAcceptFailException = 2116,
    FriendAcceptFailSameUid = 2117,
    AcceptFriendRequestFailUserNotExist = 2118,
    AcceptFriendRequestFailAlreadyFriend = 2119,
    AcceptFriendRequestFailException = 2120,
    FriendSendReqFailNeedAccept = 2121,

    // Game 2200
    MiniGameListFailException = 2201,
    GameSetNewUserListFailException = 2202,
    GameSetNewUserListFailInsert = 2203,
    MiniGameUnlockFailInsert = 2204,
    MiniGameUnlockFailException = 2205,
    MiniGameInfoFailException = 2206,
    MiniGameSaveFailException = 2207,
    MiniGameSaveFailGameLocked = 2208,
    MiniGameUnlockFailAlreadyUnlocked = 2209,
    MiniGameSetPlayCharFailUpdate = 2210,
    MiniGameSetPlayCharFailException = 2211,
    MiniGameSaveFailFoodDecrement = 2212,

    SetUserScoreFailException = 2301,
    GetRankingFailException = 2302,
    GetUserRankFailException = 2303,

    // Item 3000 ~
    CharReceiveFailInsert = 3011,
    CharReceiveFailLevelUP = 3012,
    CharReceiveFailIncrementCharCnt = 3013,
    CharReceiveFailException= 3014,
    CharListFailException = 3015,
    CharNotExist = 3016,
    CharSetCostumeFailUpdate = 3017,
    CharSetCostumeFailException = 3018,

    SkinReceiveFailAlreadyOwn = 3021,
    SkinReceiveFailInsert = 3022,
    SkinReceiveFailException = 3023,
    SkinListFailException = 3024,

    CostumeReceiveFailInsert = 3031,
    CostumeReceiveFailLevelUP = 3032,
    CostumeReceiveFailIncrementCharCnt = 3033,
    CostumeReceiveFailException = 3034,
    CostumeListFailException = 3035,
    CharSetCostumeFailHeadNotExist= 3036,
    CharSetCostumeFailFaceNotExist = 3037,
    CharSetCostumeFailHandNotExist = 3038,

    FoodReceiveFailInsert = 3041,
    FoodReceiveFailIncrementFoodQty = 3042,
    FoodReceiveFailException = 3043,
    FoodListFailException = 3044,
    FoodGearReceiveFailInsert = 3045,
    FoodGearReceiveFailIncrementFoodGear = 3046,
    FoodGearReceiveFailException = 3047,

    GachaReceiveFailException= 3051,


    //GameDb 4000~ 
    GetGameDbConnectionFail = 4002,


    // MasterDb 5000 ~
    MasterDB_Fail_LoadData = 5001,
    MasterDB_Fail_InvalidData = 5002,

    // User
    UserInfoFailException = 6001,
    UserMoneyInfoFailException = 6002,
    UserUpdateJewelryFailIncremnet = 6003,
    SetMainCharFailException = 6004,
    GetOtherUserInfoFailException = 6005,
    UserNotExist = 6006,

    // Mail
    MailListFailException = 8001,
    MailReceiveFailException = 8002,
    MailReceiveFailAlreadyReceived = 8003,
    MailReceiveFailMailNotExist = 8004,
    MailReceiveFailUpdateReceiveDt = 8005,
    MailRewardListFailException = 8006,
    MailDeleteFailDeleteMail = 8007,
    MailDeleteFailDeleteMailReward = 8008,
    MailDeleteFailException = 8009,
    MailReceiveFailNotMailOwner = 8010,
    MailReceiveRewardsFailException = 8011,

    // Attendance
    AttendanceInfoFailException = 9001,
    AttendanceCheckFailAlreadyChecked = 9002,
    AttendanceCheckFailException = 9003,

    GetRewardFailException = 9004,
}


================================================
File: GameAPIServer.csproj
================================================
<Project Sdk="Microsoft.NET.Sdk.Web">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>

    <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
        <OutputPath>..\00_ServerBin\GameAPIServer\</OutputPath>
    </PropertyGroup>

    <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|AnyCPU'">
        <OutputPath>..\00_ServerBin\GameAPIServer\</OutputPath>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="CloudStructures" Version="3.3.0" />
        <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.1" />
        <PackageReference Include="MySqlConnector" Version="2.3.7" />
        <PackageReference Include="SqlKata" Version="2.4.0" />
        <PackageReference Include="SqlKata.Execution" Version="2.4.0" />
        <PackageReference Include="System.Configuration.ConfigurationManager" Version="8.0.0" />
        <PackageReference Include="System.Net.Security" Version="4.3.2" />
        <PackageReference Include="ZLogger" Version="2.4.1" />
    </ItemGroup>

</Project>



================================================
File: GameAPIServer.sln
================================================
﻿
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.8.34330.188
MinimumVisualStudioVersion = 10.0.40219.1
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "GameAPIServer", "GameAPIServer.csproj", "{C4BF4730-21F7-4F00-A236-706420265F0D}"
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
		{EDAEE952-47EB-4524-B8C9-00C73A782988}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{EDAEE952-47EB-4524-B8C9-00C73A782988}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{EDAEE952-47EB-4524-B8C9-00C73A782988}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{EDAEE952-47EB-4524-B8C9-00C73A782988}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal



================================================
File: Program.cs
================================================
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



================================================
File: Security.cs
================================================
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GameAPIServer;

public class Security
{
    const String AllowableCharacters = "abcdefghijklmnopqrstuvwxyz0123456789";

    public static String MakeHashingPassWord(String saltValue, String pw)
    {
        var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.ASCII.GetBytes(saltValue + pw));
        var stringBuilder = new StringBuilder();
        foreach (var b in hash)
        {
            stringBuilder.AppendFormat("{0:x2}", b);
        }

        return stringBuilder.ToString();
    }

    public static String SaltString()
    {
        var bytes = new Byte[64];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }

        return new String(bytes.Select(x => AllowableCharacters[x % AllowableCharacters.Length]).ToArray());
    }

    public static String CreateAuthToken()
    {
        var bytes = new Byte[25];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }

        return new String(bytes.Select(x => AllowableCharacters[x % AllowableCharacters.Length]).ToArray());
    }

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
  "ID":"jacking751",
  "PW":"123qwe",
  "NickName": "aaa"
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
  "ServerAddress": "http://localhost:11500",
  "HiveServerAddress": "http://localhost:11501",
  "logdir": "./log/",
  "DbConfig": {
    "Redis": "localhost",
    "GameDb": "Server=localhost;Port=3306;user=root;Password=sykim2312;Database=game_db;Pooling=true;Min Pool Size=0;Max Pool Size=100;AllowUserVariables=True;",
    "MasterDb": "Server=localhost;Port=3306;user=root;Password=sykim2312;Database=master_db;Pooling=true;Min Pool Size=0;Max Pool Size=100;AllowUserVariables=True;"
  }
}



================================================
File: appsettings.json
================================================
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Debug"
    },
    "ZLoggerConsole": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "AllowedHosts": "*",
  "ServerAddress": "http://localhost:11500",
  "HiveServerAddress": "http://localhost:11501",
  "logdir": "./log/",
  "DbConfig": {
    "Redis": "localhost",
    "GameDb": "Server=localhost;Port=3306;user=root;Password=123qwe;Database=game_db;Pooling=true;Min Pool Size=0;Max Pool Size=100;AllowUserVariables=True;",
    "MasterDb": "Server=localhost;Port=3306;user=root;Password=123qwe;Database=master_db;Pooling=true;Min Pool Size=0;Max Pool Size=100;AllowUserVariables=True;"
  }
}



================================================
File: Controllers/AttendanceCheckController.cs
================================================
﻿using GameAPIServer.DTOs;
using GameAPIServer.Servicies.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class AttendanceCheckController : ControllerBase
{
    readonly ILogger<AttendanceCheckController> _logger;
    readonly IAttendanceService _attendanceService;

    public AttendanceCheckController(ILogger<AttendanceCheckController> logger, IAttendanceService attendanceService)
    {
        _logger = logger;
        _attendanceService = attendanceService;
    }

    /// <summary>
    /// 출석 체크 API </br>
    /// 출석 체크를 하고 받은 보상을 반환합니다.
    /// </summary>
    [HttpPost]
    public async Task<AttendanceCheckResponse> CheckAttendance([FromHeader] Header header)
    {
        AttendanceCheckResponse response = new();
                              
        (response.Result, response.Rewards) = await _attendanceService.CheckAttendanceAndReceiveRewards(header.Uid);

        _logger.ZLogInformation($"[AttendanceCheck] Uid : {header.Uid}");
        return response;
    }

}



================================================
File: Controllers/AttendanceInfoController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class AttendanceInfoController : ControllerBase
{
    readonly ILogger<AttendanceInfoController> _logger;
    readonly IAttendanceService _attendanceService;

    public AttendanceInfoController(ILogger<AttendanceInfoController> logger, IAttendanceService attendanceService)
    {
        _logger = logger;
        _attendanceService = attendanceService;
    }

    /// <summary>
    /// 출석 정보 API </br>
    /// 유저의 출석 정보(누적 출석일, 최근 출석 일시)를 전달합니다.
    /// </summary>
    [HttpPost]
    public async Task<AttendanceInfoResponse> GetAttendanceInfo([FromHeader] Header header)
    {
        AttendanceInfoResponse response = new();

        (response.Result, response.AttendanceInfo) = await _attendanceService.GetAttendanceInfo(header.Uid);
        
        _logger.ZLogInformation($"[AttendanceInfo] Uid : {header.Uid}");
        return response;
    }
}



================================================
File: Controllers/CreateAccountController.cs
================================================
癤퓎sing System.Threading.Tasks;
using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;


namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class CreateAccountController : ControllerBase
{
    readonly ILogger<CreateAccountController> _logger;
    readonly IAuthService _authService;
    readonly IGameService _gameService;

    public CreateAccountController(ILogger<CreateAccountController> logger, IAuthService authService, IGameService gameService)
    {
        _logger = logger;
        _authService = authService;
        _gameService = gameService;
    }

    [HttpPost]
    public async Task<CreateHiveAccountResponse> Create([FromBody]CreateHiveAccountRequest request)
    {
        CreateHiveAccountResponse response = new();
        Int64 uid = 0;

        (response.Result, uid) = await _authService.CreateAccount(request.UserID, request.Password);


        if (response.Result != ErrorCode.None)
        {
            response.Result = await _gameService.InitNewUserGameData(uid);
        }


        return response;
    }

    

    
}




================================================
File: Controllers/LoginController.cs
================================================
﻿using System.Threading.Tasks;
using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZLogger;


namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController : ControllerBase
{
    readonly IMemoryDb _memoryDb;
    readonly ILogger<LoginController> _logger;
    readonly IAuthService _authService;
 
    public LoginController(ILogger<LoginController> logger, IMemoryDb memoryDb, IAuthService authService)
    {
        _logger = logger;
        _memoryDb = memoryDb;
        _authService = authService;
    }

    /// <summary>
    /// 로그인 API </br>
    /// 하이브 토큰을 검증하고, 유저가 없다면 생성, 토큰 발급, 로그인 시간 업데이트, 유저 데이터 로드를 합니다. 
    /// </summary>
    [HttpPost]
    public async Task<LoginResponse> Login(LoginRequest request)
    {
        LoginResponse response = new();

        //TODO: 컨트룰러에 구현 코드가 너무 많이 노출 되어 있다. 아래 코드에서는 서비스 객체의 메소드를 여러개 호출하고 있는데 1개 정도의 메소드로 묶어서 호출하는 방법을 생각해보자.
        // 즉 구현은 대부분 서비스 객체에 있어야 하고, 컨트룰러는 필요한 서비스 객체를 호출하고, 응답만 보내는 역할을 해야 한다.

        
        //하이브 토큰 체크
        var (errorCode,uid, authToken) = await _authService.Login(request.UserID, request.Password);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        response.Result = errorCode;
        response.AuthToken = authToken;

        
        //TODO: 첫 로그인 유저인 경우 아직 게임데이터가 생성 되어 있지 않으니 여기서 생성하도록 한다.
        _logger.ZLogInformation($"[Login] Uid : {uid}, Token : {authToken}");
        return response;
    }
}



================================================
File: Controllers/LogoutController.cs
================================================
﻿using System.Threading.Tasks;
using GameAPIServer.Repository.Interfaces;
using GameAPIServer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class LogoutController : ControllerBase
{
    readonly IMemoryDb _memoryDb;
    readonly ILogger<LogoutController> _logger;

    public LogoutController(ILogger<LogoutController> logger, IMemoryDb memoryDb)
    {
        _logger = logger;
        _memoryDb = memoryDb;
    }

    /// <summary>
    /// 로그아웃 API </br>
    /// 해당 유저의 토큰을 Redis에서 삭제합니다.
    /// </summary>
    [HttpPost]
    public async Task<LogoutResponse> DeleteUserToken([FromHeader] Header request)
    {
        LogoutResponse response = new();
        var errorCode = await _memoryDb.DelUserAuthAsync(request.Uid);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformation($"[Logout] Uid : {request.Uid}");
        return response;
    }
}



================================================
File: Controllers/MailDeleteController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class MailDeleteController : ControllerBase
{
    readonly ILogger<MailDeleteController> _logger;
    readonly IMailService _mailService;

    public MailDeleteController(ILogger<MailDeleteController> logger, IMailService mailService)
    {
        _logger = logger;
        _mailService = mailService;
    }

    /// <summary>
    /// 메일 삭제 API
    /// 메일함에서 메일을 삭제합니다.
    /// </summary>
    [HttpPost]
    public async Task<MailDeleteResponse> DeleteMail([FromHeader] Header header, MailDeleteRequest request)
    {
        MailDeleteResponse response = new();

        response.Result = await _mailService.DeleteMail(header.Uid, request.MailSeq);

        _logger.ZLogInformation($"[MailDelete] Uid : {header.Uid}");
        return response;
    }
}



================================================
File: Controllers/MailListController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class MailListController : ControllerBase
{
    readonly ILogger<MailListController> _logger;
    readonly IMailService _mailService;

    public MailListController(ILogger<MailListController> logger, IMailService mailService)
    {
        _logger = logger;
        _mailService = mailService;
    }

    /// <summary>
    /// 메일 목록 정보 API
    /// 유저의 메일 목록 정보를 가져옵니다.
    /// </summary>
    [HttpPost]
    public async Task<MailboxInfoResponse> GetMailList([FromHeader] Header header)
    {
        MailboxInfoResponse response = new();

        (response.Result, response.MailList) = await _mailService.GetMailList(header.Uid);

        _logger.ZLogInformation($"[MailList] Uid : {header.Uid}");
        return response;
    }
}



================================================
File: Controllers/MailReceiveController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class MailReceiveController : ControllerBase
{
    readonly ILogger<MailReceiveController> _logger;
    readonly IMailService _mailService;

    public MailReceiveController(ILogger<MailReceiveController> logger, IMailService mailService)
    {
        _logger = logger;
        _mailService = mailService;
    }

    /// <summary>
    /// 메일 보상 수령 API
    /// 메일에 포함된 보상을 모두 수령하고, 수령한 보상을 반환합니다.
    /// </summary>
    [HttpPost]
    public async Task<MailReceiveResponse> ReceiveMail([FromHeader] Header header, MailReceiveRequest request)
    {
        MailReceiveResponse response = new();

        (response.Result, response.Rewards) = await _mailService.ReceiveMail(header.Uid, request.MailSeq);

        _logger.ZLogInformation($"[MailReceive] Uid : {header.Uid}");
        return response;
    }
}



================================================
File: Controllers/TopRankingController.cs
================================================
﻿using GameAPIServer.Repository.Interfaces;
using GameAPIServer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class TopRankingController : ControllerBase
{
    readonly ILogger<TopRankingController> _logger;
    readonly IMemoryDb _memoryDb;

    public TopRankingController(ILogger<TopRankingController> logger, IMemoryDb memoryDb)
    {
        _logger = logger;
        _memoryDb = memoryDb;
    }

    /// <summary>
    /// 상위 랭킹 조회 API
    /// 상위 100명의 랭킹을 조회합니다.
    /// </summary>
    [HttpPost]
    public async Task<RankingResponse> GetTopRanking()
    {
        RankingResponse response = new();
    
        (response.Result, response.RankingData) = await _memoryDb.GetTopRanking();

        _logger.ZLogInformation($"[TopRanking] GetTopRanking");
        return response;
    }
}



================================================
File: Controllers/UserDataLoadController.cs
================================================
﻿using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class UserDataLoadController : ControllerBase
{
    readonly ILogger<UserDataLoadController> _logger;
    readonly IDataLoadService _dataLoadService;

    public UserDataLoadController(ILogger<UserDataLoadController> logger, IDataLoadService dataLoadService)
    {
        _logger = logger;
        _dataLoadService = dataLoadService;
    }

    /// <summary>
    /// 유저 데이터 로드 API
    /// 게임에 필요한 유저 정보(유저의 정보(점수,재화), 출석 정보)를 조회합니다.
    /// </summary>
    [HttpPost]
    public async Task<UserDataLoadResponse> LoadUserData([FromHeader] Header header)
    {
        UserDataLoadResponse response = new();

        (response.Result, response.UserData) = await _dataLoadService.LoadUserData(header.Uid);

        _logger.ZLogInformation($"[UserDataLoad] Uid : {header.Uid}");
        return response;
    }
}



================================================
File: Controllers/UserRankController.cs
================================================
﻿using GameAPIServer.Repository.Interfaces;
using GameAPIServer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class UserRankController : ControllerBase
{
    readonly ILogger<UserRankController> _logger;
    readonly IMemoryDb _memoryDb;
    
    public UserRankController(ILogger<UserRankController> logger, IMemoryDb memoryDb)
    {
        _logger = logger;
        _memoryDb = memoryDb;
    }

    /// <summary>
    /// 유저 랭킹 API
    /// 자신의 등수를 가져옵니다.
    /// </summary>
    [HttpPost]
    public async Task<UserRankResponse> GetUserRank([FromHeader] Header request)
    {
        var response = new UserRankResponse();

        (response.Result, response.Rank) = await _memoryDb.GetUserRankAsync(request.Uid);

        _logger.ZLogInformation($"[UserRank] Uid:{request.Uid}, Result:{response.Result}, Rank:{response.Rank}");
        return response;
    }
}



================================================
File: DTOs/AttendanceCheck.cs
================================================
癤퓎sing System.Collections.Generic;
using GameAPIServer.Models;

namespace GameAPIServer.DTOs;

public class AttendanceCheckResponse : ErrorCode
{
    public List<ReceivedReward> Rewards { get; set; }
}



================================================
File: DTOs/AttendanceInfo.cs
================================================
癤퓎sing GameAPIServer.Models;

namespace GameAPIServer.DTOs;

public class AttendanceInfoResponse : ErrorCode
{
    public GdbAttendanceInfo AttendanceInfo { get; set; }
}


================================================
File: DTOs/CreateAccount.cs
================================================
using System.ComponentModel.DataAnnotations;


namespace GameAPIServer.DTOs;

public class CreateHiveAccountRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "EMAIL CANNOT BE EMPTY")]
    [StringLength(50, ErrorMessage = "EMAIL IS TOO LONG")]
    [RegularExpression("^[a-zA-Z0-9_\\.-]+@([a-zA-Z0-9-]+\\.)+[a-zA-Z]{2,6}$", ErrorMessage = "E-mail is not valid")]
    public string UserID { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "PASSWORD CANNOT BE EMPTY")]
    [StringLength(30, ErrorMessage = "PASSWORD IS TOO LONG")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}


public class CreateHiveAccountResponse
{
    [Required]
    public global::ErrorCode Result { get; set; } = global::ErrorCode.None;
}



================================================
File: DTOs/ErrorCode.cs
================================================
癤퓆amespace GameAPIServer.DTOs;

public class ErrorCode
{
    public global::ErrorCode Result { get; set; } = global::ErrorCode.None;
}



================================================
File: DTOs/FreindDelete.cs
================================================
癤퓎sing System.ComponentModel.DataAnnotations;

namespace GameAPIServer.DTOs;

public class FriendDeleteRequest
{
    [Required]
    public int FriendUid { get; set; }
}


public class FriendDeleteResponse : ErrorCode
{
}





================================================
File: DTOs/FriendAccept.cs
================================================
癤퓎sing System.ComponentModel.DataAnnotations;

namespace GameAPIServer.DTOs;

public class FriendAcceptRequest
{
    [Required]
    public int FriendUid { get; set; }
}

public class FriendAcceptResponse : ErrorCode
{
}



================================================
File: DTOs/FriendAdd.cs
================================================
癤퓎sing System.ComponentModel.DataAnnotations;

namespace GameAPIServer.DTOs;

public class SendFriendReqRequest
{
    [Required]
    public int FriendUid { get; set; }
}


public class SendFriendReqResponse : ErrorCode
{
}





================================================
File: DTOs/FriendList.cs
================================================
癤퓎sing GameAPIServer.Models;
using System.Collections.Generic;


namespace GameAPIServer.DTOs;

public class FriendListResponse : ErrorCode
{
    public IEnumerable<GdbFriendInfo> FriendList { get; set; }
}



================================================
File: DTOs/GameDataLoad.cs
================================================
癤퓎sing GameAPIServer.Models;

namespace GameAPIServer.DTOs;

public class GameDataLoadResponse : ErrorCode
{
    public DataLoadGameInfo GameData { get; set; }
}

public class DataLoadGameInfo
{        
}

public class UserCharInfo
{
    public GdbUserCharInfo CharInfo { get; set; }
  
}



================================================
File: DTOs/Header.cs
================================================
癤퓎sing Microsoft.AspNetCore.Mvc;

namespace GameAPIServer.DTOs;

public class Header
{
    [FromHeader]
    public int Uid { get; set; }
}



================================================
File: DTOs/Login.cs
================================================
癤퓎sing System.ComponentModel.DataAnnotations;


namespace GameAPIServer.DTOs;

public class LoginRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "EMAIL CANNOT BE EMPTY")]
    [StringLength(50, ErrorMessage = "EMAIL IS TOO LONG")]
    [RegularExpression("^[a-zA-Z0-9_\\.-]+@([a-zA-Z0-9-]+\\.)+[a-zA-Z]{2,6}$", ErrorMessage = "E-mail is not valid")]
    public string UserID { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "PASSWORD CANNOT BE EMPTY")]
    [StringLength(30, ErrorMessage = "PASSWORD IS TOO LONG")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}

public class LoginResponse
{
    [Required] public global::ErrorCode Result { get; set; } = global::ErrorCode.None;
    [Required] public string AuthToken { get; set; } = "";
    [Required] public long Uid { get; set; } = 0;

    public DataLoadUserInfo userData { get; set; }
}


================================================
File: DTOs/Logout.cs
================================================
癤퓆amespace GameAPIServer.DTOs;

public class LogoutResponse : ErrorCode
{
}



================================================
File: DTOs/MailDelete.cs
================================================
癤퓎sing System.ComponentModel.DataAnnotations;

namespace GameAPIServer.DTOs;

public class MailDeleteRequest
{
    [Required]
    public int MailSeq { get; set; }
}
public class MailDeleteResponse : ErrorCode
{
}



================================================
File: DTOs/MailList.cs
================================================
癤퓎sing System.Collections.Generic;

namespace GameAPIServer.DTOs;

public class MailboxInfoResponse : ErrorCode
{
    public List<UserMailInfo> MailList { get; set; }
}



================================================
File: DTOs/MailReceive.cs
================================================
癤퓎sing System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GameAPIServer.Models;


namespace GameAPIServer.DTOs;


public class MailReceiveRequest
{
    [Required]
    public int MailSeq { get; set; }
}

public class MailReceiveResponse : ErrorCode
{
    public List<ReceivedReward> Rewards { get; set; }
}



================================================
File: DTOs/OtherUserInfo.cs
================================================
癤퓎sing System;
using System.ComponentModel.DataAnnotations;

namespace GameAPIServer.DTOs;

public class OtherUserInfoRequest
{
    [Required]
    public int Uid { get; set; }
}

public class OtherUserInfoResponse : ErrorCode
{
    public OtherUserInfo UserInfo { get; set; }
}

public class OtherUserInfo
{
    public int uid { get; set; }
    public string nickname { get; set; }
    public int total_bestscore { get; set; }
    public int total_bestscore_cur_season { get; set; }
    public int total_bestscore_prev_season { get; set; }
    public int main_char_key { get; set; }
    public int main_char_skin_key { get; set; }
    public string main_char_costume_json { get; set; }
    public long rank;
}



================================================
File: DTOs/Ranking.cs
================================================
癤퓎sing System.Collections.Generic;

namespace GameAPIServer.DTOs;

public class RankingResponse : ErrorCode
{
    public List<RankData> RankingData { get; set; }
}

public class RankData
{
    public long rank { get; set; }
    public int uid { get; set; }
    public int score { get; set; }
}



================================================
File: DTOs/SocialDataLoad.cs
================================================
癤퓎sing System.Collections.Generic;
using GameAPIServer.Models;


namespace GameAPIServer.DTOs;

public class SocialDataLoadResponse : ErrorCode
{
    public DataLoadSocialInfo SocialData { get; set; }
}

public class DataLoadSocialInfo
{
    public IEnumerable<GdbFriendInfo> FriendList { get; set; }
    public List<UserMailInfo> MailList { get; set; }

}

public class UserMailInfo
{
    public GdbMailboxInfo MailInfo { get; set; }
    public IEnumerable<GdbMailboxRewardInfo> MailItems { get; set; }
}



================================================
File: DTOs/UserDataLoad.cs
================================================
癤퓎sing GameAPIServer.Models;


namespace GameAPIServer.DTOs;

public class UserDataLoadResponse : ErrorCode
{
    public DataLoadUserInfo UserData { get; set; }
}

public class DataLoadUserInfo
{
    public GdbUserInfo UserInfo { get; set; }
    public GdbUserMoneyInfo MoneyInfo { get; set; }
    public GdbAttendanceInfo AttendanceInfo { get; set; }
}



================================================
File: DTOs/UserRank.cs
================================================
癤퓆amespace GameAPIServer.DTOs;

public class UserRankResponse : ErrorCode
{
    public long Rank { get; set; }
}



================================================
File: DTOs/UserSetMainChar.cs
================================================
癤퓎sing System.ComponentModel.DataAnnotations;

namespace GameAPIServer.DTOs;

public class UserSetMainCharRequest
{
    [Required]
    public int CharKey { get; set; }
}

public class UserSetMainCharResponse : ErrorCode
{
}



================================================
File: Middleware/CheckUserAuth.cs
================================================
﻿using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GameAPIServer.Repository;

namespace GameAPIServer.Middleware;

public class CheckUserAuthAndLoadUserData
{
    readonly IMemoryDb _memoryDb;
    readonly RequestDelegate _next;

    public CheckUserAuthAndLoadUserData(RequestDelegate next, IMemoryDb memoryDb)
    {
        _memoryDb = memoryDb;
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        //로그인, 회원가입 api는 토큰 검사를 하지 않는다.
        var formString = context.Request.Path.Value;
        if (string.Compare(formString, "/Login", StringComparison.OrdinalIgnoreCase) == 0 ||
            string.Compare(formString, "/CreateAccount", StringComparison.OrdinalIgnoreCase) == 0)
        {
            // Call the next delegate/middleware in the pipeline
            await _next(context);

            return;
        }

        // token이 있는지 검사하고 있다면 저장
        var (isTokenNotExist, token) = await IsTokenNotExistOrReturnToken(context);
        if(isTokenNotExist)
        {
            return;
        }

        //uid가 있는지 검사하고 있다면 저장
        var (isUidNotExist, uid) = await IsUidNotExistOrReturnUid(context);
        if (isUidNotExist)
        {
            return;
        }

        //uid를 키로 하는 데이터 없을 때
        (bool isOk, RdbAuthUserData userInfo) = await _memoryDb.GetUserAsync(uid);
        if (await IsInvalidUserAuthTokenNotFound(context, isOk))
        {
            return;
        }

        //토큰이 일치하지 않을 때
        if (await IsInvalidUserAuthTokenThenSendError(context, userInfo, token))
        {
            return;
        }

        //이번 api 호출 끝날 때까지 redis키 잠금 만약 이미 잠겨있다면 에러
        var userLockKey = MemoryDbKeyMaker.MakeUserLockKey(userInfo.Uid.ToString());
        if (await SetLockAndIsFailThenSendError(context, userLockKey))
        {
            return;
        }

        context.Items[nameof(RdbAuthUserData)] = userInfo;

        // Call the next delegate/middleware in the pipeline
        await _next(context);

        // 트랜잭션 해제(Redis 동기화 해제)
        await _memoryDb.UnLockUserReqAsync(userLockKey);
    }

    async Task<(bool,string)> IsTokenNotExistOrReturnToken(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("token", out var token))
        {
            return (false, token);
        }

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        var errorJsonResponse = JsonSerializer.Serialize(new MiddlewareResponse
        {
            result = ErrorCode.TokenDoesNotExist
        });
        await context.Response.WriteAsync(errorJsonResponse);

        return (true, "");
    }

    async Task<(bool, string)> IsUidNotExistOrReturnUid(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("uid", out var uid))
        {
            return (false, uid);
        }

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        var errorJsonResponse = JsonSerializer.Serialize(new MiddlewareResponse
        {
            result = ErrorCode.UidDoesNotExist
        });
        await context.Response.WriteAsync(errorJsonResponse);

        return (true, "");
    }

    async Task<bool> SetLockAndIsFailThenSendError(HttpContext context, string AuthToken)
    {
        if (await _memoryDb.LockUserReqAsync(AuthToken))
        {
            return false;
        }

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var errorJsonResponse = JsonSerializer.Serialize(new MiddlewareResponse
        {
            result = ErrorCode.AuthTokenFailSetNx
        });
        await context.Response.WriteAsync(errorJsonResponse);
        return true;
    }

    async Task<bool> IsInvalidUserAuthTokenThenSendError(HttpContext context, RdbAuthUserData userInfo, string token)
    {
        if (string.CompareOrdinal(userInfo.Token, token) == 0)
        {
            return false;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        var errorJsonResponse = JsonSerializer.Serialize(new MiddlewareResponse
        {
            result = ErrorCode.AuthTokenFailWrongAuthToken
        });
        await context.Response.WriteAsync(errorJsonResponse);

        return true;
    }

    async Task<bool> IsInvalidUserAuthTokenNotFound(HttpContext context, bool isOk)
    {
        if (!isOk)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            var errorJsonResponse = JsonSerializer.Serialize(new MiddlewareResponse
            {
                result = ErrorCode.AuthTokenKeyNotFound
            });
            await context.Response.WriteAsync(errorJsonResponse);
        }
        return !isOk;
    }

    class MiddlewareResponse
    {
        public ErrorCode result { get; set; }
    }
}



================================================
File: Middleware/VersionCheck.cs
================================================
癤퓎sing GameAPIServer.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameAPIServer.Middleware;


public class VersionCheck
{
    readonly RequestDelegate _next;
    readonly ILogger<VersionCheck> _logger;
    readonly IMasterDb _masterDb;

    public VersionCheck(RequestDelegate next, ILogger<VersionCheck> logger, IMasterDb masterDb)
    {
        _next = next;
        _logger = logger;
        _masterDb = masterDb;
    }

    public async Task Invoke(HttpContext context)
    {
        var appVersion = context.Request.Headers["AppVersion"].ToString();
        var masterDataVersion = context.Request.Headers["MasterDataVersion"].ToString();

        if (!(await VersionCompare(appVersion, masterDataVersion, context)))
        {
            return;
        }

        await _next(context);
    }

    async Task<bool> VersionCompare(string appVersion, string masterDataVersion, HttpContext context)
    {
        if (!appVersion.Equals(_masterDb._version!.app_version))
        {
            context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            var errorJsonResponse = JsonSerializer.Serialize(new MiddlewareResponse
            {
                result = ErrorCode.InValidAppVersion
            });
            await context.Response.WriteAsync(errorJsonResponse);
            return false;
        }

        if (!masterDataVersion.Equals(_masterDb._version!.master_data_version))
        {
            context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            var errorJsonResponse = JsonSerializer.Serialize(new MiddlewareResponse
            {
                result = ErrorCode.InvalidMasterDataVersion
            });
            await context.Response.WriteAsync(errorJsonResponse);
            return false;
        }

        return true;
    }

    class MiddlewareResponse
    {
        public ErrorCode result { get; set; }
    }
}




================================================
File: Models/Account.cs
================================================
癤퓎sing System;

namespace GameAPIServer.Models;


public class Account
{
    public Int64 player_id { get; set; }
    public string user_id { get; set; }
    public string pw { get; set; }
    public string salt_value { get; set; }
    public string recent_login_dt { get; set; }
    public string create_dt { get; set; }
}


================================================
File: Models/Attendance.cs
================================================
癤퓎sing System;

namespace GameAPIServer.Models;

public class GdbAttendanceInfo
{
    public int uid { get; set; }
    public int attendance_cnt { get; set; }
    public DateTime recent_attendance_dt { get; set; }
}



================================================
File: Models/Friend.cs
================================================
癤퓎sing System;

namespace GameAPIServer.Models;


public class GdbFriendInfo
{
    public int uid { get; set; }

    public string friend_uid { get; set; }
    public bool friend_yn { get; set; }
    public DateTime create_dt { get; set; }
}



================================================
File: Models/Game.cs
================================================
癤퓎sing System;

namespace GameAPIServer.Models;

public class GdbMiniGameInfo
{
    public int game_key { get; set; }
    public int play_char_key { get; set; }
    public int bestscore { get; set; }
    public DateTime create_dt { get; set; }
    public DateTime new_record_dt { get; set; }
    public DateTime recent_play_dt { get; set; }
    public int bestscore_cur_season { get; set; }
    public int bestscore_prev_season { get; set; }
}



================================================
File: Models/Item.cs
================================================
癤퓎sing System;

namespace GameAPIServer.Models;

public class GdbUserCharInfo
{
    public int uid { get; set; }
    public int char_key { get; set; }
    public int char_level { get; set; }
    public int char_cnt { get; set; }
    public int skin_key { get; set; }
    public string costume_json { get; set; }
}

public class GdbUserCostumeInfo
{
    public int uid { get; set; }
    public int costume_key { get; set; }
    public int costume_level { get; set; }
    public int costume_cnt { get; set; }
    public DateTime create_dt { get; set; }
}

public class GdbUserCharRandomSkillInfo
{
    public int uid { get; set; }
    public int char_key { get; set; }
    public int index_num { get; set; }
    public int skill_key { get; set; }
    public DateTime create_dt { get; set; }
}

public class GdbUserSkinInfo
{
    public int uid { get; set; }
    public int skin_key { get; set; }
    public DateTime create_dt { get; set; }
}

public class GdbUserFoodInfo
{
    public int uid { get; set; }
    public int food_key { get; set; }
    public int food_qty { get; set; }
    public int food_level { get; set; }
    public int food_gear_qty { get; set; }
    public DateTime create_dt { get; set; }
}

public class CharCostumeInfo
{
    public int Head { get; set; }
    public int Face { get; set; }
    public int Hand { get; set; }
}



================================================
File: Models/Mailbox.cs
================================================
癤퓎sing System;

namespace GameAPIServer.Models;

public class GdbMailboxInfo
{
    public int mail_seq { get; set; }
    public int uid { get; set; }
    public string mail_title { get; set; }
    public DateTime create_dt { get; set; }
    public DateTime expire_dt { get; set; }
    public DateTime receive_dt { get; set; }
    public bool receive_yn { get; set; }
}

public class GdbMailboxRewardInfo : RewardData
{
    public int mail_seq { get; set; }
}



================================================
File: Models/MasterDB.cs
================================================
癤퓎sing System.Collections.Generic;

namespace GameAPIServer.Models;

public class AttendanceRewardData : RewardData
{
    public int day_seq { get; set; }
}

public class RewardData
{
    public int reward_key { get; set; }
    public int reward_qty { get; set; }
    public string reward_type { get; set; }
}

public class CharacterData
{
    public int char_key { get; set; }
    public string char_name { get; set; }
    public string char_grade { get; set; }
    public int stat_run { get; set; }
    public int stat_power { get; set; }
    public int stat_jump { get; set; }
    public int game_key { get; set; }
}

public class SkinData
{
    public int skin_key { get; set; }
    public string skin_name { get; set; }
    public int char_key { get; set; }
    public int skin_bonus_percent { get; set; }
}

public class CostumeData
{
    public int costume_key { get; set; }
    public string costume_name { get; set; }
    public int costume_type { get; set; }
    public int set_key { get; set; }
}

public class CostumeSetData
{
    public int set_key { get; set; }
    public int char_key { get; set; }
    public string set_name { get; set; }
    public int set_bonus_percent { get; set; }
    public int char_bonus_percent { get; set; }
}

public class FoodData
{
    public int food_key { get; set; }
    public string food_name { get; set; }
    public int game_key { get; set; }
}

public class SkillData
{
    public int skill_key { get; set; }
    public int act_prob_percent { get; set; }
    public int char_key { get; set; } = 0;
}

public class GachaRewardData
{
    public GachaRewardInfo gachaRewardInfo { get; set; }
    public List<RewardData> gachaRewardList { get; set; }
}

public class GachaRewardInfo
{
    public int gacha_reward_key { get; set; }
    public int char_prob_percent { get; set; }
    public int skin_prob_percent { get; set; }
    public int costume_prob_percent { get; set; }
    public int food_prob_percent { get; set; }
    public int food_gear_prob_percent { get; set; }
    public int gacha_count { get; set; }
    public string gacha_reward_name { get; set; }
}

public class ItemLevelData
{
    public int level { get; set; }
    public int item_cnt { get; set; }
}

public class ReceivedReward
{
    public ReceivedReward(int key, List<RewardData> datas)
    {
        rewardKey = key;
        rewardDatas = datas;
    }

    public int rewardKey { get; set; }
    public List<RewardData> rewardDatas { get; set; }
}

public class VersionDAO
{
    public string app_version { get; set; } = "";
    public string master_data_version { get; set; } = "";
}



================================================
File: Models/RedisDB.cs
================================================
﻿using System;

namespace GameAPIServer.Models;

//RedisDB의 객체는 객체 이름 앞에 Rdb를 붙인다.

public class RdbAuthUserData
{
    public Int64 Uid { get; set; } = 0;
    public string Token { get; set; } = "";
}

public class RdbUserScoreData
{
    public Int64 uid { get; set; } = 0;
    public int total_bestscore { get; set; } = 0;
}



================================================
File: Models/User.cs
================================================
癤퓎sing System;

namespace GameAPIServer.Models;

public class GdbUserInfo
{
    public int uid { get; set; }
    public string player_id { get; set; }
    public string nickname { get; set; }
    public int main_char_key { get; set; }
    public DateTime create_dt { get; set; }
    public DateTime recent_login_dt { get; set; }
    public int total_bestscore { get; set; }
    public int total_bestscore_cur_season { get; set; }
    public int total_bestscore_prev_season { get; set; }
    public int star_point { get; set; }
}

public class GdbUserMoneyInfo
{
    public int uid { get; set; }
    public int jewelry { get; set; }
    public int gold_medal { get; set; }
    public int sunchip { get; set; }
    public int cash { get; set; }
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



================================================
File: Repository/GameDB_Attendance.cs
================================================
癤퓎sing System;
using System.Threading.Tasks;
using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Models;
using SqlKata.Execution;

namespace GameAPIServer.Repository;

public partial class GameDb : IGameDb
{
    public async Task<GdbAttendanceInfo> GetAttendanceById(Int64 uid)
    {
        return await _queryFactory.Query("user_attendance").Where("uid", uid)
                                                .FirstOrDefaultAsync<GdbAttendanceInfo>();
    }

    public async Task<int> CheckAttendanceById(Int64 uid)
    {
        return await _queryFactory.StatementAsync($"UPDATE user_attendance " +
                                                  $"SET attendance_cnt = attendance_cnt +1, " +
                                                      $"recent_attendance_dt = '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                                                  $"WHERE uid = {uid} AND " +
                                                      $"DATE(recent_attendance_dt) < '{DateTime.Today.ToString("yyyy-MM-dd")}';");
    }
}


================================================
File: Repository/GameDB_Game.cs
================================================
癤퓎sing System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Models;
using SqlKata.Execution;
using ZLogger;

namespace GameAPIServer.Repository;

public partial class GameDb : IGameDb
{
    public async Task<(ErrorCode, Int64)> CreateAccount(string userID, string pw)
    {
        try
        {
            var saltValue = Security.SaltString();
            var hashingPassword = Security.MakeHashingPassWord(saltValue, pw);

            var uid = await _queryFactory.Query("account")
                                .InsertGetIdAsync<Int64>(new
                                {
                                    user_id = userID,
                                    salt_value = saltValue,
                                    pw = hashingPassword,
                                    create_dt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                                });

            _logger.ZLogDebug(
            $"[CreateAccount] email: {userID}, salt_value : {saltValue}, hashed_pw:{hashingPassword}");

            var errorCode =ErrorCode.None;
            if(uid <= 0)
            {
                errorCode = ErrorCode.CreateAccountFailInsert;
            }

            return (errorCode, uid);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
            $"[HiveDb.CreateAccount] ErrorCode: {ErrorCode.CreateAccountFailException}, {ex}");
            return (ErrorCode.CreateAccountFailException, 0);
        }
    }

    public async Task<(ErrorCode, Int64)> VerifyUser(string userID, string pw)
    {
        try
        {
            var userInfo = await _queryFactory.Query("account")
                                    .Where("user_id", userID)
                                    .FirstOrDefaultAsync<Account>();

            if (userInfo is null)
            {
                return (ErrorCode.LoginFailUserNotExist, 0);
            }

            var hashingPassword = Security.MakeHashingPassWord(userInfo.salt_value, pw);
            if (userInfo.pw != hashingPassword)
            {
                return (ErrorCode.LoginFailPwNotMatch, 0);
            }

            return (ErrorCode.None, userInfo.player_id);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
            $"[HiveDb.VerifyUser] ErrorCode: {ErrorCode.LoginFailException}, {ex}");
            return (ErrorCode.LoginFailException, 0);
        }
    }
        

    public async Task<int> InsertInitMoneyInfo(Int64 uid, IDbTransaction transaction)
    {
        return await _queryFactory.Query("user_money").InsertAsync(
             new
             {
                 uid = uid
             }, transaction);
    }

    public async Task<int> InsertInitAttendance(Int64 uid, IDbTransaction transaction)
    {
        return await _queryFactory.Query("user_attendance").InsertAsync(
             new
             {
                 uid = uid,
                 recent_attendance_dt = DateTime.Now.AddDays(-1)
             }, transaction);
    }

    
    
}


================================================
File: Repository/GameDB_Mail.cs
================================================
癤퓎sing System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Models;
using SqlKata.Execution;

namespace GameAPIServer.Repository;

public partial class GameDb : IGameDb
{
    public async Task<IEnumerable<GdbMailboxInfo>> GetMailList(Int64 uid)
    {
        return await _queryFactory.Query("mailbox").Where("uid", uid)
                                                .Where("expire_dt", ">", DateTime.Now)
                                                .OrderBy("mail_seq")
                                                .GetAsync<GdbMailboxInfo>();
    }

    public async Task<GdbMailboxInfo> GetMailInfo(int mailSeq)
    {
        return await _queryFactory.Query("mailbox").Where("mail_seq", mailSeq)
                                                .FirstOrDefaultAsync<GdbMailboxInfo>();
    }

    public async Task<IEnumerable<GdbMailboxRewardInfo>> GetMailRewardList(int mailSeq)
    {
        return await _queryFactory.Query("mailbox_reward").Where("mail_seq", mailSeq)
                                                .GetAsync<GdbMailboxRewardInfo>();
    }

    public async Task<int> UpdateReceiveMail(int mailSeq)
    {
        return await _queryFactory.Query("mailbox").Where("mail_seq", mailSeq)
                                                .UpdateAsync(new { receive_dt = DateTime.Now, receive_yn = true });
    }

    public async Task<int> DeleteMail(int mailSeq)
    {
        return await _queryFactory.Query("mailbox").Where("mail_seq", mailSeq)
                                                .DeleteAsync();
    }

    public async Task<int> DeleteMailReward(int mailSeq)
    {
        return await _queryFactory.Query("mailbox_reward").Where("mail_seq", mailSeq)
                                                        .DeleteAsync();
    }
}


================================================
File: Repository/GameDB_User.cs
================================================
癤퓎sing System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MySqlConnector;
using SqlKata.Execution;

using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Models;


namespace GameAPIServer.Repository;

public partial class GameDb : IGameDb
{
    readonly ILogger<GameDb> _logger;
    readonly IOptions<DbConfig> _dbConfig;
    
    IDbConnection _dbConn;
    SqlKata.Compilers.MySqlCompiler _compiler;
    QueryFactory _queryFactory;

    public GameDb(ILogger<GameDb> logger, IOptions<DbConfig> dbConfig)
    {
        _dbConfig = dbConfig;
        _logger = logger;

        Open();

        _compiler = new SqlKata.Compilers.MySqlCompiler();
        _queryFactory = new SqlKata.Execution.QueryFactory(_dbConn, _compiler);
    }

    public void Dispose()
    {
        Close();
    }


    public async Task<GdbUserMoneyInfo> GetUserMoneyById(Int64 uid)
    {
        return await _queryFactory.Query("user_money").Where("uid", uid)
                                                .FirstOrDefaultAsync<GdbUserMoneyInfo>();
    }

    public async Task<int> UpdateUserjewelry(Int64 uid, int rewardQty)
    {
        return await _queryFactory.Query("user_money").Where("uid", uid)
                                                .IncrementAsync("jewelry", rewardQty);
    }

    public async Task<IEnumerable<RdbUserScoreData>> SelectAllUserScore()
    {
        return await _queryFactory.Query("user").Select("uid", "total_bestscore").GetAsync<RdbUserScoreData>();
    }

    public async Task<int> UpdateMainChar(Int64 uid, int charKey)
    {
        return await _queryFactory.Query("user").Where("uid", uid).UpdateAsync(new
        {
            main_char_key = charKey,
        });
    }

    public IDbConnection GDbConnection()
    {
        return _queryFactory.Connection;
    }

    void Open()
    {
        _dbConn = new MySqlConnection(_dbConfig.Value.GameDb);

        _dbConn.Open();
    }

    void Close()
    {
        _dbConn.Close();
    }
}

public class DbConfig
{
    public string MasterDb { get; set; }
    public string GameDb { get; set; }
    public string Redis { get; set; }
}


================================================
File: Repository/MasterDb.cs
================================================
癤퓎sing System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SqlKata.Execution;
using ZLogger;

namespace GameAPIServer.Repository;

public class MasterDb : IMasterDb
{
    
    readonly IOptions<DbConfig> _dbConfig;
    readonly ILogger<MasterDb> _logger;
    IDbConnection _dbConn;
    readonly SqlKata.Compilers.MySqlCompiler _compiler;
    readonly QueryFactory _queryFactory;
    readonly IMemoryDb _memoryDb;
    readonly IGameDb _gameDb;
    
    public VersionDAO _version { get; set; }
    public List<AttendanceRewardData> _attendanceRewardList { get; set; }    
    public List<GachaRewardData> _gachaRewardList { get; set; }
    public List<ItemLevelData> _itemLevelList { get; set; }


    public MasterDb(ILogger<MasterDb> logger, IOptions<DbConfig> dbConfig, IMemoryDb memoryDb, IGameDb gameDb)
    {
        _logger = logger;
        _dbConfig = dbConfig;

        Open();

        _compiler = new SqlKata.Compilers.MySqlCompiler();
        _queryFactory = new QueryFactory(_dbConn, _compiler);
        _memoryDb = memoryDb;
        _gameDb = gameDb;

    }

    public void Dispose()
    {
        Close();
    }

    public async Task<bool> Load()
    {
        try
        {
            _version = await _queryFactory.Query($"version").FirstOrDefaultAsync<VersionDAO>();
            
            _attendanceRewardList = (await _queryFactory.Query($"master_attendance_reward").GetAsync<AttendanceRewardData>()).ToList();
            
            _itemLevelList = (await _queryFactory.Query($"master_item_level").GetAsync<ItemLevelData>()).ToList();

            var gachaRewards = await _queryFactory.Query($"master_gacha_reward").GetAsync<GachaRewardInfo>();
            
            _gachaRewardList = new();
            
            foreach (var gachaRewardInfo in gachaRewards)
            {
                GachaRewardData gachaRewardData = new();
                gachaRewardData.gachaRewardInfo = gachaRewardInfo;
                gachaRewardData.gachaRewardList = (await _queryFactory.Query("master_gacha_reward_list")
                                                   .Where("gacha_reward_key", gachaRewardInfo.gacha_reward_key)
                                                   .GetAsync<RewardData>())
                                                   .ToList();
                _gachaRewardList.Add(gachaRewardData);
            }

            await LoadUserScore();
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            _logger.ZLogError(e,
                $"[MasterDb.Load] ErrorCode: {ErrorCode.MasterDB_Fail_LoadData}");
            return false;
        }

        if (!ValidateMasterData())
        {
            _logger.ZLogError($"[MasterDb.Load] ErrorCode: {ErrorCode.MasterDB_Fail_InvalidData}");
            return false;
        }

        return true;
    }

    public async Task<ErrorCode> LoadUserScore()
    {
        var usersScore = await _gameDb.SelectAllUserScore();
        foreach (var userScore in usersScore)
        {
            await _memoryDb.SetUserScore(userScore.uid, userScore.total_bestscore);
        }

        return ErrorCode.None;
    }

    bool ValidateMasterData()
    {
        if (_version == null || 
            _attendanceRewardList.Count == 0 ||
            _gachaRewardList.Count == 0)
        {
            return false;
        }

        return true;
    }

    void Open()
    {
        _dbConn = new MySqlConnection(_dbConfig.Value.MasterDb);
        _dbConn.Open();
    }

    void Close()
    {
        _dbConn.Close();
    }
}



================================================
File: Repository/MemoryDBDefine.cs
================================================
﻿namespace GameAPIServer.Repository;

public class RediskeyExpireTime
{
    public const ushort NxKeyExpireSecond = 3;
    public const ushort RegistKeyExpireSecond = 6000;
    public const ushort LoginKeyExpireMin = 60;
    public const ushort TicketKeyExpireSecond = 6000; // 현재 테스트를 위해 티켓은 10분동안 삭제하지 않는다. 
}



================================================
File: Repository/MemoryDb.cs
================================================
癤퓎sing GameAPIServer.Models;
using CloudStructures;
using CloudStructures.Structures;
using GameAPIServer.DTOs;
using GameAPIServer.Repository.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZLogger;


namespace GameAPIServer.Repository;

public class MemoryDb : IMemoryDb
{
    readonly RedisConnection _redisConn;
    readonly ILogger<MemoryDb> _logger;
    readonly IOptions<DbConfig> _dbConfig;

    public MemoryDb(ILogger<MemoryDb> logger, IOptions<DbConfig> dbConfig)
    {
        _logger = logger;
        _dbConfig = dbConfig;
        RedisConfig config = new ("default", _dbConfig.Value.Redis);
        _redisConn = new RedisConnection(config);
    }

    public async Task<ErrorCode> RegistUserAsync(string token, Int64 uid)
    {
        var key = MemoryDbKeyMaker.MakeUIDKey(uid.ToString());
        ErrorCode result = ErrorCode.None;

        RdbAuthUserData user = new()
        {
            Uid = uid,
            Token = token
        };

        try
        {
            RedisString<RdbAuthUserData> redis = new(_redisConn, key, LoginTimeSpan());
            if (await redis.SetAsync(user, LoginTimeSpan()) == false)
            {
                _logger.ZLogError($"[RegistUserAsync] Uid:{uid}, Token:{token},ErrorMessage:UserBasicAuth, RedisString set Error");
                result = ErrorCode.LoginFailAddRedis;
                return result;
            }
        }
        catch
        {
            _logger.ZLogError($"[RegistUserAsync] Uid:{uid}, Token:{token},ErrorMessage:Redis Connection Error");
            result = ErrorCode.LoginFailAddRedis;
            return result;
        }

        return result;
    }

    public async Task<ErrorCode> CheckUserAuthAsync(string id, string token)
    {
        var key = MemoryDbKeyMaker.MakeUIDKey(id);
        ErrorCode result = ErrorCode.None;

        try
        {
            RedisString<RdbAuthUserData> redis = new(_redisConn, key, null);
            RedisResult<RdbAuthUserData> user = await redis.GetAsync();

            if (!user.HasValue)
            {
                _logger.ZLogError( $"[CheckUserAuthAsync] Email = {id}, AuthToken = {token}, ErrorMessage:ID does Not Exist");
                result = ErrorCode.CheckAuthFailNotExist;
                return result;
            }

            if (user.Value.Uid.ToString() != id || user.Value.Token != token)
            {
                _logger.ZLogError($"[CheckUserAuthAsync] Email = {id}, AuthToken = {token}, ErrorMessage = Wrong ID or Auth Token");
                result = ErrorCode.CheckAuthFailNotMatch;
                return result;
            }
        }
        catch
        {
            _logger.ZLogError($"[CheckUserAuthAsync] Email = {id}, AuthToken = {token}, ErrorMessage:Redis Connection Error");
            result = ErrorCode.CheckAuthFailException;
            return result;
        }


        return result;
    }

    public async Task<(bool, RdbAuthUserData)> GetUserAsync(string id)
    {
        var uid = MemoryDbKeyMaker.MakeUIDKey(id);

        try
        {
            RedisString<RdbAuthUserData> redis = new(_redisConn, uid, null);
            RedisResult<RdbAuthUserData> user = await redis.GetAsync();
            if (!user.HasValue)
            {
                _logger.ZLogError(
                    $"[GetUserAsync] UID = {uid}, ErrorMessage = Not Assigned User, RedisString get Error");
                return (false, null);
            }

            return (true, user.Value);
        }
        catch
        {
            _logger.ZLogError($"[GetUserAsync] UID:{uid},ErrorMessage:ID does Not Exist");
            return (false, null);
        }
    }

    public async Task<bool> LockUserReqAsync(string key)
    {
        try
        {
            RedisString<RdbAuthUserData> redis = new(_redisConn, key, NxKeyTimeSpan());
            if (await redis.SetAsync(new RdbAuthUserData
            {
                // emtpy value
            }, NxKeyTimeSpan(), StackExchange.Redis.When.NotExists) == false)
            {
                return false;
            }
        }
        catch
        {
            _logger.ZLogError($"[SetUserReqLockAsync] Key = {key}, ErrorMessage:Redis Connection Error");
            return false;
        }

        return true;
    }

    public async Task<bool> UnLockUserReqAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        try
        {
            RedisString<RdbAuthUserData> redis = new(_redisConn, key, null);
            var redisResult = await redis.DeleteAsync();
            return redisResult;
        }
        catch
        {
            _logger.ZLogError($"[DelUserReqLockAsync] Key = {key}, ErrorMessage:Redis Connection Error");
            return false;
        }
    }

    public async Task<ErrorCode> DelUserAuthAsync(Int64 uid)
    {
        try
        {
            RedisString<RdbAuthUserData> redis = new(_redisConn, MemoryDbKeyMaker.MakeUIDKey(uid.ToString()), null);
            await redis.DeleteAsync();
            return ErrorCode.None;
        }
        catch
        {
            _logger.ZLogError(
                   $"[DelUserAuthAsync] UID = {uid}, ErrorCode : {ErrorCode.LogoutRedisDelFailException}");
            return ErrorCode.LogoutRedisDelFailException;
        }
    }

    public TimeSpan LoginTimeSpan()
    {
        return TimeSpan.FromMinutes(RediskeyExpireTime.LoginKeyExpireMin);
    }

    public TimeSpan TicketKeyTimeSpan()
    {
        return TimeSpan.FromSeconds(RediskeyExpireTime.TicketKeyExpireSecond);
    }

    public TimeSpan NxKeyTimeSpan()
    {
        return TimeSpan.FromSeconds(RediskeyExpireTime.NxKeyExpireSecond);
    }

    public async Task<ErrorCode> SetUserScore(Int64 uid, int score)
    {
        try
        {
            var set = new RedisSortedSet<Int64>(_redisConn, "user-ranking", null);
            await set.AddAsync(uid, score);

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                   $"[SetUserScore] UID = {uid}, ErrorCode : {ErrorCode.SetUserScoreFailException}");
            return ErrorCode.SetUserScoreFailException;
        }
    }

    public async Task<(ErrorCode, List<RankData>)> GetTopRanking()
    {
        try
        {
            List<RankData> ranking = new();

            var set = new RedisSortedSet<int>(_redisConn, "user-ranking", null);
            var rankDatas =  await set.RangeByRankWithScoresAsync(0,100,order:StackExchange.Redis.Order.Descending);

            var rank = 0;
            var score = -1;
            var count = 1;
            foreach (var rankData in rankDatas)
            {
                if(rankData.Score == score)
                {
                    count++;
                }
                else
                {
                    rank += count;
                    count = 1;
                    score = (int)rankData.Score;
                }
                ranking.Add(new RankData
                {
                    rank = rank,
                    uid = rankData.Value,
                    score = score
                });
            }

            return (ErrorCode.None, ranking);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                   $"[GetUserRanking] ErrorCode : {ErrorCode.GetRankingFailException}");
            return (ErrorCode.GetRankingFailException, null);
        }
    }

    public async Task<(ErrorCode, Int64)> GetUserRankAsync(Int64 uid)
    {
        try
        {
            var set = new RedisSortedSet<Int64>(_redisConn, "user-ranking", null);
            var rank = await set.RankAsync(uid, order: StackExchange.Redis.Order.Descending);
            return (ErrorCode.None, rank.Value + 1);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                                  $"[GetUserRankAsync] UID = {uid}, ErrorCode : {ErrorCode.GetUserRankFailException}");
            return (ErrorCode.GetUserRankFailException, 0);
        }
    }
}


================================================
File: Repository/MemoryDbKeyMaker.cs
================================================
癤퓆amespace GameAPIServer.Repository;

public class MemoryDbKeyMaker
{
    const string loginUID = "UID_";
    const string userLockKey = "ULock_";

    public static string MakeUIDKey(string id)
    {
        return loginUID + id;
    }

    public static string MakeUserLockKey(string id)
    {
        return userLockKey + id;
    }
}


================================================
File: Repository/Interfaces/IGameDb.cs
================================================
癤퓎sing GameAPIServer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace GameAPIServer.Repository.Interfaces;

public interface IGameDb
{
    public Task<(ErrorCode, Int64)> CreateAccount(string userID, string pw);

    public Task<(ErrorCode, Int64)> VerifyUser(string userID, string pw);


    public Task<int> InsertInitMoneyInfo(Int64 uid, IDbTransaction transaction);
    public Task<int> InsertInitAttendance(Int64 uid, IDbTransaction transaction);
  
    
    public Task<IEnumerable<GdbMailboxInfo>> GetMailList(Int64 uid);
    public Task<GdbMailboxInfo> GetMailInfo(int mailSeq);
    public Task<IEnumerable<GdbMailboxRewardInfo>> GetMailRewardList(int mailSeq);
    public Task<int> DeleteMail(int mailSeq);
    public Task<int> DeleteMailReward(int mailSeq);
    public Task<int> UpdateReceiveMail(int mailSeq);
    public Task<int> UpdateMainChar(Int64 uid, int charKey);

    public Task<GdbUserMoneyInfo> GetUserMoneyById(Int64 uid);
    
    
    public Task<IEnumerable<RdbUserScoreData>> SelectAllUserScore();

    public Task<GdbAttendanceInfo> GetAttendanceById(Int64 uid);
    public Task<int> CheckAttendanceById(Int64 uid);
    public Task<int> UpdateUserjewelry(Int64 uid, int rewardQty);
    public IDbConnection GDbConnection();
}


================================================
File: Repository/Interfaces/IMasterDb.cs
================================================
癤퓎sing System.Collections.Generic;
using System.Threading.Tasks;
using GameAPIServer.Models;

namespace GameAPIServer.Repository.Interfaces;

public interface IMasterDb
{
    public VersionDAO _version { get; }
    public List<AttendanceRewardData> _attendanceRewardList { get; }    
    public List<GachaRewardData> _gachaRewardList { get; }
    public List<ItemLevelData> _itemLevelList { get; set; }

    public Task<bool> Load();
}



================================================
File: Repository/Interfaces/IMemoryDb.cs
================================================
癤퓎sing GameAPIServer.Models; 
using GameAPIServer.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameAPIServer.Repository.Interfaces;

public interface IMemoryDb
{
    public Task<ErrorCode> RegistUserAsync(string token, Int64 uid);
    public Task<ErrorCode> CheckUserAuthAsync(string id, string authToken);
    public Task<(bool, RdbAuthUserData)> GetUserAsync(string id);
    public Task<bool> LockUserReqAsync(string key);
    public Task<bool> UnLockUserReqAsync(string key);
    public Task<ErrorCode> DelUserAuthAsync(Int64 uid);
    public  Task<ErrorCode> SetUserScore(Int64 uid, int score);
    public  Task<(ErrorCode, List<RankData>)> GetTopRanking();
    public Task<(ErrorCode, Int64)> GetUserRankAsync(Int64 uid);
}



================================================
File: Services/AttendanceService.cs
================================================
﻿using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZLogger;


namespace GameAPIServer.Servicies;

public class AttendanceService : IAttendanceService
{
    readonly ILogger<AttendanceService> _logger;
    readonly IGameDb _gameDb;
    readonly IMasterDb _masterDb;
    readonly IItemService _itemService;

    public AttendanceService(ILogger<AttendanceService> logger, IGameDb gameDb, IMasterDb masterDb, IItemService itemService)
    {
        _logger = logger;
        _gameDb = gameDb;
        _masterDb = masterDb;
        _itemService = itemService;
    }

    public async Task<(ErrorCode, GdbAttendanceInfo)> GetAttendanceInfo(Int64 uid)
    {
        try
        {
            return (ErrorCode.None, await _gameDb.GetAttendanceById(uid));
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Attendance.GetAttendance] ErrorCode: {ErrorCode.AttendanceInfoFailException}, Uid: {uid}");
            return (ErrorCode.AttendanceInfoFailException, null);
        }
    }

    public async Task<(ErrorCode, List<ReceivedReward>)> CheckAttendanceAndReceiveRewards(Int64 uid)
    {
        try
        {
            List<ReceivedReward> totalRewards = [];

            //출석 체크
            var rowCount = await _gameDb.CheckAttendanceById(uid);
            if (rowCount != 1)
            {
                return (ErrorCode.AttendanceCheckFailAlreadyChecked, null);
            }

            var attendanceInfo = await _gameDb.GetAttendanceById(uid);
            var attendanceCnt = attendanceInfo.attendance_cnt;

            //출석 보상 수령
            var reward = _masterDb._attendanceRewardList.Find(reward => reward.day_seq == attendanceCnt);
            
            // 가챠 보상일 경우
            if(reward.reward_type == "gacha")
            {
                for (int i = 0; i < reward.reward_qty; i++)
                {
                    var (errorCode, rewards) = await _itemService.ReceiveOneGacha(uid, reward.reward_key);
                    if (errorCode != ErrorCode.None)
                    {
                        return (errorCode, null);
                    }
                    totalRewards.Add(new ReceivedReward(reward.reward_key, rewards));
                }
               
                return (ErrorCode.None, totalRewards);
            }
            // 일반 보상일 경우
            else
            {
                await _itemService.ReceiveReward(uid, reward);
                totalRewards.Add(new ReceivedReward(reward.reward_key, [reward]));
                return (ErrorCode.None, totalRewards);
            }
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Attendance.CheckAttendance] ErrorCode: {ErrorCode.AttendanceCheckFailException}, Uid: {uid}");
            return (ErrorCode.AttendanceCheckFailException, null);
        }
    }
}





================================================
File: Services/AuthService.cs
================================================
癤퓎sing GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ZLogger;
using GameAPIServer.Repository.Interfaces;

namespace GameAPIServer.Servicies;

public class AuthService : IAuthService
{
    readonly ILogger<AuthService> _logger;
    readonly IGameDb _gameDb;
    readonly IMemoryDb _memoryDb;
    string _hiveServerAPIAddress;

    public AuthService(ILogger<AuthService> logger, IConfiguration configuration, IGameDb gameDb, IMemoryDb memoryDb)
    {
        _gameDb = gameDb;
        _logger = logger;
        _hiveServerAPIAddress = configuration.GetSection("HiveServerAddress").Value + "/verifytoken";
        _memoryDb = memoryDb;
    }

    public async Task<(ErrorCode, Int64)> CreateAccount(string userID, string passWord)
    {
        var (result, uid) = await _gameDb.CreateAccount(userID, passWord);

        return (result, uid);
    }
    
    public async Task<(ErrorCode, Int64, string)> Login(string userID, string passWord)
    {
        (var result, var uid) = await _gameDb.VerifyUser(userID, passWord);
        if (result != ErrorCode.None)
        {
            return (result, 0, "");
        }

        var token = Security.CreateAuthToken();
        result = await _memoryDb.RegistUserAsync(token, uid);

        return (result, uid, token);
    }

    

}



================================================
File: Services/DataLoadService.cs
================================================
癤퓎sing GameAPIServer.DTOs;
using GameAPIServer.Servicies.Interfaces;
using System;
using System.Threading.Tasks;

namespace GameAPIServer.Servicies;

public class DataLoadService : IDataLoadService
{
    readonly IGameService _gameService;
    readonly IUserService _userService;
    readonly IItemService _itemService;
    readonly IMailService _mailService;
    readonly IAttendanceService _attendanceService;

    public DataLoadService(IMailService mailService, IAttendanceService attendanceService, IUserService userService, IItemService itemService, IGameService gameService)
    {
        _mailService = mailService;
        _attendanceService = attendanceService;
        _userService = userService;
        _itemService = itemService;
        _gameService = gameService;
    }

    public async Task<(ErrorCode, DataLoadUserInfo)> LoadUserData(Int64 uid)
    {
        DataLoadUserInfo loadData = new();
                        
        (var errorCode, loadData.MoneyInfo) = await _userService.GetUserMoneyInfo(uid);
        if (errorCode != ErrorCode.None)
        {
            return (errorCode, null);
        }

        (errorCode, loadData.AttendanceInfo) = await _attendanceService.GetAttendanceInfo(uid);
        if (errorCode != ErrorCode.None)
        {
            return (errorCode, null);
        }

        return (ErrorCode.None, loadData);
    }


}



================================================
File: Services/GameService.cs
================================================
癤퓎sing GameAPIServer.Servicies.Interfaces;
using GameAPIServer.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ZLogger;
using GameAPIServer.Repository.Interfaces;

namespace GameAPIServer.Servicies;

public class GameService :IGameService
{
    readonly ILogger<GameService> _logger;
    readonly IGameDb _gameDb;
    readonly IMasterDb _masterDb;
    readonly IMemoryDb _memoryDb;


    public GameService(ILogger<GameService> logger, IGameDb gameDb, IMasterDb masterDb, IMemoryDb memoryDb)
    {
        _logger = logger;
        _gameDb = gameDb;
        _masterDb = masterDb;
        _memoryDb = memoryDb;
    }


    public async Task<ErrorCode> InitNewUserGameData(Int64 uid)
    {
        var transaction = _gameDb.GDbConnection().BeginTransaction();
        try
        {
            var rowCount = await _gameDb.InsertInitMoneyInfo(uid, transaction);
            if (rowCount != 1)
            {
                transaction.Rollback();
                return ErrorCode.InitNewUserGameDataFailMoney;
            }

            rowCount = await _gameDb.InsertInitAttendance(uid, transaction);
            if (rowCount != 1)
            {
                transaction.Rollback();
                return ErrorCode.InitNewUserGameDataFailAttendance;
            }

            transaction.Commit();
            return ErrorCode.None;
        }
        catch (Exception e)
        {
            transaction.Rollback();
            _logger.ZLogError(e,
                $"[Game.InitNewUserGameData] ErrorCode: {ErrorCode.InitNewUserGameDataFailException}, uid : {uid}");
            return ErrorCode.GameSetNewUserListFailException;
        }
        finally
        {
            transaction.Dispose();
        }
    }

    

}



================================================
File: Services/ItemService.cs
================================================
﻿using GameAPIServer.Models;
using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Servicies.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Servicies;

public class ItemService : IItemService
{
    readonly ILogger<ItemService> _logger;
    readonly IGameDb _gameDb;
    readonly IMasterDb _masterDb;

    public ItemService(ILogger<ItemService> logger, IGameDb gameDb, IMasterDb masterDb)
    {
        _logger = logger;
        _gameDb = gameDb;
        _masterDb = masterDb;
    }

           
  

    public async Task<(ErrorCode,List<RewardData>)> ReceiveOneGacha(Int64 uid, int gachaKey)
    {
        try
        {
            List<RewardData> rewardDatas = [];

            // 가챠 정보 가져오기
            var gacha = _masterDb._gachaRewardList.Find(item => item.gachaRewardInfo.gacha_reward_key == gachaKey);
            var gachaInfo = gacha.gachaRewardInfo;

            // 가챠 확률을 위한 총합
            var totalPercent = gachaInfo.char_prob_percent
                             + gachaInfo.skin_prob_percent 
                             + gachaInfo.costume_prob_percent 
                             + gachaInfo.food_prob_percent 
                             + gachaInfo.food_gear_prob_percent;

            // 가챠 확률
            int[] probs = { gachaInfo.char_prob_percent,
                            gachaInfo.skin_prob_percent,
                            gachaInfo.costume_prob_percent,
                            gachaInfo.food_prob_percent,
                            gachaInfo.food_gear_prob_percent };

            // 가챠 타입
            string[] types = { "char", "skin", "costume", "food", "food_gear" };

            // 가챠의 뽑기횟수만큼 반복
            for (int i = 0; i < gachaInfo.gacha_count; i++)
            {
                //숫자를 뽑고 확률 배열에 따라 타입 고르기
                var randomPoint = new Random().Next(1, totalPercent + 1);
                for (int j = 0; j < probs.Length; j++)
                {
                    if (randomPoint <= probs[j])
                    {
                        // 정해진 타입의 보상들 중 다시 랜덤 뽑기
                        var rewards = gacha.gachaRewardList.FindAll(item => item.reward_type == types[j]);
                        var randomIndex = new Random().Next(0, rewards.Count);
                        var reward = rewards[randomIndex];
                        // 보상 지급
                        var errorCode = await ReceiveReward(uid, reward);
                        if (errorCode != ErrorCode.None)
                        {
                            return (errorCode, null);
                        }
                        rewardDatas.Add(reward);
                        break;
                    }
                    randomPoint -= probs[j];
                }
            }
            
            return (ErrorCode.None, rewardDatas);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Item.ReceiveOneGacha] ErrorCode: {ErrorCode.GachaReceiveFailException}, Uid: {uid}, GachaKey: {gachaKey}");
            return (ErrorCode.GachaReceiveFailException, null);
        }
    }

    public async Task<ErrorCode> ReceiveReward(Int64 uid, RewardData reward)
    {
        int rowCount;
        var errorCode = ErrorCode.None;
        try
        {
            switch (reward.reward_type)
            {
                case "money": //보석
                    rowCount = await _gameDb.UpdateUserjewelry(uid, reward.reward_qty);
                    if (rowCount != 1)
                    {
                        return ErrorCode.UserUpdateJewelryFailIncremnet;
                    }
                    break;                    
            }
            if(errorCode != ErrorCode.None)
            {
                return errorCode;
            }

            return errorCode;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[GetReward] ErrorCode: {ErrorCode.GetRewardFailException}, Uid: {uid}");
            return ErrorCode.GetRewardFailException;
        }
    }
}



================================================
File: Services/MailService.cs
================================================
﻿using GameAPIServer.Models;
using GameAPIServer.DTOs;
using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Servicies.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Servicies;

public class MailService : IMailService
{
    readonly ILogger<MailService> _logger;
    readonly IGameDb _gameDb;
    readonly IItemService _itemService;

    public MailService(ILogger<MailService> logger, IGameDb gameDb, IItemService itemService)
    {
        _logger = logger;
        _gameDb = gameDb;
        _itemService = itemService;
    }

    public async Task<(ErrorCode,List<UserMailInfo>)> GetMailList(Int64 uid)
    {
        try
        {
            List<UserMailInfo> userMailInfoList = new();
            
            var mailList = await _gameDb.GetMailList(uid);
            foreach (var mail in mailList)
            {
                UserMailInfo userMailInfo = new();
                userMailInfo.MailInfo = mail;
                userMailInfo.MailItems = await _gameDb.GetMailRewardList(mail.mail_seq);
                userMailInfoList.Add(userMailInfo);
            }

            return (ErrorCode.None, userMailInfoList);
        }
        catch (System.Exception e)
        {
            _logger.ZLogError(e,
                                   $"[Mail.GetMailList] ErrorCode: {ErrorCode.MailListFailException}, Uid: {uid}");
            return (ErrorCode.MailListFailException, null);
        }
    }

    public async Task<(ErrorCode,List<ReceivedReward>)> ReceiveMail(Int64 uid, int mailSeq)
    {
        try
        {
            //메일의 존재, 수령여부, 소유권 확인
            var mailInfo = await _gameDb.GetMailInfo(mailSeq);
            if(mailInfo == null)
            {
                return (ErrorCode.MailReceiveFailMailNotExist, null);
            }
            if (mailInfo.receive_yn == true)
            {
                return (ErrorCode.MailReceiveFailAlreadyReceived, null);
            }
            if(mailInfo.uid != uid)
            {
                return ((ErrorCode.MailReceiveFailNotMailOwner, null));
            }

            //메일 보상 확인
            var mailRewards = await _gameDb.GetMailRewardList(mailSeq);

            //메일 보상 수령
            return await ReceiveMailRewards(mailInfo.uid, mailSeq, mailRewards);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                   $"[Mail.ReceiveMail] ErrorCode: {ErrorCode.MailReceiveFailException}, Uid: {uid}, MailSeq: {mailSeq}");
            return (ErrorCode.MailReceiveFailException, null);
        }
    }

    async Task<(ErrorCode,List<ReceivedReward>)> ReceiveMailRewards(Int64 uid, int mailSeq, IEnumerable<RewardData> mailRewards)
    {
        try
        {
            List<ReceivedReward> totalRewards = new();

            //메일 보상 마다 반복 수령
            foreach (var reward in mailRewards)
            {
                // 가챠 보상일 경우
                if (reward.reward_type == "gacha")
                {
                    for (int i = 0; i < reward.reward_qty; i++)
                    {
                        var (errorCode, rewards) = await _itemService.ReceiveOneGacha(uid, reward.reward_key);
                        if (errorCode != ErrorCode.None)
                        {
                            return (errorCode, null);
                        }
                        totalRewards.Add(new ReceivedReward(reward.reward_key,rewards));
                    }
                }
                // 일반 보상일 경우
                else
                {
                    var errorCode = await _itemService.ReceiveReward(uid, reward);
                    if (errorCode != ErrorCode.None)
                    {
                        return (errorCode, null);
                    }
                    totalRewards.Add(new ReceivedReward(reward.reward_key,[reward]));
                }
            }

            //수령일자 및 수령여부 업데이트
            var rowCount = await _gameDb.UpdateReceiveMail(mailSeq);
            if (rowCount != 1)
            {
                return (ErrorCode.MailReceiveFailUpdateReceiveDt, null);
            }

            return (ErrorCode.None, totalRewards);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Mail.ReceiveMail] ErrorCode: {ErrorCode.MailReceiveRewardsFailException}, MailSeq: {mailSeq}");
            return (ErrorCode.MailReceiveRewardsFailException, null);
        }
    }

    public async Task<ErrorCode> DeleteMail(Int64 uid, int mailSeq)
    {
        try
        {
            //메일의 존재, 소유권 확인
            var mailInfo = await _gameDb.GetMailInfo(mailSeq);
            if (mailInfo == null)
            {
                return ErrorCode.MailReceiveFailMailNotExist;
            }
            if (mailInfo.uid != uid)
            {
                return ErrorCode.MailReceiveFailNotMailOwner;
            }

            //메일 삭제
            var rowCount = await _gameDb.DeleteMail(mailSeq);
            if (rowCount != 1)
            {
                return ErrorCode.MailDeleteFailDeleteMail;
            }

            //메일 보상 삭제
            rowCount = await _gameDb.DeleteMailReward(mailSeq);
            if (rowCount < 1)
            {
                return ErrorCode.MailDeleteFailDeleteMailReward;
            }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Mail.DeleteMail] ErrorCode: {ErrorCode.MailDeleteFailException}, Uid: {uid}, MailSeq: {mailSeq}");
            return ErrorCode.MailDeleteFailException;
        }
    }
}



================================================
File: Services/UserService.cs
================================================
癤퓎sing GameAPIServer.Models;
using GameAPIServer.DTOs;
using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Servicies.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ZLogger;

namespace GameAPIServer.Servicies;

public class UserService : IUserService
{
    readonly ILogger<UserService> _logger;
    readonly IGameDb _gameDb;
    readonly IMemoryDb _memoryDb;

    public UserService(ILogger<UserService> logger, IGameDb gameDb, IMemoryDb memoryDb)
    {
        _logger = logger;
        _gameDb = gameDb;
        _memoryDb = memoryDb;
    }


    public async Task<(ErrorCode, GdbUserMoneyInfo)> GetUserMoneyInfo(Int64 uid)
    {
        try
        {
            return (ErrorCode.None, await _gameDb.GetUserMoneyById(uid));
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[User.GetUserMoneyInfo] ErrorCode: {ErrorCode.UserMoneyInfoFailException}, Uid: {uid}");
            return (ErrorCode.UserMoneyInfoFailException, null);
        }
    }

   

  
}



================================================
File: Services/Interfaces/IAttendanceService.cs
================================================
癤퓎sing GameAPIServer.DTOs;
using GameAPIServer.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameAPIServer.Servicies.Interfaces;

public interface IAttendanceService
{
    public Task<(ErrorCode, GdbAttendanceInfo)> GetAttendanceInfo(Int64 uid);
    public Task<(ErrorCode, List<ReceivedReward>)> CheckAttendanceAndReceiveRewards(Int64 uid);
}



================================================
File: Services/Interfaces/IAuthService.cs
================================================
癤퓎sing System.Threading.Tasks;
using System;

namespace GameAPIServer.Servicies.Interfaces;

public interface IAuthService
{
    public Task<(ErrorCode, Int64)> CreateAccount(string userID, string passWord);
        
    public Task<(ErrorCode, Int64, string)> Login(string userID, string passWord);


}



================================================
File: Services/Interfaces/IDataLoadService.cs
================================================
癤퓎sing GameAPIServer.DTOs;
using System;
using System.Threading.Tasks;

namespace GameAPIServer.Servicies.Interfaces;

public interface IDataLoadService
{
    public Task<(ErrorCode, DataLoadUserInfo)> LoadUserData(Int64 uid);
        
}



================================================
File: Services/Interfaces/IGameService.cs
================================================
癤퓎sing System;
using System.Threading.Tasks;

namespace GameAPIServer.Servicies.Interfaces;

public interface IGameService
{
    public Task<ErrorCode> InitNewUserGameData(Int64 uid);
   
}
 


================================================
File: Services/Interfaces/IItemService.cs
================================================
癤퓎sing GameAPIServer.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameAPIServer.Servicies.Interfaces;

public interface IItemService
{
    public Task<(ErrorCode, List<RewardData>)> ReceiveOneGacha(Int64 uid, int gachaKey);

    public Task<ErrorCode> ReceiveReward(Int64 uid, RewardData reward);
  

}



================================================
File: Services/Interfaces/IMailService.cs
================================================
癤퓎sing GameAPIServer.DTOs;
using GameAPIServer.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameAPIServer.Servicies.Interfaces;

public interface IMailService
{
    public Task<(ErrorCode, List<UserMailInfo>)> GetMailList(Int64 uid);
    public Task<(ErrorCode, List<ReceivedReward>)> ReceiveMail(Int64 uid, int mailSeq);
    public Task<ErrorCode> DeleteMail(Int64 uid, int mailSeq);
}



================================================
File: Services/Interfaces/IUserService.cs
================================================
癤퓎sing GameAPIServer.DTOs;
using GameAPIServer.Models;
using System;
using System.Threading.Tasks;

namespace GameAPIServer.Servicies.Interfaces;

public interface IUserService
{    
    public Task<(ErrorCode, GdbUserMoneyInfo)> GetUserMoneyInfo(Int64 uid);
    
    
}


