using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Contracts;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Conversations.ChangeConversationStatus;
using WaslX.Application.Features.Conversations.DeleteConversation;
using WaslX.Application.Features.Conversations.GetConversationDetail;
using WaslX.Application.Features.Conversations.GetConversationMessages;
using WaslX.Application.Features.Conversations.GetConversations;
using WaslX.Application.Features.Conversations.MarkConversationRead;
using WaslX.Application.Features.Conversations.SendConversationMedia;
using WaslX.Application.Features.Conversations.SendConversationMessage;
using WaslX.Application.Features.Notes.AddNote;
using WaslX.Application.Features.Notes.GetNotes;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController(ISender sender) : ControllerBase
{
    /// <summary>Lists the caller's shared-inbox conversations (agents see own; managers/admins see all).</summary>
    [HttpGet]
    public async Task<IActionResult> GetConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken cancellationToken = default)
    {
        var query = new GetConversationsQuery(User.GetTenantId(), CurrentUserId(), IsPrivileged(), page, pageSize);
        return (await sender.Send(query, cancellationToken)).ToActionResult();
    }

    /// <summary>Cursor-paginated message history for one conversation.</summary>
    [HttpGet("{id:int}/messages")]
    public async Task<IActionResult> GetMessages(int id, [FromQuery] int? before = null, [FromQuery] int pageSize = 30, CancellationToken cancellationToken = default)
    {
        var query = new GetConversationMessagesQuery(User.GetTenantId(), CurrentUserId(), IsPrivileged(), id, before, pageSize);
        return (await sender.Send(query, cancellationToken)).ToActionResult();
    }

    /// <summary>Sends a text reply within a conversation.</summary>
    [HttpPost("{id:int}/messages")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] SendConversationMessageRequest request, CancellationToken cancellationToken)
    {
        var command = new SendConversationMessageCommand(User.GetTenantId(), CurrentUserId(), IsPrivileged(), id, request.Text, User.GetEmail());
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    /// <summary>Uploads a file (image/video/document) and sends it as a reply within a conversation.</summary>
    [HttpPost("{id:int}/media")]
    [RequestSizeLimit(25_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SendMedia(int id, [FromForm] IFormFile? file, [FromForm] string? caption, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return Result.Failure<SendMessageResult>(AppErrors.MediaFileRequired).ToActionResult();

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        var command = new SendConversationMediaCommand(
            User.GetTenantId(), CurrentUserId(), IsPrivileged(), id,
            buffer.ToArray(), file.FileName, contentType, caption, User.GetEmail());
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    /// <summary>Rich detail for the customer-context panel + status controls.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken cancellationToken)
    {
        var query = new GetConversationDetailQuery(User.GetTenantId(), CurrentUserId(), IsPrivileged(), id);
        return (await sender.Send(query, cancellationToken)).ToActionResult();
    }

    /// <summary>Lists a conversation's internal team notes (team-only; never sent to the customer).</summary>
    [HttpGet("{id:int}/notes")]
    public async Task<IActionResult> GetNotes(int id, CancellationToken cancellationToken)
    {
        var query = new GetNotesQuery(User.GetTenantId(), CurrentUserId(), IsPrivileged(), id);
        return (await sender.Send(query, cancellationToken)).ToActionResult();
    }

    /// <summary>Adds an internal team note to a conversation (team-only; never sent to the customer).</summary>
    [HttpPost("{id:int}/notes")]
    public async Task<IActionResult> AddNote(int id, [FromBody] AddNoteRequest request, CancellationToken cancellationToken)
    {
        var command = new AddNoteCommand(User.GetTenantId(), CurrentUserId(), IsPrivileged(), id, request.Content, User.GetEmail());
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    /// <summary>Applies a manual lifecycle transition (server-side state machine rejects invalid moves).</summary>
    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeConversationStatusRequest request, CancellationToken cancellationToken)
    {
        var command = new ChangeConversationStatusCommand(User.GetTenantId(), CurrentUserId(), IsPrivileged(), id, request.Status);
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    /// <summary>Marks a conversation read (clears its unread count). Internal only — no WhatsApp read receipt is sent.</summary>
    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        var command = new MarkConversationReadCommand(User.GetTenantId(), CurrentUserId(), IsPrivileged(), id);
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    /// <summary>Soft-deletes a conversation (hidden from the inbox; a future inbound message starts a new one).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var command = new DeleteConversationCommand(User.GetTenantId(), CurrentUserId(), IsPrivileged(), id);
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    // NOTE: the JWT NameIdentifier is the Identity (GUID) user id, whereas Conversation.AssignedUserId
    // is the domain User.Id (int). Until an Identity->domain-user id claim is added, the agent-scoping
    // filter only works for the privileged path; unassigned conversations correctly surface to
    // managers/admins only. Assignment (and a proper domain-user-id claim) is a follow-up story.
    private int CurrentUserId() => int.TryParse(User.GetUserId(), out var id) ? id : 0;

    private bool IsPrivileged() => User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("SuperAdmin");
}
