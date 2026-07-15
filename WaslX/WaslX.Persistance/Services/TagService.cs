using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Tags;
using WaslX.Application.Features.Tags.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class TagService(ApplicationDbContext db) : ITagService
{
    public async Task<Result<IReadOnlyList<TagResponse>>> GetAllAsync(int? tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<IReadOnlyList<TagResponse>>(AppErrors.NoTenantContext);

        var tags = await db.Tags.AsNoTracking()
            .Where(t => t.TenantId == tid)
            .OrderBy(t => t.Name)
            .Select(t => new TagResponse(t.Id, t.Name, t.Color, t.Description))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TagResponse>>(tags);
    }

    public async Task<Result<TagResponse>> CreateAsync(int? tenantId, UpsertTagRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<TagResponse>(AppErrors.NoTenantContext);

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return Result.Failure<TagResponse>(AppErrors.TagNameRequired);
        if (await db.Tags.AnyAsync(t => t.TenantId == tid && t.Name == name, cancellationToken))
            return Result.Failure<TagResponse>(AppErrors.DuplicateTagName);

        var tag = new Tag
        {
            TenantId = tid,
            Name = name,
            Color = request.Color?.Trim() ?? string.Empty,
            Description = request.Description?.Trim() ?? string.Empty
        };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new TagResponse(tag.Id, tag.Name, tag.Color, tag.Description));
    }

    public async Task<Result<TagResponse>> UpdateAsync(int? tenantId, int id, UpsertTagRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<TagResponse>(AppErrors.NoTenantContext);

        var tag = await db.Tags.FirstOrDefaultAsync(t => t.TenantId == tid && t.Id == id, cancellationToken);
        if (tag is null)
            return Result.Failure<TagResponse>(AppErrors.TagNotFound);

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return Result.Failure<TagResponse>(AppErrors.TagNameRequired);
        if (name != tag.Name && await db.Tags.AnyAsync(t => t.TenantId == tid && t.Name == name && t.Id != id, cancellationToken))
            return Result.Failure<TagResponse>(AppErrors.DuplicateTagName);

        tag.Name = name;
        tag.Color = request.Color?.Trim() ?? string.Empty;
        tag.Description = request.Description?.Trim() ?? string.Empty;
        tag.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new TagResponse(tag.Id, tag.Name, tag.Color, tag.Description));
    }

    public async Task<Result> DeleteAsync(int? tenantId, int id, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var tag = await db.Tags.FirstOrDefaultAsync(t => t.TenantId == tid && t.Id == id, cancellationToken);
        if (tag is null)
            return Result.Failure(AppErrors.TagNotFound);

        // Clear join rows before removing the tag. ConversationTag -> Tag cascades, but
        // CampaignTag -> Tag is Restrict, so it MUST be cleared explicitly or the delete throws an FK error.
        var convLinks = await db.ConversationTags.Where(ct => ct.TagId == id).ToListAsync(cancellationToken);
        if (convLinks.Count > 0) db.ConversationTags.RemoveRange(convLinks);
        var campLinks = await db.CampaignTags.Where(ct => ct.TagId == id).ToListAsync(cancellationToken);
        if (campLinks.Count > 0) db.CampaignTags.RemoveRange(campLinks);
        db.Tags.Remove(tag);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ApplyToConversationAsync(int? tenantId, int conversationId, int tagId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var conversationExists = await db.Conversations
            .AnyAsync(c => c.TenantId == tid && c.Id == conversationId, cancellationToken);
        if (!conversationExists)
            return Result.Failure(AppErrors.ConversationNotFound);

        var tagExists = await db.Tags.AnyAsync(t => t.TenantId == tid && t.Id == tagId, cancellationToken);
        if (!tagExists)
            return Result.Failure(AppErrors.TagNotFound);

        var alreadyApplied = await db.ConversationTags
            .AnyAsync(ct => ct.ConversationId == conversationId && ct.TagId == tagId, cancellationToken);
        if (!alreadyApplied)
        {
            db.ConversationTags.Add(new ConversationTag { ConversationId = conversationId, TagId = tagId });
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> RemoveFromConversationAsync(int? tenantId, int conversationId, int tagId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var conversationExists = await db.Conversations
            .AnyAsync(c => c.TenantId == tid && c.Id == conversationId, cancellationToken);
        if (!conversationExists)
            return Result.Failure(AppErrors.ConversationNotFound);

        var tagExists = await db.Tags.AnyAsync(t => t.TenantId == tid && t.Id == tagId, cancellationToken);
        if (!tagExists)
            return Result.Failure(AppErrors.TagNotFound);

        var link = await db.ConversationTags
            .FirstOrDefaultAsync(ct => ct.ConversationId == conversationId && ct.TagId == tagId, cancellationToken);
        if (link is not null)
        {
            db.ConversationTags.Remove(link);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
