namespace CycleSync.Api.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Email domains permitted to sign in. Empty means "no restriction" (dev only).</summary>
    public string[] AllowedDomains { get; set; } = [];

    public GoogleOptions Google { get; set; } = new();

    public JwtOptions Jwt { get; set; } = new();

    public sealed class GoogleOptions
    {
        public string? ClientId { get; set; }
    }

    public sealed class JwtOptions
    {
        public string SigningKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = "cyclesync";
        public string Audience { get; set; } = "cyclesync";
        public int LifetimeMinutes { get; set; } = 480;
    }
}
