using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Audit;
using WaslX.Application.Abstractions.Campaigns;
using WaslX.Application.Features.Campaigns.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Tenant-scoped outbound WhatsApp broadcast campaigns (FR-CMP). Resolves the audience from the
/// stored JSON filter, materialises per-recipient rows on launch and enqueues the Hangfire send
/// engine (immediately, or scheduled when a future <c>ScheduledAt</c> is set). Every query is
/// filtered by <c>TenantId</c> — a missing filter would be a cross-tenant leak.
/// </summary>
internal sealed class CampaignService(ApplicationDbContext db, IBackgroundJobClient jobs, IAuditService audit) : ICampaignService
{
    // Campaign.Status values.
    private const string Draft = "Draft";
    private const string Scheduled = "Scheduled";
    private const string Running = "Running";
    private const string Paused = "Paused";
    private const string Completed = "Completed";
    private const string Cancelled = "Cancelled";

    // CampaignRecipient.Status values.
    private const string Queued = "queued";
    private const string Sent = "sent";
    private const string Delivered = "delivered";
    private const string Read = "read";
    private const string Failed = "failed";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<Result<IReadOnlyList<CampaignResponse>>> GetAllAsync(int? tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<IReadOnlyList<CampaignResponse>>(AppErrors.NoTenantContext);

        var campaigns = await db.Campaigns.AsNoTracking()
            .Where(c => c.TenantId == tid)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, c.Name, c.Status, c.TemplateName, c.ScheduledAt, c.CreatedAt })
            .ToListAsync(cancellationToken);

        if (campaigns.Count == 0)
            return Result.Success<IReadOnlyList<CampaignResponse>>([]);

        var ids = campaigns.Select(c => c.Id).ToList();
        var recipientStatuses = await db.CampaignRecipients.AsNoTracking()
            .Where(r => ids.Contains(r.CampaignId))
            .Select(r => new { r.CampaignId, r.Status })
            .ToListAsync(cancellationToken);

        var byCampaign = recipientStatuses
            .GroupBy(r => r.CampaignId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Status).ToList());

        var responses = campaigns.Select(c =>
        {
            var counts = Tally(byCampaign.TryGetValue(c.Id, out var s) ? s : []);
            return new CampaignResponse(c.Id, c.Name, c.Status, c.TemplateName,
                counts.Audience, counts.Sent, counts.Delivered, counts.Read, counts.Failed,
                c.ScheduledAt, c.CreatedAt);
        }).ToList();

        return Result.Success<IReadOnlyList<CampaignResponse>>(responses);
    }

    public async Task<Result<CampaignDetailResponse>> GetByIdAsync(int? tenantId, int id, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<CampaignDetailResponse>(AppErrors.NoTenantContext);

        var campaign = await db.Campaigns.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tid && c.Id == id, cancellationToken);
        if (campaign is null)
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignNotFound);

        return Result.Success(await BuildDetailAsync(campaign, cancellationToken));
    }

    public async Task<Result<CampaignDetailResponse>> CreateAsync(int? tenantId, int? createdByDomainUserId, CreateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<CampaignDetailResponse>(AppErrors.NoTenantContext);

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignNameRequired);

        var templateName = request.TemplateName?.Trim() ?? string.Empty;
        if (templateName.Length == 0)
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignTemplateRequired);

        // A campaign always sends from the tenant's WhatsApp number (FK is required). Prefer a
        // connected one; fall back to any number the tenant owns so a draft can be prepared.
        var waAccountId = await ResolveWaAccountIdAsync(tid, cancellationToken);
        if (waAccountId is null)
            return Result.Failure<CampaignDetailResponse>(AppErrors.WhatsAppAccountNotFound);

        var campaign = new Campaign
        {
            TenantId = tid,
            WaAccountId = waAccountId.Value,
            CreatedBy = createdByDomainUserId ?? 0,
            Name = name,
            TemplateName = templateName,
            MessageBody = request.MessageBody?.Trim() ?? string.Empty,
            AudienceFilter = request.AudienceFilter?.Trim() ?? string.Empty,
            Status = Draft,
            ScheduledAt = request.ScheduledAt
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await BuildDetailAsync(campaign, cancellationToken));
    }

    public async Task<Result<CampaignDetailResponse>> UpdateAsync(int? tenantId, int id, UpdateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<CampaignDetailResponse>(AppErrors.NoTenantContext);

        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.TenantId == tid && c.Id == id, cancellationToken);
        if (campaign is null)
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignNotFound);
        if (campaign.Status != Draft)
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignNotEditable);

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignNameRequired);

        var templateName = request.TemplateName?.Trim() ?? string.Empty;
        if (templateName.Length == 0)
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignTemplateRequired);

        campaign.Name = name;
        campaign.TemplateName = templateName;
        campaign.MessageBody = request.MessageBody?.Trim() ?? string.Empty;
        campaign.AudienceFilter = request.AudienceFilter?.Trim() ?? string.Empty;
        campaign.ScheduledAt = request.ScheduledAt;
        campaign.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await BuildDetailAsync(campaign, cancellationToken));
    }

    public async Task<Result> DeleteAsync(int? tenantId, int id, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.TenantId == tid && c.Id == id, cancellationToken);
        if (campaign is null)
            return Result.Failure(AppErrors.CampaignNotFound);
        if (campaign.Status == Running)
            return Result.Failure(AppErrors.CampaignInvalidStatusTransition);

        // CampaignRecipient/CampaignTag → Campaign are configured Cascade, so the DB removes children.
        db.Campaigns.Remove(campaign);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public Task<Result<AudiencePreviewResponse>> PreviewAudienceAsync(int? tenantId, AudiencePreviewRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is null)
            return Task.FromResult(Result.Failure<AudiencePreviewResponse>(AppErrors.NoTenantContext));

        // The audience is an explicit uploaded phone list — the count is simply how many valid,
        // de-duplicated numbers it holds; no customer query is needed.
        var numbers = ParsePhoneNumbers(request.AudienceFilter);
        var sample = numbers.Take(20).Select(n => new AudienceSampleItem(0, n, n)).ToList();
        return Task.FromResult(Result.Success(new AudiencePreviewResponse(numbers.Count, sample)));
    }

    /// <summary>
    /// Lists the tenant's existing customers for the "pick from my contacts" campaign audience mode,
    /// optionally narrowed to those with a conversation carrying <paramref name="tagId"/> and/or a
    /// conversation active within the last <paramref name="lastContactWithinDays"/> days. Each row
    /// carries the customer's most recent conversation time so the UI can sort by recency.
    /// </summary>
    public async Task<Result<IReadOnlyList<AudienceContactDto>>> GetAudienceContactsAsync(int? tenantId, int? tagId, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<IReadOnlyList<AudienceContactDto>>(AppErrors.NoTenantContext);

        var query = db.Customers.AsNoTracking().Where(c => c.TenantId == tid);

        if (tagId is { } tg)
            query = query.Where(c => c.Conversations.Any(conv => conv.ConversationTags.Any(t => t.TagId == tg)));

        // Keep customers with a conversation last active inside the [dateFrom, dateTo] window
        // (dateTo is inclusive of its whole day). Each bound is optional.
        var from = dateFrom?.Date;
        var toExclusive = dateTo?.Date.AddDays(1);
        if (from is { } f && toExclusive is { } te)
            query = query.Where(c => c.Conversations.Any(conv => conv.LastMessageAt >= f && conv.LastMessageAt < te));
        else if (from is { } fOnly)
            query = query.Where(c => c.Conversations.Any(conv => conv.LastMessageAt >= fOnly));
        else if (toExclusive is { } teOnly)
            query = query.Where(c => c.Conversations.Any(conv => conv.LastMessageAt < teOnly));

        var rows = await query
            .Select(c => new AudienceContactDto(
                c.Id,
                c.Name,
                c.PhoneNumber,
                c.Conversations.Max(conv => (DateTime?)conv.LastMessageAt)))
            .ToListAsync(cancellationToken);

        // Order by recency in memory (most recently contacted first) and cap for the picker.
        var contacts = rows
            .OrderByDescending(x => x.LastContactAt ?? DateTime.MinValue)
            .Take(500)
            .ToList();

        return Result.Success<IReadOnlyList<AudienceContactDto>>(contacts);
    }

    public async Task<Result<CampaignDetailResponse>> LaunchAsync(int? tenantId, int id, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<CampaignDetailResponse>(AppErrors.NoTenantContext);

        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.TenantId == tid && c.Id == id, cancellationToken);
        if (campaign is null)
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignNotFound);
        if (campaign.Status is not (Draft or Scheduled))
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignInvalidStatusTransition);

        // A live send needs a CONNECTED number.
        var connectedWaId = await db.WhatsAppAccounts.AsNoTracking()
            .Where(w => w.TenantId == tid && w.Status == WhatsAppAccountStatus.Connected)
            .Select(w => (int?)w.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (connectedWaId is null)
            return Result.Failure<CampaignDetailResponse>(AppErrors.WhatsAppAccountNotFound);
        campaign.WaAccountId = connectedWaId.Value;

        // Resolve the uploaded recipient phone list → customer ids, creating a Customer for any
        // number that doesn't exist yet (so the recipient FK holds and the number becomes a reusable
        // contact — the same find-or-create the inbound webhook uses).
        var phoneNumbers = ParsePhoneNumbers(campaign.AudienceFilter);
        if (phoneNumbers.Count == 0)
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignHasNoRecipients);

        var customerIds = new List<int>(phoneNumbers.Count);
        foreach (var phone in phoneNumbers)
        {
            var customer = await WhatsAppService.FindOrCreateCustomerAsync(db, tid, phone, cancellationToken);
            customerIds.Add(customer.Id);
        }

        // Re-materialise recipients (a re-launch from Draft replaces any earlier rows).
        var existing = await db.CampaignRecipients.Where(r => r.CampaignId == campaign.Id).ToListAsync(cancellationToken);
        if (existing.Count > 0) db.CampaignRecipients.RemoveRange(existing);

        var now = DateTime.UtcNow;
        foreach (var customerId in customerIds.Distinct())
        {
            db.CampaignRecipients.Add(new CampaignRecipient
            {
                CampaignId = campaign.Id,
                CustomerId = customerId,
                Status = Queued
            });
        }

        campaign.SentCount = 0;
        campaign.DeliveredCount = 0;
        campaign.ReadCount = 0;
        campaign.FailedCount = 0;
        campaign.UpdatedAt = now;

        var isFutureSchedule = campaign.ScheduledAt is { } when && when > now;
        campaign.Status = isFutureSchedule ? Scheduled : Running;
        await db.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(tid, campaign.Id,
            isFutureSchedule ? $"Campaign scheduled for {campaign.ScheduledAt:u}" : "Campaign launched", cancellationToken);

        if (isFutureSchedule)
            jobs.Schedule<ICampaignSendJob>(j => j.SendCampaignAsync(campaign.Id, CancellationToken.None), campaign.ScheduledAt!.Value - now);
        else
            jobs.Enqueue<ICampaignSendJob>(j => j.SendCampaignAsync(campaign.Id, CancellationToken.None));

        return Result.Success(await BuildDetailAsync(campaign, cancellationToken));
    }

    public async Task<Result<CampaignDetailResponse>> PauseAsync(int? tenantId, int id, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<CampaignDetailResponse>(AppErrors.NoTenantContext);

        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.TenantId == tid && c.Id == id, cancellationToken);
        if (campaign is null)
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignNotFound);
        if (campaign.Status is not (Running or Scheduled))
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignInvalidStatusTransition);

        campaign.Status = Paused;
        campaign.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(tid, campaign.Id, "Campaign paused", cancellationToken);

        return Result.Success(await BuildDetailAsync(campaign, cancellationToken));
    }

    public async Task<Result<CampaignDetailResponse>> ResumeAsync(int? tenantId, int id, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<CampaignDetailResponse>(AppErrors.NoTenantContext);

        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.TenantId == tid && c.Id == id, cancellationToken);
        if (campaign is null)
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignNotFound);
        if (campaign.Status != Paused)
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignInvalidStatusTransition);

        campaign.Status = Running;
        campaign.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        // Re-enqueue: the job picks up whatever recipients are still queued.
        jobs.Enqueue<ICampaignSendJob>(j => j.SendCampaignAsync(campaign.Id, CancellationToken.None));

        return Result.Success(await BuildDetailAsync(campaign, cancellationToken));
    }

    public async Task<Result<CampaignDetailResponse>> CancelAsync(int? tenantId, int id, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<CampaignDetailResponse>(AppErrors.NoTenantContext);

        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.TenantId == tid && c.Id == id, cancellationToken);
        if (campaign is null)
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignNotFound);
        if (campaign.Status is Completed or Cancelled)
            return Result.Failure<CampaignDetailResponse>(AppErrors.CampaignInvalidStatusTransition);

        campaign.Status = Cancelled;
        campaign.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(tid, campaign.Id, "Campaign cancelled", cancellationToken);

        return Result.Success(await BuildDetailAsync(campaign, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<CampaignRecipientResponse>>> GetRecipientsAsync(int? tenantId, int id, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<IReadOnlyList<CampaignRecipientResponse>>(AppErrors.NoTenantContext);

        var exists = await db.Campaigns.AnyAsync(c => c.TenantId == tid && c.Id == id, cancellationToken);
        if (!exists)
            return Result.Failure<IReadOnlyList<CampaignRecipientResponse>>(AppErrors.CampaignNotFound);

        var recipients = await db.CampaignRecipients.AsNoTracking()
            .Where(r => r.CampaignId == id)
            .OrderBy(r => r.Id)
            .Select(r => new CampaignRecipientResponse(
                r.Id, r.CustomerId, r.Customer.Name, r.Customer.PhoneNumber,
                r.Status, r.Error, r.SentAt, r.DeliveredAt, r.ReadAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CampaignRecipientResponse>>(recipients);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends an immutable audit entry for a campaign lifecycle transition. Best-effort: the
    /// primary action has already been persisted, so a failure here must never surface to the caller.
    /// </summary>
    private async Task WriteAuditAsync(int tenantId, int campaignId, string details, CancellationToken cancellationToken)
    {
        try
        {
            await audit.WriteAsync(tenantId, null, AuditAction.StatusChanged, "Campaign", campaignId.ToString(), details, cancellationToken);
        }
        catch
        {
            // Swallow: the campaign action already succeeded and was persisted.
        }
    }

    private async Task<int?> ResolveWaAccountIdAsync(int tid, CancellationToken cancellationToken)
    {
        var connected = await db.WhatsAppAccounts.AsNoTracking()
            .Where(w => w.TenantId == tid && w.Status == WhatsAppAccountStatus.Connected)
            .Select(w => (int?)w.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (connected is not null)
            return connected;

        return await db.WhatsAppAccounts.AsNoTracking()
            .Where(w => w.TenantId == tid)
            .Select(w => (int?)w.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Extracts the explicit recipient phone numbers from a campaign's stored audience filter
    /// (JSON <c>{ "phoneNumbers": ["201..."] }</c>), normalised to digits-only (Meta wa_id form)
    /// and de-duplicated. Numbers with fewer than 8 digits are dropped as invalid. A blank or
    /// unparseable filter yields an empty list (→ the launch guard reports "no recipients").
    /// </summary>
    private static List<string> ParsePhoneNumbers(string? audienceFilter)
    {
        var filter = ParseFilter(audienceFilter);
        if (filter?.PhoneNumbers is not { Count: > 0 } raw)
            return [];

        var seen = new HashSet<string>();
        var result = new List<string>();
        foreach (var candidate in raw)
        {
            var normalized = NormalizePhone(candidate);
            if (normalized.Length >= 8 && seen.Add(normalized))
                result.Add(normalized);
        }
        return result;
    }

    /// <summary>Strips everything but digits (+, spaces, dashes, parens) to match Meta's wa_id form.</summary>
    private static string NormalizePhone(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? string.Empty : new string(raw.Where(char.IsDigit).ToArray());

    private static AudienceFilterJson? ParseFilter(string? audienceFilter)
    {
        if (string.IsNullOrWhiteSpace(audienceFilter))
            return null;
        try
        {
            return JsonSerializer.Deserialize<AudienceFilterJson>(audienceFilter, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<CampaignDetailResponse> BuildDetailAsync(Campaign campaign, CancellationToken cancellationToken)
    {
        var statuses = await db.CampaignRecipients.AsNoTracking()
            .Where(r => r.CampaignId == campaign.Id)
            .Select(r => r.Status)
            .ToListAsync(cancellationToken);
        var counts = Tally(statuses);

        return new CampaignDetailResponse(
            campaign.Id, campaign.Name, campaign.Status, campaign.TemplateName,
            campaign.MessageBody, campaign.AudienceFilter,
            counts.Audience, counts.Sent, counts.Delivered, counts.Read, counts.Failed,
            campaign.ScheduledAt, campaign.CreatedAt);
    }

    /// <summary>
    /// Funnel counts derived from per-recipient status (analytics source of truth). A recipient
    /// that reached a later stage still counts toward the earlier ones (read ⊂ delivered ⊂ sent).
    /// </summary>
    private static (int Audience, int Sent, int Delivered, int Read, int Failed) Tally(IReadOnlyCollection<string> statuses)
    {
        int sent = 0, delivered = 0, read = 0, failed = 0;
        foreach (var s in statuses)
        {
            switch (s)
            {
                case Read: read++; delivered++; sent++; break;
                case Delivered: delivered++; sent++; break;
                case Sent: sent++; break;
                case Failed: failed++; break;
            }
        }
        return (statuses.Count, sent, delivered, read, failed);
    }

    private sealed class AudienceFilterJson
    {
        // The campaign audience is an explicit uploaded recipient list (from a CSV/Excel file),
        // stored as { "phoneNumbers": ["201...", ...] }. No customer-attribute predicates.
        public List<string>? PhoneNumbers { get; set; }
    }
}
