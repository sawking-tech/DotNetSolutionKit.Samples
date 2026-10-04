# Sample: nightly

The flags of full, generated from the template's dev once a day when it has moved. A red CI here is a break in dev before a release.

[![CI of nightly by day](reports/nightly/ci.svg)](https://dnsk.sawking.tech/samples.html#nightly)

Generated from DotNetSolutionKit dev at [cfed0de](https://github.com/sawking-tech/DotNetSolutionKit/commit/cfed0deec35a44c4e907562f27e8fe7992d75c9f), not a release, with:

```bash
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Orders -M false --GitHubCiCd true --Messaging outbox -I true --DiffApi true --FeatureFlags true --HierarchyRules true --Storage true --ClickHouse true
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Billing --Messaging outbox -I true --DiffApi true --FeatureFlags true --Storage true --ClickHouse true
```

Nothing here is edited by hand: the branch is replaced when the template moves on.
