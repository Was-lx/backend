using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Contracts;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Conversations.GetConversationMessages;
using WaslX.Application.Features.Conversations.GetConversations;
using WaslX.Application.Features.Conversations.SendConversationMessage;

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

    // NOTE: the JWT NameIdentifier is the Identity (GUID) user id, whereas Conversation.AssignedUserId
    // is the domain User.Id (int). Until an Identity->domain-user id claim is added, the agent-scoping
    // filter only works for the privileged path; unassigned conversations correctly surface to
    // managers/admins only. Assignment (and a proper domain-user-id claim) is a follow-up story.
    private int CurrentUserId() => int.TryParse(User.GetUserId(), out var id) ? id : 0;

    private bool IsPrivileged() => User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("SuperAdmin");
}
