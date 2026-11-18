#!/usr/bin/env bash
# Judges /work: what changed, build and tests of the solution, then the hidden acceptance
# suite /judge/cases/$BENCH_CASE/acceptance against the API started on 127.0.0.1:5080.
# Environment: BENCH_CASE, BENCH_SOLUTION, BENCH_API, BENCH_CONNECTION, BENCH_JUDGE
# (acceptance|unchanged), optional BENCH_JWT_KEY and BENCH_JWT_ISSUER.
set -u
out=/bench/out
cd /work
[ -d .git ] || { git init -q && git add -A && git commit -qm start; }

git add -A
git diff --cached --numstat HEAD > $out/numstat.txt
git diff --cached --binary HEAD > $out/changes.patch
git status --porcelain > $out/status.txt

[ "$BENCH_JUDGE" = "acceptance" ] || exit 0

dotnet build "$BENCH_SOLUTION" > $out/build.log 2>&1
echo $? > $out/build.exit
[ "$(cat $out/build.exit)" = "0" ] || exit 0

timeout 15m dotnet test "$BENCH_SOLUTION" --no-build > $out/test.log 2>&1
echo $? > $out/test.exit

export ASPNETCORE_URLS=http://127.0.0.1:5080
export "ConnectionStrings__${BENCH_CONNECTION}=Data Source=/tmp/judge.db"
[ -n "${BENCH_JWT_KEY:-}" ] && export Jwt__Key="$BENCH_JWT_KEY"
dotnet run --no-build --project "$BENCH_API" > $out/api.log 2>&1 &
api=$!
for _ in $(seq 1 90); do
  code=$(curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:5080/ || true)
  [ "$code" != "000" ] && break
  kill -0 $api 2>/dev/null || break
  sleep 1
done

cd "/judge/cases/$BENCH_CASE/acceptance"
BENCH_BASE_URL=http://127.0.0.1:5080 timeout 15m dotnet test \
  --logger "trx;LogFileName=acceptance.trx" --results-directory $out > $out/acceptance.log 2>&1
echo $? > $out/acceptance.exit
kill $api 2>/dev/null
exit 0
