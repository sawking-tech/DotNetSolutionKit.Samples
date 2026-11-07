using ST.DotNetSolutionKit.Samples.Common.Domain.Context;

namespace ST.DotNetSolutionKit.Samples.Common.Infrastructure.Security;

public sealed class ApplicationExecutionContext : IDomainExecutionContext
{
    public IUserContext Actor { get; }
    public TimeProvider TimeProvider { get; }

    public ApplicationExecutionContext(IUserContext actor, TimeProvider timeProvider)
    {
        Actor = actor;
        TimeProvider = timeProvider;
    }
}