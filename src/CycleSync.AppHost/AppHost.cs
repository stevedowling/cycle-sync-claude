var builder = DistributedApplication.CreateBuilder(args);

// SQL Server with a persistent data volume and a 'cyclesync' database.
// (Requires a container runtime, e.g. Docker, to run locally.)
var sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume();

var database = sqlServer.AddDatabase("cyclesync");

// Applies EF Core migrations on startup, then completes.
var migrations = builder.AddProject<Projects.CycleSync_MigrationService>("migrations")
    .WithReference(database)
    .WaitFor(database);

// The ASP.NET Core API. Waits for migrations to finish before serving.
var api = builder.AddProject<Projects.CycleSync_Api>("api")
    .WithReference(database)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

// The React (Vite) single-page app. In development its dev server proxies /api to the API; the
// proxy target is read from SERVER_HTTP (see src/web/vite.config.ts), so hand it the API's address.
// http (not https) keeps the Node proxy from having to trust the ASP.NET dev certificate.
var web = builder.AddViteApp("web", "../web")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("SERVER_HTTP", api.GetEndpoint("http"));

// When published, the built SPA is copied into the API's wwwroot and served same-origin.
api.PublishWithContainerFiles(web, "wwwroot");

builder.Build().Run();
