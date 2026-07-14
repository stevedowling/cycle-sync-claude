namespace CycleSync.Api.Integrations.Maps;

public sealed class MapsOptions
{
    public const string SectionName = "AzureMaps";

    /// <summary>Azure Maps subscription key. Injected as configuration/secret; absent in offline tests.</summary>
    public string? SubscriptionKey { get; set; }

    /// <summary>Base URL for the Azure Maps search service.</summary>
    public string BaseUrl { get; set; } = "https://atlas.microsoft.com";
}
