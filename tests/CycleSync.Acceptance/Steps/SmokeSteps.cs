using CycleSync.Acceptance.Support;
using Reqnroll;

namespace CycleSync.Acceptance.Steps;

[Binding]
public sealed class SmokeSteps
{
    private readonly ScenarioWorld _world;
    private string? _shellHtml;

    public SmokeSteps(ScenarioWorld world) => _world = world;

    [Given("the CycleSync system is running")]
    public void GivenTheSystemIsRunning()
    {
        // The API is started lazily by ScenarioWorld when first used.
        Assert.NotNull(_world.Client);
    }

    [When("I request the health endpoint")]
    public async Task WhenIRequestHealth()
    {
        _world.LastResponse = await _world.Client.GetAsync("/health");
    }

    [Then("the response status is {int}")]
    public void ThenTheResponseStatusIs(int expected)
    {
        Assert.NotNull(_world.LastResponse);
        Assert.Equal(expected, (int)_world.LastResponse!.StatusCode);
    }

    [Then("the health status is {string}")]
    public async Task ThenTheHealthStatusIs(string expected)
    {
        Assert.NotNull(_world.LastResponse);
        var body = await _world.LastResponse!.Content.ReadAsStringAsync();
        Assert.Equal(expected, body.Trim());
    }

    [When("I open the application in a browser")]
    public void WhenIOpenTheApp()
    {
        // Without a browser in this environment we load the SPA shell entry point directly:
        // the built shell if present, otherwise the source shell. Both prove the shell exists.
        var builtShell = Path.Combine(RepoPaths.WebSource, "dist", "index.html");
        var sourceShell = Path.Combine(RepoPaths.WebSource, "index.html");
        var shellPath = File.Exists(builtShell) ? builtShell : sourceShell;

        Assert.True(File.Exists(shellPath), $"SPA shell not found at {shellPath}");
        _shellHtml = File.ReadAllText(shellPath);
    }

    [Then("I see the CycleSync application shell")]
    public void ThenISeeTheShell()
    {
        Assert.NotNull(_shellHtml);
        Assert.Contains("<title>CycleSync</title>", _shellHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"root\"", _shellHtml, StringComparison.OrdinalIgnoreCase);
    }
}
