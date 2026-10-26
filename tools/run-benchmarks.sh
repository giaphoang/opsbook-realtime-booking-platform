#!/usr/bin/env bash
# Benchmarks for the OpsBook realtime delivery work.
# Runs inside mcr.microsoft.com/dotnet/sdk:8.0 with /work mounted at the repo root.
set -euo pipefail

BASE_URL="http://127.0.0.1:5199"

# 1. Boot the API with the in-memory database (no SQL Server dependency).
ASPNETCORE_URLS="$BASE_URL" \
Ops__UseInMemoryDatabase=true \
ASPNETCORE_ENVIRONMENT=Development \
dotnet /work/SignalRApi/bin/Debug/net8.0/SignalRApi.dll &
SERVER_PID=$!

echo "Waiting for the API to come up..."
for _ in $(seq 1 60); do
  if curl -sf "$BASE_URL/swagger/index.html" > /dev/null 2>&1 \
     || curl -sf "$BASE_URL/api/LatencyBenchmark" -o /dev/null 2>&1; then
    break
  fi
  sleep 1
done

echo "API is up. Running delivery-latency benchmark (32 iterations per mode)."

# 2. Polling baseline: receiver sees each committed write only on the next poll tick.
POLLING=$(curl -s -X POST "$BASE_URL/api/LatencyBenchmark" \
  -H 'Content-Type: application/json' \
  -d '{"Mode":0,"Iterations":32,"PollingIntervalMs":1000}')
echo "POLLING_RESULT=$POLLING"

# 3. SignalR push baseline: same writes, event delivered on commit.
PUSH=$(curl -s -X POST "$BASE_URL/api/LatencyBenchmark" \
  -H 'Content-Type: application/json' \
  -d '{"Mode":1,"Iterations":32,"PollingIntervalMs":0}')
echo "PUSH_RESULT=$PUSH"

# 4. Load test with k6: 1x vs 4x traffic on the read path.
K6_VERSION="v0.50.0"
if [ ! -x /tmp/k6/k6 ]; then
  curl -sL "https://github.com/grafana/k6/releases/download/${K6_VERSION}/k6-${K6_VERSION}-linux-amd64.tar.gz" \
    | tar -xz -C /tmp
fi
K6_BIN=$(find /tmp -name k6 -type f | head -1)

cat > /tmp/load-test.js <<'EOF'
import http from 'k6/http';

export function runScenario(vus, duration) {
  return {
    vus: vus,
    duration: duration,
    executor: 'constant-vus',
  };
}

export const options = __ENV.VUS
  ? { vus: Number(__ENV.VUS), duration: __ENV.DURATION || '30s' }
  : { executor: 'constant-vus', vus: 10, duration: '30s' };

export default function () {
  http.get('http://127.0.0.1:5199/api/MoneyCases/TotalMoneyCaseAmount');
}
EOF

echo "Warmup (3s)..."
"$K6_BIN" run --quiet --warmup 0s -u 5 -d 3s /tmp/load-test.js > /dev/null 2>&1 || true

echo "Load run 1x traffic (10 virtual users, 30s)"
"$K6_BIN" run --quiet -u 10 -d 30s --summary-export /tmp/k6-1x.json /tmp/load-test.js | tail -20 || true

echo "Load run 4x traffic (40 virtual users, 30s)"
"$K6_BIN" run --quiet -u 40 -d 30s --summary-export /tmp/k6-4x.json /tmp/load-test.js | tail -20 || true

kill $SERVER_PID 2>/dev/null || true
wait $SERVER_PID 2>/dev/null || true
