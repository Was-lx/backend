using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Abstractions.Rag;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;

namespace WaslX.Persistance.Services.AiAgent;

public class AiAgentReplyService(
    WaslX.Persistance.Data.ApplicationDbContext db,
    IKnowledgeRetriever knowledgeRetriever,
    ILLMProvider llmProvider,
    IWhatsAppService whatsAppService,
    IInboxRealtimeNotifier notifier,
    ILogger<AiAgentReplyService> logger) : IAiAgentReplyService
{
    public async Task ReplyAsync(int tenantId, int conversationId, int messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("AiAgentReplyService: Started for Tenant {TenantId}, Conv {ConvId}, Msg {MsgId}", tenantId, conversationId, messageId);
            var conversation = await db.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
                
            if (conversation == null || conversation.TenantId != tenantId) 
            {
                logger.LogInformation("AiAgentReplyService: Early return - conversation is null or tenant mismatch");
                return;
            }

            var waAccount = await db.WhatsAppAccounts.AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == conversation.WhatsAppAccountId, cancellationToken);
            if (waAccount == null)
            {
                logger.LogInformation("AiAgentReplyService: Early return - waAccount is null");
                return;
            }

            var tenantSettings = await db.TenantAiAgentSettings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
            if (tenantSettings == null || !tenantSettings.Enabled)
            {
                logger.LogInformation("AiAgentReplyService: Early return - tenantSettings null or not enabled");
                return;
            }

            var numberSettings = await db.AiAgentNumberSettings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.WhatsAppAccountId == waAccount.Id, cancellationToken);
            
            bool aiEnabled;
            bool autoReplyEnabled;
            int maxMessages;

            if (numberSettings is not null)
            {
                aiEnabled = numberSettings.Enabled;
                autoReplyEnabled = numberSettings.AutoReplyEnabled;
                maxMessages = numberSettings.MaxConversationMessages;
            }
            else
            {
                // Fall back to tenant-level toggle
                aiEnabled = tenantSettings.Enabled;
                autoReplyEnabled = true; // Default to true if falling back
                maxMessages = 10;
            }

            if (!aiEnabled || !autoReplyEnabled)
            {
                logger.LogInformation("AiAgentReplyService: Early return - aiEnabled or autoReplyEnabled is false");
                return;
            }

            var customer = await db.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == conversation.CustomerId, cancellationToken);
                
            var currentMessage = await db.Messages.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
            if (currentMessage == null || currentMessage.MessageType != MessageType.Text)
            {
                logger.LogInformation("AiAgentReplyService: Early return - currentMessage is null or not text");
                return;
            }

            // Load history
            var history = await db.Messages.AsNoTracking()
                .Where(m => m.ConversationId == conversationId && m.Id <= messageId)
                .OrderByDescending(m => m.Id)
                .Take(maxMessages > 0 ? maxMessages : 10)
                .ToListAsync(cancellationToken);
            history.Reverse();

            // Load RAG
            var ragResult = await knowledgeRetriever.RetrieveAsync(tenantId, currentMessage.Content, 3, null, cancellationToken);
            var ragContext = ragResult.IsSuccess && ragResult.Value.Chunks.Any() 
                ? string.Join("\n\n", ragResult.Value.Chunks.Select(c => c.Content))
                : "No specific knowledge context available.";

            // Build Prompt
            var systemPrompt = BuildSystemPrompt(tenantSettings, ragContext);
            var userPrompt = BuildUserPrompt(customer, history);

            // Call LLM (via Groq ILLMProvider)
            logger.LogInformation("AiAgentReplyService: Calling LLM via ILLMProvider (Groq)");
            var llmRequest = new LlmRequest(
                systemPrompt,
                new[] { new LlmMessage("user", userPrompt) },
                Temperature: 0.7,
                MaxTokens: 500);
            var llmResult = await llmProvider.GenerateAsync(llmRequest, cancellationToken);
            if (!llmResult.IsSuccess)
            {
                logger.LogWarning("LLM call failed for conversation {ConvId}: {Error}", conversationId, llmResult.Error.Description);
                return;
            }

            var aiResponseText = llmResult.Value.Text;

            // Evaluate Confidence
            bool isOutOfContext = aiResponseText.Contains("[OUT_OF_CONTEXT]", StringComparison.OrdinalIgnoreCase) || aiResponseText.Contains("[TAKEOVER]", StringComparison.OrdinalIgnoreCase);

            if (isOutOfContext)
            {
                logger.LogInformation("AI Agent determined message is out of context for conversation {ConvId}", conversationId);
                
                // 1. Reply to the customer
                aiResponseText = "عذراً، هذا الاستفسار خارج نطاق المعلومات المتوفرة لدي حالياً. سيتم تحويلك إلى أحد ممثلي خدمة العملاء.";
                await whatsAppService.SendTextAsync(tenantId, customer?.PhoneNumber ?? string.Empty, aiResponseText, null, SenderType.AI, cancellationToken);

                // 2. Escalate to human (Pause AI)
                conversation.AiMode = WaslX.Domain.SharedEnums.AiConversationMode.Paused;
                conversation.HandledByAi = true;
                await db.SaveChangesAsync(cancellationToken);
                
                await notifier.ConversationTakenOverAsync(tenantId, new ConversationTakenOverPayload(conversationId, DateTime.UtcNow), cancellationToken);
                
                await notifier.ConversationAiModeChangedAsync(tenantId, new ConversationAiModeChangedPayload(conversationId, conversation.AiMode.ToString()), cancellationToken);
                await notifier.ConversationChangedAsync(tenantId, new ConversationChangedPayload(
                    conversationId, conversation.Status.ToString(), conversation.AssignedUserId, conversation.LastMessageAt, conversation.HandledByAi, conversation.AiMode.ToString()), cancellationToken);
                return;
            }

            // Send Reply
            var sendResult = await whatsAppService.SendTextAsync(tenantId, customer?.PhoneNumber ?? string.Empty, aiResponseText, null, SenderType.AI, cancellationToken);
            if (sendResult.IsSuccess)
            {
                conversation.HandledByAi = true;
                await db.SaveChangesAsync(cancellationToken);

                await notifier.ConversationChangedAsync(tenantId, new ConversationChangedPayload(
                    conversationId, conversation.Status.ToString(), conversation.AssignedUserId, conversation.LastMessageAt, conversation.HandledByAi, conversation.AiMode.ToString()), cancellationToken);
                
                logger.LogInformation("AI Agent successfully replied to conversation {ConvId}", conversationId);
            }
            else
            {
                logger.LogWarning("AI Agent failed to send WhatsApp reply for conversation {ConvId}: {Error}", conversationId, sendResult.Error.Description);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process AI agent reply for conversation {ConvId}", conversationId);
        }
    }

    private string BuildSystemPrompt(TenantAiAgentSettings settings, string ragContext)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"You are {settings.PersonaName}, an AI assistant for this company.");
        sb.AppendLine($"Tone Instructions: {settings.ToneInstructions}");
        sb.AppendLine("Rules:");
        sb.AppendLine("1. You MUST ONLY answer questions based on the provided KNOWLEDGE CONTEXT below.");
        sb.AppendLine("2. DO NOT use your general knowledge, and DO NOT hallucinate or invent any information.");
        sb.AppendLine("3. If the user's message is a simple greeting (like 'hi', 'hello'), you can reply with a polite greeting.");
        sb.AppendLine("4. For any other question, if it cannot be fully answered using ONLY the KNOWLEDGE CONTEXT, or if the user is angry or requesting a human, you MUST reply exactly with the word [OUT_OF_CONTEXT]. Do not add any other text.");
        sb.AppendLine();
        sb.AppendLine("--- KNOWLEDGE CONTEXT ---");
        sb.AppendLine(ragContext);
        sb.AppendLine("-------------------------");
        return sb.ToString();
    }

    private string BuildUserPrompt(Customer? customer, List<Message> history)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Customer Name: {customer?.Name ?? "Unknown"}");
        sb.AppendLine("--- CONVERSATION HISTORY ---");
        foreach (var msg in history)
        {
            var sender = msg.SenderType == SenderType.Customer ? "Customer" : "Assistant";
            sb.AppendLine($"{sender}: {msg.Content}");
        }
        return sb.ToString();
    }
}
