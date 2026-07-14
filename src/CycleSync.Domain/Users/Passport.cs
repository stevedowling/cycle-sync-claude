namespace CycleSync.Domain.Users;

/// <summary>
/// A passport a user holds, identified by the issuing country. Drives visa guidance later.
/// Stored as free text (country name or code) so the UI can present what the user entered.
/// </summary>
public sealed class Passport
{
    public string Country { get; private set; }

    private Passport()
    {
        // EF Core
        Country = string.Empty;
    }

    public Passport(string country)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("Passport country is required.", nameof(country));
        }

        Country = country.Trim();
    }
}
