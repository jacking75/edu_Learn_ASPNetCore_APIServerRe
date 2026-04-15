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
