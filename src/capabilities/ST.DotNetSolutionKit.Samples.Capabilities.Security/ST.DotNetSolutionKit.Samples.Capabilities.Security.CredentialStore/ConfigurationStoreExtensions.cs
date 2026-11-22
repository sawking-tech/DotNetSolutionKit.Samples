using Microsoft.Extensions.Configuration;

namespace ST.DotNetSolutionKit.Samples.Capabilities.Security.CredentialStore;

/// <summary>
/// Adds the configuration store to a service's configuration.
/// </summary>
public static class ConfigurationStoreExtensions
{
    /// <summary>
    /// Reads the shared folder and this service's folder from Infisical, on top of whatever was
    /// configured before.
    /// </summary>
    /// <param name="builder">The configuration being built.</param>
    /// <param name="servicePath">This service's folder, for example <c>/auth</c>.</param>
    /// <param name="optional">
    /// Whether the service may start without the store. Leave false outside a developer machine - see
    /// <see cref="ConfigurationStoreOptions.Optional"/> for why a service that starts without its secrets is worse
    /// than one that refuses to start.
    /// </param>
    /// <param name="storeFactory">Overrides how the store is created. Used by tests.</param>
    /// <remarks>
    /// Added last so the store wins over files: an <c>appsettings.json</c> shipped in the image should never
    /// be able to shadow the value the environment was given.
    ///
    /// The identity itself is read from what is already configured - environment variables in every
    /// deployed environment - so the credentials for the store never live in the repository.
    /// </remarks>
    public static IConfigurationBuilder AddPlatformConfiguration(
        this IConfigurationBuilder builder,
        string servicePath,
        bool optional = false,
        Func<InfisicalOptions, IConfigurationStore>? storeFactory = null) =>
        builder.AddConfigurationStore(InfisicalOptions.SectionName, servicePath, optional,
            new InfisicalOptions(), storeFactory ?? (options => new InfisicalConfigurationStore(options)));

    private static IConfigurationBuilder AddConfigurationStore<TOptions>(
        this IConfigurationBuilder builder,
        string sectionName,
        string servicePath,
        bool optional,
        TOptions options,
        Func<TOptions, IConfigurationStore> storeFactory)
        where TOptions : ConfigurationStoreOptions
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

        return builder.Add(new ConfigurationStoreSource(options, () => storeFactory(options)));
    }
}
