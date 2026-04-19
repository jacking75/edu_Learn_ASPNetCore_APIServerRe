# 단위 테스트 가이드

통합 테스트는 실제 DB와 Redis를 띄워서 API를 end-to-end로 테스트한다.
단위 테스트는 **외부 의존성(DB, Redis, HTTP) 없이 비즈니스 로직만 빠르게 테스트**한다.

두 테스트는 목적이 다르며 함께 사용한다.

| 구분 | 단위 테스트 | 통합 테스트 |
|:---|:---|:---|
| 대상 | Service, Domain 로직 | Controller → Service → DB 전체 흐름 |
| 속도 | 매우 빠름 (밀리초) | 느림 (초 단위) |
| 외부 의존성 | 없음 (Mock 사용) | 있음 (실제 DB, Redis) |
| 실행 환경 | 어디서든 가능 | Docker 환경 필요 |
| 목적 | 로직 정확성 검증 | 전체 흐름 검증 |

---

## 목차

1. [테스트 프로젝트 설정](#1-테스트-프로젝트-설정)
2. [Mock 사용법 (NSubstitute)](#2-mock-사용법-nsubstitute)
3. [Service 계층 테스트](#3-service-계층-테스트)
4. [게임 로직 특화 테스트 패턴](#4-게임-로직-특화-테스트-패턴)
5. [테스트 작성 원칙](#5-테스트-작성-원칙)

---

## 1. 테스트 프로젝트 설정

```bash
# 솔루션 루트에서
dotnet new xunit -n GameAPIServer.Tests
cd GameAPIServer.Tests

# 테스트에 필요한 패키지 설치
dotnet add package NSubstitute          # Mock 라이브러리
dotnet add package FluentAssertions     # 읽기 쉬운 Assert

# 메인 프로젝트 참조 추가
dotnet add reference ../GameAPIServer/GameAPIServer.csproj
```

```xml
<!-- GameAPIServer.Tests/GameAPIServer.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="xunit" Version="2.7.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7" />
    <PackageReference Include="NSubstitute" Version="5.1.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\GameAPIServer\GameAPIServer.csproj" />
  </ItemGroup>
</Project>
```

```bash
# 테스트 실행
dotnet test

# 상세 출력
dotnet test --verbosity normal

# 특정 테스트만 실행
dotnet test --filter "FullyQualifiedName~AttendanceServiceTests"
```

---

## 2. Mock 사용법 (NSubstitute)

단위 테스트에서는 DB, Redis 등 외부 의존성을 **Mock(가짜 객체)**으로 대체한다.
NSubstitute는 인터페이스 기반으로 Mock을 쉽게 만들 수 있다.

```csharp
// IGameDb 인터페이스를 Mock으로 대체
IGameDb mockGameDb = Substitute.For<IGameDb>();

// Mock의 동작 정의: GetAttendance(임의의 long)을 호출하면 특정 값을 반환
mockGameDb.GetAttendance(Arg.Any<long>())
          .Returns(Task.FromResult<GdbAttendanceInfo?>(new GdbAttendanceInfo
          {
              Uid = 1,
              TotalAttendance = 5,
              LastAttendance = DateTime.UtcNow.Date.AddDays(-1) // 어제 출석
          }));

// Mock이 특정 메서드를 실제로 호출했는지 검증
await mockGameDb.Received(1).UpdateAttendance(Arg.Any<long>(), Arg.Any<DateTime>());

// Mock이 호출되지 않았는지 검증
await mockGameDb.DidNotReceive().GiveReward(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<int>());
```

---

## 3. Service 계층 테스트

### AttendanceService 단위 테스트 예시

```csharp
// Tests/Services/AttendanceServiceTests.cs
using GameAPIServer.Services;
using GameAPIServer.Repository;
using GameAPIServer.Models.DAO;
using NSubstitute;
using FluentAssertions;

public class AttendanceServiceTests
{
    // 공통으로 사용하는 Mock 객체
    readonly IGameDb _mockGameDb;
    readonly IMasterDb _mockMasterDb;
    readonly ILogger<AttendanceService> _mockLogger;
    readonly AttendanceService _sut; // System Under Test (테스트 대상)

    public AttendanceServiceTests()
    {
        _mockGameDb = Substitute.For<IGameDb>();
        _mockMasterDb = Substitute.For<IMasterDb>();
        _mockLogger = Substitute.For<ILogger<AttendanceService>>();

        // 테스트 대상 생성 시 Mock을 주입
        _sut = new AttendanceService(_mockLogger, _mockGameDb, _mockMasterDb);
    }

    [Fact]
    [Trait("Category", "AttendanceService")]
    public async Task 오늘_처음_출석하면_성공을_반환한다()
    {
        // Arrange (준비)
        long uid = 12345;
        var attendanceInfo = new GdbAttendanceInfo
        {
            Uid = uid,
            TotalAttendance = 5,
            ConsecutiveDays = 5,
            LastAttendance = DateTime.UtcNow.Date.AddDays(-1) // 어제 출석
        };

        _mockGameDb.GetAttendance(uid).Returns(Task.FromResult<GdbAttendanceInfo?>(attendanceInfo));
        _mockGameDb.UpdateAttendance(uid, Arg.Any<DateTime>()).Returns(Task.FromResult(1));

        var reward = new MasterAttendanceReward { AttendanceDay = 6, RewardItemCode = 1001, RewardQuantity = 10 };
        _mockMasterDb.GetAttendanceReward(6).Returns(reward);
        _mockGameDb.GiveReward(uid, reward.RewardItemCode, reward.RewardQuantity)
                   .Returns(Task.FromResult(ErrorCode.None));

        // Act (실행)
        var (errorCode, receivedReward) = await _sut.CheckAttendance(uid);

        // Assert (검증) - FluentAssertions 사용
        errorCode.Should().Be(ErrorCode.None);
        receivedReward.Should().NotBeNull();
        receivedReward!.RewardItemCode.Should().Be(1001);

        // DB 업데이트가 1번 호출되었는지 검증
        await _mockGameDb.Received(1).UpdateAttendance(uid, Arg.Any<DateTime>());
    }

    [Fact]
    [Trait("Category", "AttendanceService")]
    public async Task 오늘_이미_출석했으면_에러를_반환한다()
    {
        // Arrange
        long uid = 12345;
        var attendanceInfo = new GdbAttendanceInfo
        {
            Uid = uid,
            LastAttendance = DateTime.UtcNow.Date // 오늘 이미 출석
        };

        _mockGameDb.GetAttendance(uid).Returns(Task.FromResult<GdbAttendanceInfo?>(attendanceInfo));

        // Act
        var (errorCode, reward) = await _sut.CheckAttendance(uid);

        // Assert
        errorCode.Should().Be(ErrorCode.AttendanceAlreadyCheckedToday);
        reward.Should().BeNull();

        // DB 업데이트가 호출되지 않았는지 검증 (중복 출석 방어)
        await _mockGameDb.DidNotReceive().UpdateAttendance(Arg.Any<long>(), Arg.Any<DateTime>());
    }

    [Fact]
    [Trait("Category", "AttendanceService")]
    public async Task DB_조회_실패_시_에러를_반환한다()
    {
        // Arrange
        long uid = 12345;
        _mockGameDb.GetAttendance(uid)
                   .Returns(Task.FromException<GdbAttendanceInfo?>(new Exception("DB 연결 실패")));

        // Act
        var (errorCode, reward) = await _sut.CheckAttendance(uid);

        // Assert
        errorCode.Should().Be(ErrorCode.AttendanceFailException);
        reward.Should().BeNull();
    }

    // Theory: 여러 입력값으로 같은 테스트를 반복할 때
    [Theory]
    [InlineData(1, 1001)]   // 1일 출석 → 1001번 아이템
    [InlineData(7, 1007)]   // 7일 출석 → 1007번 아이템
    [InlineData(30, 1030)]  // 30일 출석 → 1030번 아이템
    [Trait("Category", "AttendanceService")]
    public async Task 출석일수에_맞는_보상을_반환한다(int attendanceDay, int expectedItemCode)
    {
        // Arrange
        long uid = 12345;
        var attendanceInfo = new GdbAttendanceInfo
        {
            Uid = uid,
            TotalAttendance = attendanceDay - 1,
            ConsecutiveDays = attendanceDay - 1,
            LastAttendance = DateTime.UtcNow.Date.AddDays(-1)
        };

        _mockGameDb.GetAttendance(uid).Returns(Task.FromResult<GdbAttendanceInfo?>(attendanceInfo));
        _mockGameDb.UpdateAttendance(uid, Arg.Any<DateTime>()).Returns(Task.FromResult(1));

        var reward = new MasterAttendanceReward
        {
            AttendanceDay = attendanceDay,
            RewardItemCode = expectedItemCode,
            RewardQuantity = 1
        };
        _mockMasterDb.GetAttendanceReward(attendanceDay).Returns(reward);
        _mockGameDb.GiveReward(uid, expectedItemCode, 1).Returns(Task.FromResult(ErrorCode.None));

        // Act
        var (errorCode, receivedReward) = await _sut.CheckAttendance(uid);

        // Assert
        errorCode.Should().Be(ErrorCode.None);
        receivedReward!.RewardItemCode.Should().Be(expectedItemCode);
    }
}
```

---

## 4. 게임 로직 특화 테스트 패턴

### 재화 트랜잭션 테스트

```csharp
public class ShopServiceTests
{
    readonly IGameDb _mockGameDb;
    readonly IMasterDb _mockMasterDb;
    readonly ShopService _sut;

    public ShopServiceTests()
    {
        _mockGameDb = Substitute.For<IGameDb>();
        _mockMasterDb = Substitute.For<IMasterDb>();
        _sut = new ShopService(Substitute.For<ILogger<ShopService>>(), _mockGameDb, _mockMasterDb);
    }

    [Fact]
    public async Task 재화가_충분하면_아이템을_구매할_수_있다()
    {
        // Arrange
        long uid = 1;
        var shopItem = new MasterShopItem { ShopItemId = 1, ItemCode = 1001, Price = 100 };
        _mockMasterDb.GetShopItem(1).Returns(shopItem);
        _mockGameDb.GetMoney(uid).Returns(Task.FromResult(500L)); // 500골드 보유
        _mockGameDb.DeductMoney(uid, 100).Returns(Task.FromResult(ErrorCode.None));
        _mockGameDb.AddItem(uid, 1001, 1).Returns(Task.FromResult(ErrorCode.None));

        // Act
        var errorCode = await _sut.BuyItem(uid, shopItemId: 1);

        // Assert
        errorCode.Should().Be(ErrorCode.None);
        await _mockGameDb.Received(1).DeductMoney(uid, 100);
        await _mockGameDb.Received(1).AddItem(uid, 1001, 1);
    }

    [Fact]
    public async Task 재화가_부족하면_구매에_실패한다()
    {
        // Arrange
        long uid = 1;
        var shopItem = new MasterShopItem { ShopItemId = 1, ItemCode = 1001, Price = 100 };
        _mockMasterDb.GetShopItem(1).Returns(shopItem);
        _mockGameDb.GetMoney(uid).Returns(Task.FromResult(50L)); // 50골드만 보유

        // Act
        var errorCode = await _sut.BuyItem(uid, shopItemId: 1);

        // Assert
        errorCode.Should().Be(ErrorCode.ShopInsufficientMoney);

        // 재화 차감 및 아이템 지급이 호출되지 않았는지 확인
        await _mockGameDb.DidNotReceive().DeductMoney(Arg.Any<long>(), Arg.Any<int>());
        await _mockGameDb.DidNotReceive().AddItem(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task 존재하지_않는_상품은_구매할_수_없다()
    {
        // Arrange
        long uid = 1;
        _mockMasterDb.GetShopItem(999).Returns((MasterShopItem?)null); // 없는 상품

        // Act
        var errorCode = await _sut.BuyItem(uid, shopItemId: 999);

        // Assert
        errorCode.Should().Be(ErrorCode.ShopItemNotFound);
    }
}
```

### 시간 의존 로직 테스트

쿨타임, 이벤트 기간 등 현재 시간에 의존하는 로직은 시간을 주입 가능하게 만들어야 테스트할 수 있다.

```csharp
// 나쁜 예: DateTime.UtcNow를 직접 사용하면 테스트에서 시간 제어 불가
public async Task<ErrorCode> UseItem(long uid, int itemCode)
{
    var lastUse = await _db.GetLastUseTime(uid, itemCode);
    if (DateTime.UtcNow < lastUse.AddSeconds(30)) // ← 테스트에서 제어 불가
        return ErrorCode.ItemCoolTimeNotExpired;
    ...
}

// 좋은 예: ITimeProvider 인터페이스로 추상화
public interface ITimeProvider
{
    DateTime UtcNow { get; }
}

public class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

// 테스트용 가짜 시간 제공자
public class FakeTimeProvider : ITimeProvider
{
    public DateTime UtcNow { get; set; } = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}

// 테스트에서 시간을 원하는 대로 제어
[Fact]
public async Task 쿨타임_중에는_아이템을_사용할_수_없다()
{
    // Arrange
    var fakeTime = new FakeTimeProvider();
    fakeTime.UtcNow = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    var sut = new ItemService(_mockGameDb, _mockMasterDb, fakeTime);

    var lastUse = fakeTime.UtcNow.AddSeconds(-10); // 10초 전에 사용
    _mockGameDb.GetLastUseTime(uid, itemCode).Returns(Task.FromResult(lastUse));

    // Act
    var errorCode = await sut.UseItem(uid: 1, itemCode: 1001); // 쿨타임 30초인 아이템

    // Assert
    errorCode.Should().Be(ErrorCode.ItemCoolTimeNotExpired);
}
```

---

## 5. 테스트 작성 원칙

### AAA 패턴 (Arrange, Act, Assert)

모든 테스트를 이 3단계로 작성한다.

```csharp
[Fact]
public async Task 테스트_이름()
{
    // Arrange: 테스트에 필요한 데이터와 Mock 설정
    ...

    // Act: 테스트 대상 메서드 호출
    var result = await _sut.SomeMethod(...);

    // Assert: 결과 검증
    result.Should().Be(expected);
}
```

### 테스트 이름 규칙

```csharp
// [테스트 대상]_[조건]_[기대 결과] 형식으로 한글로 작성
// 한글로 쓰면 실패 시 로그에서 즉시 무슨 테스트인지 알 수 있다

public async Task 오늘_처음_출석하면_성공을_반환한다() { }
public async Task 오늘_이미_출석했으면_에러를_반환한다() { }
public async Task DB_조회_실패_시_에러를_반환한다() { }
```

### 테스트 당 하나의 검증

```csharp
// 나쁜 예: 한 테스트에서 여러 시나리오를 검증
[Fact]
public async Task 출석_전체_테스트()
{
    // 첫 출석 성공 검증
    ...
    // 중복 출석 실패 검증
    ...
    // DB 에러 검증
    ...
    // → 첫 번째 실패 시 나머지는 실행되지 않아 어디서 실패했는지 파악이 어려움
}

// 좋은 예: 시나리오마다 독립적인 테스트
[Fact] public async Task 오늘_처음_출석하면_성공을_반환한다() { ... }
[Fact] public async Task 오늘_이미_출석했으면_에러를_반환한다() { ... }
[Fact] public async Task DB_조회_실패_시_에러를_반환한다() { ... }
```

### 무엇을 테스트할 것인가

| 테스트해야 하는 것 | 테스트하지 않아도 되는 것 |
|:---|:---|
| 비즈니스 규칙 (중복 출석 방지, 재화 차감 로직) | 단순 CRUD를 그대로 호출하는 코드 |
| 경계값 처리 (재화 0, 최대값) | 프레임워크 동작 (ASP.NET Core 라우팅 등) |
| 에러 처리 흐름 | 외부 라이브러리 내부 동작 |
| 시간 의존 로직 (쿨타임, 이벤트 기간) | 자동 생성 코드 |
