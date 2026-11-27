# The automation of this repository

How the branches live: what starts them, what pushes them, and what the runs mean. The reader of this
file is the one who changes the automation, not the one who reads the samples - those the
[README](../README.md) and [the samples page](https://dotskit.sawking.tech/samples.html) describe.

## The chain

Every branch of this repository is written by automation; no hand edits anywhere. The workflows live
on the default branch (`full`, itself a sample), and the scripts they run are in this folder, `.samples/`.

```
release.yml (DotNetSolutionKit)  ── tags v<version>, publishes to nuget.org
        │
samples-dispatch.yml (DotNetSolutionKit)  ── on the Release run that carries a tag
        │  repository_dispatch "released", with the tag as the payload
        ▼
regenerate.yml  ── on that dispatch, and on a hand run only: no clock
        │  replaces every release branch (single, gateway, full, full-alt)
        │  full-dev follows the template's dev instead
lifecycle.yml  ── daily 03:47 UTC; also listens for the dispatch
        │  rebuilds the lifecycle branch when the template's master moved
        ▼
each branch's CI  ── the push starts it, as any push would
        │
report.yml  ── after every CI, Coverage, Regenerate or Secret scan run
           writes reports/<branch>/<version>.json|.md and ci.svg into the branch
```

Two facts of the plumbing that the chain rests on:

- **The branches carry workflow files**, which `GITHUB_TOKEN` may not push, and a push by
  `GITHUB_TOKEN` starts no workflows. Every automated push goes through `SAMPLES_DEPLOY_KEY`,
  a deploy key with write access: it does not expire, and its push starts the branch's CI
  like any other push.
- **The report follows the branch it was about, not the branch the run ran on.** Regenerate and
  lifecycle run on the default branch and rebuild others, so they upload an artifact named
  `sample-branch-<branch>` per branch, and report.yml writes into the branch the artifact names.

## What starts a regeneration

- **A release.** `samples-dispatch.yml` in the template's repository wakes this one the minute the
  publication is done: it reads the tag of the Release run and dispatches `released` with it. Before
  that, `released.yml` here polled nuget.org twice an hour; it is gone, and so is the hourly cron of
  `regenerate.yml` - a clock rebuilds nothing a release has not already asked for. The wait for
  nuget.org itself does not go away: the index lists a published package up to half an hour late, and
  `generate.sh` waits for it (`WAIT_MINUTES`, 40 by default) before it installs.
- **The template's dev moving.** `full-dev` follows `dev` and is rebuilt by the next release-triggered
  run if dev has moved since; no clock rebuilds it, which is the cost of the reactive chain: the row
  shows dev as of the last release, not as of this morning. It is rebuilt by hand before a release:
  `gh workflow run regenerate.yml -f force=true`, and `promote.yml` refuses a dev whose `full-dev` CI
  has not run on it. `lifecycle.yml` keeps its own daily cron, which keeps the branch in step with the
  template's master between releases.
- **A hand run.** `gh workflow run regenerate.yml -f force=true` (or the lifecycle workflow): the
  check normally skips a branch already built from the current ref, and `force` is how the automation
  itself is exercised after a change to it.

## When green means nothing ran

The runner image moves under the runs: GitHub updates it without a commit anywhere, and a new SDK
there can break a fresh generation on its own. In November 2026 the image took .NET SDK 8.0.428
silently; `dotnet sln add` there pulls a project's references in, the release branches generated
after that lost projects from All.sln, and the break was found only by a force run days later.
Until then, and until a canary watches the environment (planned: a minimal generation on the unpinned
SDK, run on a detected SDK change, gating Regenerate and lifecycle), a quiet stretch of green days
means *nothing was rebuilt* - not that the recipes still work. A release, or a hand `force` run,
is what exercises the recipes on the SDK the runners carry today.

## The branches

- **The samples** - `single`, `gateway`, `full`, `full-alt`, `full-dev`, `lifecycle` - are the
  public board: the site reads their reports and draws their days. Their recipes are rows of
  [variants.json](variants.json); `full-dev` carries a `ref` (the template's dev), `lifecycle` a
  `tag: false` (owned by its own workflow), the rest regenerate from the newest release tag.
- **Everything else is plumbing and lives as long as it lives**: pull-request branches of fixes to
  the automation (merged and deleted, as any repository's), the default branch's own `.samples/`
  and workflows. A branch the site does not know about is not a sample.

## The rules a change to the automation obeys

- A workflow change is rehearsed on a temporary branch, for each case it handles, before its pull
  request - a workflow that does not work never reaches the default branch.
- The scripts of `.samples/` are the only place a recipe lives: `variants.json` names the commands,
  `generate.sh` runs them, `lifecycle.sh` writes the life of a solution. Nothing about a sample is
  edited in the sample branch itself - it is replaced whole.
- The reports are written by report.yml and the workflows alone. A report edited by hand is a lie
  the next run will expose.
