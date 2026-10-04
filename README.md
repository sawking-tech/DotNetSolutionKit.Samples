# Sample: full

Every flag: the bus with an outbox, Infisical, feature flags, the API diff, access rules over a tenant tree, object storage, ClickHouse; two services.

[![CI of full by day](reports/full/ci.svg)](https://dnsk.sawking.tech/samples.html#full)

Generated from [DotNetSolutionKit v2.7.0](https://github.com/sawking-tech/DotNetSolutionKit/releases/tag/v2.7.0) with:

```bash
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Orders -M false --GitHubCiCd true --Messaging outbox -I true --DiffApi true --FeatureFlags true --HierarchyRules true --Storage true --ClickHouse true
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Billing --Messaging outbox -I true --DiffApi true --FeatureFlags true --Storage true --ClickHouse true
```

Nothing here is edited by hand: the branch is replaced when the template moves on.

## The samples

| Branch | What it shows | CI |
|---|---|---|
| [`single`](https://github.com/sawking-tech/DotNetSolutionKit.Samples/tree/single) | One service with the defaults: PostgreSQL, Hangfire, docker compose files, and CI. | [![CI](https://github.com/sawking-tech/DotNetSolutionKit.Samples/actions/workflows/ci.yml/badge.svg?branch=single)](https://github.com/sawking-tech/DotNetSolutionKit.Samples/actions/workflows/ci.yml?query=branch%3Asingle) |
| [`gateway`](https://github.com/sawking-tech/DotNetSolutionKit.Samples/tree/gateway) | Two services behind a YARP gateway that validates the token and forwards the user. | [![CI](https://github.com/sawking-tech/DotNetSolutionKit.Samples/actions/workflows/ci.yml/badge.svg?branch=gateway)](https://github.com/sawking-tech/DotNetSolutionKit.Samples/actions/workflows/ci.yml?query=branch%3Agateway) |
| [`full`](https://github.com/sawking-tech/DotNetSolutionKit.Samples/tree/full) | Every flag: the bus with an outbox, Infisical, feature flags, the API diff, access rules over a tenant tree, object storage, ClickHouse; two services. | [![CI](https://github.com/sawking-tech/DotNetSolutionKit.Samples/actions/workflows/ci.yml/badge.svg?branch=full)](https://github.com/sawking-tech/DotNetSolutionKit.Samples/actions/workflows/ci.yml?query=branch%3Afull) |
| [`nightly`](https://github.com/sawking-tech/DotNetSolutionKit.Samples/tree/nightly) | The flags of full, generated from the template's dev once a day when it has moved. A red CI here is a break in dev before a release. | [![CI](https://github.com/sawking-tech/DotNetSolutionKit.Samples/actions/workflows/ci.yml/badge.svg?branch=nightly)](https://github.com/sawking-tech/DotNetSolutionKit.Samples/actions/workflows/ci.yml?query=branch%3Anightly) |

The CI of each branch by day, as [the samples page](https://dnsk.sawking.tech/samples.html) shows it:

[![CI of single by day](https://raw.githubusercontent.com/sawking-tech/DotNetSolutionKit.Samples/single/reports/single/ci.svg)](https://dnsk.sawking.tech/samples.html#single)

[![CI of gateway by day](https://raw.githubusercontent.com/sawking-tech/DotNetSolutionKit.Samples/gateway/reports/gateway/ci.svg)](https://dnsk.sawking.tech/samples.html#gateway)

[![CI of full by day](https://raw.githubusercontent.com/sawking-tech/DotNetSolutionKit.Samples/full/reports/full/ci.svg)](https://dnsk.sawking.tech/samples.html#full)

[![CI of nightly by day](https://raw.githubusercontent.com/sawking-tech/DotNetSolutionKit.Samples/nightly/reports/nightly/ci.svg)](https://dnsk.sawking.tech/samples.html#nightly)

Once a day [regenerate](.github/workflows/regenerate.yml) checks the template's master: what reaches
master is a release, and every release branch is generated again from a new one, pushed, and its CI runs.
nightly follows dev instead, where the next release is built: it is generated again when dev has moved.
After each CI run of a branch, [report](.github/workflows/report.yml) writes the run into the branch's
reports/<branch>/ folder: tests, coverage, time and the branch's files, one file per release, and
ci.svg, the picture of its days above.
The automation is in [.samples](.samples).
