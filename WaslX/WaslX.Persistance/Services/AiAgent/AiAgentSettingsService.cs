using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Features.AiAgent.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services.AiAgent;

public class AiAgentSettingsService(ApplicationDbContext db) : IAiAgentSettingsService
{
    public async Task<Result<AiAgentSettingsResponse>> GetSettingsAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var settings = await db.TenantAiAgentSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
            
        var wabaAccounts = await db.WhatsAppAccounts
            .Where(w => w.TenantId == tenantId)
            .Select(w => w.Id)
            .ToListAsync(cancellationToken);

        var numberSettings = await db.AiAgentNumberSettings
            .Where(n => wabaAccounts.Contains(n.WhatsAppAccountId))
            .ToListAsync(cancellationToken);

        if (settings == null)
        {
            settings = new TenantAiAgentSettings
            {
                TenantId = tenantId,
                Enabled = false,
                PersonaName = "WaslX AI Assistant",
                ToneInstructions = "Professional, helpful, and concise.",
                HandoffThreshold = 0.5m
            };
            db.TenantAiAgentSettings.Add(settings);
            await db.SaveChangesAsync(cancellationToken);
        }

        var numberDtos = numberSettings.Select(n => new AiAgentNumberSettingsDto(
            n.WhatsAppAccountId,
            n.Enabled,
            n.AutoReplyEnabled,
            n.AllowOutsideWindow,
            n.MaxConversationMessages)).ToList();

        return Result.Success(new AiAgentSettingsResponse(
            settings.Enabled,
            numberDtos,
            settings.PersonaName,
            settings.ToneInstructions,
            settings.HandoffThreshold));
    }

    public async Task<Result<AiAgentSettingsResponse>> UpdateSettingsAsync(int tenantId, UpdateAiAgentSettingsRequest request, int? updatedByUserId, CancellationToken cancellationToken = default)
    {
        var settings = await db.TenantAiAgentSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

        if (settings == null)
        {
            settings = new TenantAiAgentSettings { TenantId = tenantId };
            db.TenantAiAgentSettings.Add(settings);
        }

        settings.Enabled = request.Enabled;
        settings.PersonaName = request.PersonaName;
        settings.ToneInstructions = request.ToneInstructions;
        settings.HandoffThreshold = request.HandoffThreshold;
        settings.UpdatedAtUtc = DateTime.UtcNow;
        settings.UpdatedByUserId = updatedByUserId;

        var wabaAccounts = await db.WhatsAppAccounts
            .Where(w => w.TenantId == tenantId)
            .Select(w => w.Id)
            .ToListAsync(cancellationToken);

        var existingNumberSettings = await db.AiAgentNumberSettings
            .Where(n => wabaAccounts.Contains(n.WhatsAppAccountId))
            .ToListAsync(cancellationToken);

        foreach (var numReq in request.PerNumber)
        {
            if (!wabaAccounts.Contains(numReq.WhatsAppAccountId)) continue;

            var existing = existingNumberSettings.FirstOrDefault(n => n.WhatsAppAccountId == numReq.WhatsAppAccountId);
            if (existing == null)
            {
                existing = new AiAgentNumberSettings { WhatsAppAccountId = numReq.WhatsAppAccountId };
                db.AiAgentNumberSettings.Add(existing);
            }

            existing.Enabled = numReq.Enabled;
            existing.AutoReplyEnabled = numReq.AutoReplyEnabled;
            existing.AllowOutsideWindow = numReq.AllowOutsideWindow;
            existing.MaxConversationMessages = numReq.MaxConversationMessages;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            existing.UpdatedByUserId = updatedByUserId;
        }

        await db.SaveChangesAsync(cancellationToken);

        var finalNumberSettings = await db.AiAgentNumberSettings
            .Where(n => wabaAccounts.Contains(n.WhatsAppAccountId))
            .ToListAsync(cancellationToken);

        var numberDtos = finalNumberSettings.Select(n => new AiAgentNumberSettingsDto(
            n.WhatsAppAccountId,
            n.Enabled,
            n.AutoReplyEnabled,
            n.AllowOutsideWindow,
            n.MaxConversationMessages)).ToList();

        return Result.Success(new AiAgentSettingsResponse(
            settings.Enabled,
            numberDtos,
            settings.PersonaName,
            settings.ToneInstructions,
            settings.HandoffThreshold));
    }
}
