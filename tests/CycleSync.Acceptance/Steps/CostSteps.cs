using System.Net;
using System.Text.Json;
using CycleSync.Acceptance.Support;
using Reqnroll;

namespace CycleSync.Acceptance.Steps;

[Binding]
public sealed class CostSteps(ScenarioWorld world)
{
    // --- profile preconditions -------------------------------------------------------------

    [Given("the user's home location is {string}")]
    public async Task GivenTheUsersHomeLocationIs(string home)
    {
        world.PendingHomeLocation = home;
        await PutProfile();
    }

    [Given("the user's preferred currency is {string}")]
    public async Task GivenTheUsersPreferredCurrencyIs(string currency)
    {
        world.PendingCurrency = currency;
        await PutProfile();
    }

    [Given("another signed-in user {string} whose home location is {string}")]
    public async Task GivenAnotherSignedInUserWithHome(string email, string home)
    {
        await world.SignInAsync(email);
        await world.PutJsonAsync("/api/me/profile", new
        {
            homeLocation = home,
            preferredCurrency = (string?)null,
            preferredLanguage = (string?)null,
        });
    }

    // --- viewing an estimate ---------------------------------------------------------------

    [When("the user views the cost estimate for {string}")]
    public async Task WhenTheUserViewsTheCostEstimateFor(string name)
    {
        var offCycleId = await TryOffCycleIdAsync(name);
        if (offCycleId is Guid id)
        {
            await world.GetAsync($"/api/off-cycles/{id}/cost-estimate");
        }
        else
        {
            await world.GetAsync($"/api/locations/{await LocationIdAsync(name)}/cost-estimate");
        }
    }

    [Then("an estimate is shown for flights, accommodation and daily expenses")]
    public void ThenAnEstimateIsShown()
    {
        Assert.Equal(HttpStatusCode.OK, world.LastResponse!.StatusCode);
        var estimate = world.LastJsonClone();
        Assert.True(estimate.GetProperty("flights").GetDecimal() > 0);
        Assert.True(estimate.GetProperty("accommodation").GetDecimal() > 0);
        Assert.True(estimate.GetProperty("dailyExpenses").GetDecimal() > 0);
    }

    [Then("the estimate is expressed in {string}")]
    [Then("the estimate is expressed in the user's currency {string}")]
    public void ThenTheEstimateIsExpressedIn(string currency) =>
        Assert.Equal(currency, world.LastJsonClone().GetProperty("currency").GetString());

    [Then("the estimate shows a confidence level")]
    public void ThenTheEstimateShowsAConfidenceLevel() =>
        Assert.Contains(world.LastJsonClone().GetProperty("confidence").GetString(), new[] { "Low", "Medium", "High" });

    [Then("the estimate shows when it was generated")]
    public void ThenTheEstimateShowsWhenItWasGenerated() =>
        Assert.True(world.LastJsonClone().GetProperty("generatedAt").TryGetDateTimeOffset(out _));

    [Then("accommodation and daily expenses are calculated for {int} nights")]
    public void ThenAccommodationAndDailyExpensesAreCalculatedForNights(int nights)
    {
        var estimate = world.LastJsonClone();
        Assert.Equal(nights, estimate.GetProperty("nights").GetInt32());
        Assert.True(estimate.GetProperty("accommodation").GetDecimal() > 0);
        Assert.True(estimate.GetProperty("dailyExpenses").GetDecimal() > 0);
    }

    [Then("the flight estimate for {string} differs from that for {string}")]
    public async Task ThenTheFlightEstimateDiffers(string emailA, string emailB)
    {
        var id = (await TryOffCycleIdAsync(world.LastCreated!.Value.Name))!.Value;
        var flightsA = await FlightEstimateAsAsync(emailA, id);
        var flightsB = await FlightEstimateAsAsync(emailB, id);
        Assert.NotEqual(flightsA, flightsB);
    }

    // --- helpers ---------------------------------------------------------------------------

    private async Task<decimal> FlightEstimateAsAsync(string email, Guid offCycleId)
    {
        world.SetCurrent(email);
        await world.GetAsync($"/api/off-cycles/{offCycleId}/cost-estimate");
        Assert.Equal(HttpStatusCode.OK, world.LastResponse!.StatusCode);
        return world.LastJsonClone().GetProperty("flights").GetDecimal();
    }

    private async Task PutProfile() =>
        await world.PutJsonAsync("/api/me/profile", new
        {
            homeLocation = world.PendingHomeLocation,
            preferredCurrency = world.PendingCurrency,
            preferredLanguage = world.PendingLanguage,
        });

    private async Task<Guid?> TryOffCycleIdAsync(string name)
    {
        await world.GetAsync("/api/off-cycles");
        var match = world.LastJsonClone().EnumerateArray()
            .FirstOrDefault(o => o.GetProperty("name").GetString() == name);
        return match.ValueKind == JsonValueKind.Object ? match.GetProperty("id").GetGuid() : null;
    }

    private async Task<Guid> LocationIdAsync(string name)
    {
        await world.GetAsync("/api/locations");
        return world.LastJsonClone().EnumerateArray()
            .First(l => l.GetProperty("name").GetString() == name)
            .GetProperty("id").GetGuid();
    }
}
