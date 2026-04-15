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
