#!/usr/bin/env bash
#
# Writes the report of one CI run into the sample branch checked out in the current folder:
#
#   ./report.sh <run id>
#
# The reports live in reports/<branch>/, so two branches merged together keep each other's reports.
# reports/<branch>/<version>.json keeps every run of the branch generated from that version (a release tag,
# or the template's commit for full-dev) and the branch's files; <version>.md is the same for a reader.
# reports/<branch>/ci.svg draws the branch's runs by day for its README (ci-svg.py).
# reports/<branch>/index.json lists the reports with their last run: the site reads it from
# raw.githubusercontent.com, which cannot list a folder. Reports an earlier run left directly in reports/
# are moved into the branch's folder. Needs gh with access to the run, its log and its coverage artifact.
set -euo pipefail

run="$1"
repo="${GITHUB_REPOSITORY:-sawking-tech/DotNetSolutionKit.Samples}"
work="$(mktemp -d)"
version=$(tr -d '\r\n' < template-version)
branch=$(git rev-parse --abbrev-ref HEAD)

gh api "repos/$repo/actions/runs/$run" > "$work/run.json"

# dotnet test prints one line per test assembly: "Passed!  - Failed: 0, Passed: 332, Skipped: 0, ..."
gh run view "$run" -R "$repo" --log 2>/dev/null | grep -E '(Passed|Failed)! +- Failed:' > "$work/tests" || true
tests=$(sed -E 's/.*Failed: *([0-9]+), Passed: *([0-9]+), Skipped: *([0-9]+), Total: *([0-9]+).*/\1 \2 \3 \4/' "$work/tests" \
    | awk '{f+=$1; p+=$2; s+=$3; t+=$4} END {printf "{\"failed\":%d,\"passed\":%d,\"skipped\":%d,\"total\":%d}", f, p, s, t}')

coverage=null
if gh run download "$run" -R "$repo" -n coverage -D "$work/coverage" 2>/dev/null && [ -f "$work/coverage/Summary.json" ]; then
    coverage=$(jq -c '.summary | {line: .linecoverage, branch: .branchcoverage}' "$work/coverage/Summary.json")
fi

entry=$(jq -c --argjson tests "$tests" --argjson coverage "$coverage" '{
    run: .id, url: .html_url, commit: .head_sha, conclusion: .conclusion, event: .event,
    started: .run_started_at, finished: .updated_at,
    seconds: ((.updated_at | fromdateiso8601) - (.run_started_at | fromdateiso8601)),
    tests: $tests, coverage: $coverage }' "$work/run.json")

dir="reports/$branch"
mkdir -p "$dir"
for old in reports/*.json reports/*.md; do
    [ -f "$old" ] || continue
    if [ "$(basename "$old")" = index.json ]; then git rm -q "$old"; else git mv "$old" "$dir/"; fi
done
file="$dir/$version.json"
[ -f "$file" ] || echo '{"runs": []}' > "$file"
git ls-files | grep -v '^reports/' > "$work/files"
jq --arg branch "$branch" --arg version "$version" --argjson entry "$entry" --rawfile files "$work/files" '
    .branch = $branch | .version = $version
    | .runs = ([.runs[] | select(.run != $entry.run)] + [$entry])
    | .files = ($files | split("\n") | map(select(length > 0)))' "$file" > "$work/report.json"
mv "$work/report.json" "$file"

{
    echo "# $branch from $version"
    echo
    echo "| Run | Started | Result | Tests passed / total | Line coverage | Branch coverage |"
    echo "|---|---|---|---|---|---|"
    jq -r '.runs[] | "| [\(.run)](\(.url)) | \(.started) | \(.conclusion) | \(.tests.passed) / \(.tests.total) | \(.coverage.line // "-")% | \(.coverage.branch // "-")% |"' "$file"
    echo
    echo "$(jq '.files | length' "$file") files in the branch; the list is in [$version.json]($version.json)."
} > "$dir/$version.md"
jq -s 'map(select(.runs | length > 0) | {version, file: "\(.version).json", runs: (.runs | length), last: (.runs[-1] | {conclusion, started, tests, coverage, seconds})})
    | sort_by(.last.started) | reverse' $(ls "$dir"/*.json | grep -v '/index.json$') > "$dir/index.json"
# The same days as a picture, for the README: GitHub shows an image there, not the site's script.
python3 "$(dirname "$0")/ci-svg.py" "$dir" "$branch" "$dir/ci.svg"
echo "$file: $(jq -c '.runs[-1] | {conclusion, tests, coverage, seconds}' "$file")"
