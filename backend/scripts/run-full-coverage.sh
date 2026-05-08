#!/usr/bin/env bash
# Run full-solution .NET tests with Coverlet + ReportGenerator outputs.
# Usage (from repo root): backend/scripts/run-full-coverage.sh
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/../.." && pwd)"
results_dir="${repo_root}/TestResultsFullCoverage"
migrations_file_filter="**/MockHealthSystem.Infrastructure/Migrations/*.cs"

# Avoid IDE/session proxy interference during restore/test runs.
export HTTP_PROXY=
export HTTPS_PROXY=
export http_proxy=
export https_proxy=
export ALL_PROXY=
export all_proxy=
export GIT_HTTP_PROXY=
export GIT_HTTPS_PROXY=
export SOCKS_PROXY=
export SOCKS5_PROXY=
export socks_proxy=
export socks5_proxy=

rm -rf "${results_dir}"
mkdir -p "${results_dir}"

set +e
dotnet test "${repo_root}/backend/MockHealthSystem.sln" \
  --results-directory "${results_dir}" \
  --collect:"XPlat Code Coverage" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura \
  DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile="${migrations_file_filter}"
test_exit_code=$?
set -e

if ! command -v reportgenerator >/dev/null 2>&1; then
  echo "reportgenerator command not found. Install with: dotnet tool install -g dotnet-reportgenerator-globaltool"
  exit 1
fi

shopt -s globstar nullglob
coverage_reports=("${results_dir}"/**/coverage.cobertura.xml)
shopt -u globstar nullglob

if [ ${#coverage_reports[@]} -eq 0 ]; then
  echo "No coverage.cobertura.xml files found under ${results_dir}."
  exit "${test_exit_code}"
fi

reportgenerator \
  -reports:"${results_dir}/**/coverage.cobertura.xml" \
  -targetdir:"${results_dir}/report-html" \
  -reporttypes:"Html;TextSummary"

echo "Coverage artifacts generated:"
echo "  - Cobertura XML files: ${results_dir}/**/coverage.cobertura.xml"
echo "  - HTML report: ${results_dir}/report-html/index.html"
echo "  - Text summary: ${results_dir}/report-html/Summary.txt"
exit "${test_exit_code}"
