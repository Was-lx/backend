using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Customers;
using WaslX.Application.Features.Customers.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// The Contacts directory (tenant-scoped): paginated + filtered customer listing over the operational
/// tables. Read-only. Correlated sub-selects are kept EF-translatable; tags and assigned-user names
/// are stitched in a light follow-up query to avoid a heavy nested projection. (CSV/Excel export is
/// produced on the client from the returned rows.)
/// </summary>
internal sealed class CustomerService(ApplicationDbContext db) : ICustomerService
{
    // Applies every filter to the tenant-scoped customer query (no ordering / paging).
    // assignedUserId arrives as an Identity GUID and is resolved to the numeric domain user id first.
    private IQueryable<Customer> BuildFilteredQuery(int tid, CustomerListFilter f, int? resolvedAssignedUserId)
    {
        var q = db.Customers.AsNoTracking().Where(c => c.TenantId == tid);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var like = $"%{f.Search.Trim()}%";
            q = q.Where(c => EF.Functions.Like(c.Name, like) || EF.Functions.Like(c.PhoneNumber, like));
        }

        if (f.TagId is { } tg)
            q = q.Where(c => c.Conversations.Any(conv => conv.ConversationTags.Any(x => x.TagId == tg)));

        if (resolvedAssignedUserId is { } au)
            q = q.Where(c => c.Conversations.Any(conv => conv.AssignedUserId == au));

        // Last-contacted date window (dateTo inclusive of the whole day). Each bound optional.
        var from = f.DateFrom?.Date;
        var toExclusive = f.DateTo?.Date.AddDays(1);
        if (from is { } lo && toExclusive is { } hi)
            q = q.Where(c => c.Conversations.Any(conv => conv.LastMessageAt >= lo && conv.LastMessageAt < hi));
        else if (from is { } loOnly)
            q = q.Where(c => c.Conversations.Any(conv => conv.LastMessageAt >= loOnly));
        else if (toExclusive is { } hiOnly)
            q = q.Where(c => c.Conversations.Any(conv => conv.LastMessageAt < hiOnly));

        return q;
    }

    /// <summary>Maps the Identity (GUID) assignee filter to the numeric domain user id it stores as.</summary>
    private async Task<int?> ResolveAssigneeAsync(int tid, string? assignedUserGuid, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(assignedUserGuid))
            return null;

        var email = await db.Set<ApplicationUser>().AsNoTracking()
            .Where(au => au.Id == assignedUserGuid)
            .Select(au => au.Email)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(email))
            return null;

        return await db.Users.AsNoTracking()
            .Where(u => u.TenantId == tid && u.Email == email)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(ct);
    }

    private sealed class CustomerRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int ConversationCount { get; set; }
        public DateTime? LastContactAt { get; set; }
        public int? AssignedUserId { get; set; }
    }

    private Task<List<CustomerRow>> ProjectRowsAsync(IQueryable<Customer> ordered, CancellationToken ct) =>
        ordered.Select(c => new CustomerRow
        {
            Id = c.Id,
            Name = c.Name,
            Phone = c.PhoneNumber,
            ConversationCount = c.Conversations.Count,
            LastContactAt = c.Conversations.Max(conv => (DateTime?)conv.LastMessageAt),
            // Assigned agent of the most-recently-active conversation (best "owner" signal).
            AssignedUserId = c.Conversations
                .OrderByDescending(conv => conv.LastMessageAt)
                .Select(conv => conv.AssignedUserId)
                .FirstOrDefault(),
        }).ToListAsync(ct);

    /// <summary>Fetches tags + assigned-user names for a page of customers and assembles the DTOs.</summary>
    private async Task<List<CustomerListItemDto>> ToDtosAsync(int tid, List<CustomerRow> rows, CancellationToken ct)
    {
        var ids = rows.Select(r => r.Id).ToList();

        var tagRows = ids.Count == 0
            ? new List<TagPair>()
            : await db.Conversations.AsNoTracking()
                .Where(conv => ids.Contains(conv.CustomerId))
                .SelectMany(conv => conv.ConversationTags.Select(x => new TagPair(conv.CustomerId, x.Tag.Name)))
                .ToListAsync(ct);
        var tagsByCustomer = tagRows
            .GroupBy(x => x.CustomerId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Name).Distinct().ToList());

        var userIds = rows.Where(r => r.AssignedUserId != null).Select(r => r.AssignedUserId!.Value).Distinct().ToList();
        var userNames = userIds.Count == 0
            ? new Dictionary<int, string>()
            : await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        return rows.Select(r => new CustomerListItemDto(
            r.Id, r.Name, r.Phone,
            r.ConversationCount, r.LastContactAt,
            r.AssignedUserId,
            r.AssignedUserId != null && userNames.TryGetValue(r.AssignedUserId.Value, out var un) ? un : null,
            tagsByCustomer.TryGetValue(r.Id, out var tgs) ? tgs : Array.Empty<string>())).ToList();
    }

    private readonly record struct TagPair(int CustomerId, string Name);

    public async Task<Result<CustomerListResponse>> GetCustomersAsync(int? tenantId, CustomerListFilter filter, CancellationToken ct = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<CustomerListResponse>(AppErrors.NoTenantContext);

        var page = filter.Page < 1 ? 1 : filter.Page;
        // Cap at 2000 so the client can pull the full filtered set in one call for CSV/Excel export.
        var pageSize = Math.Clamp(filter.PageSize, 1, 2000);

        var resolvedAssignee = await ResolveAssigneeAsync(tid, filter.AssignedUserId, ct);
        var q = BuildFilteredQuery(tid, filter, resolvedAssignee);
        var total = await q.CountAsync(ct);

        var ordered = q
            .OrderByDescending(c => c.Conversations.Max(conv => (DateTime?)conv.LastMessageAt))
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var rows = await ProjectRowsAsync(ordered, ct);
        var items = await ToDtosAsync(tid, rows, ct);

        return Result.Success(new CustomerListResponse(items, total));
    }
}
