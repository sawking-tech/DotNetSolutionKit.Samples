namespace ST.DotNetSolutionKit.Samples.Common.Infrastructure.Configuration.Secrets;

/// <summary>
/// Where a service reads its secrets from.
/// </summary>
/// <remarks>
/// The credentials themselves come from the environment, never from a file in the repository: the whole
/// point of the secret store is that the repository does not contain the values, and a repository holding
/// the key to the store would only move the problem one step.
/// </remarks>
public sealed class InfisicalOptions
{
    public const string SectionName = "Infisical";

    /// <summary>
    /// The Infisical instance, for example <c>https://app.infisical.com</c>.
    /// </summary>
    public string HostUri { get; set; } = "https://app.infisical.com";

    /// <summary>
    /// The project holding the platform's secrets.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Which environment to read: <c>dev</c>, <c>staging</c> or <c>prod</c>.
    /// </summary>
    public string EnvironmentSlug { get; set; } = string.Empty;

    /// <summary>
    /// The folder holding this service's own secrets, for example <c>/auth</c>.
    /// </summary>
    /// <remarks>
    /// Read after the shared folder, so a service can override a shared value without the shared value
    /// having to know which services exist.
    /// </remarks>
    public string ServicePath { get; set; } = string.Empty;

    /// <summary>
    /// The folder holding secrets every service needs.
    /// </summary>
    public string SharedPath { get; set; } = "/";

    /// <summary>
    /// Machine identity client id. Supplied through the environment.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Machine identity client secret. Supplied through the environment.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Whether a service may start when the store cannot be read.
    /// </summary>
    /// <remarks>
    /// False everywhere it matters: a service that starts without its secrets does not fail, it
    /// misbehaves - connecting to nothing, signing with an empty key - and the cause surfaces far from
    /// here. It is left configurable only so a developer can run locally without the store.
    /// </remarks>
    public bool Optional { get; set; }

    /// <summary>
    /// Whether the store is read at all. Off, the service takes every value from its files and
    /// environment: for a service generated with the store and run before one exists.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether enough is configured to read anything at all.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ProjectId)
        && !string.IsNullOrWhiteSpace(EnvironmentSlug)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);
}
