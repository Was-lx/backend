using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.Campaigns;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Hangfire send engine for a campaign. Loads the campaign's queued recipients and sends each via
/// the approved WhatsApp template, updating per-recipient status/timestamps and the campaign's
/// aggregate counts. Defensive: a per-recipient failure is recorded (Status=failed + Error) and the
/// sweep continues; a mid-run Pause/Cancel stops it; the whole job never throws out to Hangfire.
/// </summary>
internal sealed class CampaignSendJob(
    ApplicationDbContext db,
    IWhatsAppService whatsApp,
    IWhatsAppTemplateService templateService,
    ILogger<CampaignSendJob> logger) : ICampaignSendJob
{
    private const string Running = "Running";
    private const string Scheduled = "Scheduled";
    private const string Paused = "Paused";
    private const string Cancelled = "Cancelled";
    private const string Completed = "Completed";

    private const string Queued = "queued";
    private const string Sent = "sent";
    private const string Failed = "failed";

    private const string DefaultLanguage = "en_US";

    public async Task SendCampaignAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);
        if (campaign is null)
        {
            logger.LogWarning("Campaign send skipped: campaign {CampaignId} no longer exists", campaignId);
            return;
        }

        // A scheduled run flips the campaign live; anything not Running/Scheduled is a no-op.
        if (campaign.Status == Scheduled)
        {
            campaign.Status = Running;
            campaign.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        if (campaign.Status != Running)
        {
            logger.LogInformation("Campaign {CampaignId} not in a runnable state ({Status}); nothing to send", campaignId, campaign.Status);
            return;
        }

        // Resolve the template's ACTUAL language from Meta — the same source the campaign builder lists
        // from — instead of the local TemplateReviews table, whose status/language only updates via the
        // message_template_status_update webhook and is often stale or absent. Relying on it made the
        // send fall back to en_US and get rejected for "en" templates ("template does not exist in en_US").
        // Falls back to the locally-mirrored approved review, then the default, if Meta is unavailable.
        var languageCode = DefaultLanguage;
        var templatesResult = await templateService.GetTemplatesAsync(campaign.TenantId, "APPROVED", cancellationToken);
        var metaLanguage = templatesResult.IsSuccess
            ? templatesResult.Value.FirstOrDefault(t => string.Equals(t.Name, campaign.TemplateName, StringComparison.OrdinalIgnoreCase))?.Language
            : null;
        if (!string.IsNullOrWhiteSpace(metaLanguage))
        {
            languageCode = metaLanguage;
        }
        else
        {
            var localLanguage = await db.TemplateReviews.AsNoTracking()
                .Where(t => t.TenantId == campaign.TenantId
                    && t.MessageTemplateName == campaign.TemplateName
                    && t.Status == "APPROVED")
                .Select(t => t.Language)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(localLanguage))
                languageCode = localLanguage;
        }

        var recipients = await db.CampaignRecipients
            .Where(r => r.CampaignId == campaignId && r.Status == Queued)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

        foreach (var recipient in recipients)
        {
            // Honour a Pause/Cancel issued from another request mid-run.
            var liveStatus = await db.Campaigns.AsNoTracking()
                .Where(c => c.Id == campaignId)
                .Select(c => c.Status)
                .FirstOrDefaultAsync(cancellationToken);
            if (liveStatus is Paused or Cancelled)
            {
                logger.LogInformation("Campaign {CampaignId} {Status} mid-run; stopping the send sweep", campaignId, liveStatus);
                return;
            }

            var phone = await db.Customers.AsNoTracking()
                .Where(c => c.Id == recipient.CustomerId && c.TenantId == campaign.TenantId)
                .Select(c => c.PhoneNumber)
                .FirstOrDefaultAsync(cancellationToken);

            var now = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(phone))
            {
                recipient.Status = Failed;
                recipient.Error = "Customer phone number not found";
                recipient.UpdatedAt = now;
                campaign.FailedCount++;
                await db.SaveChangesAsync(cancellationToken);
                continue;
            }

            try
            {
                var send = await whatsApp.SendTemplateAsync(
                    campaign.TenantId, phone, campaign.TemplateName, languageCode,
                    parameters: null, senderUserId: null, cancellationToken: cancellationToken);

                if (send.IsSuccess)
                {
                    recipient.Status = Sent;
                    recipient.SentAt = now;
                    recipient.Error = null;
                    // Store Meta's message id so the status webhook can later flip this recipient to
                    // delivered/read/failed by matching on it.
                    recipient.WhatsAppMessageId = send.Value.WhatsAppMessageId;
                    campaign.SentCount++;
                }
                else
                {
                    recipient.Status = Failed;
                    recipient.Error = Truncate(send.Error.Description, 500);
                    campaign.FailedCount++;
                }
            }
            catch (Exception ex)
            {
                // One bad recipient must never abort the whole campaign run.
                recipient.Status = Failed;
                recipient.Error = Truncate(ex.Message, 500);
                campaign.FailedCount++;
                logger.LogError(ex, "Campaign {CampaignId}: failed to send to recipient {RecipientId}", campaignId, recipient.Id);
            }

            recipient.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        // Complete the campaign only if it is still Running and nothing is left queued.
        var finalStatus = await db.Campaigns.AsNoTracking()
            .Where(c => c.Id == campaignId)
            .Select(c => c.Status)
            .FirstOrDefaultAsync(cancellationToken);
        if (finalStatus == Running)
        {
            var anyQueued = await db.CampaignRecipients
                .AnyAsync(r => r.CampaignId == campaignId && r.Status == Queued, cancellationToken);
            if (!anyQueued)
            {
                campaign.Status = Completed;
                campaign.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Campaign {CampaignId} completed", campaignId);
            }
        }
    }

    private static string Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.Length <= max ? value : value[..max];
}
