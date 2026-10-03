using System.Text.Json;
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
    /// <summary>
    /// Where this run's values came from: <c>store</c>, or <c>snapshot</c> with the time the snapshot was
    /// written. Read at startup to warn that a service runs on a copy.
    /// </summary>
    public const string LoadedFromKey = "Infisical:LoadedFrom";

    /// <summary>Why the snapshot could not be written, when it could not; read at startup to warn.</summary>
    public const string SnapshotErrorKey = "Infisical:SnapshotError";

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
        catch (ConfigurationException)
        {
            // The store was configured but could not be read. The values it gave last time come first,
            // when a snapshot was kept; without one, running without the store is allowed only where the
            // service was told it may.
            if (ReadSnapshot() is { } snapshot)
            {
                Data = snapshot;
                return;
            }

            if (_options.Optional)
                return;

            throw;
        }

        // A snapshot that cannot be written does not stop the service, which has its values; the reason is
        // kept for the startup warning, so a missing snapshot is found now rather than during an outage.
        try
        {
            WriteSnapshot(loaded);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            loaded[SnapshotErrorKey] = exception.Message;
        }

        loaded[LoadedFromKey] = "store";
        Data = loaded;
    }

    private void WriteSnapshot(Dictionary<string, string?> values)
    {
        if (string.IsNullOrWhiteSpace(_options.SnapshotPath))
            return;

        var path = Path.GetFullPath(_options.SnapshotPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Written beside and moved over: a process stopped mid-write leaves the previous snapshot whole.
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(values));
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.Move(temporary, path, overwrite: true);
    }

    private Dictionary<string, string?>? ReadSnapshot()
    {
        if (string.IsNullOrWhiteSpace(_options.SnapshotPath) || !File.Exists(_options.SnapshotPath))
            return null;

        var values = JsonSerializer.Deserialize<Dictionary<string, string?>>(File.ReadAllText(_options.SnapshotPath));
        if (values is null)
            return null;

        var snapshot = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase)
        {
            [LoadedFromKey] = $"snapshot of {File.GetLastWriteTimeUtc(_options.SnapshotPath):yyyy-MM-dd HH:mm} UTC",
        };
        return snapshot;
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
