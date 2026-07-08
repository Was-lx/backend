using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Hangfire-invoked processor that turns a stored raw webhook payload into inbound messages
/// and message-status updates. Resolves the tenant from the payload's phone_number_id.
/// </summary>
internal sealed class WhatsAppWebhookProcessor(
    ApplicationDbContext db,
    ILogger<WhatsAppWebhookProcessor> logger) : IWhatsAppWebhookProcessor
{
    public async Task ProcessAsync(int webhookLogId, CancellationToken cancellationToken = default)
    {
        var log = await db.WhatsAppWebhookLogs.FirstOrDefaultAsync(x => x.Id == webhookLogId, cancellationToken);
        if (log is null)
        {
            logger.LogWarning("WhatsApp webhook log {LogId} not found", webhookLogId);
            return;
        }

        if (log.Processed)
            return;

        try
        {
            using var doc = JsonDocument.Parse(log.RawPayload);
            var root = doc.RootElement;

            if (root.TryGetProperty("entry", out var entries) && entries.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in entries.EnumerateArray())
                {
                    if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var change in changes.EnumerateArray())
                    {
                        if (change.TryGetProperty("value", out var value))
                            await ProcessValueAsync(value, cancellationToken);
                    }
                }
            }

            log.Processed = true;
            log.ProcessingError = null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process WhatsApp webhook log {LogId}", webhookLogId);
            log.ProcessingError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
        }
        finally
        {
            log.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessValueAsync(JsonElement value, CancellationToken cancellationToken)
    {
        // Resolve the tenant via the business phone number id in metadata.
        if (!value.TryGetProperty("metadata", out var metadata) ||
            !metadata.TryGetProperty("phone_number_id", out var phoneNumberIdEl))
            return;

        var phoneNumberId = phoneNumberIdEl.GetString();
        if (string.IsNullOrWhiteSpace(phoneNumberId))
            return;

        var account = await db.WhatsAppAccounts.FirstOrDefaultAsync(x => x.PhoneNumberId == phoneNumberId, cancellationToken);
        if (account is null)
        {
            logger.LogWarning("No WhatsApp account matches phone_number_id {PhoneNumberId}", phoneNumberId);
            return;
        }

        if (value.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array)
        {
            foreach (var message in messages.EnumerateArray())
                await HandleInboundMessageAsync(account, message, cancellationToken);
        }

        if (value.TryGetProperty("statuses", out var statuses) && statuses.ValueKind == JsonValueKind.Array)
        {
            foreach (var status in statuses.EnumerateArray())
                await HandleStatusAsync(status, cancellationToken);
        }
    }

    private async Task HandleInboundMessageAsync(WhatsAppAccount account, JsonElement message, CancellationToken cancellationToken)
    {
        var from = message.TryGetProperty("from", out var fromEl) ? fromEl.GetString() : null;
        var waMessageId = message.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(waMessageId))
            return;

        // Idempotency: skip messages we've already stored (Meta retries webhooks).
        if (await db.Messages.AnyAsync(m => m.WhatsAppMessageId == waMessageId, cancellationToken))
            return;

        var typeString = message.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : "text";
        var messageType = MapMessageType(typeString);
        var content = ExtractContent(message, typeString);
        var timestamp = message.TryGetProperty("timestamp", out var tsEl) && long.TryParse(tsEl.GetString(), out var unix)
            ? DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime
            : DateTime.UtcNow;

        var customer = await WhatsAppService.FindOrCreateCustomerAsync(db, account.TenantId, from!, cancellationToken);
        var conversation = await WhatsAppService.FindOrCreateConversationAsync(db, account.TenantId, account.Id, customer, cancellationToken);

        var inbound = new Message
        {
            ConversationId = conversation.Id,
            SenderType = SenderType.Customer,
            MessageType = messageType,
            Content = content,
            WhatsAppMessageId = waMessageId!,
            Status = MessageStatus.Delivered,
            Timestamp = timestamp
        };
        await db.Messages.AddAsync(inbound, cancellationToken);

        conversation.LastMessageAt = timestamp;
        if (conversation.Status == ConversationStatus.Resolved)
            conversation.Status = ConversationStatus.Reopened;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Stored inbound WhatsApp message {WaMessageId} for tenant {TenantId}", waMessageId, account.TenantId);
    }

    private async Task HandleStatusAsync(JsonElement status, CancellationToken cancellationToken)
    {
        var waMessageId = status.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        var statusString = status.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(waMessageId) || string.IsNullOrWhiteSpace(statusString))
            return;

        var message = await db.Messages.FirstOrDefaultAsync(m => m.WhatsAppMessageId == waMessageId, cancellationToken);
        if (message is null)
            return;

        message.Status = MapStatus(statusString, message.Status);
        message.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static MessageType MapMessageType(string? type) => type switch
    {
        "image" => MessageType.Image,
        "document" => MessageType.Document,
        "audio" => MessageType.Audio,
        "video" => MessageType.Video,
        "location" => MessageType.Location,
        "template" => MessageType.Template,
        _ => MessageType.Text
    };

    private static MessageStatus MapStatus(string status, MessageStatus current) => status switch
    {
        "sent" => MessageStatus.Sent,
        "delivered" => MessageStatus.Delivered,
        "read" => MessageStatus.Read,
        "failed" => MessageStatus.Failed,
        _ => current
    };

    /// <summary>Text body, media caption, or the media id as a reference (binary storage is out of scope).</summary>
    private static string ExtractContent(JsonElement message, string? type)
    {
        if (type == "text" && message.TryGetProperty("text", out var text) && text.TryGetProperty("body", out var body))
            return body.GetString() ?? string.Empty;

        if (type is not null && message.TryGetProperty(type, out var media))
        {
            if (media.TryGetProperty("caption", out var caption))
                return caption.GetString() ?? string.Empty;
            if (media.TryGetProperty("id", out var mediaId))
                return mediaId.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
