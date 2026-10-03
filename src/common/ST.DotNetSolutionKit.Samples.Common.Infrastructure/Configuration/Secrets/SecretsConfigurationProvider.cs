using Microsoft.Extensions.Configuration;
using ST.DotNetSolutionKit.Samples.Common.Exceptions;

namespace ST.DotNetSolutionKit.Samples.Common.Infrastructure.Configuration.Secrets;

/// <summary>
/// Makes the secret store one more configuration source, so nothing downstream knows a value came from it.
/// </summary>
/// <remarks>
/// Secrets are read from the shared folder first and the service's own folder second, so a service can
/// override a shared value. A secret name maps to a configuration path by the usual convention -
/// <c>ConnectionStrings__DefaultConnection</c> becomes <c>ConnectionStrings:DefaultConnection</c> -
/// which is the same spelling environment variables already use.
/// </remarks>
public sealed class SecretsConfigurationProvider : ConfigurationProvider
{
    private readonly InfisicalOptions _options;
    private readonly ISecretStore _store;

    public SecretsConfigurationProvider(InfisicalOptions options, ISecretStore store)
    {
        _options = options;
        _store = store;
    }

    public override void Load()
    {
        // Configuration is built synchronously, before the host exists. Blocking here is deliberate: a
        // service must not reach its first request before it knows whether it has its secrets.
        LoadAsync().GetAwaiter().GetResult();
    }

    private async Task LoadAsync()
    {
        if (!_options.IsConfigured)
        {
            if (!_options.Optional)
            {
                throw new ConfigurationException(
                    "The secret store is not configured: project, environment and machine identity are all required.");
            }

            // Nothing configured and explicitly optional - a local run reading its values from files.
            return;
        }

        var loaded = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var path in PathsToRead())
            {
                foreach (var (key, value) in await _store.ReadAsync(path))
                {
                    loaded[ToConfigurationKey(key)] = value;
                }
            }
        }
        catch (ConfigurationException) when (_options.Optional)
        {
            // Reaching this means the store was configured but could not be read. Allowed only where the
            // service was told it may run without it.
            return;
        }

        Data = loaded;
    }

    private IEnumerable<string> PathsToRead()
    {
        if (!string.IsNullOrWhiteSpace(_options.SharedPath))
        {
            yield return _options.SharedPath;
        }

        // Read last so it wins: the service's own value overrides the shared one of the same name.
        if (!string.IsNullOrWhiteSpace(_options.ServicePath)
            && !string.Equals(_options.ServicePath, _options.SharedPath, StringComparison.OrdinalIgnoreCase))
        {
            yield return _options.ServicePath;
        }
    }

    /// <summary>
    /// A secret name uses a double underscore where a configuration path uses a colon, because a colon is
    /// not allowed in a secret name - the same convention environment variables follow.
    /// </summary>
    internal static string ToConfigurationKey(string secretName) =>
        secretName.Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
}

/// <summary>
/// The source that produces <see cref="SecretsConfigurationProvider"/>.
/// </summary>
public sealed class SecretsConfigurationSource : IConfigurationSource
{
    private readonly InfisicalOptions _options;
    private readonly Func<InfisicalOptions, ISecretStore> _storeFactory;

    public SecretsConfigurationSource(InfisicalOptions options, Func<InfisicalOptions, ISecretStore>? storeFactory = null)
    {
        _options = options;
        _storeFactory = storeFactory ?? (o => new InfisicalSecretStore(o));
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new SecretsConfigurationProvider(_options, _storeFactory(_options));
}
