using System.Net;
using System.Text.Json;
using CycleSync.Acceptance.Support;
using Reqnroll;

namespace CycleSync.Acceptance.Steps;

/// <summary>
/// Phase 5 hardening: asserts the API's error taxonomy. Every failure must be a typed RFC 7807
/// problem detail carrying a stable <c>type</c> slug (not-found, validation, forbidden, …) plus a
/// human-readable title and detail — never a body-less status code.
/// </summary>
[Binding]
public sealed class ErrorTaxonomySteps(ScenarioWorld world)
{
    [When("the user requests an off-cycle that does not exist")]
    public Task WhenTheUserRequestsAMissingOffCycle() =>
        world.GetAsync($"/api/off-cycles/{Guid.NewGuid()}");

    [When("the user requests a location that does not exist")]
    public Task WhenTheUserRequestsAMissingLocation() =>
        world.GetAsync($"/api/locations/{Guid.NewGuid()}");

    [When("the user creates an off-cycle {string} for a location that does not exist")]
    public Task WhenTheUserCreatesAnOffCycleForAMissingLocation(string name) =>
        world.PostJsonAsync("/api/off-cycles", new
        {
            name,
            locationId = Guid.NewGuid(),
            startDate = "2026-10-05",
            endDate = "2026-10-09",
        });

    [Then("the response is a problem of type {string} with status {int}")]
    public void ThenTheResponseIsAProblem(string type, int status)
    {
        Assert.Equal((HttpStatusCode)status, world.LastResponse!.StatusCode);

        using var doc = JsonDocument.Parse(world.LastBody!);
        var root = doc.RootElement;
        Assert.Equal(type, root.GetProperty("type").GetString());
        Assert.Equal(status, root.GetProperty("status").GetInt32());
    }

    [Then("the problem carries a human-readable title and detail")]
    public void ThenTheProblemCarriesTitleAndDetail()
    {
        using var doc = JsonDocument.Parse(world.LastBody!);
        var root = doc.RootElement;
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("detail").GetString()));
    }
}
