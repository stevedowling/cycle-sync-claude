using System.Net;
using System.Text.Json;
using CycleSync.Acceptance.Support;
using Reqnroll;

namespace CycleSync.Acceptance.Steps;

[Binding]
public sealed class OffCycleSteps(ScenarioWorld world)
{
    // --- creation & editing ----------------------------------------------------------------

    [When("the user creates an off-cycle {string} at {string} from {string} to {string}")]
    public async Task WhenTheUserCreatesAnOffCycle(string name, string location, string start, string end)
    {
        var locationId = await world.EnsureLocationAsync(location);
        await world.PostJsonAsync("/api/off-cycles", new { name, locationId, startDate = start, endDate = end });
        world.LastCreated = ("offcycle", name);
    }

    [Given("an off-cycle {string} at {string} from {string} to {string}")]
    public async Task GivenAnOffCycle(string name, string location, string start, string end)
    {
        var locationId = await world.EnsureLocationAsync(location);
        await world.PostJsonAsync("/api/off-cycles", new { name, locationId, startDate = start, endDate = end });
        world.LastCreated = ("offcycle", name);
    }

    [When("the user changes the dates to {string} to {string}")]
    public async Task WhenTheUserChangesTheDates(string start, string end)
    {
        var offCycle = await FindOffCycleAsync(world.LastCreated!.Value.Name);
        var id = offCycle.GetProperty("id").GetGuid();
        var name = offCycle.GetProperty("name").GetString();
        await world.PutJsonAsync($"/api/off-cycles/{id}", new { name, startDate = start, endDate = end });
    }

    [Then("an off-cycle {string} exists at {string}")]
    public async Task ThenAnOffCycleExistsAt(string name, string location)
    {
        var offCycle = await FindOffCycleAsync(name);
        Assert.Equal(location, offCycle.GetProperty("locationName").GetString());
    }

    [Then("its dates are {string} to {string}")]
    public async Task ThenItsDatesAre(string start, string end)
    {
        var offCycle = await FindOffCycleAsync(world.LastCreated!.Value.Name);
        Assert.Equal(start, offCycle.GetProperty("startDate").GetString());
        Assert.Equal(end, offCycle.GetProperty("endDate").GetString());
    }

    [Then("cost estimates are recalculated for the new dates")]
    public async Task ThenCostEstimatesAreRecalculated()
    {
        var offCycle = await FindOffCycleAsync(world.LastCreated!.Value.Name);
        var id = offCycle.GetProperty("id").GetGuid();
        var expectedNights = offCycle.GetProperty("nights").GetInt32();

        await world.GetAsync($"/api/off-cycles/{id}/cost-estimate");
        Assert.Equal(HttpStatusCode.OK, world.LastResponse!.StatusCode);
        Assert.Equal(expectedNights, world.LastJsonClone().GetProperty("nights").GetInt32());
    }

    // --- attendance ------------------------------------------------------------------------

    [When("the user sets their attendance to {string} for {string}")]
    [Given("the user has attendance status {string} for {string}")]
    public async Task WhenTheUserSetsAttendance(string status, string name)
    {
        var id = await OffCycleIdAsync(name);
        await world.PutJsonAsync($"/api/off-cycles/{id}/attendance", new { status });
    }

    [Given("{string} has attendance status {string} for {string}")]
    public async Task GivenUserHasAttendanceStatus(string email, string status, string name)
    {
        var previous = world.CurrentEmail;
        if (!world.HasSession(email))
        {
            await world.SignInAsync(email);
        }

        world.SetCurrent(email);
        var id = await OffCycleIdAsync(name);
        await world.PutJsonAsync($"/api/off-cycles/{id}/attendance", new { status });

        if (previous is not null)
        {
            world.SetCurrent(previous);
        }
    }

    [Then("their attendance status for {string} is {string}")]
    public async Task ThenTheirAttendanceStatusIs(string name, string status) =>
        Assert.Equal(status, await StatusForAsync(name, world.UserId(world.CurrentEmail!)));

    [Then("{string} has attendance status {string} for {string}")]
    public async Task ThenUserHasAttendanceStatus(string email, string status, string name) =>
        Assert.Equal(status, await StatusForAsync(name, world.UserId(email)));

    [When("anyone views the {string} attendance summary")]
    public async Task WhenAnyoneViewsTheAttendanceSummary(string name)
    {
        var id = await OffCycleIdAsync(name);
        await world.GetAsync($"/api/off-cycles/{id}/attendance");
    }

    [Then("it shows {int} {string}, {int} {string} and {int} {string}")]
    public void ThenTheSummaryShows(int n1, string s1, int n2, string s2, int n3, string s3)
    {
        var counts = world.LastJsonClone().GetProperty("counts");
        Assert.Equal(n1, counts.GetProperty(s1).GetInt32());
        Assert.Equal(n2, counts.GetProperty(s2).GetInt32());
        Assert.Equal(n3, counts.GetProperty(s3).GetInt32());
    }

    // --- helpers ---------------------------------------------------------------------------

    private async Task<string?> StatusForAsync(string name, Guid userId)
    {
        var id = await OffCycleIdAsync(name);
        await world.GetAsync($"/api/off-cycles/{id}/attendance");
        var roster = world.LastJsonClone().GetProperty("roster");
        var entry = roster.EnumerateArray().Single(e => e.GetProperty("userId").GetGuid() == userId);
        return entry.GetProperty("status").GetString();
    }

    private async Task<Guid> OffCycleIdAsync(string name) =>
        (await FindOffCycleAsync(name)).GetProperty("id").GetGuid();

    private async Task<JsonElement> FindOffCycleAsync(string name)
    {
        await world.GetAsync("/api/off-cycles");
        return world.LastJsonClone().EnumerateArray().Single(o => o.GetProperty("name").GetString() == name);
    }
}
