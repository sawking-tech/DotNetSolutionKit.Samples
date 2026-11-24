using ST.DotNetSolutionKit.Samples.Common.Application.Execution;
using ST.DotNetSolutionKit.Samples.Common.Domain.Context;

namespace ST.DotNetSolutionKit.Samples.Common.Infrastructure.Security;

/// <inheritdoc cref="ISystemExecutionContextFactory"/>
public sealed class SystemExecutionContextFactory(TimeProvider timeProvider) : ISystemExecutionContextFactory
{
    public IDomainExecutionContext Create() => new ApplicationExecutionContext(SystemUserContext.Instance, timeProvider);
}
