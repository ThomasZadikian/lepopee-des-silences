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

if [[ ! -f "$repo_root/$solution_path" ]]; then
  echo "Unknown solution: $solution_path" >&2
  exit 66
fi

mkdir -p "$results_dir" "$coverage_dir"

dotnet restore "$repo_root/$solution_path"
dotnet build "$repo_root/$solution_path" --configuration Release --no-restore \
  -p:ContinuousIntegrationBuild=true \
  -p:Deterministic=true

dotnet test "$repo_root/$solution_path" \
  --configuration Release \
  --no-build \
  --settings "$runsettings" \
  --collect "XPlat Code Coverage" \
  --results-directory "$results_dir" \
  --logger "trx;LogFilePrefix=$component_name" \
  --blame-hang-timeout 5m

if ! find "$results_dir" -name coverage.cobertura.xml -print -quit | grep -q .; then
  echo "No Cobertura report was produced for $component_name." >&2
  exit 1
fi

dotnet tool run reportgenerator \
  "-reports:$results_dir/**/coverage.cobertura.xml" \
  "-targetdir:$coverage_dir" \
  "-reporttypes:HtmlInline_AzurePipelines;Cobertura;JsonSummary;TextSummary" \
  "-assemblyfilters:+$assembly_filter;-*.UnitTests;-*.IntegrationTests" \
  "-filefilters:-**/Migrations/**;-**/*.Designer.cs;-**/obj/**" \
  "-minimumCoverageThresholds:lineCoverage=80" \
  "-minimumCoverageThresholds:branchCoverage=80" \
  "-minimumCoverageThresholds:methodCoverage=80"

if [[ -f "$coverage_dir/Summary.txt" ]]; then
  cat "$coverage_dir/Summary.txt"
fi
