using System.Linq;
using Hangfire;
using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Abstractions.AutoEscalation;
using WaslX.Application.Abstractions.Rag;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Features.Classification.Models;
using WaslX.Application.Features.Escalation.Models;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;
using WaslX.Domain.Specifications;
using WaslX.Domain.SharedEnums;

namespace WaslX.Application.Features.Classification;

public interface IClassificationOrchestrator
{
    Task ProcessClassificationAsync(int tenantId, int conversationId, int messageId, CancellationToken cancellationToken = default);
}

public class ClassificationOrchestrator(
    IUnitOfWork unitOfWork,
    IMessageClassifier classifier,
    IInboxRealtimeNotifier realtimeNotifier,
    IConversationEscalationService escalationService,
    IKnowledgeRetriever knowledgeRetriever,
    IAiUsageQuotaService usageQuota,
    ILogger<ClassificationOrchestrator> logger) : IClassificationOrchestrator
{
    public async Task ProcessClassificationAsync(int tenantId, int conversationId, int messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Pre-call spend guard: skip classification entirely once the tenant's monthly AI quota
            // is exhausted, instead of billing an unbounded number of LLM calls regardless of plan.
            if (!await usageQuota.IsWithinQuotaAsync(tenantId, cancellationToken))
            {
                logger.LogWarning("Classification skipped for tenant {TenantId}: monthly AI quota exceeded", tenantId);
                return;
            }

            var msgRepo = unitOfWork.GetRepository<Message, int>();
            var classRepo = unitOfWork.GetRepository<MessageClassification, int>();

            var message = await msgRepo.GetByIdAsync(messageId, cancellationToken);
            if (message == null || message.ConversationId != conversationId)
            {
                logger.LogWarning("Message {MessageId} not found or conversation mismatch.", messageId);
                return;
            }

            // Verify tenant by conversation if needed, or trust tenantId param.

            // Get recent messages for context (last 5 messages in this conversation)
            var recentMessagesSpec = new RecentMessagesSpecification(conversationId, messageId);
            var recentMessages = await msgRepo.GetAllWithSpecAsync(recentMessagesSpec, false);

            string? knowledgeContext = null;
            try
            {
                var knowledgeResult = await knowledgeRetriever.RetrieveAsync(tenantId, message.Content, topK: 3, cancellationToken: cancellationToken);
                if (knowledgeResult.IsSuccess && knowledgeResult.Value.Chunks.Count > 0)
                {
                    knowledgeContext = string.Join("\n", knowledgeResult.Value.Chunks.Select(c => $"[{c.Title ?? c.SourceType}] {c.Content}"));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Knowledge retrieval failed for conversation {ConversationId}, proceeding without context.", conversationId);
            }

            var input = new MessageClassificationInput
            {
                TenantId = tenantId,
                ConversationId = conversationId,
                MessageId = messageId,
                MessageText = message.Content,
                RecentMessages = recentMessages.Select(m => m.Content).ToList(),
                CustomerMemorySummary = knowledgeContext
            };

            var result = await classifier.ClassifyAsync(input, cancellationToken);

            var classification = new MessageClassification
            {
                TenantId = tenantId,
                ConversationId = conversationId,
                MessageId = messageId,
                Topic = result.Topic,
                Language = result.Language,
                Sentiment = result.Sentiment,
                Priority = result.Priority,
                Escalate = result.Escalate,
                Reason = result.Reason,
                ClassifierVersion = result.ClassifierVersion
            };

            await classRepo.AddAsync(classification, cancellationToken);

            int? escalationId = null;
            if (result.Escalate)
            {
                await unitOfWork.CompleteAsync();

                var escalationInput = new EscalationInput
                {
                    TenantId = tenantId,
                    ConversationId = conversationId,
                    MessageId = messageId,
                    ClassificationId = classification.Id,
                    Topic = result.Topic,
                    Sentiment = result.Sentiment,
                    Priority = result.Priority,
                    Reason = result.Reason
                };

                var escalationResult = await escalationService.EscalateAsync(escalationInput, cancellationToken);
                if (escalationResult.IsSuccess)
                {
                    escalationId = escalationResult.Value.EscalationId;

                    if (escalationId.HasValue && escalationResult.Value.Created)
                    {
                        var scoringInput = new EscalationScoringInput
                        {
                            TenantId = tenantId,
                            ConversationId = conversationId,
                            EscalationId = escalationId.Value,
                            Topic = result.Topic,
                            Sentiment = result.Sentiment,
                            Priority = result.Priority,
                            EscalationReason = result.Reason
                        };

                        BackgroundJob.Enqueue<IEscalationTargetScoringService>(
                            service => service.ScoreAsync(scoringInput, CancellationToken.None));
                    }
                }
                else
                {
                    logger.LogWarning("Escalation failed for conversation {ConversationId}: {Error}",
                        conversationId, escalationResult.Error.Description);
                    classification.Escalate = false;
                }
            }

            await unitOfWork.CompleteAsync();

            var payload = new MessageClassificationPayload(
                conversationId,
                messageId,
                new MessageClassificationDto(
                    result.Topic,
                    result.Language,
                    result.Sentiment,
                    result.Priority,
                    classification.Escalate,
                    result.Reason
                ));

            await realtimeNotifier.MessageClassificationUpdatedAsync(tenantId, payload, cancellationToken);

            if (classification.Escalate && escalationId.HasValue)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Conversation {ConversationId}: Escalation {EscalationId} created successfully.", conversationId, escalationId.Value);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to orchestrate classification for message {MessageId}", messageId);
        }
    }

    private class RecentMessagesSpecification : Specification<Message, int>
    {
        public RecentMessagesSpecification(int conversationId, int currentMessageId)
            : base(m => m.ConversationId == conversationId && m.Id < currentMessageId && m.MessageType == MessageType.Text)
        {
            AddOrderByDesc(m => m.Id);
            ApplyPagination(0, 5); // Take last 5
        }
    }
}
