using System.Text;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using WaslX.Api.Authorization;
using WaslX.Application.Abstractions.Permissions;
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
                });

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
