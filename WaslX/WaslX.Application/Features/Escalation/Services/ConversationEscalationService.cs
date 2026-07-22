using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.AutoEscalation;
using WaslX.Application.Abstractions.Notifications;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Features.Escalation.Models;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Domain.Specifications;

namespace WaslX.Application.Features.Escalation.Services;

public sealed class ConversationEscalationService(
    IUnitOfWork unitOfWork,
    IInboxRealtimeNotifier notifier,
    INotificationService notificationService,
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

            var customerName = await GetCustomerNameAsync(input.TenantId, conversation.CustomerId, cancellationToken);

            await NotifyManagersAdminsAsync(input.TenantId, payload, cancellationToken);
            await CreatePersistentNotificationsAsync(input.TenantId, input.ConversationId, input.Reason, input.Priority, input.Topic, customerName, cancellationToken);

            await WriteAuditAsync(input.TenantId, input.ConversationId, escalation.Id, input.ClassificationId,
                input.Reason, input.Priority, input.Sentiment, cancellationToken);
            await unitOfWork.CompleteAsync();

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
            logger.LogWarning(ex, "Failed to send SignalR escalation notification for tenant {TenantId}", tenantId);
        }
    }

    private async Task CreatePersistentNotificationsAsync(
        int tenantId, int conversationId, string reason, string priority, string topic,
        string customerName, CancellationToken cancellationToken)
    {
        try
        {
            var title = "\U0001f6a8 AI Escalation";
            var body = FormatNotificationBody(customerName, reason, priority, topic);

            var userRepo = unitOfWork.GetRepository<User, int>();
            var spec = new ManagerAdminSpec(tenantId);
            var recipients = await userRepo.GetAllWithSpecAsync(spec, false);

            foreach (var recipient in recipients)
            {
                await notificationService.CreateAsync(
                    tenantId,
                    recipient.Id,
                    "escalation",
                    title,
                    body,
                    "conversation",
                    conversationId,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create persistent escalation notifications for tenant {TenantId}, conversation {ConversationId}", tenantId, conversationId);
        }
    }

    private async Task<string> GetCustomerNameAsync(int tenantId, int customerId, CancellationToken cancellationToken)
    {
        try
        {
            var customerRepo = unitOfWork.GetRepository<Customer, int>();
            var customer = await customerRepo.GetByIdAsync(customerId, cancellationToken);
            return customer?.Name ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string FormatNotificationBody(string customerName, string reason, string priority, string topic)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(customerName) && customerName != "Unknown")
            lines.Add($"Customer: {customerName}");

        if (!string.IsNullOrWhiteSpace(reason))
            lines.Add($"Reason: {reason}");

        if (!string.IsNullOrWhiteSpace(priority))
            lines.Add($"Priority: {char.ToUpper(priority[0]) + priority[1..]}");

        if (!string.IsNullOrWhiteSpace(topic) && !string.Equals(topic, "general", StringComparison.OrdinalIgnoreCase))
            lines.Add($"Topic: {char.ToUpper(topic[0]) + topic[1..]}");

        lines.Add("Suggested by AI");

        return string.Join("\n", lines);
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

    private class ManagerAdminSpec : Specification<User, int>
    {
        public ManagerAdminSpec(int tenantId)
            : base(u => u.TenantId == tenantId && u.Status == "Active"
                        && (u.Role.Name == "Manager" || u.Role.Name == "Admin"))
        {
            AddInclude(u => u.Role);
        }
    }
}
