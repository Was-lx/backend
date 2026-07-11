using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WaslX.Application.Abstractions.Authentication;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Media;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Infrastructure.Authentication;
using WaslX.Infrastructure.Email;
using WaslX.Infrastructure.Identity;
using WaslX.Infrastructure.Media;
using WaslX.Infrastructure.Settings;
using WaslX.Infrastructure.WhatsApp;

namespace WaslX.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
            services.Configure<MailSettings>(configuration.GetSection(MailSettings.SectionName));
            services.Configure<HangfireSettings>(configuration.GetSection(HangfireSettings.SectionName));
            services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));
            services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));
            services.Configure<CloudinarySettings>(configuration.GetSection(CloudinarySettings.SectionName));

            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IAuthService, AuthSerive>();

            services.AddScoped<IEmailSender, EmailService>();

            // Meta WhatsApp Cloud API client (typed HttpClient via IHttpClientFactory).
            services.AddHttpClient<IMetaGraphApiService, MetaGraphApiService>();

            services.AddScoped<IMediaStorageService, CloudinaryMediaStorageService>();

            return services;
        }
    }
}
