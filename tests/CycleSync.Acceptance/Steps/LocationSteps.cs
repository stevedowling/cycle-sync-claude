using System.Net;
using System.Text.Json;
using CycleSync.Acceptance.Support;
using Reqnroll;

namespace CycleSync.Acceptance.Steps;

[Binding]
public sealed class LocationSteps(ScenarioWorld world)
{
    private string? _lastPersistedName;

    [Given("Azure Maps returns results for {string}")]
    public void GivenAzureMapsReturnsResultsFor(string query)
    {
        // The offline FakeMapsSearch gazetteer knows this query; this step documents the precondition.
        Assert.NotEmpty(query);
    }

    [When("the user searches for {string}")]
    public async Task WhenTheUserSearchesFor(string query) =>
        await world.GetAsync($"/api/locations/search?q={Uri.EscapeDataString(query)}");

    [Then("the results include a location named {string}")]
    public void ThenResultsIncludeLocationNamed(string name)
    {
        Assert.Equal(HttpStatusCode.OK, world.LastResponse!.StatusCode);
        Assert.Contains(SearchResults(), r => NameOf(r) == name);
    }

    [Then("each result has coordinates and a country")]
    public void ThenEachResultHasCoordinatesAndCountry()
    {
        var results = SearchResults();
        Assert.NotEmpty(results);
        foreach (var result in results)
        {
            Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("country").GetString()));
            var coordinates = result.GetProperty("coordinates");
            Assert.Equal(JsonValueKind.Number, coordinates.GetProperty("latitude").ValueKind);
            Assert.Equal(JsonValueKind.Number, coordinates.GetProperty("longitude").ValueKind);
        }
    }

    [When("the user selects {string}")]
    public async Task WhenTheUserSelects(string name)
    {
        var result = SearchResults().Single(r => NameOf(r) == name);
        await PersistAsync(result);
    }

    [Given("a persistent location {string} exists")]
    [Given("a persistent location {string} already exists")]
    public async Task GivenPersistentLocationExists(string name) => await EnsurePersistedAsync(name);

    [Then("a persistent location {string} exists")]
    public async Task ThenPersistentLocationExists(string name) =>
        Assert.NotEmpty(await LocationsNamedAsync(name));

    [Then("it is visible to all users")]
    public async Task ThenItIsVisibleToAllUsers()
    {
        var name = _lastPersistedName!;
        var viewer = world.CurrentEmail;

        await world.SignInAsync("colleague@cyclesync.example");
        Assert.NotEmpty(await LocationsNamedAsync(name));

        if (viewer is not null)
        {
            world.SetCurrent(viewer);
        }
    }

    [Then("there is exactly one location {string}")]
    public async Task ThenExactlyOneLocation(string name) =>
        Assert.Single(await LocationsNamedAsync(name));

    [When("any user attempts to delete {string}")]
    public async Task WhenAnyUserAttemptsToDelete(string name)
    {
        var id = await LocationIdAsync(name);
        await world.DeleteAsync($"/api/locations/{id}");
    }

    [Then("the operation is not permitted")]
    public void ThenTheOperationIsNotPermitted() =>
        Assert.Contains(world.LastResponse!.StatusCode, new[]
        {
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.MethodNotAllowed,
        });

    [Then("the location {string} still exists")]
    public async Task ThenTheLocationStillExists(string name) =>
        Assert.NotEmpty(await LocationsNamedAsync(name));

    // --- helpers ---------------------------------------------------------------------------

    private async Task EnsurePersistedAsync(string name)
    {
        await world.GetAsync($"/api/locations/search?q={Uri.EscapeDataString(name)}");
        var result = SearchResults().Single(r => NameOf(r) == name);
        await PersistAsync(result);
    }

    private async Task PersistAsync(JsonElement searchResult)
    {
        var coordinates = searchResult.GetProperty("coordinates");
        var azureMapsId = searchResult.TryGetProperty("azureMapsId", out var idElement) && idElement.ValueKind == JsonValueKind.String
            ? idElement.GetString()
            : null;

        _lastPersistedName = NameOf(searchResult);

        await world.PostJsonAsync("/api/locations", new
        {
            name = NameOf(searchResult),
            country = searchResult.GetProperty("country").GetString(),
            latitude = coordinates.GetProperty("latitude").GetDouble(),
            longitude = coordinates.GetProperty("longitude").GetDouble(),
            azureMapsId,
        });
    }

    private async Task<Guid> LocationIdAsync(string name)
    {
        var match = (await LocationsNamedAsync(name)).First();
        return match.GetProperty("id").GetGuid();
    }

    private async Task<IReadOnlyList<JsonElement>> LocationsNamedAsync(string name)
    {
        await world.GetAsync("/api/locations");
        return world.LastJsonClone().EnumerateArray().Where(l => NameOf(l) == name).ToList();
    }

    private IReadOnlyList<JsonElement> SearchResults() => world.LastJsonClone().EnumerateArray().ToList();

    private static string? NameOf(JsonElement element) => element.GetProperty("name").GetString();
}
