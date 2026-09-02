#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 3 ]]; then
  echo "Usage: $0 <component-name> <solution-path> <assembly-filter>" >&2
  exit 64
fi

component_name=$1
solution_path=$2
assembly_filter=$3
repo_root=$(git rev-parse --show-toplevel)
results_dir="$repo_root/artifacts/test-results/$component_name"
coverage_dir="$repo_root/artifacts/coverage/$component_name"
runsettings="$repo_root/eng/coverage/coverage.runsettings"
coverage_threshold=80
internal_target=85

if [[ ! -f "$repo_root/$solution_path" ]]; then
  echo "Unknown solution: $solution_path" >&2
  exit 66
fi

mkdir -p "$results_dir" "$coverage_dir"

dotnet restore "$repo_root/$solution_path"
dotnet build "$repo_root/$solution_path" --configuration Release --no-restore \
  -p:ContinuousIntegrationBuild=true \
  -p:Deterministic=true

# Coverage instrumentation is memory intensive and solution-level `dotnet test` otherwise
# runs the unit/integration projects concurrently. In the Game Engine this can starve the
# integration testhost long enough for the 5-minute hang detector to kill an otherwise
# healthy run. Limit MSBuild to one test project at a time; xUnit still keeps its normal
# in-project parallelism, while every project continues to emit its own Cobertura report.
dotnet test "$repo_root/$solution_path" \
  --configuration Release \
  --no-build \
  --settings "$runsettings" \
  --collect "XPlat Code Coverage" \
  --results-directory "$results_dir" \
  --logger "trx;LogFilePrefix=$component_name" \
  --blame-hang-timeout 5m \
  -m:1

if ! find "$results_dir" -name coverage.cobertura.xml -print -quit | grep -q .; then
  echo "No Cobertura report was produced for $component_name." >&2
  exit 1
fi

dotnet tool run reportgenerator \
  "-reports:$results_dir/**/coverage.cobertura.xml" \
  "-targetdir:$coverage_dir" \
  "-reporttypes:HtmlInline_AzurePipelines;Cobertura;JsonSummary;TextSummary" \
  "-assemblyfilters:+$assembly_filter;-*.UnitTests;-*.IntegrationTests" \
  "-filefilters:-**/Migrations/**;-**/*.Designer.cs;-**/obj/**"

if [[ -f "$coverage_dir/Summary.txt" ]]; then
  cat "$coverage_dir/Summary.txt"
fi

summary_json="$coverage_dir/Summary.json"
if [[ ! -f "$summary_json" ]]; then
  echo "ReportGenerator did not produce $summary_json." >&2
  exit 1
fi

python3 - "$component_name" "$summary_json" "$coverage_threshold" "$internal_target" <<'PY'
import json
import math
import sys

component, summary_path, threshold_text, target_text = sys.argv[1:]
threshold = float(threshold_text)
internal_target = float(target_text)

with open(summary_path, encoding="utf-8") as stream:
    summary = json.load(stream)["summary"]

metrics = (
    ("lines", int(summary["coveredlines"]), int(summary["coverablelines"])),
    ("branches", int(summary["coveredbranches"]), int(summary["totalbranches"])),
    ("methods", int(summary["coveredmethods"]), int(summary["totalmethods"])),
)

failures = []
below_internal_target = []
print(f"Coverage quality gate for {component}: required >= {threshold:.0f}% for lines, branches and methods.")
print(f"Internal quality target: >= {internal_target:.0f}% (advisory only; does not fail CI).")

for label, covered, total in metrics:
    ratio = 100.0 if total == 0 else (covered / total) * 100.0
    required = 0 if total == 0 else math.ceil(total * threshold / 100.0)
    print(f"  {label:8}: {ratio:6.2f}% ({covered}/{total}; CI minimum covered: {required})")
    if ratio + 1e-12 < threshold:
        failures.append((label, ratio, required - covered))
    elif ratio + 1e-12 < internal_target:
        below_internal_target.append((label, ratio))

if failures:
    print("Coverage gate failed:", file=sys.stderr)
    for label, ratio, missing in failures:
        print(
            f"  - {label}: {ratio:.2f}% < {threshold:.0f}% "
            f"({max(missing, 0)} additional covered item(s) required at the current denominator)",
            file=sys.stderr,
        )
    sys.exit(1)

if below_internal_target:
    print("Internal coverage target not yet reached (CI remains green):")
    for label, ratio in below_internal_target:
        print(f"  - {label}: {ratio:.2f}% < {internal_target:.0f}%")

print("Coverage gate passed.")
PY
