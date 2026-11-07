using ST.DotNetSolutionKit.Samples.Common.Domain.Context;

namespace ST.DotNetSolutionKit.Samples.Common.Infrastructure.Security;

public class AuthContext : IAuthContext
{
    public AuthMethod Type { get; }
    public string Id { get; }
    public DateTimeOffset ExpireAt { get; }

    public AuthContext(AuthMethod type, string id, DateTimeOffset expireAt)
    {
        Type = type;
        Id = id;
        ExpireAt = expireAt;
    }
}