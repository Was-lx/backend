using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;

namespace WaslX.Persistance.Services;

public class ConversationWindowService(ILogger<ConversationWindowService> logger) : IConversationWindowService
{
    public ConversationWindowState UpdateFromInboundMessage(Conversation conversation, DateTime messageTimestamp, bool hasReferral)
    {
        var hours = hasReferral ? ConversationWindowConstants.FreeEntryPointHours : ConversationWindowConstants.CustomerServiceHours;
        var type = hasReferral ? ConversationWindowType.FreeEntryPoint72h : ConversationWindowType.CustomerService24h;
        var oldExpiration = conversation.WindowExpiresAt;
        
        conversation.LastCustomerMessageAt = messageTimestamp;
        conversation.WindowExpiresAt = messageTimestamp.AddHours(hours);
        conversation.WindowType = type;

        var trigger = hasReferral ? "Referral72h" : "InboundMessage";
        logger.LogInformation(
            "[Window] {Trigger} | ConvId={ConversationId} CustId={CustomerId} Type={Type} Old={Old} New={New} At={Utc} Reason=CustomerInbound",
            trigger, conversation.Id, conversation.CustomerId, type, oldExpiration?.ToString("O"), conversation.WindowExpiresAt?.ToString("O"), DateTime.UtcNow.ToString("O"));

        return EvaluateConversation(conversation);
    }

    public ConversationWindowState? UpdateFromMetaStatus(Conversation conversation, DateTime? metaExpiry)
    {
        if (metaExpiry is null || metaExpiry <= conversation.WindowExpiresAt)
            return null; // Extend-only rule

        var oldExpiration = conversation.WindowExpiresAt;
        conversation.WindowExpiresAt = metaExpiry;

        logger.LogInformation(
            "[Window] StatusWebhook | ConvId={ConversationId} CustId={CustomerId} Type={Type} Old={Old} New={New} At={Utc} Reason=MetaExpirationTimestamp",
            conversation.Id, conversation.CustomerId, conversation.WindowType, oldExpiration?.ToString("O"), metaExpiry?.ToString("O"), DateTime.UtcNow.ToString("O"));

        return EvaluateConversation(conversation);
    }

    public ConversationWindowState SynchronizeAfterSuccessfulSend(Conversation conversation)
    {
        var healed = conversation.LastCustomerMessageAt?.AddHours(ConversationWindowConstants.CustomerServiceHours) 
                     ?? DateTime.UtcNow.AddHours(ConversationWindowConstants.CustomerServiceHours);
                     
        if (healed <= conversation.WindowExpiresAt)
            return EvaluateConversation(conversation); // Never reduce, never overwrite valid 72h

        var oldExpiration = conversation.WindowExpiresAt;
        conversation.WindowExpiresAt = healed;

        logger.LogInformation(
            "[Window] SuccessfulSend | ConvId={ConversationId} CustId={CustomerId} Type={Type} Old={Old} New={New} At={Utc} Reason=HealedAfterAcceptedSend",
            conversation.Id, conversation.CustomerId, conversation.WindowType, oldExpiration?.ToString("O"), healed.ToString("O"), DateTime.UtcNow.ToString("O"));

        return EvaluateConversation(conversation);
    }

    public void SynchronizeAfterFailedSend(Conversation? conversation)
    {
        if (conversation is null || conversation.WindowExpiresAt <= DateTime.UtcNow)
            return;

        var oldExpiration = conversation.WindowExpiresAt;
        conversation.WindowExpiresAt = DateTime.UtcNow;

        logger.LogInformation(
            "[Window] Error131047 | ConvId={ConversationId} CustId={CustomerId} Type={Type} Old={Old} New={New} At={Utc} Reason=WindowClosedByMeta",
            conversation.Id, conversation.CustomerId, conversation.WindowType, oldExpiration?.ToString("O"), DateTime.UtcNow.ToString("O"), DateTime.UtcNow.ToString("O"));
    }

    public ConversationWindowState EvaluateConversation(Conversation conversation)
    {
        var expiry = conversation.WindowExpiresAt ?? conversation.LastCustomerMessageAt?.AddHours(ConversationWindowConstants.CustomerServiceHours);
        var isOpen = expiry.HasValue && expiry.Value > DateTime.UtcNow;
        var remaining = isOpen ? expiry!.Value - DateTime.UtcNow : TimeSpan.Zero;

        return new ConversationWindowState
        {
            IsOpen = isOpen,
            RemainingTime = remaining,
            WindowExpiresAt = expiry,
            WindowType = conversation.WindowType
        };
    }

    public TimeSpan CalculateRemainingTime(Conversation conversation)
    {
        return EvaluateConversation(conversation).RemainingTime;
    }

    public ConversationWindowType DetermineWindowType(Conversation conversation)
    {
        return conversation.WindowType;
    }
}
