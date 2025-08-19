using System;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// EF Core DbContext (SQL Server)
builder.Services.AddDbContext<ApplicationWriteDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

// Health checks
var hc = builder.Services.AddHealthChecks()

    // custom-sql: elle SQL sorgusu
    .AddCheck("custom-sql",
        new CustomSqlHealthCheck(builder.Configuration.GetConnectionString("SqlServer")!, "SELECT 1"),
        tags: new[] { "ready", "db" })

    // sqlserver: paketle gelen bağlantı kontrolü
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("SqlServer")!,
        name: "sqlserver",
        healthQuery: "SELECT 1",
        tags: new[] { "ready", "db" })

    // redis: Redis ping
    .AddRedis(
        redisConnectionString: builder.Configuration.GetConnectionString("Redis")!,
        name: "redis",
        timeout: TimeSpan.FromSeconds(3),
        tags: new[] { "ready", "cache" })

    // ApplicationWriteDbContext: EF Core context kontrolü
    .AddDbContextCheck<ApplicationWriteDbContext>(
        name: "ApplicationWriteDbContext",
        tags: new[] { "ready", "db" })

    // liveness
    .AddCheck("self", () => HealthCheckResult.Healthy("Uygulama çalışıyor"), tags: new[] { "live" });

// UI
builder.Services.AddHealthChecksUI(setup =>
{
    setup.AddHealthCheckEndpoint("All details", "/health/details");
    setup.SetEvaluationTimeInSeconds(15);
    setup.MaximumHistoryEntriesPerEndpoint(60);
}).AddInMemoryStorage();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapGet("/", () => Results.Redirect("/health-ui"));

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

// ↓↓↓ Türkçe JSON için özel writer
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready"),
    ResponseWriter = TurkishHealthResponseWriter.WriteAsync
});

app.MapHealthChecks("/health/details", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = TurkishHealthResponseWriter.WriteAsync
});
// ↑↑↑

app.MapHealthChecksUI(o =>
{
    o.UIPath = "/health-ui";
    o.ApiPath = "/health-ui-api";
});

app.Run();
