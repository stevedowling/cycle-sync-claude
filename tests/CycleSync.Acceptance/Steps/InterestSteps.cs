using System.Net;
using System.Text.Json;
using CycleSync.Acceptance.Support;
using Reqnroll;

namespace CycleSync.Acceptance.Steps;

[Binding]
public sealed class InterestSteps(ScenarioWorld world)
{
    [When("the user marks interest in {string}")]
    [Given("the user is already interested in {string}")]
    public async Task WhenTheUserMarksInterest(string name)
    {
        var id = await LocationIdAsync(name);
        var response = await world.PutJsonAsync($"/api/locations/{id}/interest", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [When("the user removes interest in {string}")]
    public async Task WhenTheUserRemovesInterest(string name)
    {
        var id = await LocationIdAsync(name);
        var response = await world.DeleteAsync($"/api/locations/{id}/interest");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Given("{string} is interested in {string}")]
    public async Task GivenOtherUserIsInterested(string email, string name)
    {
        var viewer = world.CurrentEmail;

        if (world.HasSession(email))
        {
            world.SetCurrent(email);
        }
        else
        {
            await world.SignInAsync(email);
        }

        var id = await LocationIdAsync(name);
        var response = await world.PutJsonAsync($"/api/locations/{id}/interest", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        if (viewer is not null)
        {
            world.SetCurrent(viewer);
        }
    }

    [When("the user sorts locations by interest")]
    public async Task WhenTheUserSortsByInterest() =>
        await world.GetAsync("/api/locations?sort=interest");

    [Then("{string} appears in the user's interested locations")]
    public async Task ThenAppearsInInterested(string name)
    {
        await world.GetAsync("/api/me/interests");
        Assert.Contains(InterestedNames(), n => n == name);
    }

    [Then("{string} is not in the user's interested locations")]
    public async Task ThenNotInInterested(string name)
    {
        await world.GetAsync("/api/me/interests");
        Assert.DoesNotContain(InterestedNames(), n => n == name);
    }

    [Then("the interest count for {string} is {int}")]
    public async Task ThenInterestCountIs(string name, int expected)
    {
        var id = await LocationIdAsync(name);
        await world.GetAsync($"/api/locations/{id}");
        Assert.Equal(HttpStatusCode.OK, world.LastResponse!.StatusCode);
        Assert.Equal(expected, world.LastJsonClone().GetProperty("interestCount").GetInt32());
    }

    [Then("{string} is ranked above {string}")]
    public void ThenRankedAbove(string higher, string lower)
    {
        var names = world.LastJsonClone().EnumerateArray()
            .Select(l => l.GetProperty("name").GetString())
            .ToList();

        var higherIndex = names.IndexOf(higher);
        var lowerIndex = names.IndexOf(lower);

        Assert.True(higherIndex >= 0, $"'{higher}' was not in the sorted list.");
        Assert.True(lowerIndex >= 0, $"'{lower}' was not in the sorted list.");
        Assert.True(higherIndex < lowerIndex,
            $"expected '{higher}' (index {higherIndex}) to rank above '{lower}' (index {lowerIndex}).");
    }

    // --- helpers ---------------------------------------------------------------------------

    private IEnumerable<string?> InterestedNames() =>
        world.LastJsonClone().EnumerateArray().Select(l => l.GetProperty("name").GetString());

    private async Task<Guid> LocationIdAsync(string name)
    {
        await world.GetAsync("/api/locations");
        var match = world.LastJsonClone().EnumerateArray()
            .First(l => l.GetProperty("name").GetString() == name);
        return match.GetProperty("id").GetGuid();
    }
}
