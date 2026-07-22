using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.Screening;
using WaslX.Application.Features.Escalation.Screening;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;

namespace WaslX.Application.Features.Escalation.Services
{
    public class EscalationModeService(
        IUnitOfWork unitOfWork,
        ILogger<EscalationModeService> logger) : IEscalationModeService
    {
        public async Task<Result<EscalationModeSettings>> GetSettingsAsync(int tenantId, CancellationToken cancellationToken = default)
        {
            var settingsRepo = unitOfWork.GetRepository<TenantEscalationSettings, int>();
            var settings = await settingsRepo.GetWithSpecAsync(new TenantEscalationSettingsSpec(tenantId));

            var mode = settings?.Mode.ToString() ?? "Recommend";
            return Result.Success(new EscalationModeSettings(mode));
        }

        public async Task<Result<EscalationModeSettings>> UpdateSettingsAsync(int tenantId, int actorUserId, string mode, CancellationToken cancellationToken = default)
        {
            if (!Enum.TryParse<EscalationMode>(mode, true, out var parsedMode))
            {
                return Result.Failure<EscalationModeSettings>(AppErrors.InvalidMode);
            }

            var settingsRepo = unitOfWork.GetRepository<TenantEscalationSettings, int>();
            var settings = await settingsRepo.GetWithSpecAsync(new TenantEscalationSettingsSpec(tenantId));

            if (settings is null)
            {
                settings = new TenantEscalationSettings
                {
                    TenantId = tenantId,
                    Mode = parsedMode,
                    UpdatedByUserId = actorUserId,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                await settingsRepo.AddAsync(settings, cancellationToken);
            }
            else
            {
                settings.Mode = parsedMode;
                settings.UpdatedByUserId = actorUserId;
                settings.UpdatedAtUtc = DateTime.UtcNow;
                settingsRepo.Update(settings);
            }

            await unitOfWork.CompleteAsync();

            logger.LogInformation("Tenant {TenantId} escalation mode changed to {Mode} by user {UserId}", tenantId, mode, actorUserId);

            return Result.Success(new EscalationModeSettings(parsedMode.ToString()));
        }

        private class TenantEscalationSettingsSpec(int tenantId)
            : Domain.Specifications.Specification<TenantEscalationSettings, int>(s => s.TenantId == tenantId);
    }

    internal static class AppErrors
    {
        internal static readonly Error InvalidMode = new Error("Escalation.InvalidMode", "Invalid escalation mode. Use 'Recommend' or 'AutoAssign'.", 400);
        internal static readonly Error NotFound = new Error("Escalation.NotFound", "Escalation not found.", 404);
        internal static readonly Error Forbidden = new Error("Escalation.Forbidden", "You do not have permission to perform this action.", 403);
        internal static readonly Error AlreadyAssigned = new Error("Escalation.AlreadyAssigned", "Escalation is already assigned.", 400);
        internal static readonly Error NoTarget = new Error("Escalation.NoTarget", "No eligible agent target available.", 400);
        internal static readonly Error NotRecommended = new Error("Escalation.NotRecommended", "Escalation is not in recommended status.", 400);
        internal static readonly Error AlreadyCancelled = new Error("Escalation.AlreadyCancelled", "Escalation has already been cancelled.", 400);
        internal static readonly Error CannotReject = new Error("Escalation.CannotReject", "Escalation cannot be rejected in its current status.", 400);
    }
}
