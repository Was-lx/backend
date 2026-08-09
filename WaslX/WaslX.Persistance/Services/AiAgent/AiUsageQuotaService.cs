using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.AI;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services.AiAgent;

internal sealed class AiUsageQuotaService(ApplicationDbContext db) : IAiUsageQuotaService
{
    public async Task<bool> IsWithinQuotaAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var aiQuota = await db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.Plan.AiQuota)
            .FirstOrDefaultAsync(cancellationToken);

        // No plan resolved or no cap configured — don't block on a misconfigured/missing plan;
        // that's a data problem, not a reason to silently stop serving every customer.
        if (aiQuota <= 0)
            return true;

        var periodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Two independent LLM-costing operations happen per inbound message today (classification +
        // AI reply) — count both against the same monthly cap so the quota reflects actual spend.
        var classifications = await db.Set<MessageClassification>().AsNoTracking()
            .CountAsync(c => c.TenantId == tenantId && c.CreatedAt >= periodStart, cancellationToken);

        var aiReplies = await db.Messages.AsNoTracking()
            .CountAsync(m => m.SenderType == SenderType.AI && m.Conversation.TenantId == tenantId && m.CreatedAt >= periodStart, cancellationToken);

        return classifications + aiReplies < aiQuota;
    }
}
