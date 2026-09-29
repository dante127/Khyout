using Khyout.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Phase 3 replaces these stubs with real implementations:
// /health/ready will probe the database and background workers.
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

app.Run();

/// <summary>Exposed for integration-test hosting (WebApplicationFactory) in Phase 3.</summary>
public partial class Program { }
