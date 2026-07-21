namespace WaslX.Application.Abstractions.Campaigns;

/// <summary>
/// Hangfire background send engine for a campaign. Loads the campaign's queued recipients and
/// sends each via the WhatsApp template send service, updating per-recipient status/timestamps
/// and the campaign's aggregate counts. Best-effort per recipient: one failed send is recorded
/// and skipped, never rethrown; a mid-run Pause/Cancel stops the sweep.
/// </summary>
public interface ICampaignSendJob
{
    Task SendCampaignAsync(int campaignId, CancellationToken cancellationToken = default);
}
