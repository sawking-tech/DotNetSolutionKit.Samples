# Sample: full

Every flag: the bus with an outbox, Infisical, feature flags, the API diff, access rules over a tenant tree, object storage, ClickHouse; two services.

Generated from [DotNetSolutionKit v2.6.0](https://github.com/sawking-tech/DotNetSolutionKit/releases/tag/v2.6.0) with:

```bash
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Orders -M false --GitHubCiCd true --Messaging outbox -I true --DiffApi true --FeatureFlags true --HierarchyRules true --Storage true --ClickHouse true
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Billing --Messaging outbox -I true --DiffApi true --FeatureFlags true --Storage true --ClickHouse true
```

Nothing here is edited by hand: the branch is replaced when the template has a new release.

## The samples

| Branch | What it shows | CI |
|---|---|---|
| [`single`](https://github.com/sawking-tech/DotNetSolutionKit.Samples/tree/single) | One service with the defaults: PostgreSQL, Hangfire, docker compose files, and CI. | [![CI](https://github.com/sawking-tech/DotNetSolutionKit.Samples/actions/workflows/ci.yml/badge.svg?branch=single)](https://github.com/sawking-tech/DotNetSolutionKit.Samples/actions/workflows/ci.yml?query=branch%3Asingle) |
| [`gateway`](https://github.com/sawking-tech/DotNetSolutionKit.Samples/tree/gateway) | Two services behind a YARP gateway that validates the token and forwards the user. | [![CI](https://github.com/sawking-tech/DotNetSolutionKit.Samples/actions/workflows/ci.yml/badge.svg?branch=gateway)](https://github.com/sawking-tech/DotNetSolutionKit.Samples/actions/workflows/ci.yml?query=branch%3Agateway) |
| [`full`](https://github.com/sawking-tech/DotNetSolutionKit.Samples/tree/full) | Every flag: the bus with an outbox, Infisical, feature flags, the API diff, access rules over a tenant tree, object storage, ClickHouse; two services. | [![CI](https://github.com/sawking-tech/DotNetSolutionKit.Samples/actions/workflows/ci.yml/badge.svg?branch=full)](https://github.com/sawking-tech/DotNetSolutionKit.Samples/actions/workflows/ci.yml?query=branch%3Afull) |

Once a day [regenerate](.github/workflows/regenerate.yml) checks for a new release of the template.
When there is one, every branch is generated again from it and pushed, and its CI runs. Only releases
are sampled, never the commits between them. The automation is in [.samples](.samples).
