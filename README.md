# Sample: full-alt

SQL Server, xUnit, secrets from Vault, Kubernetes manifests and the audit journal, which rides on the bus with an outbox; two services.

[![CI of full-alt by day](reports/full-alt/ci.svg)](https://dnsk.sawking.tech/samples.html#full-alt)

Generated from [DotNetSolutionKit v2.7.0](https://github.com/sawking-tech/DotNetSolutionKit/releases/tag/v2.7.0) with:

```bash
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Orders -M false --GitHubCiCd true --Database mssql --TestFramework xunit --Vault true --Deploy k8s --Messaging outbox --Audit true
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Billing --Database mssql --TestFramework xunit --Vault true --Deploy k8s --Messaging outbox --Audit true
```

Nothing here is edited by hand: the branch is replaced when the template moves on.
