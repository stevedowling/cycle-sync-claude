using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CycleSync.Acceptance.Support;

/// <summary>
/// Per-scenario shared state: the running API, signed-in users, and the most recent response.
/// Reqnroll creates one instance per scenario via constructor injection and disposes it after.
/// </summary>
public sealed class ScenarioWorld : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CycleSyncApiFactory _factory = new();
    private readonly Dictionary<string, SignedInUser> _users = new(StringComparer.OrdinalIgnoreCase);
    private HttpClient? _client;

    public string? CurrentEmail { get; private set; }
    public HttpResponseMessage? LastResponse { get; set; }
    public string? LastBody { get; private set; }

    /// <summary>The controllable clock the API uses; lets steps generate data at a chosen time.</summary>
    public ControllableTimeProvider Clock => _factory.Clock;

    // Accumulated profile edits (a PUT replaces the whole profile, so steps build it up).
    public string? PendingHomeLocation { get; set; }
    public string? PendingCurrency { get; set; }
    public string? PendingLanguage { get; set; }

    /// <summary>
    /// The most recently created entity (a location or an off-cycle) and its name. Lets a single
    /// "it is visible to all users" step assert against whichever kind was just created.
    /// </summary>
    public (string Kind, string Name)? LastCreated { get; set; }

    public HttpClient Client
    {
        get
        {
            _factory.EnsureSchemaCreated();
            return _client ??= _factory.CreateClient();
        }
    }

    public string? CurrentToken =>
        CurrentEmail is not null && _users.TryGetValue(CurrentEmail, out var user) ? user.Token : null;

    public bool HasSession(string email) => _users.ContainsKey(email);

    public IEnumerable<string> SignedInEmails => _users.Keys;

    public string TokenFor(string email) => _users[email].Token;

    public Guid UserId(string email) => _users[email].Id;

    public void SetCurrent(string email) => CurrentEmail = email;

    public void ClearSession() => CurrentEmail = null;

    public async Task<HttpResponseMessage> SignInAsync(string email)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/google", new { idToken = email });
        await CaptureAsync(response);

        if (response.IsSuccessStatusCode && LastBody is not null)
        {
            using var doc = JsonDocument.Parse(LastBody);
            var token = doc.RootElement.GetProperty("token").GetString()!;
            var id = doc.RootElement.GetProperty("user").GetProperty("id").GetGuid();
            _users[email] = new SignedInUser(email, token, id);
            CurrentEmail = email;
        }

        return response;
    }

    public Task<HttpResponseMessage> GetAsync(string path) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, path));

    public Task<HttpResponseMessage> DeleteAsync(string path) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Delete, path));

    public Task<HttpResponseMessage> PutJsonAsync(string path, object body) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Put, path) { Content = JsonContent.Create(body) });

    public Task<HttpResponseMessage> PostJsonAsync(string path, object body) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) });

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        if (CurrentToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CurrentToken);
        }

        var response = await Client.SendAsync(request);
        await CaptureAsync(response);
        return response;
    }

    public JsonElement LastJsonClone()
    {
        using var doc = JsonDocument.Parse(LastBody ?? "null");
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Ensures a location with the given display name is persisted (searching then persisting via the
    /// real API, which is de-duplicated) and returns its id. Shared by the location and off-cycle
    /// steps so an off-cycle can be created against a location that a scenario has not persisted
    /// explicitly. Marks it as the last-created entity.
    /// </summary>
    public async Task<Guid> EnsureLocationAsync(string name)
    {
        await GetAsync($"/api/locations/search?q={Uri.EscapeDataString(name)}");
        var result = LastJsonClone().EnumerateArray().Single(r => r.GetProperty("name").GetString() == name);
        var coordinates = result.GetProperty("coordinates");
        var azureMapsId = result.TryGetProperty("azureMapsId", out var idElement) && idElement.ValueKind == JsonValueKind.String
            ? idElement.GetString()
            : null;

        await PostJsonAsync("/api/locations", new
        {
            name = result.GetProperty("name").GetString(),
            country = result.GetProperty("country").GetString(),
            latitude = coordinates.GetProperty("latitude").GetDouble(),
            longitude = coordinates.GetProperty("longitude").GetDouble(),
            azureMapsId,
        });
        LastCreated = ("location", name);

        await GetAsync("/api/locations");
        var location = LastJsonClone().EnumerateArray().First(l => l.GetProperty("name").GetString() == name);
        return location.GetProperty("id").GetGuid();
    }

    public T? LastAs<T>() => LastBody is null ? default : JsonSerializer.Deserialize<T>(LastBody, Json);

    private async Task CaptureAsync(HttpResponseMessage response)
    {
        LastResponse = response;
        LastBody = await response.Content.ReadAsStringAsync();
    }

    public void Dispose()
    {
        LastResponse?.Dispose();
        _client?.Dispose();
        _factory.Dispose();
    }

    private sealed record SignedInUser(string Email, string Token, Guid Id);
}
