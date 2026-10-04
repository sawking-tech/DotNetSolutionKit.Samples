// Part of DotNetSolutionKit (https://dnsk.sawking.tech/). MIT License, Copyright (c) 2025 Vladimir Savkin.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace ST.DotNetSolutionKit.Samples.Common.Application.FeatureManagement;

/// <summary>
/// Writes feature values back into the shared file.
/// </summary>
/// <remarks>
/// <para>
/// The default store, and the only one a platform needs before it has somewhere central to keep
/// configuration. Where an external store exists, this one still matters: it holds the fallback, so
/// keeping it current is what stops a store outage from reverting the platform to decisions made
/// weeks ago.
/// </para>
/// <para>
/// Writes replace the file atomically — a temporary file next to it, then a move — so a process
/// killed mid-write leaves the previous version intact rather than half a document. A lock keeps two
/// requests in the same process from interleaving; two <em>processes</em> writing the same file is
/// not defended against here, and is the reason a real deployment points this at a shared, writable
/// path rather than at a copy inside each container image.
/// </para>
/// <para>
/// Only values are touched. Description, owner, expiry and the rest are left exactly as they were,
/// because they are decisions someone wrote down, not state this class owns.
/// </para>
/// </remarks>
public sealed class FileFeatureStore : IFeatureStore
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly FeatureStoreOptions _options;
    private readonly ILogger<FileFeatureStore> _logger;

    public FileFeatureStore(FeatureStoreOptions options, ILogger<FileFeatureStore> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SetAsync(
        string key,
        bool enabled,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var path = _options.Path;
            var document = await ReadAsync(path, cancellationToken);

            var features = document["Features"]?.AsObject()
                ?? throw new InvalidOperationException(
                    $"'{path}' has no Features section, so there is nothing to change.");

            var feature = features[key]?.AsObject()
                ?? throw new InvalidOperationException(
                    $"Feature '{key}' is not declared in '{path}'. Declare it before setting a value: " +
                    "a value without a declaration has no description, owner or expiry, and nobody " +
                    "would know later what it was for.");

            if (environment is null)
            {
                feature["enabled"] = enabled;
            }
            else
            {
                var environments = feature["environments"]?.AsObject();
                if (environments is null)
                {
                    environments = new JsonObject();
                    feature["environments"] = environments;
                }

                environments[environment] = enabled;
            }

            await WriteAsync(path, document, cancellationToken);

            _logger.LogInformation(
                "Feature {FeatureKey} set to {Enabled} for {Environment} in {Path}.",
                key, enabled, environment ?? "the default", path);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static async Task<JsonObject> ReadAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException($"The feature file '{path}' does not exist.");

        await using var stream = File.OpenRead(path);
        var node = await JsonNode.ParseAsync(stream, cancellationToken: ct)
            ?? throw new InvalidOperationException($"The feature file '{path}' is empty.");

        return node.AsObject();
    }

    private static async Task WriteAsync(string path, JsonObject document, CancellationToken ct)
    {
        var temporary = path + ".tmp";

        await using (var stream = File.Create(temporary))
        {
            await JsonSerializer.SerializeAsync(stream, document, WriteOptions, ct);
        }

        // Replace rather than write in place: a process that dies here leaves the old file whole.
        File.Move(temporary, path, overwrite: true);
    }
}

/// <summary>Where the writable copy of the feature file lives.</summary>
public sealed class FeatureStoreOptions
{
    public const string SectionName = "FeatureStore";

    /// <summary>
    /// Path of the file values are written to. Defaults to the copy shipped beside the application,
    /// which is fine locally and wrong in a container: point it at a mounted, writable path so a
    /// change survives the next deployment.
    /// </summary>
    public string Path { get; set; } = "features.json";
}
