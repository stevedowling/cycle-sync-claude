using CycleSync.Api.Integrations.Intelligence;
using CycleSync.Api.Integrations.Maps;

namespace CycleSync.Api.Integrations;

public static class IntegrationsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the server-side integration abstractions used by the Locations feature: maps
    /// search (Azure Maps) and location-intelligence generation. Acceptance tests replace these
    /// with deterministic doubles where needed.
    /// </summary>
    public static IServiceCollection AddCycleSyncIntegrations(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MapsOptions>(configuration.GetSection(MapsOptions.SectionName));
        services.AddHttpClient<IMapsSearch, AzureMapsSearch>();

        services.AddSingleton<ILocationIntelligenceGenerator, HeuristicLocationIntelligenceGenerator>();

        return services;
    }
}
