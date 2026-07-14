namespace CycleSync.Domain.Users;

/// <summary>
/// A CycleSync user. All users have equal rights (no roles/admins) per the design principles.
/// Provisioned on first Google sign-in and enriched via profile management.
/// </summary>
public sealed class User
{
    private readonly List<Passport> _passports = [];

    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string DisplayName { get; private set; }
    public GeoPlace? HomeLocation { get; private set; }
    public string? PreferredCurrency { get; private set; }
    public string? PreferredLanguage { get; private set; }
    public IReadOnlyCollection<Passport> Passports => _passports;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private User()
    {
        // EF Core
        Email = string.Empty;
        DisplayName = string.Empty;
    }

    public static User Provision(string email, string displayName, TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        var now = clock.GetUtcNow();
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? email.Trim() : displayName.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void UpdateProfile(GeoPlace? homeLocation, string? preferredCurrency, string? preferredLanguage, TimeProvider clock)
    {
        HomeLocation = homeLocation;
        PreferredCurrency = string.IsNullOrWhiteSpace(preferredCurrency) ? null : preferredCurrency.Trim();
        PreferredLanguage = string.IsNullOrWhiteSpace(preferredLanguage) ? null : preferredLanguage.Trim();
        UpdatedAt = clock.GetUtcNow();
    }

    /// <summary>Adds a passport if not already held. Returns true if it was added.</summary>
    public bool AddPassport(string country, TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        var normalized = country.Trim();
        if (_passports.Any(p => string.Equals(p.Country, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        _passports.Add(new Passport(normalized));
        UpdatedAt = clock.GetUtcNow();
        return true;
    }

    /// <summary>Removes a passport if held. Returns true if one was removed.</summary>
    public bool RemovePassport(string country, TimeProvider clock)
    {
        var removed = _passports.RemoveAll(p => string.Equals(p.Country, country, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            UpdatedAt = clock.GetUtcNow();
            return true;
        }

        return false;
    }
}
