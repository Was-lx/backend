using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.Notifications;
using WaslX.Application.Abstractions.Performance;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Abstractions.Screening;
using WaslX.Application.Features.Escalation.Screening;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Domain.Specifications;

namespace WaslX.Application.Features.Escalation.Services
{
    public class EscalationAssignmentService(
        IUnitOfWork unitOfWork,
        IInboxRealtimeNotifier realtimeNotifier,
        IAgentPerformanceUpdateService performanceUpdate,
        INotificationService notificationService,
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

            await CreateAssignmentHistoryAsync(escalation.ConversationId, assigneeId, "Escalation recommendation confirmed", cancellationToken);
            await WriteAuditLogAsync(tenantId, actorUserId, escalation.Id, previousOwnerId, assigneeId, "Confirm", "recommend", null);

            await unitOfWork.CompleteAsync();

            if (previousOwnerId is { } prevId && prevId != assigneeId)
            {
                await performanceUpdate.RecordConversationClosedAsync(prevId, resolved: false, cancellationToken);
            }

            await performanceUpdate.RecordConversationAssignedAsync(assigneeId, cancellationToken);

            var customerName = await GetCustomerNameAsync(conversation.CustomerId, cancellationToken);
            var result = BuildRecommendation(escalation, conversation, assignee?.Name, previousOwnerId);

            await NotifyAssignedAgentAsync(tenantId, escalation.ConversationId, assigneeId,
                customerName, escalation.EscalationReason, escalation.Priority, escalation.Topic, "Recommend", cancellationToken);

            if (previousOwnerId.HasValue && previousOwnerId.Value != assigneeId)
                await NotifyOldAgentReassignedAsync(tenantId, escalation.ConversationId, previousOwnerId.Value,
                    customerName, escalation.Priority, escalation.Topic, cancellationToken);

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

            await CreateAssignmentHistoryAsync(escalation.ConversationId, assigneeId, $"Override: {reason}", cancellationToken);
            await WriteAuditLogAsync(tenantId, actorUserId, escalation.Id, previousOwnerId, assigneeId, "Override", "recommend", reason);

            await unitOfWork.CompleteAsync();

            if (previousOwnerId is { } prevId && prevId != assigneeId)
            {
                await performanceUpdate.RecordConversationClosedAsync(prevId, resolved: false, cancellationToken);
            }

            await performanceUpdate.RecordConversationAssignedAsync(assigneeId, cancellationToken);

            var customerName = await GetCustomerNameAsync(conversation.CustomerId, cancellationToken);
            var result = BuildRecommendation(escalation, conversation, assignee?.Name, previousOwnerId);

            await NotifyAssignedAgentAsync(tenantId, escalation.ConversationId, assigneeId,
                customerName, escalation.EscalationReason, escalation.Priority, escalation.Topic, "Override", cancellationToken);

            if (previousOwnerId.HasValue && previousOwnerId.Value != assigneeId)
                await NotifyOldAgentReassignedAsync(tenantId, escalation.ConversationId, previousOwnerId.Value,
                    customerName, escalation.Priority, escalation.Topic, cancellationToken);

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

                await CreateAssignmentHistoryAsync(escalation.ConversationId, assigneeId, "Auto-assigned by AI escalation", cancellationToken);
                await WriteAuditLogAsync(tenantId, null, escalation.Id, previousOwnerId, assigneeId, "AutoAssign", "autoAssign", null);

                await unitOfWork.CompleteAsync();

                if (previousOwnerId is { } prevId && prevId != assigneeId)
                {
                    await performanceUpdate.RecordConversationClosedAsync(prevId, resolved: false, cancellationToken);
                }

                await performanceUpdate.RecordConversationAssignedAsync(assigneeId, cancellationToken);

                var customerName = await GetCustomerNameAsync(conversation.CustomerId, cancellationToken);
                var result = BuildRecommendation(escalation, conversation, assignee?.Name, previousOwnerId);

                await NotifyAssignedAgentAsync(tenantId, escalation.ConversationId, assigneeId,
                    customerName, escalation.EscalationReason, escalation.Priority, escalation.Topic, "AutoAssign", cancellationToken);

                if (previousOwnerId.HasValue && previousOwnerId.Value != assigneeId)
                    await NotifyOldAgentReassignedAsync(tenantId, escalation.ConversationId, previousOwnerId.Value,
                        customerName, escalation.Priority, escalation.Topic, cancellationToken);

                await NotifyManagersAutoAssignAsync(tenantId, escalation.ConversationId,
                    assignee?.Name ?? "Unknown", escalation.SuggestedScore, escalation.SuggestedReason,
                    escalation.Priority, escalation.Topic, customerName, cancellationToken);

                await realtimeNotifier.EscalationAutoAssignedAsync(tenantId, result, cancellationToken);
                await realtimeNotifier.ConversationOwnershipTransferredAsync(tenantId, new OwnershipTransferredPayload(
                    conversation.Id, previousOwnerId, assigneeId, "AutoAssign", DateTime.UtcNow, escalation.AssignedAtUtc), cancellationToken);

                logger.LogInformation("Escalation {EscalationId} auto-assigned to user {AssigneeId}", escalationId, assigneeId);

                return Result.Success(result);
            }
        }

        public async Task<Result<EscalationRecommendation>> RejectAsync(
            int tenantId, int actorUserId, int escalationId, string? reason, CancellationToken cancellationToken = default)
        {
            var escalation = await LoadEscalationAsync(escalationId, cancellationToken);
            if (escalation is null)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotFound);

            if (escalation.TenantId != tenantId)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotFound);

            if (escalation.Status != EscalationStatus.Recommended && escalation.Status != EscalationStatus.Open)
                return Result.Failure<EscalationRecommendation>(AppErrors.CannotReject);

            var conversationRepo = unitOfWork.GetRepository<Conversation, int>();
            var conversation = await conversationRepo.GetByIdAsync(escalation.ConversationId, cancellationToken);
            if (conversation is null)
                return Result.Failure<EscalationRecommendation>(AppErrors.NotFound);

            escalation.Status = EscalationStatus.Cancelled;
            escalation.ConfirmedByUserId = actorUserId;
            escalation.ConfirmedAtUtc = DateTime.UtcNow;
            unitOfWork.GetRepository<Domain.Entities.Escalation, int>().Update(escalation);

            await WriteAuditLogAsync(tenantId, actorUserId, escalation.Id, conversation.AssignedUserId, conversation.AssignedUserId ?? 0, "Reject", "recommend", reason);

            await unitOfWork.CompleteAsync();

            var customerName = await GetCustomerNameAsync(conversation.CustomerId, cancellationToken);
            var result = BuildRecommendation(escalation, conversation, null, conversation.AssignedUserId);

            await NotifyManagersRejectAsync(tenantId, escalation.ConversationId, actorUserId,
                escalation.Priority, escalation.Topic, customerName, reason, cancellationToken);

            await realtimeNotifier.EscalationRejectedAsync(tenantId, result, cancellationToken);

            logger.LogInformation("Escalation {EscalationId} rejected by user {UserId}, reason: {Reason}", escalationId, actorUserId, reason);

            return Result.Success(result);
        }

        public async Task<Result<IReadOnlyList<EscalationCandidateSnapshotDto>>> GetCandidatesAsync(
            int tenantId, int escalationId, CancellationToken cancellationToken = default)
        {
            var escalation = await LoadEscalationAsync(escalationId, cancellationToken);
            if (escalation is null)
                return Result.Failure<IReadOnlyList<EscalationCandidateSnapshotDto>>(AppErrors.NotFound);

            if (escalation.TenantId != tenantId)
                return Result.Failure<IReadOnlyList<EscalationCandidateSnapshotDto>>(AppErrors.NotFound);

            var snapshots = escalation.CandidateSnapshots?
                .OrderBy(s => s.RankingOrder)
                .Select(s => new EscalationCandidateSnapshotDto(
                    s.AgentId, s.AgentName, s.OverallScore, s.PerformanceScore,
                    s.ResponseSpeedScore, s.WorkloadScore, s.ActiveChats,
                    s.RankingOrder, s.Status, s.Reason))
                .ToList() ?? new List<EscalationCandidateSnapshotDto>();

            return Result.Success<IReadOnlyList<EscalationCandidateSnapshotDto>>(snapshots);
        }

        #region Private Helpers

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

        private async Task CreateAssignmentHistoryAsync(int conversationId, int assigneeId, string reason, CancellationToken cancellationToken)
        {
            var assignmentRepo = unitOfWork.GetRepository<Assignment, int>();
            await assignmentRepo.AddAsync(new Assignment
            {
                ConversationId = conversationId,
                AssignedToUserId = assigneeId,
                Method = AssignmentMethod.Escalation,
                Reason = reason,
                AssignedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        private async Task NotifyAssignedAgentAsync(
            int tenantId, int conversationId, int assigneeId,
            string customerName, string reason, string priority, string topic,
            string assignmentSource, CancellationToken cancellationToken)
        {
            try
            {
                var title = "Conversation Assigned to You";
                var body = FormatAgentNotificationBody(customerName, reason, priority, topic, assignmentSource);
                await notificationService.CreateAsync(
                    tenantId, assigneeId, "escalation-assignment",
                    title, body, "conversation", conversationId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to create agent notification for escalation assignment, tenant {TenantId}, agent {AgentId}", tenantId, assigneeId);
            }
        }

        private async Task NotifyOldAgentReassignedAsync(
            int tenantId, int conversationId, int oldAgentId,
            string customerName, string priority, string topic,
            CancellationToken cancellationToken)
        {
            try
            {
                var title = "Conversation Reassigned";
                var body = $"Conversation with {customerName} has been reassigned to another agent.";
                await notificationService.CreateAsync(
                    tenantId, oldAgentId, "escalation-reassigned",
                    title, body, "conversation", conversationId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to notify old agent of reassignment, tenant {TenantId}, agent {AgentId}", tenantId, oldAgentId);
            }
        }

        private async Task NotifyManagersRejectAsync(
            int tenantId, int conversationId, int rejectedByUserId,
            string priority, string topic, string customerName, string? reason,
            CancellationToken cancellationToken)
        {
            try
            {
                var userRepo = unitOfWork.GetRepository<User, int>();
                var spec = new ManagerAdminSpec(tenantId);
                var recipients = await userRepo.GetAllWithSpecAsync(spec, false);

                var title = "Escalation Rejected";
                var body = $"The escalation for conversation with {customerName} was rejected.";
                if (!string.IsNullOrWhiteSpace(reason))
                    body += $" Reason: {reason}";

                foreach (var recipient in recipients)
                {
                    if (recipient.Id == rejectedByUserId) continue;
                    await notificationService.CreateAsync(
                        tenantId, recipient.Id, "escalation-rejected",
                        title, body, "conversation", conversationId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send reject notifications, tenant {TenantId}", tenantId);
            }
        }

        private async Task NotifyManagersAutoAssignAsync(
            int tenantId, int conversationId,
            string assigneeName, decimal? score, string reason,
            string priority, string topic, string customerName,
            CancellationToken cancellationToken)
        {
            try
            {
                var userRepo = unitOfWork.GetRepository<User, int>();
                var spec = new ManagerAdminSpec(tenantId);
                var recipients = await userRepo.GetAllWithSpecAsync(spec, false);

                var title = "AI Auto Assignment";
                var body = FormatManagerAutoAssignBody(customerName, assigneeName, score, reason, priority, topic);

                foreach (var recipient in recipients)
                {
                    await notificationService.CreateAsync(
                        tenantId, recipient.Id, "escalation-auto-assign",
                        title, body, "conversation", conversationId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to create manager auto-assign notifications for tenant {TenantId}", tenantId);
            }
        }

        private static string FormatAgentNotificationBody(string customerName, string reason, string priority, string topic, string assignmentSource)
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

            lines.Add($"Assigned via: {assignmentSource}");

            return string.Join("\n", lines);
        }

        private static string FormatManagerAutoAssignBody(string customerName, string assigneeName, decimal? score, string reason, string priority, string topic)
        {
            var lines = new List<string>();

            if (!string.IsNullOrWhiteSpace(customerName) && customerName != "Unknown")
                lines.Add($"Customer: {customerName}");

            lines.Add($"Assigned to: {assigneeName}");

            if (score.HasValue)
                lines.Add($"Score: {score.Value:P0}");

            if (!string.IsNullOrWhiteSpace(reason))
                lines.Add($"Reason: {reason}");

            if (!string.IsNullOrWhiteSpace(priority))
                lines.Add($"Priority: {char.ToUpper(priority[0]) + priority[1..]}");

            if (!string.IsNullOrWhiteSpace(topic) && !string.Equals(topic, "general", StringComparison.OrdinalIgnoreCase))
                lines.Add($"Topic: {char.ToUpper(topic[0]) + topic[1..]}");

            lines.Add("Assigned automatically by AI");

            return string.Join("\n", lines);
        }

        private async Task<string> GetCustomerNameAsync(int customerId, CancellationToken cancellationToken)
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
            var snapshots = escalation.CandidateSnapshots?
                .OrderBy(s => s.RankingOrder)
                .Select(s => new EscalationCandidateSnapshotDto(
                    s.AgentId, s.AgentName, s.OverallScore, s.PerformanceScore,
                    s.ResponseSpeedScore, s.WorkloadScore, s.ActiveChats,
                    s.RankingOrder, s.Status, s.Reason))
                .ToList() ?? new List<EscalationCandidateSnapshotDto>();

            return new EscalationRecommendation
            {
                EscalationId = escalation.Id,
                ConversationId = escalation.ConversationId,
                SuggestedAssigneeId = escalation.SuggestedAssigneeId,
                SuggestedAssigneeName = escalation.SuggestedAssignee?.Name ?? assigneeName ?? string.Empty,
                Reason = escalation.SuggestedReason,
                Score = escalation.SuggestedScore,
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
                CreatedAtUtc = escalation.CreatedAt,
                Priority = escalation.Priority,
                Topic = escalation.Topic,
                Sentiment = escalation.Sentiment,
                Candidates = snapshots
            };
        }

        #endregion

        #region Specifications

        private class TenantEscalationSettingsSpec(int tenantId)
            : Domain.Specifications.Specification<TenantEscalationSettings, int>(s => s.TenantId == tenantId);

        private class ManagerAdminSpec : Specification<User, int>
        {
            public ManagerAdminSpec(int tenantId)
                : base(u => u.TenantId == tenantId && u.Status == "Active"
                            && (u.Role.Name == "Manager" || u.Role.Name == "Admin"))
            {
                AddInclude(u => u.Role);
            }
        }

#pragma warning disable CS8603
        private class EscalationWithIncludesSpec : Domain.Specifications.Specification<Domain.Entities.Escalation, int>
        {
            public EscalationWithIncludesSpec(int escalationId) : base(e => e.Id == escalationId)
            {
                AddInclude(e => e.Conversation);
                AddInclude(e => e.SuggestedAssignee!);
                AddInclude(e => e.CandidateSnapshots);
            }
        }

        private class EscalationByConversationSpec : Domain.Specifications.Specification<Domain.Entities.Escalation, int>
        {
            public EscalationByConversationSpec(int conversationId)
                : base(e => e.ConversationId == conversationId)
            {
                AddInclude(e => e.Conversation);
                AddInclude(e => e.SuggestedAssignee!);
                AddInclude(e => e.CandidateSnapshots);
            }
        }
#pragma warning restore CS8603

        #endregion
    }
}
