using System.Net;
using System.Text.Json;
using CycleSync.Acceptance.Support;
using Microsoft.IdentityModel.JsonWebTokens;
using Reqnroll;

namespace CycleSync.Acceptance.Steps;

[Binding]
public sealed class AuthSteps(ScenarioWorld world)
{
    private string? _pendingGoogleEmail;

    [Given("the workspace is restricted to the {string} domain")]
    public void GivenWorkspaceRestricted(string domain)
    {
        // The test host is configured with AllowedDomains = ["cyclesync.example"].
        Assert.Equal("cyclesync.example", domain);
    }

    [Given("a Google account {string}")]
    public void GivenAGoogleAccount(string email) => _pendingGoogleEmail = email;

    [When("the user completes Google sign-in")]
    public async Task WhenUserCompletesSignIn()
    {
        await world.SignInAsync(_pendingGoogleEmail!);
    }

    [Then("a CycleSync session is established")]
    public void ThenSessionEstablished()
    {
        Assert.Equal(HttpStatusCode.OK, world.LastResponse!.StatusCode);
        Assert.True(world.HasSession(_pendingGoogleEmail!));
    }

    [Then("no CycleSync session is established")]
    public void ThenNoSession()
    {
        Assert.False(world.HasSession(_pendingGoogleEmail!));
    }

    [Then("the user has full access rights")]
    public async Task ThenFullAccess()
    {
        Assert.Equal(HttpStatusCode.OK, (await world.GetAsync("/api/me/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await world.GetAsync("/api/locations")).StatusCode);
    }

    [Then("access is denied with reason {string}")]
    public void ThenAccessDenied(string reason)
    {
        Assert.Equal(HttpStatusCode.Forbidden, world.LastResponse!.StatusCode);
        using var doc = JsonDocument.Parse(world.LastBody!);
        Assert.Equal(reason, doc.RootElement.GetProperty("detail").GetString());
    }

    [Given("no active session")]
    public void GivenNoActiveSession() => world.ClearSession();

    [When("I request the list of locations")]
    public async Task WhenIRequestLocations() => await world.GetAsync("/api/locations");

    [Given("a signed-in user {string}")]
    public async Task GivenASignedInUser(string email) => await world.SignInAsync(email);

    [Then("neither user has administrative privileges over the other")]
    public void ThenNoAdminPrivileges()
    {
        var handler = new JsonWebTokenHandler();
        foreach (var email in world.SignedInEmails)
        {
            var token = handler.ReadJsonWebToken(world.TokenFor(email));
            Assert.DoesNotContain(token.Claims, c =>
                c.Type.Equals("role", StringComparison.OrdinalIgnoreCase) ||
                c.Type.Equals(System.Security.Claims.ClaimTypes.Role, StringComparison.OrdinalIgnoreCase) ||
                c.Type.Equals("admin", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Then("both users can perform the same actions")]
    public async Task ThenBothCanPerformSameActions()
    {
        foreach (var email in world.SignedInEmails.ToArray())
        {
            world.SetCurrent(email);
            Assert.Equal(HttpStatusCode.OK, (await world.GetAsync("/api/me/profile")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await world.GetAsync("/api/users")).StatusCode);
        }
    }
}
