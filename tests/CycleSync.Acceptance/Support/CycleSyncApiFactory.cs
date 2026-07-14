using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CycleSync.Acceptance.Support;

/// <summary>
/// Boots the real CycleSync API in-process for API-level acceptance scenarios.
/// Runs in the Development environment so the ServiceDefaults health endpoints are mapped.
/// </summary>
public sealed class CycleSyncApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}
