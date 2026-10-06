#!/usr/bin/env bash
#
# The life of a solution, applied in one run into an empty folder, each step a commit:
#
#   ./lifecycle.sh <template branch> <folder>
#
# A team made the solution with the template two minor versions before the current one, alone, as before
# dotskit; changed it; then dotskit describes it, upgrades it to the current version, adds a service, adds a
# flag to a service it has, and adds one more with --force. dotskit builds the solution after each command and
# stops the run when the build fails; at the end the team's change must still be there.
#
# The current version is the template branch's version.json; the start is the last patch of the second minor
# below it among the releases on nuget.org, across a major too (3.0 starts from the 2.x before the last one).
# dotskit and the template the solution is upgraded to are the sources of that branch: the run checks the next
# release before it is out. TEMPLATE_DIR names a checkout of the template to use instead of cloning one, for a
# run by hand.
set -euo pipefail

ref="$1" out="$2"
# The branch the result goes to, for its CI trigger and its README: lifecycle, or a branch of a rehearsal.
branch="${BRANCH:-lifecycle}"
template_repo="${TEMPLATE_REPO:-sawking-tech/DotNetSolutionKit}"
work="$(mktemp -d)"
names=(-N ST -P DotNetSolutionKit.Samples)
marker="// The team's own line, kept through every step of dotskit."

if [ -n "${TEMPLATE_DIR:-}" ]; then
    sources="$TEMPLATE_DIR"
else
    git clone -q --depth 1 --branch "$ref" "https://github.com/$template_repo.git" "$work/template"
    sources="$work/template"
fi

# The start: among the releases below the current version, the second minor down, and its last patch.
current=$(jq -r .version "$sources/version.json")
current="${current%%-*}"
released=$(curl -fsS https://api.nuget.org/v3-flatcontainer/sawking.dotnetsolutionkit/index.json | jq -r '.versions[]')
minors=$(grep -E '^[0-9]+\.[0-9]+\.[0-9]+$' <<< "$released" | cut -d. -f1,2 | sort -uV \
    | while read -r m; do
        # An if, so a minor at or above the current one ends the loop with status 0, not a silent exit.
        if [ "$(printf '%s\n%s\n' "$m" "$current" | sort -V | head -1)" = "$m" ] && [ "${current#"$m".}" = "$current" ]; then echo "$m"; fi
    done)
start_minor=$(tail -2 <<< "$minors" | head -1)
[ "$(wc -l <<< "$minors")" -ge 2 ] || { echo "nuget.org has fewer than two minors below $current to start from" >&2; exit 1; }
start=$(grep -E "^${start_minor//./\\.}\.[0-9]+$" <<< "$released" | sort -V | tail -1)
echo "the life of a solution: from $start, made with the template alone, to $current by dotskit"

dotnet build "$sources/tool/DotsKit" -c Release -o "$work/tool" -nologo -v q
export DOTSKIT_TEMPLATE_SOURCE="$sources/template"
dotskit() { dotnet "$work/tool/dotskit.dll" "$@"; }

mkdir -p "$out"
cd "$out"
git init -q
commit() {
    git add -A
    git -c user.name="github-actions[bot]" -c user.email="41898282+github-actions[bot]@users.noreply.github.com" commit -qm "$1"
}

# The solution as a team made it before dotskit: the template alone, each service added by its script.
hive=(--debug:custom-hive "$work/hive")
dotnet new install "SawKing.DotNetSolutionKit::$start" "${hive[@]}" > /dev/null
# The solution's own command: --Solution since 2.8, -M false before it. Since 2.8 a service adds itself to
# All.sln by a command it asks to run, which --allow-scripts yes allows; a version before refuses the option.
if [ "$(printf '%s\n2.8.0\n' "$start" | sort -V | head -1)" = "2.8.0" ]; then
    whole=(--Solution) scripts=(--allow-scripts yes)
else
    whole=(-M false) scripts=()
fi
dotnet new DotNetSolutionKit "${names[@]}" -S Orders "${whole[@]}" --GitHubCiCd true --Messaging outbox --Storage true "${scripts[@]}" "${hive[@]}"
dotnet new DotNetSolutionKit "${names[@]}" -S Billing --Messaging outbox --Storage true "${scripts[@]}" "${hive[@]}"
# Before 2.8 a service joined All.sln by this script; since, it adds itself, and there is no script.
[ ! -f src/services/manual-add-projects.sh ] || bash src/services/manual-add-projects.sh < /dev/null
commit "Made with DotNetSolutionKit $start alone, as before dotskit"

program=$(ls src/services/*.Orders/*.Orders.API/Program.cs)
printf '\n%s\n' "$marker" >> "$program"
commit "The team's own change"

step() {
    local message="$1"
    shift
    dotskit "$@" --yes
    commit "$message"
}
# A solution before 2.7 does not say its version, and the run knows it: it made the solution.
step "dotskit init: the manifest of a solution made without dotskit" init --template-version "$start"
step "dotskit upgrade: from $start to $current" upgrade
step "dotskit new: a service, Catalog" new -S Catalog --MongoDB true --Notify email
step "dotskit new: a flag of a service it has, ClickHouse for Orders" new -S Orders --ClickHouse true
step "dotskit new --force: feature flags for Billing" new -S Billing --FeatureFlags true --force

grep -qF "$marker" "$program" || { echo "the team's line in $program is gone" >&2; exit 1; }
echo "the solution went from $start to $current, grew, and keeps the team's change"

# The branch's own CI runs on a push to it, as on a sample branch (generate.sh); and it says what it is.
for wf in .github/workflows/*.yml; do
    sed -i "s/branches: \[main, master\]/branches: [main, master, $branch]/" "$wf"
done
commit_of=$(git -C "$sources" rev-parse HEAD 2>/dev/null || echo "$ref")
echo "$commit_of" > template-version
{
    echo "# Sample: $branch"
    echo
    echo "The life of a solution, each step a commit of this branch: made with DotNetSolutionKit $start alone, as"
    echo "before dotskit; the team's own change; then dotskit describes it (init), upgrades it to $current, adds a"
    echo "service, adds a flag to a service it has, and adds one more with --force. dotskit built the solution after"
    echo "each command, and the team's change is still in Orders. The CI of this branch runs on the result."
    echo
    echo "Built from the template's $ref at ${commit_of:0:7}. Nothing here is edited by hand: the branch is replaced"
    echo "when the template moves on."
} > README.md
commit "The branch's CI, its README and what it was built from"
