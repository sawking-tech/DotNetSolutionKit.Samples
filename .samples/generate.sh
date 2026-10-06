#!/usr/bin/env bash
#
# Generates one sample from a release of the template, or from a branch of it, into an empty folder:
#
#   ./generate.sh <tag or branch> <branch> <folder>
#
# The template comes from the release's package when it has one, from the sources at that tag or branch
# otherwise.
# What to generate for each branch is in variants.json. The branch marked "default" is the repository's
# default branch: it also carries this automation (.samples/ and the regenerate workflow), so a
# regeneration of it does not delete what regenerates it.
set -euo pipefail

tag="$1" branch="$2" out="$3"
here="$(cd "$(dirname "$0")" && pwd)"
template_repo="${TEMPLATE_REPO:-sawking-tech/DotNetSolutionKit}"
work="$(mktemp -d)"
commit=""
# TEMPLATE_HIVE keeps the install apart from the templates of the machine, for a run outside CI.
hive=()
[ -n "${TEMPLATE_HIVE:-}" ] && hive=(--debug:custom-hive "$TEMPLATE_HIVE")

if gh release download "$tag" -R "$template_repo" -p '*.nupkg' -D "$work/package" 2>/dev/null; then
    dotnet new install "$work"/package/*.nupkg --force "${hive[@]}"
else
    git clone -q --depth 1 --branch "$tag" "https://github.com/$template_repo.git" "$work/template"
    commit=$(git -C "$work/template" rev-parse HEAD)
    dotnet new install "$work/template/template" --force "${hive[@]}"
fi

mkdir -p "$out"
cd "$out"
jq -r --arg b "$branch" '.[] | select(.branch == $b) | .generate[]' "$here/variants.json" | tr -d '\r' > "$work/commands"
[ -s "$work/commands" ] || { echo "variants.json has no branch $branch" >&2; exit 1; }
while read -r args; do
    # Word splitting of the arguments is intended.
    # shellcheck disable=SC2086
    dotnet new DotNetSolutionKit $args "${hive[@]}"
done < "$work/commands"
bash src/services/manual-add-projects.sh < /dev/null

about=$(jq -r --arg b "$branch" '.[] | select(.branch == $b) | .about' "$here/variants.json")
home=$(jq -r '.[] | select(.default) | .branch' "$here/variants.json")
raw="https://raw.githubusercontent.com/${GITHUB_REPOSITORY:-sawking-tech/DotNetSolutionKit.Samples}"
fade=$(sed -n 's/^FADE_DAYS = //p' "$here/ci-svg.py")
step=$(sed -n 's/^PASS_STEP_DAYS = //p' "$here/ci-svg.py")

# What the colours of the days mean: one legend for every branch, drawn by ci-svg.py on the default
# branch, and the same in words.
legend() {
    echo "![How to read the days]($raw/$home/.samples/legend.svg)"
    echo
    echo "Each square is a day in UTC, Monday at the top and Sunday at the bottom. A day shows the result of the"
    echo "last CI run up to it: green for a pass, red for a failure. A day without a run shows how long ago the"
    echo "run was: after a pass it turns a step bluer every $step days, after a failure a little darker every day,"
    echo "ice blue or dark red from day $fade on. A day keeps its colour; a new run makes its day green or red"
    echo "again. A pale square is a day before the first run; the fading squares on the right are the weeks to"
    echo "come."
}
{
    echo "# Sample: $branch"
    echo
    echo "$about"
    echo
    # The report of the branch's CI draws it after each run; a branch generated for the first time shows it
    # from its first run on.
    echo "[![CI of $branch by day](reports/$branch/ci.svg)](https://dnsk.sawking.tech/samples.html#$branch)"
    echo
    # The default branch shows the legend once, under the days of every branch further down.
    [ "$branch" = "$home" ] || { legend; echo; }
    if [ "$(jq -r --arg b "$branch" '.[] | select(.branch == $b) | .ref // empty' "$here/variants.json")" ]; then
        echo "Generated from DotNetSolutionKit $tag at [${commit:0:7}](https://github.com/$template_repo/commit/$commit), not a release, with:"
    else
        echo "Generated from [DotNetSolutionKit $tag](https://github.com/$template_repo/releases/tag/$tag) with:"
    fi
    echo
    echo '```bash'
    sed 's/^/dotnet new DotNetSolutionKit /' "$work/commands"
    echo '```'
    echo
    echo "Nothing here is edited by hand: the branch is replaced when the template moves on."
} > README.md
# What the branch was generated from: the release tag, or the commit for a branch of the template.
echo "${commit:-$tag}" > template-version

# The generated CI runs on a push to main or master and on pull requests: right for a team, where the
# other branches go through pull requests. A sample branch is pushed straight, so its name joins them.
for wf in .github/workflows/*.yml; do
    [ -f "$wf" ] && sed -i "s/branches: \[main, master\]/branches: [main, master, $branch]/" "$wf"
done

if [ "$(jq -r --arg b "$branch" '.[] | select(.branch == $b) | .default // false' "$here/variants.json")" = "true" ]; then
    mkdir -p .samples .github/workflows
    cp "$here/generate.sh" "$here/report.sh" "$here/ci-svg.py" "$here/variants.json" .samples/
    python3 "$here/ci-svg.py" --legend .samples/legend.svg
    cp "$here/../.github/workflows/regenerate.yml" "$here/../.github/workflows/report.yml" .github/workflows/
    {
        echo
        echo "## The samples"
        echo
        echo "| Branch | What it shows | CI |"
        echo "|---|---|---|"
        repo="https://github.com/${GITHUB_REPOSITORY:-sawking-tech/DotNetSolutionKit.Samples}"
        jq -r --arg r "$repo" '.[] | "| [`\(.branch)`](\($r)/tree/\(.branch)) | \(.about) | [![CI](\($r)/actions/workflows/ci.yml/badge.svg?branch=\(.branch))](\($r)/actions/workflows/ci.yml?query=branch%3A\(.branch)) |"' "$here/variants.json"
        echo
        echo "The CI of each branch by day, as [the samples page](https://dnsk.sawking.tech/samples.html) shows it:"
        echo
        jq -r --arg raw "$raw" '.[] | "[![CI of \(.branch) by day](\($raw)/\(.branch)/reports/\(.branch)/ci.svg)](https://dnsk.sawking.tech/samples.html#\(.branch))\n"' "$here/variants.json"
        legend
        echo
        echo "Once a day [regenerate](.github/workflows/regenerate.yml) checks the template's master: what reaches"
        echo "master is a release, and every release branch is generated again from a new one, pushed, and its CI runs."
        echo "full-dev follows dev instead, where the next release is built: it is generated again when dev has moved."
        echo "After each CI run of a branch, [report](.github/workflows/report.yml) writes the run into the branch's"
        echo "reports/<branch>/ folder: tests, coverage, time and the branch's files, one file per release, and"
        echo "ci.svg, the picture of its days above."
        echo "The automation is in [.samples](.samples)."
    } >> README.md
fi
