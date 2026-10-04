# Sample: nightly

The flags of full, generated from the template's dev once a day when it has moved. A red CI here is a break in dev before a release.

Generated from DotNetSolutionKit dev at [19094e9](https://github.com/sawking-tech/DotNetSolutionKit/commit/19094e90851f8bc2973de7f51dceac20c654a8a8), not a release, with:

```bash
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Orders -M false --GitHubCiCd true --Messaging outbox -I true --DiffApi true --FeatureFlags true --HierarchyRules true --Storage true --ClickHouse true
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Billing --Messaging outbox -I true --DiffApi true --FeatureFlags true --Storage true --ClickHouse true
```

Nothing here is edited by hand: the branch is replaced when the template moves on.
