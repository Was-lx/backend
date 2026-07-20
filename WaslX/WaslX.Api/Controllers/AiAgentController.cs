using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Features.AiAgent.Dtos;

namespace WaslX.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/ai")]
public class AiAgentController(IAiAgentSettingsService settingsService, ILogger<AiAgentController> logger) : ControllerBase
{
    [HttpGet("agent-settings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var result = await settingsService.GetSettingsAsync(tenantId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("agent-settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateAiAgentSettingsRequest request, CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var userId = User.GetDomainUserId();

        var result = await settingsService.UpdateSettingsAsync(tenantId.Value, request, userId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("agent-knowledge")]
    public async Task<IActionResult> GetKnowledge(CancellationToken cancellationToken)
    {
        // Placeholder for future implementation (handled by RAG layer but exposed here)
        return Ok(new List<AiKnowledgeFileDto>());
    }

    [HttpGet("agent-conversations")]
    public async Task<IActionResult> GetHandledConversations(CancellationToken cancellationToken)
    {
        // In a real implementation this would query the ConversationService for HandledByAi = true
        return Ok(new List<AiHandledConversationDto>());
    }
}
