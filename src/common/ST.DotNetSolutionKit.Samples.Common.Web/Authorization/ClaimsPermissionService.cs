using Microsoft.AspNetCore.Http;
using ST.DotNetSolutionKit.Samples.Common.Application.Authorization;
using ST.DotNetSolutionKit.Samples.Common.Domain.Context;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Security;

namespace ST.DotNetSolutionKit.Samples.Common.Web.Authorization;

/// <summary>
/// Reads the current user's permissions from the <c>permissions</c> claims of the token.
/// </summary>
/// <remarks>
/// Needs nothing but the token, so it works for a solution with a single service. A JSON array in the
/// token becomes one claim per permission. A system call, made by the platform itself rather than by a
/// user, holds every permission.
/// </remarks>
public sealed class ClaimsPermissionService(
    IHttpContextAccessor httpContextAccessor,
    IUserContext userContext) : IPermissionService
{
    public Task<bool> UserHasAllPermissionsAsync(
        IReadOnlyCollection<string> allRequiredPermissions,
        CancellationToken cancellationToken)
    {
        if (allRequiredPermissions.Count == 0 || userContext.IsSystemCall)
            return Task.FromResult(true);

        var held = httpContextAccessor.HttpContext?.User
            .FindAll(AuthClaims.Permissions)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        return Task.FromResult(allRequiredPermissions.All(held.Contains));
    }
}
