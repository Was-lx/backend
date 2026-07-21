using System.Text;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using WaslX.Api.Authorization;
using WaslX.Api.HealthChecks;
using WaslX.Api.Realtime;
using WaslX.Application.Abstractions.Permissions;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Infrastructure.Settings;

namespace WaslX.Api
{
    public static class DependencyInjection
    {
        public const string CorsPolicy = "AngularApp";

        public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Accept/return enums as their string names (e.g. window Status "Open") to match the SPA.
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            services.AddOpenApi();

            services.AddHttpContextAccessor();

            // Data Protection backs the encrypted-at-rest platform credential store (US-6.6).
            services.AddDataProtection();

            // System health monitoring surfaced to the Platform Owner (US-6.10a). Each check is
            // lightweight and never throws; the SuperAdmin health endpoint reports each component's status.
            services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>("database", tags: ["system"])
                .AddCheck<HangfireHealthCheck>("hangfire", tags: ["system"])
                .AddCheck<AiProviderConfigHealthCheck>("ai-provider", tags: ["config"])
                .AddCheck<WhatsAppConfigHealthCheck>("whatsapp", tags: ["config"]);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    // SignalR (WebSockets) can't send an Authorization header, so the JWT arrives
                    // as an "access_token" query parameter on the hub path — hand it to the handler.
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                                context.Token = accessToken;
                            return Task.CompletedTask;
                        }
                    };
                });

            // camelCase payloads so SignalR messages match the REST API's JSON shape on the client.
            services.AddSignalR().AddJsonProtocol(options =>
                options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
            services.AddScoped<IInboxRealtimeNotifier, InboxRealtimeNotifier>();

            // Fine-grained permission enforcement: [HasPermission("code")] resolves to a
            // "perm:code" policy checked server-side against the per-tenant RBAC matrix.
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

            services.AddAuthorization(options =>
            {
                // Defense in depth: any endpoint without explicit auth metadata still
                // requires an authenticated user ([AllowAnonymous] opts out where needed).
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            var frontendUrl = configuration["App:FrontendBaseUrl"] ?? "http://localhost:4300";
            services.AddCors(options =>
                options.AddPolicy(CorsPolicy, policy =>
                    policy.SetIsOriginAllowed(origin =>
                              string.Equals(origin, frontendUrl, StringComparison.OrdinalIgnoreCase) ||
                              (Uri.TryCreate(origin, UriKind.Absolute, out var u) && u.Host == "localhost"))
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials()));

            // Hangfire uses its own dedicated database, separate from the app's data.
            var connectionString = configuration.GetConnectionString("HangfireConnection");
            var hangfire = configuration.GetSection(HangfireSettings.SectionName).Get<HangfireSettings>()!;

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString));

            services.AddHangfireServer(options => options.WorkerCount = hangfire.WorkerCount);

            return services;
        }
    }
}
