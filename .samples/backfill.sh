#!/usr/bin/env bash
#
# Backfills the missing version reports of one sample branch:
#
#   ./backfill.sh <branch> [version ...]
#
# For each version the branch has no report of, oldest first: generate the branch from that release
# (generate.sh), replace the branch with the generation, wait for the push's CI run and its report,
# then take the next version. A version whose template does not know the branch's flags fails the
# generation: it is logged and skipped. The branch ends up generated from the newest version that
# worked. Without versions, every stable release on nuget.org is a candidate.
#
# The reports of earlier runs stay on the branch; the push uses the deploy key through GIT_SSH_COMMAND,
# as the regeneration does, so the workflows the push starts run.
set -uo pipefail

branch="$1"; shift || { echo "usage: backfill.sh <branch> [version ...]" >&2; exit 1; }
repo="${GITHUB_REPOSITORY:-sawking-tech/DotNetSolutionKit.Samples}"
template_repo="${TEMPLATE_REPO:-sawking-tech/DotNetSolutionKit}"
here="$(cd "$(dirname "$0")" && pwd)"
sample="$RUNNER_TEMP/sample"
wait_minutes="${WAIT_MINUTES:-60}"

if [ "$#" -gt 0 ]; then
    versions="$1"
else
    versions=$(curl -fsS https://api.nuget.org/v3-flatcontainer/sawking.dotnetsolutionkit/index.json \
        | jq -r '[.versions[] | select(test("^[0-9]+\\.[0-9]+\\.[0-9]+$"))] | join(" ")')
fi
echo "$branch: candidates $versions"

replace() {
    local version="$1" key="$RUNNER_TEMP/deploy_key"
    printf '%s\n' "$DEPLOY_KEY" > "$key" && chmod 600 "$key"
    export GIT_SSH_COMMAND="ssh -i $key -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new"
    git clone -q "git@github.com:$repo.git" "$RUNNER_TEMP/branch"
    cd "$RUNNER_TEMP/branch"
    # The exact ref: a bare name matches by its tail, and a pull request's branch such as feat/full-alt
    # would pass for full-alt.
    if git ls-remote --exit-code origin "refs/heads/$branch" >/dev/null; then
        git checkout -q "$branch"
    else
        git checkout -q --orphan "$branch"
    fi
    git rm -rqf . 2>/dev/null || true
    # The reports of earlier runs stay: they are the branch's history, which the site shows.
    git checkout -q HEAD -- reports 2>/dev/null || true
    cp -a "$sample/." .
    git add -A
    git -c user.name="github-actions[bot]" -c user.email="41898282+github-actions[bot]@users.noreply.github.com" \
        commit -qm "Generated from DotNetSolutionKit v$version"
    git push -q origin "$branch"
    git rev-parse HEAD
}

# One checking run, its conclusion reached and its report on the branch, before the next version:
# the report of a version a later step overwrites must exist first.
wait_ci() {
    local sha="$1" version="$2" deadline=$((SECONDS + wait_minutes * 60)) run
    while :; do
        run=$(gh run list --branch "$branch" --workflow ci.yml --json databaseId,headSha,conclusion \
            -q ".[] | select(.headSha == \"$sha\")" -L 1)
        if [ -n "$run" ] && [ "$(jq -r .conclusion <<< "$run")" != "null" ]; then
            echo "$branch $version: CI $(jq -r .conclusion <<< "$run") (run $(jq -r .databaseId <<< "$run"))"
            break
        fi
        if [ "$SECONDS" -ge "$deadline" ]; then
            echo "$branch $version: no CI conclusion in $wait_minutes minutes; the next version waits for a hand" >&2
            return 1
        fi
        sleep 60
    done
    while ! gh api -q .sha "repos/$repo/contents/reports/$branch/$version.json?ref=$branch" >/dev/null 2>&1; do
        if [ "$SECONDS" -ge "$deadline" ]; then
            echo "$branch $version: the report did not arrive in $wait_minutes minutes; the next version waits for a hand" >&2
            return 1
        fi
        sleep 60
    done
    echo "$branch $version: report is on the branch"
}

for version in $versions; do
    if gh api "repos/$repo/contents/reports/$branch/v$version.json?ref=$branch" >/dev/null 2>&1; then
        echo "$branch: v$version is reported already"
        continue
    fi
    echo "$branch: generating from v$version"
    rm -rf "$sample" "$RUNNER_TEMP/branch"
    if ! bash "$here/generate.sh" "v$version" "$branch" "$sample"; then
        echo "$branch: v$version does not generate the branch; skipped" >&2
        continue
    fi
    sha=$(replace "$version") || exit 1
    wait_ci "$sha" "v$version" || exit 1
    cd "$here"
done
echo "$branch: backfilled"
