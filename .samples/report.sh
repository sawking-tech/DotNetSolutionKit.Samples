#!/usr/bin/env bash
#
# Writes the report of one CI run into the sample branch checked out in the current folder:
#
#   ./report.sh <run id>
#
# reports/<version>.json keeps every run of the branch generated from that version (a release tag, or the
# template's commit for nightly) and the branch's files; reports/<version>.md is the same for a reader.
# The site reads the JSON. Needs gh with access to the run, its log and its coverage artifact.
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

mkdir -p reports
file="reports/$version.json"
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
} > "reports/$version.md"
echo "$file: $(jq -c '.runs[-1] | {conclusion, tests, coverage, seconds}' "$file")"
