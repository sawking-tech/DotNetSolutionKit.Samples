# Sample: nightly

The flags of full, generated from the template's dev once a day when it has moved. A red CI here is a break in dev before a release.

Generated from DotNetSolutionKit dev at [0f36e03](https://github.com/sawking-tech/DotNetSolutionKit/commit/0f36e03d5091eae3ed8eef9e7f5acf2046a171dd), not a release, with:

```bash
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Orders -M false --GitHubCiCd true --Messaging outbox -I true --DiffApi true --FeatureFlags true --HierarchyRules true --Storage true --ClickHouse true
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Billing --Messaging outbox -I true --DiffApi true --FeatureFlags true --Storage true --ClickHouse true
```

Nothing here is edited by hand: the branch is replaced when the template moves on.
