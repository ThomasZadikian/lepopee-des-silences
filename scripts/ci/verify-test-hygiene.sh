#!/usr/bin/env bash
set -euo pipefail

repo_root=$(git rev-parse --show-toplevel)
cd "$repo_root"

forbidden_patterns=(
  '\.(only|skip|todo)\('
  '\[(Fact|Theory)\s*\([^]]*Skip\s*='
  '#if\s+DISABLE_TEST'
)

status=0
for pattern in "${forbidden_patterns[@]}"; do
  if rg --line-number --glob '*.{cs,ts,tsx,js,jsx}' "$pattern" \
      services packages apps/game-client/src; then
    status=1
  fi
done

if [[ $status -ne 0 ]]; then
  echo "Focused, skipped or disabled tests are forbidden in CI." >&2
  exit "$status"
fi

echo "Test hygiene check passed."
