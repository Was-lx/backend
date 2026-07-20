using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;

namespace WaslX.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/conversations")]
public class AiAgentConversationController(IUnitOfWork unitOfWork, IInboxRealtimeNotifier notifier) : ControllerBase
{
    [HttpPost("{id}/ai/takeover")]
    public async Task<IActionResult> TakeOver(int id, CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var conversationRepo = unitOfWork.GetRepository<Conversation, int>();
        var conversation = await conversationRepo.GetByIdAsync(id, cancellationToken);
        
        if (conversation == null || conversation.TenantId != tenantId.Value) 
            return NotFound();

        conversation.HandledByAi = false;
        await unitOfWork.CompleteAsync();

        await notifier.ConversationTakenOverAsync(tenantId.Value, new ConversationTakenOverPayload(id, DateTime.UtcNow), cancellationToken);
        
        return Ok();
    }
}
