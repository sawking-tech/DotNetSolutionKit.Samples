using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ST.DotNetSolutionKit.Samples.Billing.Infrastructure.EntityFramework.DataSeeding.Seeders;

namespace ST.DotNetSolutionKit.Samples.Billing.Infrastructure.EntityFramework.DataSeeding;

public class DataSeeder
{
    private readonly ILogger<DataSeeder> _logger;
    private readonly bool _isProduction;

    public DataSeeder(
        ILogger<DataSeeder> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _isProduction = environment.IsProduction();
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting data seeding...");
        
        // Seeding logic will be added here
        
        _logger.LogInformation("Data seeding completed");
    }

    private async Task RunSeederAsync(
        DataSeederBase seeder,
        string seederName,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Running seeder: {SeederName}", seederName);
        
        try
        {
            await seeder.SeedAsync(cancellationToken);
            _logger.LogDebug("Completed seeder: {SeederName}", seederName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run seeder: {SeederName}", seederName);
            throw;
        }
    }
}