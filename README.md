# Sample: full

Every flag: the bus with an outbox, Infisical, feature flags, the API diff, access rules over a tenant tree, object storage, ClickHouse; two services.

Generated from [DotNetSolutionKit v2.1.1](https://github.com/Vovanda/DotNetSolutionKit/releases/tag/v2.1.1) with:

```bash
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Orders -M false --GitHubCiCd true --Messaging outbox -I true --DiffApi true --FeatureFlags true --HierarchyRules true --Storage true --ClickHouse true
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Billing --Messaging outbox -I true --DiffApi true --FeatureFlags true --Storage true --ClickHouse true
```

Nothing here is edited by hand: the branch is replaced when the template has a new release.
