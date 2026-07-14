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

// The React (Vite) single-page app. Proxies /api to the API in development.
var web = builder.AddViteApp("web", "../web")
    .WithReference(api)
    .WaitFor(api);

// When published, the built SPA is copied into the API's wwwroot and served same-origin.
api.PublishWithContainerFiles(web, "wwwroot");

builder.Build().Run();
