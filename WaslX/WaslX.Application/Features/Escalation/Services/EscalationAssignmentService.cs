using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Abstractions.Screening;
using WaslX.Application.Features.Escalation.Screening;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;

namespace WaslX.Application.Features.Escalation.Services
{
    public class EscalationAssignmentService(
        IUnitOfWork unitOfWork,
        IInboxRealtimeNotifier realtimeNotifier,
        ILogger<EscalationAssignmentService> logger) : IEscalationAssignmentService
    {
        public async Task<Result<EscalationRecommendation>> GetRecommendationAsync(int tenantId, int conversationId, CancellationToken cancellationToken = default)
        {
            var escalation = await LoadEscalationByConversationAsync(conversationId, cancellationToken);
            if (escalation is null)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotFound);

            if (escalation.TenantId != tenantId)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotFound);

            var result = BuildRecommendation(escalation, escalation.Conversation, escalation.SuggestedAssignee?.Name, null);
            return Result.Success(result);
        }

        public async Task<Result<EscalationRecommendation>> ConfirmAsync(
            int tenantId, int actorUserId, int escalationId, int assigneeId, CancellationToken cancellationToken = default)
        {
            var escalation = await LoadEscalationAsync(escalationId, cancellationToken);
            if (escalation is null)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotFound);

            if (escalation.TenantId != tenantId)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotFound);

            if (escalation.Status != EscalationStatus.Recommended)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotRecommended);

            var conversationRepo = unitOfWork.GetRepository<Conversation, int>();
            var conversation = await conversationRepo.GetByIdAsync(escalation.ConversationId, cancellationToken);
            if (conversation is null)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotFound);

            var previousOwnerId = conversation.AssignedUserId;

            conversation.AssignedUserId = assigneeId;
            conversationRepo.Update(conversation);

            escalation.Status = EscalationStatus.Assigned;
            escalation.ConfirmedByUserId = actorUserId;
            escalation.ConfirmedAtUtc = DateTime.UtcNow;
            escalation.AssignedToId = assigneeId;
            escalation.AssignedAtUtc = DateTime.UtcNow;
            escalation.ModeAtDecision = EscalationMode.Recommend;
            unitOfWork.GetRepository<Domain.Entities.Escalation, int>().Update(escalation);

            var userRepo = unitOfWork.GetRepository<User, int>();
            var assignee = await userRepo.GetByIdAsync(assigneeId, cancellationToken);

            await WriteAuditLogAsync(tenantId, actorUserId, escalation.Id, previousOwnerId, assigneeId, "Confirm", "recommend", null);

            await unitOfWork.CompleteAsync();

            var result = BuildRecommendation(escalation, conversation, assignee?.Name, previousOwnerId);

            await realtimeNotifier.EscalationAssignmentConfirmedAsync(tenantId, result, cancellationToken);
            await realtimeNotifier.ConversationOwnershipTransferredAsync(tenantId, new OwnershipTransferredPayload(
                conversation.Id, previousOwnerId, assigneeId, "Confirm", DateTime.UtcNow, escalation.AssignedAtUtc), cancellationToken);

            logger.LogInformation("Escalation {EscalationId} confirmed by user {UserId}, assigned to {AssigneeId}", escalationId, actorUserId, assigneeId);

            return Result.Success(result);
        }

        public async Task<Result<EscalationRecommendation>> OverrideAsync(
            int tenantId, int actorUserId, int escalationId, int assigneeId, string reason, CancellationToken cancellationToken = default)
        {
            var escalation = await LoadEscalationAsync(escalationId, cancellationToken);
            if (escalation is null)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotFound);

            if (escalation.TenantId != tenantId)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotFound);

            if (escalation.Status != EscalationStatus.Recommended)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotRecommended);

            var conversationRepo = unitOfWork.GetRepository<Conversation, int>();
            var conversation = await conversationRepo.GetByIdAsync(escalation.ConversationId, cancellationToken);
            if (conversation is null)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotFound);

            var previousOwnerId = conversation.AssignedUserId;

            conversation.AssignedUserId = assigneeId;
            conversationRepo.Update(conversation);

            escalation.Status = EscalationStatus.Assigned;
            escalation.ConfirmedByUserId = actorUserId;
            escalation.ConfirmedAtUtc = DateTime.UtcNow;
            escalation.AssignedToId = assigneeId;
            escalation.AssignedAtUtc = DateTime.UtcNow;
            escalation.OverrideReason = reason;
            escalation.ModeAtDecision = EscalationMode.Recommend;
            unitOfWork.GetRepository<Domain.Entities.Escalation, int>().Update(escalation);

            var userRepo = unitOfWork.GetRepository<User, int>();
            var assignee = await userRepo.GetByIdAsync(assigneeId, cancellationToken);

            await WriteAuditLogAsync(tenantId, actorUserId, escalation.Id, previousOwnerId, assigneeId, "Override", "recommend", reason);

            await unitOfWork.CompleteAsync();

            var result = BuildRecommendation(escalation, conversation, assignee?.Name, previousOwnerId);

            await realtimeNotifier.EscalationOverrideAppliedAsync(tenantId, result, cancellationToken);
            await realtimeNotifier.ConversationOwnershipTransferredAsync(tenantId, new OwnershipTransferredPayload(
                conversation.Id, previousOwnerId, assigneeId, "Override", DateTime.UtcNow, escalation.AssignedAtUtc), cancellationToken);

            logger.LogInformation("Escalation {EscalationId} overridden by user {UserId}, assigned to {AssigneeId}, reason: {Reason}", escalationId, actorUserId, assigneeId, reason);

            return Result.Success(result);
        }

        public async Task<Result<EscalationRecommendation>> HandleScoringResultAsync(
            int tenantId, int escalationId, CancellationToken cancellationToken = default)
        {
            var escalation = await LoadEscalationAsync(escalationId, cancellationToken);
            if (escalation is null)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotFound);

            if (escalation.TenantId != tenantId)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotFound);

            if (escalation.SuggestedAssigneeId is null)
            {
                escalation.Status = EscalationStatus.Open;
                unitOfWork.GetRepository<Domain.Entities.Escalation, int>().Update(escalation);
                await unitOfWork.CompleteAsync();
                return Result.Failure<EscalationRecommendation>(AppErrors.NoTarget);
            }

            var settingsRepo = unitOfWork.GetRepository<TenantEscalationSettings, int>();
            var settings = await settingsRepo.GetWithSpecAsync(new TenantEscalationSettingsSpec(tenantId));
            var mode = settings?.Mode ?? EscalationMode.Recommend;

            var conversationRepo = unitOfWork.GetRepository<Conversation, int>();
            var conversation = await conversationRepo.GetByIdAsync(escalation.ConversationId, cancellationToken);
            if (conversation is null)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotFound);

            var previousOwnerId = conversation.AssignedUserId;
            escalation.ModeAtDecision = mode;

            if (mode == EscalationMode.Recommend)
            {
                escalation.Status = EscalationStatus.Recommended;
                unitOfWork.GetRepository<Domain.Entities.Escalation, int>().Update(escalation);
                await unitOfWork.CompleteAsync();

                var result = BuildRecommendation(escalation, conversation, null, previousOwnerId);

                logger.LogInformation("Escalation {EscalationId} set to recommended status, awaiting Manager/Admin confirm", escalationId);

                return Result.Success(result);
            }
            else
            {
                var assigneeId = escalation.SuggestedAssigneeId.Value;

                conversation.AssignedUserId = assigneeId;
                conversationRepo.Update(conversation);

                escalation.Status = EscalationStatus.Assigned;
                escalation.AssignedToId = assigneeId;
                escalation.AssignedAtUtc = DateTime.UtcNow;
                unitOfWork.GetRepository<Domain.Entities.Escalation, int>().Update(escalation);

                var userRepo = unitOfWork.GetRepository<User, int>();
                var assignee = await userRepo.GetByIdAsync(assigneeId, cancellationToken);

                await WriteAuditLogAsync(tenantId, null, escalation.Id, previousOwnerId, assigneeId, "AutoAssign", "autoAssign", null);

                await unitOfWork.CompleteAsync();

                var result = BuildRecommendation(escalation, conversation, assignee?.Name, previousOwnerId);

                await realtimeNotifier.EscalationAutoAssignedAsync(tenantId, result, cancellationToken);
                await realtimeNotifier.ConversationOwnershipTransferredAsync(tenantId, new OwnershipTransferredPayload(
                    conversation.Id, previousOwnerId, assigneeId, "AutoAssign", DateTime.UtcNow, escalation.AssignedAtUtc), cancellationToken);

                logger.LogInformation("Escalation {EscalationId} auto-assigned to user {AssigneeId}", escalationId, assigneeId);

                return Result.Success(result);
            }
        }

        private async Task<Domain.Entities.Escalation?> LoadEscalationAsync(int escalationId, CancellationToken cancellationToken)
        {
            var repo = unitOfWork.GetRepository<Domain.Entities.Escalation, int>();
            return await repo.GetWithSpecAsync(new EscalationWithIncludesSpec(escalationId));
        }

        private async Task<Domain.Entities.Escalation?> LoadEscalationByConversationAsync(int conversationId, CancellationToken cancellationToken)
        {
            var repo = unitOfWork.GetRepository<Domain.Entities.Escalation, int>();
            return await repo.GetWithSpecAsync(new EscalationByConversationSpec(conversationId));
        }

        private async Task WriteAuditLogAsync(int tenantId, int? actorUserId, int escalationId, int? previousOwnerId, int newOwnerId, string transitionType, string mode, string? overrideReason)
        {
            var auditRepo = unitOfWork.GetRepository<AuditLog, int>();
            var details = System.Text.Json.JsonSerializer.Serialize(new
            {
                TransitionType = transitionType,
                Mode = mode,
                PreviousOwnerId = previousOwnerId,
                NewOwnerId = newOwnerId,
                OverrideReason = overrideReason
            });

            await auditRepo.AddAsync(new AuditLog
            {
                TenantId = tenantId,
                ActorUserId = actorUserId,
                Action = AuditAction.Updated,
                EntityType = "Escalation",
                EntityId = escalationId,
                Details = details
            });
        }

        private static EscalationRecommendation BuildRecommendation(
            Domain.Entities.Escalation escalation, Conversation conversation, string? assigneeName, int? previousOwnerId)
        {
            return new EscalationRecommendation
            {
                EscalationId = escalation.Id,
                ConversationId = escalation.ConversationId,
                SuggestedAssigneeId = escalation.SuggestedAssigneeId,
                SuggestedAssigneeName = escalation.SuggestedAssignee?.Name ?? string.Empty,
                Reason = escalation.SuggestedReason,
                Mode = (escalation.ModeAtDecision ?? EscalationMode.Recommend).ToString(),
                Status = escalation.Status.ToString(),
                AssignedToId = escalation.AssignedToId ?? conversation.AssignedUserId,
                AssignedToName = assigneeName,
                PreviousOwnerId = previousOwnerId,
                PreviousOwnerName = null,
                OverrideReason = escalation.OverrideReason,
                OwnershipTransferredAtUtc = escalation.AssignedAtUtc,
                ConfirmedAtUtc = escalation.ConfirmedAtUtc,
                AssignedAtUtc = escalation.AssignedAtUtc,
                CreatedAtUtc = escalation.CreatedAt
            };
        }

        private class TenantEscalationSettingsSpec(int tenantId)
            : Domain.Specifications.Specification<TenantEscalationSettings, int>(s => s.TenantId == tenantId);

#pragma warning disable CS8603 // Possible null reference return in EF navigation include expression trees.
        private class EscalationWithIncludesSpec : Domain.Specifications.Specification<Domain.Entities.Escalation, int>
        {
            public EscalationWithIncludesSpec(int escalationId) : base(e => e.Id == escalationId)
            {
                AddInclude(e => e.Conversation);
                AddInclude(e => e.SuggestedAssignee!);
            }
        }

        private class EscalationByConversationSpec : Domain.Specifications.Specification<Domain.Entities.Escalation, int>
        {
            public EscalationByConversationSpec(int conversationId)
                : base(e => e.ConversationId == conversationId)
            {
                AddInclude(e => e.Conversation);
                AddInclude(e => e.SuggestedAssignee!);
            }
        }
#pragma warning restore CS8603
    }
}
