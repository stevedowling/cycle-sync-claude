using CycleSync.Infrastructure;
using CycleSync.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddCycleSyncDatabase(builder.Configuration);
builder.Services.AddHostedService<MigrationWorker>();

var host = builder.Build();
host.Run();
