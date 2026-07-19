using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using WaslX.Application.Abstractions.Behaviors;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Abstractions.AutoEscalation;
using WaslX.Application.Abstractions.Performance;
using WaslX.Application.Abstractions.Screening;
using WaslX.Application.Features.Classification;
using WaslX.Application.Features.Escalation.Services;
using WaslX.Application.Features.Escalation.Providers;

namespace WaslX.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        var mapsterConfig = TypeAdapterConfig.GlobalSettings;
        mapsterConfig.Scan(assembly);
        services.AddSingleton(mapsterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddScoped<IClassificationOrchestrator, ClassificationOrchestrator>();
        services.AddScoped<IEscalationTargetScoringService, EscalationTargetScoringService>();
        services.AddScoped<IAgentPerformanceProvider, DefaultAgentPerformanceProvider>();
        services.AddScoped<IEscalationModeService, EscalationModeService>();
        services.AddScoped<IEscalationAssignmentService, EscalationAssignmentService>();
        services.AddScoped<IConversationEscalationService, ConversationEscalationService>();
        services.AddScoped<IAgentPerformanceUpdateService, AgentPerformanceUpdateService>();

        return services;
    }
}
