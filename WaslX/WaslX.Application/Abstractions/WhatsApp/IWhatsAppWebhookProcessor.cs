namespace WaslX.Application.Abstractions.WhatsApp;

/// <summary>
/// Processes a persisted webhook log row out-of-band (Hangfire). Reloads the raw payload,
/// resolves the tenant, and materialises inbound messages / status updates into the
/// conversation model. Marks the log row Processed (or records the error).
/// </summary>
public interface IWhatsAppWebhookProcessor
{
    Task ProcessAsync(int webhookLogId, CancellationToken cancellationToken = default);
}
