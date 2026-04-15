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
