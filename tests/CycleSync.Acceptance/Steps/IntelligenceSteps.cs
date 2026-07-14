using System.Net;
using System.Text.Json;
using CycleSync.Acceptance.Support;
using Reqnroll;

namespace CycleSync.Acceptance.Steps;

[Binding]
public sealed class IntelligenceSteps(ScenarioWorld world)
{
    [Given("intelligence for {string} was generated {int} day ago")]
    [Given("intelligence for {string} was generated {int} days ago")]
    public async Task GivenIntelligenceGeneratedDaysAgo(string name, int days)
    {
        var id = await LocationIdAsync(name);
        var now = world.Clock.GetUtcNow();

        // Generate (and cache) the intelligence at a point in the past, then restore the clock.
        world.Clock.SetUtcNow(now.AddDays(-days));
        var response = await world.GetAsync($"/api/locations/{id}/intelligence");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        world.Clock.SetUtcNow(now);
    }

    [When("the user opens the {string} details")]
    public async Task WhenTheUserOpensDetails(string name)
    {
        var id = await LocationIdAsync(name);
        await world.GetAsync($"/api/locations/{id}/intelligence");
    }

    [Then("AI-generated intelligence is shown")]
    public void ThenIntelligenceIsShown() =>
        Assert.Equal(HttpStatusCode.OK, world.LastResponse!.StatusCode);

    [Then("it includes climate summary and best times to visit")]
    public void ThenIncludesClimateAndBestTimes()
    {
        var intelligence = world.LastJsonClone();
        Assert.False(string.IsNullOrWhiteSpace(intelligence.GetProperty("climateSummary").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(intelligence.GetProperty("bestTimesToVisit").GetString()));
    }

    [Then("it shows the generation timestamp")]
    [Then("every AI-generated figure displays when it was generated")]
    public void ThenShowsGenerationTimestamp() =>
        Assert.True(world.LastJsonClone().GetProperty("generatedAt").TryGetDateTimeOffset(out _));

    [Then("it shows a confidence indicator")]
    [Then("every AI-generated figure displays a confidence level")]
    public void ThenShowsConfidence()
    {
        var confidence = world.LastJsonClone().GetProperty("confidence").GetString();
        Assert.Contains(confidence, new[] { "Low", "Medium", "High" });
    }

    [Then("the previously generated intelligence is shown")]
    [Then("no new generation is triggered")]
    public void ThenPreviouslyGeneratedIntelligenceIsShown()
    {
        // Reuse (rather than regeneration) is observable: the timestamp is the earlier one, so the
        // returned intelligence is materially older than "now".
        var generatedAt = world.LastJsonClone().GetProperty("generatedAt").GetDateTimeOffset();
        var age = world.Clock.GetUtcNow() - generatedAt;
        Assert.True(age > TimeSpan.FromHours(12), $"expected cached intelligence, but it was generated {age} ago");
    }

    [Then("visa guidance is shown for a {string} passport holder")]
    public void ThenVisaGuidanceShownFor(string country)
    {
        var visaNotes = world.LastJsonClone().GetProperty("visaNotes").GetString();
        Assert.False(string.IsNullOrWhiteSpace(visaNotes));
        Assert.Contains(country, visaNotes!, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Guid> LocationIdAsync(string name)
    {
        await world.GetAsync("/api/locations");
        var match = world.LastJsonClone().EnumerateArray()
            .First(l => l.GetProperty("name").GetString() == name);
        return match.GetProperty("id").GetGuid();
    }
}
