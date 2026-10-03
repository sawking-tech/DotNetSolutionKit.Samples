using Microsoft.AspNetCore.Http;
using ST.DotNetSolutionKit.Samples.Common.Application.Configuration;

namespace ST.DotNetSolutionKit.Samples.Common.Web.Authentication;

/// <summary>
/// Response.Cookies helpers for the cookie-based auth flow (login / refresh / logout).
///
/// Provides a single opinionated place for the access and refresh cookie options so concrete
/// projects do not re-derive `Secure` / `SameSite` / `Path` / `Expires` per controller and end
/// up with subtly different attributes across endpoints. The `Secure` and `SameSite` values are
/// computed from the current request: an https request or an ingress-forwarded
/// `X-Forwarded-Proto: https` header promotes the cookie to `Secure=true` + `SameSite=None`
/// (required for cross-site cookie delivery under modern browsers); http requests fall back to
/// `Secure=false` + `SameSite=Lax` so plain http local development keeps working.
///
/// Cookie names are exposed as constants so gateway auth middleware can pick them up without
/// hard-coding a duplicate string.
/// </summary>
public static class AuthCookieExtensions
{
    /// <summary>
    /// Name of the access JWT cookie. Kept in sync with
    /// <see cref="ServiceAuthenticationSetup.AccessTokenCookieName"/> so the gateway JWT pipeline
    /// picks it up as a fallback for the Authorization header.
    /// </summary>
    public const string AccessTokenCookieName = ServiceAuthenticationSetup.AccessTokenCookieName;

    /// <summary>
    /// Name of the refresh token cookie. Restricted to the auth endpoints via
    /// <see cref="IRefreshTokenConfiguration.CookiePath"/>.
    /// </summary>
    public const string RefreshTokenCookieName = "refresh_token";

    /// <summary>
    /// Fixed path for the access token cookie. It must accompany every gateway request, so it
    /// lives at the root and is not restricted to the auth prefix like the refresh cookie.
    /// </summary>
    public const string AccessTokenCookiePath = "/";

    /// <summary>
    /// Writes the access JWT into an HttpOnly cookie with a lifetime aligned to
    /// <paramref name="jwtConfig"/>. Options mirror <see cref="SetRefreshTokenCookie"/>.
    /// </summary>
    public static void SetAccessTokenCookie(
        this HttpContext httpContext,
        string token,
        TimeProvider timeProvider,
        IJwtPublicConfiguration jwtConfig)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrEmpty(token);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(jwtConfig);

        var isSecure = IsSecureRequest(httpContext.Request);

        httpContext.Response.Cookies.Append(AccessTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = isSecure ? SameSiteMode.None : SameSiteMode.Lax,
            Path = AccessTokenCookiePath,
            Expires = timeProvider.GetUtcNow().AddMinutes(jwtConfig.LifetimeMinutes)
        });
    }

    /// <summary>
    /// Deletes the access token cookie. Must use the same <see cref="AccessTokenCookiePath"/>
    /// otherwise the browser will not delete the original entry.
    /// </summary>
    public static void ClearAccessTokenCookie(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.Cookies.Delete(AccessTokenCookieName, new CookieOptions
        {
            Path = AccessTokenCookiePath
        });
    }

    /// <summary>
    /// Writes the refresh token into an HttpOnly cookie scoped to
    /// <see cref="IRefreshTokenConfiguration.CookiePath"/>.
    /// </summary>
    public static void SetRefreshTokenCookie(
        this HttpContext httpContext,
        string token,
        TimeProvider timeProvider,
        IRefreshTokenConfiguration refreshConfig)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrEmpty(token);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(refreshConfig);

        var isSecure = IsSecureRequest(httpContext.Request);
        var path = string.IsNullOrWhiteSpace(refreshConfig.CookiePath) ? "/" : refreshConfig.CookiePath;

        httpContext.Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = isSecure ? SameSiteMode.None : SameSiteMode.Lax,
            Path = path,
            Expires = timeProvider.GetUtcNow().AddDays(refreshConfig.ExpirationDays)
        });
    }

    /// <summary>
    /// Deletes the refresh token cookie. The <see cref="IRefreshTokenConfiguration.CookiePath"/>
    /// must match the one used at write time.
    /// </summary>
    public static void ClearRefreshTokenCookie(
        this HttpContext httpContext,
        IRefreshTokenConfiguration refreshConfig)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(refreshConfig);

        httpContext.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            Path = string.IsNullOrWhiteSpace(refreshConfig.CookiePath) ? "/" : refreshConfig.CookiePath
        });
    }

    /// <summary>
    /// True when the request is served over TLS directly OR terminated at an ingress that
    /// forwards <c>X-Forwarded-Proto: https</c>. Determines whether cookies can carry
    /// <c>Secure=true</c> and <c>SameSite=None</c>.
    /// </summary>
    private static bool IsSecureRequest(HttpRequest request) =>
        request.IsHttps
        || string.Equals(request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase);
}
