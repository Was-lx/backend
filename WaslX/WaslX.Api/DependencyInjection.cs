using System.Text;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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

            services.AddAuthorization();

            services.AddCors(options =>
                options.AddPolicy(CorsPolicy, policy =>
                    policy.WithOrigins(configuration["App:FrontendBaseUrl"] ?? "http://localhost:4200")
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
