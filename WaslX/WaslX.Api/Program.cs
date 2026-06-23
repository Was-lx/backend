using Hangfire;
using Serilog;
using WaslX.Api;
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors(WaslX.Api.DependencyInjection.CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard(app.Configuration["Hangfire:DashboardPath"] ?? "/hangfire");

app.MapControllers();

app.Run();
