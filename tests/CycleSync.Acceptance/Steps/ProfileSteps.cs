using System.Net;
using System.Text.Json;
using CycleSync.Acceptance.Support;
using Reqnroll;

namespace CycleSync.Acceptance.Steps;

[Binding]
public sealed class ProfileSteps(ScenarioWorld world)
{
    [When("the user signs in for the first time")]
    public async Task WhenUserSignsInFirstTime() => await world.SignInAsync(world.CurrentEmail!);

    [Then("a profile exists with their name and email")]
    public async Task ThenProfileExists()
    {
        await world.GetAsync("/api/me/profile");
        Assert.Equal(HttpStatusCode.OK, world.LastResponse!.StatusCode);

        var profile = world.LastJsonClone();
        Assert.Equal(world.CurrentEmail, profile.GetProperty("email").GetString());
        Assert.False(string.IsNullOrWhiteSpace(profile.GetProperty("displayName").GetString()));
    }

    [Then("the profile has no passports yet")]
    public async Task ThenNoPassports()
    {
        var passports = await GetPassports();
        Assert.Empty(passports);
    }

    [When("the user sets their home location to {string}")]
    public async Task WhenSetHome(string value)
    {
        world.PendingHomeLocation = value;
        await PutProfile();
    }

    [When("sets their preferred currency to {string}")]
    public async Task WhenSetCurrency(string value)
    {
        world.PendingCurrency = value;
        await PutProfile();
    }

    [When("sets their preferred language to {string}")]
    public async Task WhenSetLanguage(string value)
    {
        world.PendingLanguage = value;
        await PutProfile();
    }

    [Then("the profile reflects home location {string}")]
    public async Task ThenReflectsHome(string value) =>
        Assert.Equal(value, (await GetProfile()).GetProperty("homeLocation").GetString());

    [Then("the profile reflects currency {string}")]
    public async Task ThenReflectsCurrency(string value) =>
        Assert.Equal(value, (await GetProfile()).GetProperty("preferredCurrency").GetString());

    [Then("the profile reflects language {string}")]
    public async Task ThenReflectsLanguage(string value) =>
        Assert.Equal(value, (await GetProfile()).GetProperty("preferredLanguage").GetString());

    [When("the user adds a passport for {string}")]
    [When("adds a passport for {string}")]
    public async Task WhenAddPassport(string country) =>
        await world.PostJsonAsync("/api/me/passports", new { country });

    [Given("the user holds passports for {string} and {string}")]
    public async Task GivenHoldsPassports(string first, string second)
    {
        await world.PostJsonAsync("/api/me/passports", new { country = first });
        await world.PostJsonAsync("/api/me/passports", new { country = second });
    }

    [Given("the user holds a passport for {string}")]
    public async Task GivenHoldsPassport(string country) =>
        await world.PostJsonAsync("/api/me/passports", new { country });

    [When("the user removes the {string} passport")]
    public async Task WhenRemovePassport(string country) =>
        await world.DeleteAsync($"/api/me/passports/{Uri.EscapeDataString(country)}");

    [Then("the profile lists {int} passports")]
    [Then("the profile lists {int} passport")]
    public async Task ThenProfileListsPassports(int count) =>
        Assert.Equal(count, (await GetPassports()).Count);

    [Then("the passports include {string} and {string}")]
    public async Task ThenPassportsIncludeBoth(string first, string second)
    {
        var passports = await GetPassports();
        Assert.Contains(first, passports);
        Assert.Contains(second, passports);
    }

    [Then("the passports include {string}")]
    public async Task ThenPassportsInclude(string country) =>
        Assert.Contains(country, await GetPassports());

    [Given("another signed-in user {string}")]
    public async Task GivenAnotherSignedInUser(string email) => await world.SignInAsync(email);

    [When("{string} views the profile of {string}")]
    public async Task WhenUserViewsProfileOf(string viewer, string target)
    {
        world.SetCurrent(viewer);
        await world.GetAsync($"/api/users/{world.UserId(target)}");
    }

    [Then("they can see the home location and passports")]
    public void ThenTheyCanSeeProfile()
    {
        Assert.Equal(HttpStatusCode.OK, world.LastResponse!.StatusCode);
        var profile = world.LastJsonClone();
        Assert.Equal(JsonValueKind.Array, profile.GetProperty("passports").ValueKind);
        Assert.True(profile.TryGetProperty("homeLocation", out _));
    }

    private async Task PutProfile()
    {
        await world.PutJsonAsync("/api/me/profile", new
        {
            homeLocation = world.PendingHomeLocation,
            preferredCurrency = world.PendingCurrency,
            preferredLanguage = world.PendingLanguage,
        });
    }

    private async Task<JsonElement> GetProfile()
    {
        await world.GetAsync("/api/me/profile");
        return world.LastJsonClone();
    }

    private async Task<IReadOnlyList<string>> GetPassports()
    {
        await world.GetAsync("/api/me/passports");
        return world.LastAs<List<string>>() ?? [];
    }
}
