namespace CycleSync.Acceptance.Support;

/// <summary>
/// Per-scenario shared state: the running API, an HTTP client, and the most recent response.
/// Reqnroll creates one instance per scenario via constructor injection and disposes it after.
/// </summary>
public sealed class ScenarioWorld : IDisposable
{
    private readonly CycleSyncApiFactory _factory = new();
    private HttpClient? _client;

    public HttpClient Client => _client ??= _factory.CreateClient();

    public HttpResponseMessage? LastResponse { get; set; }

    public void Dispose()
    {
        LastResponse?.Dispose();
        _client?.Dispose();
        _factory.Dispose();
    }
}
