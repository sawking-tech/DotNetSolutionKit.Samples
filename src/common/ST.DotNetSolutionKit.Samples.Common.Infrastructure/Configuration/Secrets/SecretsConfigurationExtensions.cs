using Microsoft.Extensions.Configuration;

namespace ST.DotNetSolutionKit.Samples.Common.Infrastructure.Configuration.Secrets;

/// <summary>
/// Adds the secret store to a service's configuration.
/// </summary>
public static class SecretsConfigurationExtensions
{

    /// <summary>
    /// Reads the shared path and this service's path from HashiCorp Vault, on top of whatever was
    /// configured before: the same order and rules as <see cref="AddPlatformSecrets"/>, with the
    /// <c>Vault</c> section.
    /// </summary>
    /// <param name="builder">The configuration being built.</param>
    /// <param name="servicePath">This service's secret, for example <c>auth</c>.</param>
    /// <param name="optional">Whether the service may start without the store; see <see cref="SecretStoreOptions.Optional"/>.</param>
    /// <param name="storeFactory">Overrides how the store is created. Used by tests.</param>
    public static IConfigurationBuilder AddPlatformVaultSecrets(
        this IConfigurationBuilder builder,
        string servicePath,
        bool optional = false,
        Func<VaultOptions, ISecretStore>? storeFactory = null) =>
        builder.AddSecretStore(VaultOptions.SectionName, servicePath, optional,
            new VaultOptions(), storeFactory ?? (options => new VaultSecretStore(options)));

    private static IConfigurationBuilder AddSecretStore<TOptions>(
        this IConfigurationBuilder builder,
        string sectionName,
        string servicePath,
        bool optional,
        TOptions options,
        Func<TOptions, ISecretStore> storeFactory)
        where TOptions : SecretStoreOptions
    {
        options.ServicePath = servicePath;
        options.Optional = optional;
        builder.Build().GetSection(sectionName).Bind(options);

        if (!options.Enabled)
        {
            return builder;
        }

        // Bind() does not overwrite with an empty value, so a path supplied by configuration wins over the
        // argument only when it was actually set.
        if (string.IsNullOrWhiteSpace(options.ServicePath))
        {
            options.ServicePath = servicePath;
        }

        return builder.Add(new SecretsConfigurationSource(options, () => storeFactory(options)));
    }
}
