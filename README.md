# Sample: nightly

The flags of full, generated from the template's dev once a day when it has moved. A red CI here is a break in dev before a release.

[![CI of nightly by day](reports/nightly/ci.svg)](https://dnsk.sawking.tech/samples.html#nightly)

Generated from DotNetSolutionKit dev at [978b089](https://github.com/sawking-tech/DotNetSolutionKit/commit/978b089237f9ef4c16615d18a8f26f8d8372c178), not a release, with:

```bash
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Orders -M false --GitHubCiCd true --Messaging outbox -I true --DiffApi true --FeatureFlags true --HierarchyRules true --Storage true --ClickHouse true
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Billing --Messaging outbox -I true --DiffApi true --FeatureFlags true --Storage true --ClickHouse true
```

Nothing here is edited by hand: the branch is replaced when the template moves on.
