using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Channels;
using WaslX.Application.Features.Channels.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class ChannelService(ApplicationDbContext db) : IChannelService
{
    public async Task<Result<IReadOnlyList<ChannelResponse>>> GetAllAsync(int? tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<IReadOnlyList<ChannelResponse>>(AppErrors.NoTenantContext);

        var channels = await db.Channels.AsNoTracking()
            .Where(c => c.TenantId == tid)
            .OrderBy(c => c.Name)
            .Select(c => new ChannelResponse(
                c.Id,
                c.Name,
                c.Description,
                c.ChannelWhatsAppAccounts.Select(l => new ChannelWhatsAppSummary(
                    l.WhatsAppAccountId, l.WhatsAppAccount.PhoneNumber, l.WhatsAppAccount.PlatformName)).ToList(),
                c.AgentChannelAccesses.Count))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ChannelResponse>>(channels);
    }

    public async Task<Result<ChannelResponse>> GetByIdAsync(int? tenantId, int id, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<ChannelResponse>(AppErrors.NoTenantContext);

        var channel = await db.Channels.AsNoTracking()
            .Where(c => c.TenantId == tid && c.Id == id)
            .Select(c => new ChannelResponse(
                c.Id,
                c.Name,
                c.Description,
                c.ChannelWhatsAppAccounts.Select(l => new ChannelWhatsAppSummary(
                    l.WhatsAppAccountId, l.WhatsAppAccount.PhoneNumber, l.WhatsAppAccount.PlatformName)).ToList(),
                c.AgentChannelAccesses.Count))
            .FirstOrDefaultAsync(cancellationToken);

        return channel is null
            ? Result.Failure<ChannelResponse>(AppErrors.ChannelNotFound)
            : Result.Success(channel);
    }

    public async Task<Result<ChannelResponse>> CreateAsync(int? tenantId, UpsertChannelRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<ChannelResponse>(AppErrors.NoTenantContext);

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return Result.Failure<ChannelResponse>(AppErrors.ChannelNameRequired);
        if (await db.Channels.AnyAsync(c => c.TenantId == tid && c.Name == name, cancellationToken))
            return Result.Failure<ChannelResponse>(AppErrors.DuplicateChannelName);

        var channel = new Channel { TenantId = tid, Name = name, Description = request.Description?.Trim() };
        db.Channels.Add(channel);
        await db.SaveChangesAsync(cancellationToken);

        await ReconcileWhatsAppLinksAsync(channel.Id, tid, request.WhatsAppAccountIds, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(tid, channel.Id, cancellationToken);
    }

    public async Task<Result<ChannelResponse>> UpdateAsync(int? tenantId, int id, UpsertChannelRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<ChannelResponse>(AppErrors.NoTenantContext);

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.TenantId == tid && c.Id == id, cancellationToken);
        if (channel is null)
            return Result.Failure<ChannelResponse>(AppErrors.ChannelNotFound);

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return Result.Failure<ChannelResponse>(AppErrors.ChannelNameRequired);
        if (name != channel.Name && await db.Channels.AnyAsync(c => c.TenantId == tid && c.Name == name && c.Id != id, cancellationToken))
            return Result.Failure<ChannelResponse>(AppErrors.DuplicateChannelName);

        channel.Name = name;
        channel.Description = request.Description?.Trim();
        channel.UpdatedAt = DateTime.UtcNow;

        await ReconcileWhatsAppLinksAsync(channel.Id, tid, request.WhatsAppAccountIds, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(tid, channel.Id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int? tenantId, int id, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.TenantId == tid && c.Id == id, cancellationToken);
        if (channel is null)
            return Result.Failure(AppErrors.ChannelNotFound);

        // Restrict FKs: clear the join rows before removing the channel.
        var links = await db.ChannelWhatsAppAccounts.Where(l => l.ChannelId == id).ToListAsync(cancellationToken);
        var accesses = await db.AgentChannelAccesses.Where(a => a.ChannelId == id).ToListAsync(cancellationToken);
        db.ChannelWhatsAppAccounts.RemoveRange(links);
        db.AgentChannelAccesses.RemoveRange(accesses);
        db.Channels.Remove(channel);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>Makes the channel's WhatsApp links match the requested set (tenant's numbers only). Does not save.</summary>
    private async Task ReconcileWhatsAppLinksAsync(int channelId, int tenantId, IReadOnlyList<int>? requestedIds, CancellationToken cancellationToken)
    {
        var desired = requestedIds is null ? new HashSet<int>() : requestedIds.ToHashSet();

        // Keep only numbers that actually belong to this tenant.
        if (desired.Count > 0)
        {
            var owned = await db.WhatsAppAccounts
                .Where(w => w.TenantId == tenantId && desired.Contains(w.Id))
                .Select(w => w.Id)
                .ToListAsync(cancellationToken);
            desired = owned.ToHashSet();
        }

        var existing = await db.ChannelWhatsAppAccounts.Where(l => l.ChannelId == channelId).ToListAsync(cancellationToken);
        var existingIds = existing.Select(l => l.WhatsAppAccountId).ToHashSet();

        var toRemove = existing.Where(l => !desired.Contains(l.WhatsAppAccountId)).ToList();
        if (toRemove.Count > 0) db.ChannelWhatsAppAccounts.RemoveRange(toRemove);

        foreach (var waId in desired.Where(d => !existingIds.Contains(d)))
            db.ChannelWhatsAppAccounts.Add(new ChannelWhatsAppAccount { ChannelId = channelId, WhatsAppAccountId = waId });
    }
}
