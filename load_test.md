# 부하 테스트 가이드

부하 테스트는 서버가 실제 트래픽을 견딜 수 있는지 배포 전에 검증하는 작업이다.
모니터링(Prometheus + Grafana)은 운영 중 상태를 보는 것이고,
부하 테스트는 **배포 전에 미리 한계를 발견**하는 것이다.

이 프로젝트에서는 **k6**를 사용한다. JavaScript로 시나리오를 작성하고, 결과를 Grafana로 시각화할 수 있다.

---

## 목차

1. [k6 설치 및 기본 사용법](#1-k6-설치-및-기본-사용법)
2. [게임 서버 시나리오 작성](#2-게임-서버-시나리오-작성)
3. [부하 테스트 종류](#3-부하-테스트-종류)
4. [결과 해석 및 병목 찾기](#4-결과-해석-및-병목-찾기)
5. [Prometheus + Grafana 연동](#5-prometheus--grafana-연동)

---

## 1. k6 설치 및 기본 사용법

### 설치

```bash
# macOS
brew install k6

# Linux (Ubuntu/Debian)
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6

# Docker
docker run --rm -i grafana/k6 run - <script.js
```

### 가장 간단한 테스트

```javascript
// simple_test.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 10,          // 동시 가상 유저 수
    duration: '30s',  // 테스트 시간
};

export default function () {
    // 로그인 API 호출
    const response = http.post('http://localhost:11500/Login', JSON.stringify({
        PlayerID: 1,
        HiveToken: 'test_token'
    }), {
        headers: { 'Content-Type': 'application/json' }
    });

    // 응답 검증
    check(response, {
        '상태코드 200': (r) => r.status === 200,
        '응답 시간 500ms 이하': (r) => r.timings.duration < 500,
        '에러코드 없음': (r) => JSON.parse(r.body).Result === 0,
    });

    sleep(1); // 1초 대기 (실제 유저는 연속으로 요청하지 않음)
}
```

```bash
# 실행
k6 run simple_test.js

# 결과 예시
# scenarios: (100.00%) 1 scenario, 10 max VUs, 1m0s max duration
# default: 10 looping VUs for 30s (gracefulStop: 30s)
#
# ✓ 상태코드 200
# ✓ 응답 시간 500ms 이하
# ✓ 에러코드 없음
#
# checks.........................: 100.00% ✓ 297 ✗ 0
# data_received..................: 14 kB 460 B/s
# http_req_duration..............: avg=42ms min=12ms med=38ms max=312ms p(90)=78ms p(95)=95ms
# http_reqs......................: 297 9.9/s
# iterations.....................: 99 3.3/s
# vus............................: 10 min=10 max=10
```

---

## 2. 게임 서버 시나리오 작성

실제 유저 행동을 시뮬레이션하는 시나리오다.

### 로그인 → 게임 플레이 시나리오

```javascript
// game_scenario.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { SharedArray } from 'k6/data';

const BASE_URL = 'http://localhost:11500';

// 테스트용 계정 데이터 (사전에 DB에 생성해 둠)
const testUsers = new SharedArray('users', function () {
    return Array.from({ length: 100 }, (_, i) => ({
        playerId: i + 1,
        hiveToken: `test_token_${i + 1}`,
    }));
});

export const options = {
    stages: [
        { duration: '30s', target: 50 },   // 30초 동안 50명까지 증가
        { duration: '1m', target: 50 },    // 1분 동안 50명 유지
        { duration: '30s', target: 100 },  // 30초 동안 100명까지 증가
        { duration: '2m', target: 100 },   // 2분 동안 100명 유지
        { duration: '30s', target: 0 },    // 30초 동안 종료
    ],
    thresholds: {
        // 성능 기준 (이 기준을 넘으면 테스트 실패 처리)
        'http_req_duration': ['p(95)<1000'],  // 95%의 요청이 1초 이하
        'http_req_failed': ['rate<0.01'],     // 실패율 1% 미만
        'checks': ['rate>0.99'],              // 검증 통과율 99% 이상
    },
};

export default function () {
    // 1. 랜덤 유저로 로그인
    const user = testUsers[Math.floor(Math.random() * testUsers.length)];

    const loginRes = http.post(`${BASE_URL}/Login`, JSON.stringify({
        PlayerID: user.playerId,
        HiveToken: user.hiveToken,
    }), { headers: { 'Content-Type': 'application/json' } });

    check(loginRes, {
        '로그인 성공': (r) => r.status === 200 && JSON.parse(r.body).Result === 0,
    });

    if (loginRes.status !== 200) return; // 로그인 실패 시 종료

    // 쿠키 추출 (이후 요청에 사용)
    const jar = loginRes.cookies;
    const params = {
        headers: { 'Content-Type': 'application/json' },
        cookies: jar,
    };

    sleep(0.5);

    // 2. 유저 데이터 로드
    const dataRes = http.post(`${BASE_URL}/UserDataLoad`, null, params);
    check(dataRes, {
        '데이터 로드 성공': (r) => r.status === 200,
    });

    sleep(0.5);

    // 3. 우편함 확인
    const mailRes = http.get(`${BASE_URL}/MailList`, params);
    check(mailRes, {
        '우편함 조회 성공': (r) => r.status === 200,
    });

    sleep(1);

    // 4. 로그아웃
    const logoutRes = http.get(`${BASE_URL}/Logout`, params);
    check(logoutRes, {
        '로그아웃 성공': (r) => r.status === 200,
    });

    sleep(1);
}
```

### 특정 API 집중 테스트

```javascript
// stress_login.js - 로그인 API만 집중 테스트
import http from 'k6/http';
import { check } from 'k6';
import { Rate } from 'k6/metrics';

const errorRate = new Rate('errors');
const BASE_URL = 'http://localhost:11500';

export const options = {
    vus: 200,
    duration: '1m',
    thresholds: {
        errors: ['rate<0.05'],
        http_req_duration: ['p(99)<2000'],
    },
};

export default function () {
    const response = http.post(`${BASE_URL}/Login`, JSON.stringify({
        PlayerID: Math.floor(Math.random() * 1000) + 1,
        HiveToken: 'test_token',
    }), { headers: { 'Content-Type': 'application/json' } });

    const success = check(response, {
        '상태코드 200': (r) => r.status === 200,
        '1초 이내 응답': (r) => r.timings.duration < 1000,
    });

    errorRate.add(!success);
}
```

---

## 3. 부하 테스트 종류

목적에 따라 다른 종류의 테스트를 사용한다.

### 3-1. 스모크 테스트 (Smoke Test)

```javascript
// 가장 가벼운 테스트: 기본 동작 확인
export const options = {
    vus: 1,
    duration: '1m',
};
// 목적: 배포 직후 "서버가 살아있는가?" 확인
// 실행 시점: 배포 직후
```

### 3-2. 부하 테스트 (Load Test)

```javascript
// 일반적인 부하 테스트: 정상 트래픽 + 피크 트래픽 검증
export const options = {
    stages: [
        { duration: '2m', target: 100 },  // 워밍업
        { duration: '5m', target: 100 },  // 정상 부하 유지
        { duration: '2m', target: 200 },  // 피크 부하
        { duration: '5m', target: 200 },  // 피크 유지
        { duration: '2m', target: 0 },    // 종료
    ],
};
// 목적: "예상 트래픽을 견디는가?"
// 실행 시점: 신규 기능 배포 전
```

### 3-3. 스트레스 테스트 (Stress Test)

```javascript
// 한계점 찾기: 서버가 언제 무너지는가?
export const options = {
    stages: [
        { duration: '2m', target: 100 },
        { duration: '5m', target: 100 },
        { duration: '2m', target: 200 },
        { duration: '5m', target: 200 },
        { duration: '2m', target: 300 },
        { duration: '5m', target: 300 },
        { duration: '2m', target: 400 }, // 계속 늘려서 언제 실패하는지 확인
        { duration: '2m', target: 0 },
    ],
};
// 목적: "최대 몇 명까지 처리할 수 있는가?"
// 실행 시점: 신규 서버 출시 전, 대규모 이벤트 전
```

### 3-4. 스파이크 테스트 (Spike Test)

```javascript
// 갑작스러운 트래픽 폭증 대응 테스트
export const options = {
    stages: [
        { duration: '10s', target: 100 },  // 정상
        { duration: '1m', target: 100 },
        { duration: '10s', target: 1000 }, // 갑자기 10배 폭증!
        { duration: '3m', target: 1000 },
        { duration: '10s', target: 100 },  // 다시 정상
        { duration: '3m', target: 100 },
        { duration: '10s', target: 0 },
    ],
};
// 목적: "이벤트 오픈, 유명 스트리머 방송 등 갑작스러운 트래픽에 대응 가능한가?"
```

---

## 4. 결과 해석 및 병목 찾기

### 핵심 지표

| 지표 | 의미 | 게임 서버 기준 |
|:---|:---|:---|
| `http_req_duration` | 응답 시간 | p(95) < 500ms |
| `http_req_failed` | 실패율 | < 1% |
| `http_reqs` | 초당 처리 요청 수 (RPS) | 목표 RPS 이상 |
| `p(90), p(95), p(99)` | 백분위 응답 시간 | p(99) < 2000ms |

### 결과 해석 예시

```
# 좋은 결과
http_req_duration: avg=45ms, p(90)=89ms, p(95)=112ms, p(99)=245ms
http_req_failed: 0.00%
→ 매우 안정적. 99%의 요청이 245ms 이내 처리됨

# 나쁜 결과 1: 응답 시간이 점점 느려짐
http_req_duration: avg=45ms → 120ms → 450ms → 1200ms (시간이 갈수록 증가)
→ 메모리 누수, 커넥션 풀 고갈, DB 슬로우 쿼리 의심

# 나쁜 결과 2: 특정 구간에서 에러 폭발
http_req_failed: 0% → 0% → 15% → 30% (VU 300명 구간에서 급격히 증가)
→ 300 VU가 서버 한계. Rate Limiting 조정 또는 스케일 아웃 필요
```

### 병목 지점 찾기

```bash
# 1. k6 테스트 실행 중 서버 로그 확인
tail -f logs/game-server.log | grep "ERROR\|WARN\|slow"

# 2. Prometheus + Grafana에서 실시간 확인
# - CPU, 메모리 사용률
# - DB 커넥션 수
# - Redis 연결 수
# - API별 응답 시간

# 3. dotnet-counters로 .NET 런타임 상태 확인
dotnet-counters monitor --process-id <PID>
# 확인 항목:
# - GC 빈도 (너무 잦으면 메모리 할당 문제)
# - Thread Pool 크기 (고갈되면 요청 대기 발생)
# - Exception 발생 수
```

### 자주 발견되는 병목과 해결책

| 증상 | 원인 | 해결책 |
|:---|:---|:---|
| 부하 증가 시 응답 시간 급격히 증가 | DB 커넥션 풀 고갈 | MySqlConnector MaxPoolSize 조정 |
| 특정 API만 느림 | 인덱스 없는 쿼리 | EXPLAIN으로 쿼리 분석, 인덱스 추가 |
| 메모리 지속 증가 | 메모리 누수 | dotnet-counters로 GC 모니터링 |
| 간헐적 타임아웃 | Redis 연결 끊김 | CloudStructures 연결 설정 검토 |
| CPU 100% | 무거운 계산 로직 | 프로파일링으로 핫스팟 탐지 |

---

## 5. Prometheus + Grafana 연동

k6 결과를 Grafana로 실시간 시각화할 수 있다.

```yaml
# docker-compose에 k6 + InfluxDB 추가
services:
  influxdb:
    image: influxdb:1.8
    ports:
      - "8086:8086"
    environment:
      INFLUXDB_DB: k6
      INFLUXDB_HTTP_AUTH_ENABLED: false

  grafana:
    image: grafana/grafana
    ports:
      - "3000:3000"
    depends_on:
      - influxdb
```

```bash
# k6 결과를 InfluxDB로 전송 (Grafana에서 실시간 확인 가능)
k6 run --out influxdb=http://localhost:8086/k6 game_scenario.js
```

Grafana에서 "k6 Load Testing Results" 대시보드 템플릿(ID: 2587)을 임포트하면 바로 사용 가능하다.

### 부하 테스트 중 Grafana에서 확인할 패널

- **Active VUs**: 현재 동시 접속 유저 수
- **Request Rate**: 초당 요청 수 (RPS)
- **Response Time (p95, p99)**: 응답 시간 분포
- **Error Rate**: 에러 비율
- **Checks Passed**: 검증 통과 비율

이 지표와 서버 메트릭(CPU, 메모리, DB 커넥션 수)을 같은 시간축에 놓고 보면 병목 지점을 정확히 찾을 수 있다.
