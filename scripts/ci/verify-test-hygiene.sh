#!/usr/bin/env bash
set -euo pipefail

repo_root=$(git rev-parse --show-toplevel)
cd "$repo_root"

forbidden_patterns=(
  '\.(only|skip|todo)\('
  '\[(Fact|Theory)[[:space:]]*\([^]]*Skip[[:space:]]*='
  '#if[[:space:]]+DISABLE_TEST'
)

search_test_sources() {
  local pattern=$1

  if command -v rg >/dev/null 2>&1; then
    rg --line-number --glob '*.{cs,ts,tsx,js,jsx}' "$pattern" \
      services packages apps/game-client/src
    return
  fi

  grep --recursive --line-number --extended-regexp \
    --include='*.cs' --include='*.ts' --include='*.tsx' \
    --include='*.js' --include='*.jsx' \
    "$pattern" services packages apps/game-client/src
}

status=0
for pattern in "${forbidden_patterns[@]}"; do
  if search_test_sources "$pattern"; then
    status=1
  fi
done

if [[ $status -ne 0 ]]; then
  echo "Focused, skipped or disabled tests are forbidden in CI." >&2
  exit "$status"
fi

echo "Test hygiene check passed."
