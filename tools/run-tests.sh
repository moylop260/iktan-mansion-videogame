#!/usr/bin/env bash
# Runs the EditMode and PlayMode tests with code coverage.
# Usage: tools/run-tests.sh [path-to-Unity]   (default: newest editor in the Unity Hub folder)
# Close every other Unity process first: two editors cannot open the same project.
set -euo pipefail

cd "$(dirname "$0")/.."
UNITY="${1:-$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity | sort -V | tail -1)}"
OUT="TestResults"
COV="CodeCoverage"
rm -rf "$OUT" "$COV"
mkdir -p "$OUT"

for platform in EditMode PlayMode; do
  echo "== $platform tests =="
  "$UNITY" -batchmode -nographics -projectPath "$PWD" \
    -runTests -testPlatform "$platform" \
    -testResults "$OUT/$platform.xml" \
    -enableCodeCoverage -debugCodeOptimization \
    -coverageResultsPath "$COV" \
    -coverageOptions "generateHtmlReport;generateBadgeReport;generateAdditionalMetrics;assemblyFilters:+Mansion.Runtime" \
    -logFile "$OUT/$platform.log"
done

echo
echo "Results:  $OUT/*.xml"
echo "Coverage: $COV/Report/index.html (summary: $COV/Report/Summary.txt)"
