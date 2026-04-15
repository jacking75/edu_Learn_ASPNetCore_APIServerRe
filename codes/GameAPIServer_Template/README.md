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
