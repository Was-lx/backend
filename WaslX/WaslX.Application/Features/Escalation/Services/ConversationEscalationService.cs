using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.AutoEscalation;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Features.Escalation.Models;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;

namespace WaslX.Application.Features.Escalation.Services;

public sealed class ConversationEscalationService(
    IUnitOfWork unitOfWork,
    IInboxRealtimeNotifier notifier,
    ILogger<ConversationEscalationService> logger) : IConversationEscalationService
{
    public async Task<Result<EscalationResult>> EscalateAsync(
        EscalationInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            var escalationRepo = unitOfWork.GetRepository<Domain.Entities.Escalation, int>();
            var conversationRepo = unitOfWork.GetRepository<Conversation, int>();

            var conversation = await conversationRepo.GetByIdAsync(input.ConversationId, cancellationToken);
            if (conversation == null || conversation.TenantId != input.TenantId)
            {
                return Result.Failure<EscalationResult>(new Error(
                    "Escalation.ConversationNotFound", "Conversation not found in tenant.", 404));
            }

            if (conversation.IsEscalated)
            {
                return Result.Success(new EscalationResult
                {
                    ConversationId = input.ConversationId,
                    Created = false,
                    AlreadyEscalated = true,
                    Status = "open"
                });
            }

            var escalation = new Domain.Entities.Escalation
            {
                TenantId = input.TenantId,
                ConversationId = input.ConversationId,
                MessageClassificationId = input.ClassificationId,
                MessageId = input.MessageId,
                Status = EscalationStatus.Open,
                Priority = input.Priority,
                Sentiment = input.Sentiment,
                EscalationReason = input.Reason,
                CreatedBySystem = true
            };

            await escalationRepo.AddAsync(escalation, cancellationToken);

            conversation.IsEscalated = true;
            conversation.EscalatedAtUtc = DateTime.UtcNow;
            conversation.EscalationReason = input.Reason;

            await unitOfWork.CompleteAsync();

            var payload = new ConversationEscalatedPayload(
                escalation.Id,
                input.ConversationId,
                input.TenantId,
                input.Reason,
                input.Priority,
                input.Sentiment,
                "open",
                DateTime.UtcNow);

            await NotifyManagersAdminsAsync(input.TenantId, payload, cancellationToken);

            await WriteAuditAsync(input.TenantId, input.ConversationId, escalation.Id, input.ClassificationId,
                input.Reason, input.Priority, input.Sentiment, cancellationToken);

            return Result.Success(new EscalationResult
            {
                EscalationId = escalation.Id,
                ConversationId = input.ConversationId,
                Created = true,
                AlreadyEscalated = false,
                Status = "open"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to escalate conversation {ConversationId}", input.ConversationId);
            return Result.Failure<EscalationResult>(new Error(
                "Escalation.Failed", "Failed to escalate conversation.", 500));
        }
    }

    private async Task NotifyManagersAdminsAsync(int tenantId, ConversationEscalatedPayload payload,
        CancellationToken cancellationToken)
    {
        try
        {
            await notifier.ConversationEscalatedAsync(tenantId, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to notify Managers/Admins for tenant {TenantId}", tenantId);
        }
    }

    private async Task WriteAuditAsync(int tenantId, int conversationId, int escalationId, int classificationId,
        string reason, string priority, string sentiment, CancellationToken cancellationToken)
    {
        var auditRepo = unitOfWork.GetRepository<AuditLog, int>();
        var details = System.Text.Json.JsonSerializer.Serialize(new
        {
            ConversationId = conversationId,
            EscalationId = escalationId,
            ClassificationId = classificationId,
            Reason = reason,
            Priority = priority,
            Sentiment = sentiment
        });

        await auditRepo.AddAsync(new AuditLog
        {
            TenantId = tenantId,
            ActorUserId = null,
            Action = AuditAction.EscalationTriggered,
            EntityType = "Escalation",
            EntityId = escalationId,
            Details = details
        }, cancellationToken);
    }
}
