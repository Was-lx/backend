using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;

namespace WaslX.Application.Abstractions.WhatsApp;


public interface IConversationWindowService
{

    ConversationWindowState UpdateFromInboundMessage(
        Conversation conversation,
        DateTime messageTimestamp,
        bool hasReferral);


    ConversationWindowState? UpdateFromMetaStatus(
        Conversation conversation,
        DateTime? metaExpiry);

  
    ConversationWindowState SynchronizeAfterSuccessfulSend(Conversation conversation);

    void SynchronizeAfterFailedSend(Conversation? conversation);


    ConversationWindowState EvaluateConversation(Conversation conversation);

    TimeSpan CalculateRemainingTime(Conversation conversation);

    ConversationWindowType DetermineWindowType(Conversation conversation);
}
