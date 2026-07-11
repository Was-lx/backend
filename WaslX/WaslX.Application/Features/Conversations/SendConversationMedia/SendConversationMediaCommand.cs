using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Conversations.SendConversationMedia;

/// <summary>Uploads a file (image/video/document) and sends it as a reply within a conversation.</summary>
public record SendConversationMediaCommand(
    int? TenantId, int CurrentUserId, bool IsPrivileged, int ConversationId,
    byte[] FileContent, string FileName, string ContentType, string? Caption, string? CurrentUserEmail = null)
    : ICommand<SendMessageResult>;

public class SendConversationMediaCommandHandler(IConversationService conversations)
    : ICommandHandler<SendConversationMediaCommand, SendMessageResult>
{
    public Task<Result<SendMessageResult>> Handle(SendConversationMediaCommand request, CancellationToken cancellationToken) =>
        conversations.SendMediaAsync(
            request.TenantId, request.CurrentUserId, request.IsPrivileged, request.ConversationId,
            request.FileContent, request.FileName, request.ContentType, request.Caption,
            request.CurrentUserEmail, cancellationToken);
}
