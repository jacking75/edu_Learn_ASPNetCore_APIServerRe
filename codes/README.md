# 예제 프로젝트 모음

이 디렉토리에는 ASP.NET Core 기반 게임 API 서버 학습용 프로젝트가 포함되어 있다.

## 프로젝트 목록

| 프로젝트 | 유형 | .NET | 설명 | 완성도 |
|----------|------|------|------|--------|
| [GameAPIServer_Template](./GameAPIServer_Template/) | 템플릿 | net10.0 | 단일 서버 API 템플릿 (71파일). 신규 프로젝트의 기반으로 사용 | 완성 |
| [MultiAPIServer_Template](./MultiAPIServer_Template/) | 템플릿 | net10.0 | 멀티 서버 구성 (Game + Hive + Match) | 완성 |
| [practice_omok_game-1](./practice_omok_game-1/) | 실습 | net10.0 | 오목 게임 v1 (롱폴링, 시퀀스 다이어그램 충실) | 완성 |
| [practice_omok_game-2](./practice_omok_game-2/) | 실습 | net10.0 | 오목 게임 v2 (플로우 다이어그램 우수) | 완성 |
| [api_server_training_tany_farm](./api_server_training_tany_farm/) | 과제 | net10.0 | 타니 팜 (**과제 명세만**, 본 서버는 학습자가 구현) | 명세만 |
| [zlogger_test](./zlogger_test/) | 예제 | net10.0 | ZLogger 로깅 테스트 | 예제 |

## 학습 순서 권장

1. **GameAPIServer_Template** — 프로젝트 구조와 패턴 파악
2. **MultiAPIServer_Template** — 멀티 서버(Game + Hive + Match) 구성 이해
3. **practice_omok_game-1 / omok_game-2** — 롱폴링 기반 실시간 게임 구현
4. **api_server_training_tany_farm** — 직접 구현 과제 도전

## 공통 기술 스택

- **DB**: MySQL (MySqlConnector + SqlKata)
- **캐시**: Redis (CloudStructures)
- **로깅**: ZLogger (구조화 JSON 로깅)
- **인증**: FakeHiveServer를 통한 토큰 기반 인증
- **미들웨어**: 버전 체크, 유저 인증 및 요청 락

## 참고

ASP.NET Core 팁 (Polly 재시도, RateLimit, FluentValidation 등)은 [aspnet_core_tips.md](../docs/references/aspnet_core_tips.md)를 참고한다.
