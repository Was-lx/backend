using Hangfire;
using Hangfire.Dashboard;
using Microsoft.Extensions.Options;
using Serilog;
using WaslX.Api;
using WaslX.Application.Abstractions.Maintenance;
using WaslX.Application.Abstractions.Rag;
using WaslX.Api.Authorization;
using WaslX.Api.Extensions;
using WaslX.Api.Hubs;
using WaslX.Application;
using WaslX.Infrastructure;
using WaslX.Infrastructure.Settings;
using WaslX.Persistance;

var builder = WebApplication.CreateBuilder(args);

// Serilog reads its sinks/levels from the "Serilog" section in appsettings.json.
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Register each layer's services.
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPersistence(builder.Configuration)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

// Create databases (if missing) and apply migrations before the request
// pipeline and the Hangfire server start.
await app.InitializeDatabasesAsync();

// Idempotent seed: subscription plans + a ready demo workspace/Admin, so a
// fresh database is immediately usable (sign-up needs plans to exist first).
await app.SeedDemoDataAsync();

// RAG: idempotent Qdrant collection bootstrap. Logged, not fatal — the app still starts if the
// vector store is unreachable at boot (e.g. Qdrant not running locally yet); RAG features simply
// fail per-request via the Result pattern until it's back.
using (var ragScope = app.Services.CreateScope())
{
    var vectorStore = ragScope.ServiceProvider.GetRequiredService<IVectorStore>();
    var ensureResult = await vectorStore.EnsureCollectionAsync();
    if (ensureResult.IsFailure)
        Log.Warning("Qdrant collection bootstrap failed at startup: {Error}", ensureResult.Error.Code);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Dev-only API docs — exempt from the global authenticated-user fallback policy.
    app.MapOpenApi().AllowAnonymous();
}

app.UseSerilogRequestLogging();

// In Development we serve plain HTTP too (no redirect) so the SPA/preview can call
// the API without a trusted dev certificate. Production still forces HTTPS.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors(WaslX.Api.DependencyInjection.CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Cross-tenant job data (phone numbers, tenant/conversation ids) lives behind this dashboard, so it
// must never be reachable without auth — gated by Basic Auth since it's a plain page load, not an
// XHR call the SPA's JWT interceptor can attach to.
var hangfireSettings = app.Services.GetRequiredService<IOptions<HangfireSettings>>();
app.UseHangfireDashboard(app.Configuration["Hangfire:DashboardPath"] ?? "/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter(hangfireSettings)]
});

// Recurring distribution-engine maintenance. Resolved from DI per run (IMaintenanceJobs is scoped).
// Auto-resolve stale conversations every 30 minutes; reassign work off offline agents every 10 minutes.
RecurringJob.AddOrUpdate<IMaintenanceJobs>(
    "auto-resolve-stale-conversations",
    job => job.AutoResolveAsync(CancellationToken.None),
    "*/30 * * * *");

RecurringJob.AddOrUpdate<IMaintenanceJobs>(
    "reassign-offline-agents",
    job => job.ReassignOfflineAgentsAsync(CancellationToken.None),
    "*/10 * * * *");

app.MapControllers();

// Real-time shared inbox (new messages, receipts, notes, lifecycle changes).
app.MapHub<InboxHub>("/hubs/inbox");

app.Run();
