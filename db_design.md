# 게임 API 서버 DB 설계 가이드

게임 서버의 DB는 일반 웹 서비스와 달리 고려해야 할 사항이 많다.
이 문서는 GameDB / MasterDB / MemoryDB(Redis)를 어떻게 설계하고 나눠 쓰는지를 다룬다.

---

## 목차

1. [DB 역할 분리 기준](#1-db-역할-분리-기준)
2. [GameDB 스키마 설계 원칙](#2-gamedb-스키마-설계-원칙)
3. [인덱스 설계 전략](#3-인덱스-설계-전략)
4. [DB 마이그레이션 전략](#4-db-마이그레이션-전략)
5. [자주 하는 실수](#5-자주-하는-실수)

---

## 1. DB 역할 분리 기준

이 프로젝트에서는 세 가지 저장소를 목적에 따라 명확히 나눠서 사용한다.

```
┌─────────────────────────────────────────────────────────────┐
│                     게임 API 서버                            │
│                                                             │
│   ┌──────────────┐  ┌──────────────┐  ┌────────────────┐   │
│   │   GameDB     │  │  MasterDB    │  │  MemoryDB      │   │
│   │  (MySQL)     │  │  (MySQL)     │  │  (Redis)       │   │
│   │              │  │              │  │                │   │
│   │ 유저 데이터   │  │ 기획 데이터   │  │ 세션/캐시/Lock  │   │
│   │ 아이템 보유   │  │ 아이템 정보   │  │ 매칭 결과      │   │
│   │ 재화         │  │ 상점 가격    │  │ 게임 상태      │   │
│   │ 출석 기록    │  │ 출석 보상    │  │ 랭킹          │   │
│   └──────────────┘  └──────────────┘  └────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 각 DB를 선택하는 기준

| 질문 | YES → | NO → |
|:---|:---|:---|
| 유저마다 다른 값인가? | GameDB | ↓ |
| 게임 업데이트 없이 변하는가? | GameDB | ↓ |
| 서버 재시작 후에도 유지해야 하는가? | GameDB / MasterDB | MemoryDB |
| 초당 수천 번 읽히는가? | MemoryDB (캐시) | MySQL |
| 게임 업데이트 시에만 변하는 기획 데이터인가? | MasterDB | ↓ |

### 구체적인 예시

| 데이터 | 저장소 | 이유 |
|:---|:---|:---|
| 유저 닉네임, 레벨, 경험치 | GameDB | 유저별 다른 값, 영구 저장 |
| 유저 인벤토리 아이템 목록 | GameDB | 유저별 다른 값, 영구 저장 |
| 아이템 이름, 최대 보유량 | MasterDB | 모든 유저 공통, 업데이트 시 변경 |
| 상점 아이템 가격 | MasterDB | 기획 데이터 |
| 로그인 토큰 | MemoryDB | 임시 데이터, TTL로 자동 만료 |
| 현재 진행 중인 게임 상태 | MemoryDB | 게임 종료 시 GameDB로 이관 |
| 실시간 랭킹 | MemoryDB | 빈번한 업데이트, 빠른 조회 필요 |

---

## 2. GameDB 스키마 설계 원칙

### 원칙 1: 모든 테이블에 uid(유저 ID) 컬럼

게임 DB의 거의 모든 조회는 "특정 유저의 데이터"를 대상으로 한다.

```sql
-- 나쁜 예: uid가 없어서 전체 스캔해야 함
CREATE TABLE item (
    item_seq    BIGINT NOT NULL AUTO_INCREMENT,
    item_code   INT NOT NULL,
    quantity    INT NOT NULL,
    PRIMARY KEY (item_seq)
);

-- 좋은 예: uid로 바로 조회 가능
CREATE TABLE item (
    uid         BIGINT NOT NULL,       -- 어느 유저의 아이템인가
    item_seq    BIGINT NOT NULL AUTO_INCREMENT,
    item_code   INT NOT NULL,
    quantity    INT NOT NULL,
    obtained_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (item_seq),
    INDEX idx_uid (uid)                -- uid 기준 조회를 위한 인덱스
);
```

### 원칙 2: 시간 컬럼은 UTC로 저장

서버가 여러 국가에 배포되거나, 서머타임 등의 이슈를 피하기 위해 UTC를 사용한다.

```sql
CREATE TABLE attendance (
    uid             BIGINT NOT NULL,
    attendance_day  INT NOT NULL,          -- 연속 출석 일수
    last_attendance DATETIME NOT NULL,     -- UTC 기준
    created_at      DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY (uid)
);
```

```csharp
// C#에서 UTC 사용
DateTime now = DateTime.UtcNow;  // DateTime.Now 사용 금지
```

### 원칙 3: 삭제는 소프트 삭제 고려

실제로 DB에서 행을 삭제하면 복구가 불가능하다.
특히 아이템, 우편 같이 문의가 많이 오는 데이터는 소프트 삭제를 고려한다.

```sql
-- 소프트 삭제: 실제로 지우지 않고 플래그만 세운다
CREATE TABLE mail (
    mail_seq        BIGINT NOT NULL AUTO_INCREMENT,
    uid             BIGINT NOT NULL,
    title           VARCHAR(100) NOT NULL,
    content         TEXT,
    is_received     TINYINT NOT NULL DEFAULT 0,   -- 수령 여부
    is_deleted      TINYINT NOT NULL DEFAULT 0,   -- 삭제 여부
    expiry_date     DATETIME NOT NULL,
    created_at      DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY (mail_seq),
    INDEX idx_uid_deleted (uid, is_deleted)
);

-- 조회 시 삭제된 것 제외
SELECT * FROM mail WHERE uid = ? AND is_deleted = 0;
```

### 원칙 4: 스키마에서 게임 제약조건 보호

DB 레벨에서 제약조건을 걸면 버그로 인한 데이터 오염을 방어할 수 있다.

```sql
CREATE TABLE user_info (
    uid         BIGINT NOT NULL AUTO_INCREMENT,
    player_id   BIGINT NOT NULL,              -- Hive 계정 ID
    nickname    VARCHAR(20) NOT NULL,
    level       INT NOT NULL DEFAULT 1,
    exp         BIGINT NOT NULL DEFAULT 0,
    money       BIGINT NOT NULL DEFAULT 0,

    -- 음수 방어: 재화는 절대 음수가 되어선 안 됨
    CONSTRAINT chk_money CHECK (money >= 0),
    CONSTRAINT chk_level CHECK (level >= 1),
    CONSTRAINT chk_exp CHECK (exp >= 0),

    PRIMARY KEY (uid),
    UNIQUE KEY uk_player_id (player_id),      -- 계정당 게임 계정 1개
    UNIQUE KEY uk_nickname (nickname)          -- 닉네임 중복 방지
);
```

### 표준 테이블 구조 예시

```sql
-- 유저 기본 정보
CREATE TABLE user_info (
    uid         BIGINT NOT NULL AUTO_INCREMENT,
    player_id   BIGINT NOT NULL,
    nickname    VARCHAR(20) NOT NULL,
    level       INT NOT NULL DEFAULT 1,
    exp         BIGINT NOT NULL DEFAULT 0,
    money_gold  BIGINT NOT NULL DEFAULT 0,
    money_gem   BIGINT NOT NULL DEFAULT 0,
    created_at  DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    last_login  DATETIME,
    CONSTRAINT chk_gold CHECK (money_gold >= 0),
    CONSTRAINT chk_gem CHECK (money_gem >= 0),
    PRIMARY KEY (uid),
    UNIQUE KEY uk_player_id (player_id),
    UNIQUE KEY uk_nickname (nickname)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 아이템 인벤토리
CREATE TABLE user_item (
    item_seq    BIGINT NOT NULL AUTO_INCREMENT,
    uid         BIGINT NOT NULL,
    item_code   INT NOT NULL,
    quantity    INT NOT NULL DEFAULT 1,
    obtained_at DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    CONSTRAINT chk_quantity CHECK (quantity > 0),
    PRIMARY KEY (item_seq),
    INDEX idx_uid (uid),
    INDEX idx_uid_item (uid, item_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 우편함
CREATE TABLE mail (
    mail_seq        BIGINT NOT NULL AUTO_INCREMENT,
    uid             BIGINT NOT NULL,
    title           VARCHAR(100) NOT NULL,
    content         TEXT,
    is_received     TINYINT NOT NULL DEFAULT 0,
    is_deleted      TINYINT NOT NULL DEFAULT 0,
    expiry_date     DATETIME NOT NULL,
    created_at      DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY (mail_seq),
    INDEX idx_uid_active (uid, is_deleted, expiry_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 출석 기록
CREATE TABLE attendance (
    uid                 BIGINT NOT NULL,
    total_attendance    INT NOT NULL DEFAULT 0,   -- 누적 출석 일수
    consecutive_days    INT NOT NULL DEFAULT 0,   -- 연속 출석 일수
    last_attendance     DATE,                     -- 마지막 출석 날짜 (UTC)
    PRIMARY KEY (uid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- MasterDB: 아이템 기본 정보 (읽기 전용)
CREATE TABLE master_item (
    item_code   INT NOT NULL,
    item_name   VARCHAR(50) NOT NULL,
    item_type   TINYINT NOT NULL,       -- 1: 소비, 2: 장비, 3: 재화
    max_stack   INT NOT NULL DEFAULT 1, -- 최대 보유 수량
    sell_price  INT NOT NULL DEFAULT 0,
    PRIMARY KEY (item_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 3. 인덱스 설계 전략

인덱스를 잘 설계하면 쿼리 성능이 10배~100배 차이가 난다.

### 인덱스가 필요한 컬럼

```sql
-- 규칙 1: WHERE 절에 자주 사용되는 컬럼
-- uid로 유저 아이템을 조회하는 쿼리가 많으면 uid에 인덱스
INDEX idx_uid (uid)

-- 규칙 2: 복합 조건이 자주 사용되면 복합 인덱스
-- WHERE uid = ? AND is_deleted = 0 → 복합 인덱스가 단일 인덱스보다 효율적
INDEX idx_uid_deleted (uid, is_deleted)

-- 규칙 3: 정렬(ORDER BY)에 사용되는 컬럼
-- ORDER BY created_at DESC → created_at에 인덱스
INDEX idx_created_at (created_at)

-- 규칙 4: UNIQUE 제약이 필요한 컬럼은 UNIQUE KEY로 자동 인덱스 생성
UNIQUE KEY uk_player_id (player_id)
```

### 복합 인덱스 컬럼 순서

복합 인덱스는 **선택도(Cardinality)가 높은 컬럼을 앞에** 배치한다.

```sql
-- 나쁜 예: is_deleted는 0과 1뿐이라 선택도가 낮음 → 앞에 오면 효과 감소
INDEX idx_bad (is_deleted, uid)

-- 좋은 예: uid는 유저마다 고유해서 선택도가 높음 → 앞에 배치
INDEX idx_good (uid, is_deleted)

-- 실제 쿼리
-- SELECT * FROM mail WHERE uid = 12345 AND is_deleted = 0
-- idx_good으로 uid=12345인 행만 먼저 걸러낸 후 is_deleted 필터 → 빠름
```

### 인덱스를 과도하게 만들지 말 것

```
인덱스의 단점:
- INSERT/UPDATE/DELETE 시 인덱스도 갱신 → 쓰기 성능 저하
- 인덱스 자체가 디스크 공간을 차지

게임 서버에서 쓰기가 많은 테이블 (예: 아이템, 재화):
→ 인덱스를 최소화하고, 조회는 uid 하나로 처리
```

### EXPLAIN으로 인덱스 사용 확인

```sql
-- 쿼리가 인덱스를 타는지 확인
EXPLAIN SELECT * FROM user_item WHERE uid = 12345 AND item_code = 100;

-- 결과에서 확인할 것:
-- type: ref 또는 eq_ref → 인덱스 사용 (좋음)
-- type: ALL → 풀 스캔 (느림, 인덱스 추가 필요)
-- key: 사용된 인덱스 이름
-- rows: 스캔한 행 수 (적을수록 좋음)
```

---

## 4. DB 마이그레이션 전략

### 마이그레이션이란?

서버를 운영하다 보면 테이블 구조를 변경해야 하는 상황이 생긴다.
- 새 기능 추가: 테이블 추가, 컬럼 추가
- 버그 수정: 컬럼 타입 변경
- 성능 개선: 인덱스 추가/삭제

이미 운영 중인 DB를 변경하는 것이 **마이그레이션**이다.

### 버전 관리된 SQL 파일 방식

이 프로젝트에서는 버전 번호가 붙은 SQL 파일로 마이그레이션을 관리한다.

```
DB/
├── schema/
│   ├── V001__initial_schema.sql        # 최초 스키마
│   ├── V002__add_mail_table.sql        # 우편 테이블 추가
│   ├── V003__add_attendance_table.sql  # 출석 테이블 추가
│   └── V004__add_item_index.sql        # 아이템 인덱스 추가
└── migration_history.md               # 변경 이력 설명
```

```sql
-- V002__add_mail_table.sql
-- 우편함 기능 추가 (2024-07-15)
-- 담당: 홍길동

CREATE TABLE IF NOT EXISTS mail (
    mail_seq        BIGINT NOT NULL AUTO_INCREMENT,
    uid             BIGINT NOT NULL,
    title           VARCHAR(100) NOT NULL,
    content         TEXT,
    is_received     TINYINT NOT NULL DEFAULT 0,
    is_deleted      TINYINT NOT NULL DEFAULT 0,
    expiry_date     DATETIME NOT NULL,
    created_at      DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY (mail_seq),
    INDEX idx_uid_active (uid, is_deleted, expiry_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 안전한 마이그레이션 원칙

운영 중인 서버에서 DB 변경은 매우 위험하다.

```sql
-- ✅ 안전한 작업: 컬럼 추가 (기존 데이터에 영향 없음)
ALTER TABLE user_info ADD COLUMN tutorial_step INT NOT NULL DEFAULT 0;

-- ✅ 안전한 작업: 인덱스 추가 (락이 걸리지만 데이터 손실 없음)
ALTER TABLE user_item ADD INDEX idx_uid_item (uid, item_code);
-- 또는 온라인 DDL 사용 (MySQL 5.6+)
ALTER TABLE user_item ADD INDEX idx_uid_item (uid, item_code), ALGORITHM=INPLACE, LOCK=NONE;

-- ⚠️ 주의: 컬럼 삭제 (되돌릴 수 없음)
-- 반드시 배포 전 백업 후 실행
ALTER TABLE user_info DROP COLUMN unused_column;

-- ❌ 위험: 운영 중 컬럼 타입 변경 (전체 테이블 리빌드 → 장시간 락)
ALTER TABLE user_info MODIFY COLUMN level BIGINT;
-- → pt-online-schema-change 등 무중단 마이그레이션 도구 사용 권장

-- ❌ 위험: NOT NULL 컬럼을 기본값 없이 추가 (기존 행이 전부 오류)
ALTER TABLE user_info ADD COLUMN new_field INT NOT NULL;  -- 기존 행 때문에 실패
-- → DEFAULT 값을 항상 지정
ALTER TABLE user_info ADD COLUMN new_field INT NOT NULL DEFAULT 0;  -- 안전
```

### 배포 순서 (무중단 변경)

컬럼 추가처럼 하위 호환이 가능한 경우에는 아래 순서로 배포한다.

```
1단계: DB 마이그레이션 먼저 실행
  → 새 컬럼 추가 (기존 코드는 새 컬럼을 무시)

2단계: 새 코드 배포
  → 새 코드가 새 컬럼을 사용하기 시작

3단계: 배포 후 확인
  → 오류 없이 동작하는지 모니터링
```

컬럼 삭제처럼 하위 호환이 불가능한 경우에는 아래 순서로 배포한다.

```
1단계: 새 코드에서 해당 컬럼 사용 제거 후 배포

2단계: 충분한 시간이 지난 후 (롤백 가능성이 없어진 후) DB 컬럼 삭제
```

---

## 5. 자주 하는 실수

### 실수 1: uid 컬럼 없이 아이템 테이블 설계

```sql
-- 나쁜 예
CREATE TABLE item (item_seq, item_code, quantity);
-- "uid 12345의 아이템을 조회" → 전체 스캔 필요

-- 좋은 예
CREATE TABLE item (uid, item_seq, item_code, quantity);
-- "uid 12345의 아이템을 조회" → uid 인덱스로 빠르게 조회
```

### 실수 2: 재화를 클라이언트 계산 값으로 저장

```csharp
// 나쁜 예: 클라이언트가 보낸 최종 재화를 DB에 그대로 저장
await _db.UpdateMoney(uid, request.FinalMoney); // request.FinalMoney는 변조 가능

// 좋은 예: 서버에서 증감량으로 계산
int currentMoney = await _db.GetMoney(uid);
int newMoney = currentMoney + earnedAmount; // 서버에서 계산
if (newMoney < 0) return ErrorCode.InsufficientMoney;
await _db.UpdateMoney(uid, newMoney);
```

### 실수 3: MasterDB 데이터를 GameDB에 중복 저장

```sql
-- 나쁜 예: GameDB의 user_item에 아이템 이름, 가격까지 저장
CREATE TABLE user_item (
    uid         BIGINT,
    item_code   INT,
    item_name   VARCHAR(50),  -- ← MasterDB에 있는 정보
    sell_price  INT,          -- ← MasterDB에 있는 정보
    quantity    INT
);
-- 문제: MasterDB에서 가격이 바뀌어도 GameDB의 user_item은 바뀌지 않음 → 불일치 발생

-- 좋은 예: GameDB에는 아이템 코드만 저장, 상세 정보는 MasterDB에서 조회
CREATE TABLE user_item (
    uid         BIGINT,
    item_code   INT,          -- ← 이 코드로 MasterDB를 조회
    quantity    INT
);
```

### 실수 4: 출석/보상을 중복 지급 방어 없이 구현

```csharp
// 나쁜 예: 중복 체크 없이 바로 지급
public async Task<ErrorCode> ClaimAttendanceReward(Int64 uid)
{
    await _db.GiveReward(uid, todayReward); // 여러 번 호출하면 여러 번 지급
    return ErrorCode.None;
}

// 좋은 예: 오늘 이미 받았는지 확인 후 지급
public async Task<ErrorCode> ClaimAttendanceReward(Int64 uid)
{
    var attendance = await _db.GetAttendance(uid);

    // 오늘 이미 출석했는지 확인 (UTC 날짜 기준)
    if (attendance.LastAttendance?.Date == DateTime.UtcNow.Date)
    {
        return ErrorCode.AttendanceAlreadyCheckedToday;
    }

    // 트랜잭션으로 출석 기록 + 보상 지급을 원자적으로 처리
    using var transaction = await _db.BeginTransactionAsync();
    try
    {
        await _db.UpdateAttendance(uid, DateTime.UtcNow, transaction);
        await _db.GiveReward(uid, reward, transaction);
        await transaction.CommitAsync();
        return ErrorCode.None;
    }
    catch
    {
        await transaction.RollbackAsync();
        return ErrorCode.AttendanceFailException;
    }
}
```

### 실수 5: 트랜잭션 없이 여러 테이블 동시 변경

```csharp
// 나쁜 예: 아이템 구매 시 재화 차감과 아이템 지급이 별개로 실행
await _db.DeductMoney(uid, price);   // 성공
await _db.AddItem(uid, itemCode, 1); // 실패 → 돈만 나가고 아이템은 안 받은 상황 발생

// 좋은 예: 트랜잭션으로 묶어서 원자적으로 처리
using var transaction = await _db.BeginTransactionAsync();
try
{
    await _db.DeductMoney(uid, price, transaction);
    await _db.AddItem(uid, itemCode, 1, transaction);
    await transaction.CommitAsync(); // 둘 다 성공해야 커밋
}
catch
{
    await transaction.RollbackAsync(); // 하나라도 실패하면 전부 롤백
    return ErrorCode.BuyItemFailException;
}
```
