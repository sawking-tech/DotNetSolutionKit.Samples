# Sample: nightly

The flags of full, generated from the template's master once a day when it has moved. A red CI here is a break in master before a release.

Generated from DotNetSolutionKit master at [7017c78](https://github.com/sawking-tech/DotNetSolutionKit/commit/7017c78caf68d4b09312dadc88bcbf79e0ae8208), not a release, with:

```bash
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Orders -M false --GitHubCiCd true --Messaging outbox -I true --DiffApi true --FeatureFlags true --HierarchyRules true --Storage true --ClickHouse true
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Billing --Messaging outbox -I true --DiffApi true --FeatureFlags true --Storage true --ClickHouse true
```

Nothing here is edited by hand: the branch is replaced when the template moves on.
