# Sample: full-alt

SQL Server, xUnit, secrets from Vault, Kubernetes manifests and the audit journal, which rides on the bus with an outbox; two services.

[![CI of full-alt by day](reports/full-alt/ci.svg)](https://dnsk.sawking.tech/samples.html#full-alt)

![How to read the days](https://raw.githubusercontent.com/sawking-tech/DotNetSolutionKit.Samples/full/.samples/legend.svg)

Each square is a day in UTC, Monday at the top and Sunday at the bottom. A day shows the result of the
last CI run up to it: green for a pass, red for a failure. A day without a run shows how long ago the
run was: after a pass it turns a step bluer every 2 days, after a failure a little darker every day,
ice blue or dark red from day 30 on. A day keeps its colour; a new run makes its day green or red
again. A pale square is a day before the first run; the fading squares on the right are the weeks to
come.

Generated from [DotNetSolutionKit v2.7.0](https://github.com/sawking-tech/DotNetSolutionKit/releases/tag/v2.7.0) with:

```bash
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Orders -M false --GitHubCiCd true --Database mssql --TestFramework xunit --Vault true --Deploy k8s --Messaging outbox --Audit true
dotnet new DotNetSolutionKit -N ST -P DotNetSolutionKit.Samples -S Billing --Database mssql --TestFramework xunit --Vault true --Deploy k8s --Messaging outbox --Audit true
```

Nothing here is edited by hand: the branch is replaced when the template moves on.
