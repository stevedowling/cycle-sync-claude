using CycleSync.Domain.Users;

namespace CycleSync.Api.Features.Users;

public sealed record SignInRequest(string IdToken);

public sealed record SignInResponse(string Token, UserSummary User);

public sealed record UserSummary(Guid Id, string Email, string DisplayName);

public sealed record ProfileResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? HomeLocation,
    string? PreferredCurrency,
    string? PreferredLanguage,
    IReadOnlyList<string> Passports);

public sealed record UpdateProfileRequest(string? HomeLocation, string? PreferredCurrency, string? PreferredLanguage);

public sealed record AddPassportRequest(string Country);

public static class UserMapping
{
    public static ProfileResponse ToProfile(this User user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.HomeLocation?.Name,
        user.PreferredCurrency,
        user.PreferredLanguage,
        user.Passports.Select(p => p.Country).ToArray());

    public static UserSummary ToSummary(this User user) => new(user.Id, user.Email, user.DisplayName);
}
