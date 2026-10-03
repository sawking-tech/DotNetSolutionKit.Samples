# Sample: nightly

The flags of full, generated from the template's master once a day when it has moved. A red CI here is a break in master before a release.

Generated from DotNetSolutionKit master at [d97424d](https://github.com/sawking-tech/DotNetSolutionKit/commit/d97424da5859900c58d5806df6747b5734565204), not a release, with:

```bash
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Orders -M false --GitHubCiCd true --Messaging outbox -I true --DiffApi true --FeatureFlags true --HierarchyRules true --Storage true --ClickHouse true
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Billing --Messaging outbox -I true --DiffApi true --FeatureFlags true --Storage true --ClickHouse true
```

Nothing here is edited by hand: the branch is replaced when the template moves on.
