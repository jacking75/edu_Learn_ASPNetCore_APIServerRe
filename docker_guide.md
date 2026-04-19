# Docker 운영 가이드

이 프로젝트는 MySQL, Redis, 게임 서버 모두 Docker로 실행한다.
이 문서는 Docker Compose 설정을 이해하고, 개발/운영 환경을 구성하는 방법을 다룬다.

---

## 목차

1. [전체 구성 이해](#1-전체-구성-이해)
2. [docker-compose.yml 설명](#2-docker-composeyml-설명)
3. [개발 환경 운영](#3-개발-환경-운영)
4. [Secrets(민감 정보) 관리](#4-secrets민감-정보-관리)
5. [운영 환경 배포](#5-운영-환경-배포)
6. [자주 쓰는 Docker 명령어](#6-자주-쓰는-docker-명령어)
7. [트러블슈팅](#7-트러블슈팅)

---

## 1. 전체 구성 이해

```
┌─────────────────────────────────────────────────────────────────┐
│                         Host Machine                            │
│                                                                 │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │              docker-compose 네트워크 (game-net)            │ │
│  │                                                            │ │
│  │  ┌──────────────┐  ┌────────────┐  ┌────────────────────┐ │ │
│  │  │  game-server │  │   mysql    │  │      redis         │ │ │
│  │  │  :11500      │  │  :3306     │  │     :6379          │ │ │
│  │  │              │  │            │  │                    │ │ │
│  │  │ ASP.NET Core │  │ GameDB     │  │ 세션/캐시/Lock      │ │ │
│  │  └──────────────┘  │ MasterDB   │  └────────────────────┘ │ │
│  │                    └────────────┘                          │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                 │
│  포트 노출: 11500 (API), 3306 (DB, 개발용), 6379 (Redis, 개발용)  │
└─────────────────────────────────────────────────────────────────┘
```

컨테이너들은 같은 Docker 네트워크 안에 있어서 서비스 이름으로 서로 통신한다.
예: 게임 서버에서 MySQL 접속 시 호스트명 = `mysql` (컨테이너 이름)

---

## 2. docker-compose.yml 설명

```yaml
# docker-compose.yml
version: '3.8'

networks:
  # 컨테이너 간 통신을 위한 내부 네트워크
  game-net:
    driver: bridge

volumes:
  # MySQL 데이터를 컨테이너가 삭제되어도 유지하기 위한 볼륨
  mysql-data:
  # Redis 데이터 볼륨 (이 프로젝트에서는 영구 저장 안 함)
  redis-data:

services:
  # ─────────────────────────────────────────────
  # MySQL
  # ─────────────────────────────────────────────
  mysql:
    image: mysql:8.0
    container_name: game-mysql
    networks:
      - game-net
    ports:
      - "3306:3306"         # 호스트:컨테이너 (개발 시 로컬에서 DBeaver 등으로 접속 가능)
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}   # .env 파일에서 읽음
      MYSQL_DATABASE: GameDB
      MYSQL_USER: ${MYSQL_USER}
      MYSQL_PASSWORD: ${MYSQL_PASSWORD}
    volumes:
      - mysql-data:/var/lib/mysql                   # 데이터 영구 보존
      - ./DB/schema:/docker-entrypoint-initdb.d     # 초기 스키마 자동 실행
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
      interval: 10s
      timeout: 5s
      retries: 5

  # ─────────────────────────────────────────────
  # Redis
  # ─────────────────────────────────────────────
  redis:
    image: redis:7.0-alpine
    container_name: game-redis
    networks:
      - game-net
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --save "" --appendonly no  # 영구 저장 비활성화
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 3

  # ─────────────────────────────────────────────
  # Game API Server
  # ─────────────────────────────────────────────
  game-server:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: game-api-server
    networks:
      - game-net
    ports:
      - "11500:11500"
    environment:
      # 환경변수로 appsettings.json 값을 덮어씀
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__GameDb: "Server=mysql;Database=GameDB;User=${MYSQL_USER};Password=${MYSQL_PASSWORD};"
      ConnectionStrings__Redis: "redis:6379"
    depends_on:
      mysql:
        condition: service_healthy    # MySQL이 준비된 후 시작
      redis:
        condition: service_healthy    # Redis가 준비된 후 시작
    restart: on-failure               # 크래시 시 자동 재시작
```

### 초기 스키마 자동 실행

MySQL 컨테이너는 `/docker-entrypoint-initdb.d/` 디렉토리의 `.sql` 파일을 최초 시작 시 자동 실행한다.

```
DB/
└── schema/
    ├── 01_create_tables.sql     # 테이블 생성 (번호 순서대로 실행됨)
    └── 02_insert_master_data.sql # MasterDB 초기 데이터
```

---

## 3. 개발 환경 운영

### 처음 실행

```bash
# 1. .env 파일 생성 (최초 1회)
cp .env.example .env
# .env 파일을 편집해서 비밀번호 설정

# 2. 모든 서비스 시작 (백그라운드)
docker compose up -d

# 3. 로그 확인
docker compose logs -f

# 4. 상태 확인
docker compose ps
```

### 서버 코드만 재빌드 (DB는 유지)

```bash
# 게임 서버만 재빌드 후 재시작
docker compose up -d --build game-server
```

### 전체 종료

```bash
# 컨테이너 종료 (데이터는 볼륨에 보존)
docker compose down

# 컨테이너 + 볼륨 전체 삭제 (DB 데이터도 삭제 - 주의!)
docker compose down -v
```

---

## 4. Secrets(민감 정보) 관리

### .env 파일 사용

```bash
# .env.example (Git에 올라가는 템플릿)
MYSQL_ROOT_PASSWORD=change_this_password
MYSQL_USER=gameuser
MYSQL_PASSWORD=change_this_password
ASPNETCORE_ENVIRONMENT=Development
```

```bash
# .env (실제 값, Git에 올리지 않음)
MYSQL_ROOT_PASSWORD=MyActualRootPw!2024
MYSQL_USER=gameuser
MYSQL_PASSWORD=MyActualGamePw!2024
ASPNETCORE_ENVIRONMENT=Development
```

```bash
# .gitignore에 반드시 추가
.env
```

```yaml
# docker-compose.yml에서 .env 파일 참조
services:
  mysql:
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}  # .env에서 읽음
```

### ASP.NET Core에서 환경변수 읽기

```csharp
// Program.cs - 별도 코드 없이 자동으로 환경변수를 설정으로 읽음
// 환경변수 ConnectionStrings__GameDb 는
// 설정 키 ConnectionStrings:GameDb 로 자동 매핑됨 (__ 는 : 를 의미)

var connectionString = builder.Configuration.GetConnectionString("GameDb");
// docker-compose의 환경변수: ConnectionStrings__GameDb=Server=mysql;...
// C#에서 읽을 때: GetConnectionString("GameDb")
```

### User Secrets (로컬 개발 환경)

```bash
# 로컬 개발 시 비밀번호를 appsettings.json에 넣지 않고 User Secrets에 저장
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:GameDb" "Server=localhost;Database=GameDB;User=gameuser;Password=dev_password;"
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379"

# 저장된 User Secrets 확인
dotnet user-secrets list
```

```
우선순위 (높을수록 낮은 것을 덮어씀):
1. appsettings.json              ← 가장 낮음
2. appsettings.Development.json
3. User Secrets (Development 환경)
4. 환경변수                      ← 가장 높음
```

---

## 5. 운영 환경 배포

### Dockerfile

```dockerfile
# Dockerfile
# 멀티 스테이지 빌드: 빌드 환경과 실행 환경을 분리

# 1단계: 빌드
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 패키지 복원 (소스 변경 없으면 캐시 활용)
COPY *.csproj .
RUN dotnet restore

# 소스 복사 및 빌드
COPY . .
RUN dotnet publish -c Release -o /app/publish

# 2단계: 실행
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# 빌드 결과물만 복사 (SDK는 포함되지 않아 이미지가 훨씬 작음)
COPY --from=build /app/publish .

# 비루트 유저로 실행 (보안)
USER app

ENTRYPOINT ["dotnet", "GameAPIServer.dll"]
```

```bash
# 이미지 빌드
docker build -t game-api-server:1.0 .

# 이미지 크기 확인 (SDK 포함 vs 미포함)
docker images game-api-server
```

### 운영 환경 docker-compose

```yaml
# docker-compose.prod.yml
services:
  game-server:
    image: game-api-server:${IMAGE_TAG}  # 빌드된 이미지 사용
    environment:
      ASPNETCORE_ENVIRONMENT: Production
    ports:
      - "11500:11500"
    # 운영에서는 DB 포트를 외부에 노출하지 않음
    # mysql, redis 서비스에서 ports: 섹션 제거
    deploy:
      replicas: 2          # 2개 인스턴스 실행
      restart_policy:
        condition: on-failure
        delay: 5s
        max_attempts: 3
```

```bash
# 운영 환경 배포
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

---

## 6. 자주 쓰는 Docker 명령어

```bash
# 실행 중인 컨테이너 목록
docker compose ps

# 특정 서비스 로그 (실시간)
docker compose logs -f game-server

# 특정 서비스 로그 (마지막 100줄)
docker compose logs --tail=100 game-server

# 실행 중인 컨테이너에 bash로 접속
docker compose exec mysql bash
docker compose exec game-server bash

# MySQL에 직접 접속
docker compose exec mysql mysql -u gameuser -p GameDB

# Redis CLI 접속
docker compose exec redis redis-cli

# 특정 서비스만 재시작
docker compose restart game-server

# 이미지 재빌드 후 재시작
docker compose up -d --build game-server

# 컨테이너 리소스 사용량 확인 (CPU, 메모리)
docker stats
```

---

## 7. 트러블슈팅

### 문제 1: "Can't connect to MySQL server"

```bash
# MySQL이 아직 준비 중인 경우
docker compose logs mysql | tail -20
# "ready for connections" 메시지가 나올 때까지 대기

# healthcheck 상태 확인
docker compose ps
# STATUS에 "(healthy)" 가 나와야 함

# 직접 접속 테스트
docker compose exec mysql mysqladmin ping -h localhost -u root -p
```

### 문제 2: 게임 서버가 "MasterDB 로드 실패"로 시작 안 됨

```bash
# 1. MySQL이 살아있는지 확인
docker compose ps mysql

# 2. 스키마가 올바르게 생성되었는지 확인
docker compose exec mysql mysql -u gameuser -p GameDB
mysql> SHOW TABLES;
mysql> SELECT COUNT(*) FROM master_item;  # 0이면 초기 데이터 미삽입

# 3. 볼륨 삭제 후 재시작 (스키마를 다시 실행)
docker compose down -v
docker compose up -d
```

### 문제 3: Redis 연결 오류

```bash
# Redis 상태 확인
docker compose exec redis redis-cli ping
# PONG 이 나와야 정상

# 연결 설정 확인 (ConnectionStrings:Redis)
docker compose exec game-server printenv | grep Redis
```

### 문제 4: 포트 충돌 ("port is already allocated")

```bash
# 이미 사용 중인 포트 찾기
lsof -i :3306   # MySQL 포트
lsof -i :6379   # Redis 포트
lsof -i :11500  # 게임 서버 포트

# 충돌하는 프로세스 종료 또는 docker-compose.yml에서 포트 변경
# "3306:3306" → "13306:3306" (호스트 포트만 변경)
```
