#!/usr/bin/env bash
# One pass of Claude Code on /work, with /bench/prompt.md. Writes /bench/out/transcript.jsonl.
# Environment: BENCH_MODEL, BENCH_MAX_TURNS, BENCH_TIMEOUT_MINUTES, and ANTHROPIC_API_KEY or
# CLAUDE_CODE_OAUTH_TOKEN.
set -u
cd /work
git init -q && git add -A && git commit -qm start

# A configuration directory of its own: no memory, no user CLAUDE.md, no personal setting.
export CLAUDE_CONFIG_DIR=/home/bench/.claude-pass
rm -rf "$CLAUDE_CONFIG_DIR" && mkdir -p "$CLAUDE_CONFIG_DIR"

date -u +%Y-%m-%dT%H:%M:%SZ > /bench/out/started
timeout --signal=TERM "${BENCH_TIMEOUT_MINUTES}m" \
  claude -p "$(cat /bench/prompt.md)" \
    --model "$BENCH_MODEL" \
    --output-format stream-json --verbose \
    --dangerously-skip-permissions \
    --max-turns "$BENCH_MAX_TURNS" \
  > /bench/out/transcript.jsonl 2> /bench/out/claude.stderr
echo $? > /bench/out/claude.exit
date -u +%Y-%m-%dT%H:%M:%SZ > /bench/out/ended
claude --version > /bench/out/claude.version 2>/dev/null
exit 0
