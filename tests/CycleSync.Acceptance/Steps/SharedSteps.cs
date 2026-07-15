using System.Text.Json;
using CycleSync.Acceptance.Support;
using Reqnroll;

namespace CycleSync.Acceptance.Steps;

/// <summary>
/// Cross-feature steps whose wording is shared by more than one feature (visibility to all users,
/// and the generic "operation rejected" assertion). Kept in one place so the step text is bound
/// exactly once.
/// </summary>
[Binding]
public sealed class SharedSteps(ScenarioWorld world)
{
    [Then("it is visible to all users")]
    public async Task ThenItIsVisibleToAllUsers()
    {
        var created = world.LastCreated ?? throw new InvalidOperationException("Nothing was created in this scenario.");
        var viewer = world.CurrentEmail;

        await world.SignInAsync("colleague@cyclesync.example");

        var path = created.Kind == "offcycle" ? "/api/off-cycles" : "/api/locations";
        await world.GetAsync(path);
        Assert.Contains(world.LastJsonClone().EnumerateArray(),
            e => e.GetProperty("name").GetString() == created.Name);

        if (viewer is not null)
        {
            world.SetCurrent(viewer);
        }
    }

    [Then("the operation is rejected with reason {string}")]
    public void ThenTheOperationIsRejected(string reason)
    {
        Assert.False(world.LastResponse!.IsSuccessStatusCode);
        using var doc = JsonDocument.Parse(world.LastBody!);
        Assert.Equal(reason, doc.RootElement.GetProperty("detail").GetString());
    }
}
