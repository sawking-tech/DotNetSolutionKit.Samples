using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ST.DotNetSolutionKit.Samples.Common.Application.Configuration;

public static class OptionsServiceCollectionExtensions
{
    /// <summary>
    /// Registers validated options bound to a configuration section,
    /// with the interface resolved as a singleton from the DI container.
    /// Validation runs on application startup via DataAnnotations.
    /// </summary>
    public static IServiceCollection AddValidatedOptions<TInterface, TSettings>(
        this IServiceCollection services,
        string sectionName)
        where TInterface : class
        where TSettings : class, TInterface, new()
    {
        services.ValidateOptions<TSettings>(sectionName);

        services.AddSingleton<TInterface>(sp =>
            sp.GetRequiredService<IOptions<TSettings>>().Value);

        return services;
    }

    /// <summary>
    /// Binds options to a configuration section and validates via DataAnnotations on startup.
    /// Does not register the interface in DI.
    /// </summary>
    public static void ValidateOptions<TSettings>(
        this IServiceCollection services,
        string sectionName)
        where TSettings : class, new()
    {
        services.AddOptions<TSettings>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}