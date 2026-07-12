using Hangfire;
using Serilog;
using WaslX.Api;
using WaslX.Api.Extensions;
using WaslX.Api.Hubs;
using WaslX.Application;
using WaslX.Infrastructure;
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

app.UseHangfireDashboard(app.Configuration["Hangfire:DashboardPath"] ?? "/hangfire");

app.MapControllers();

// Real-time shared inbox (new messages, receipts, notes, lifecycle changes).
app.MapHub<InboxHub>("/hubs/inbox");

app.Run();
