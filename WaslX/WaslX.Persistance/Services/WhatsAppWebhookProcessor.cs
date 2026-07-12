using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.Media;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Hangfire-invoked processor that turns a stored raw webhook payload into inbound messages
/// and message-status updates. Resolves the tenant from the payload's phone_number_id, and
/// pushes each change to the shared inbox in real time via <see cref="IInboxRealtimeNotifier"/>.
/// </summary>
internal sealed class WhatsAppWebhookProcessor(
    ApplicationDbContext db,
    IMetaGraphApiService graphApi,
    IMediaStorageService mediaStorage,
    IInboxRealtimeNotifier notifier,
    ILogger<WhatsAppWebhookProcessor> logger) : IWhatsAppWebhookProcessor
{
    private static readonly HashSet<MessageType> MediaTypes =
        [MessageType.Image, MessageType.Document, MessageType.Audio, MessageType.Video, MessageType.Sticker];

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
                        if (!change.TryGetProperty("field", out var fieldEl) || fieldEl.ValueKind != JsonValueKind.String)
                        {
                            // Legacy/default: route by the value's child arrays (inbound messages + statuses).
                            if (change.TryGetProperty("value", out var value))
                                await ProcessValueAsync(value, cancellationToken);
                            continue;
                        }

                        var field = fieldEl.GetString();
                        if (field == "message_template_status_update" && change.TryGetProperty("value", out var tplValue))
                        {
                            await HandleTemplateStatusUpdateAsync(entry, tplValue, cancellationToken);
                            continue;
                        }

                        // messages + statuses both arrive under value (no field-based routing needed).
                        if (change.TryGetProperty("value", out var msgValue))
                            await ProcessValueAsync(msgValue, cancellationToken);
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
        var caption = ExtractCaption(message, typeString);
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
            Content = caption,
            WhatsAppMessageId = waMessageId!,
            Status = MessageStatus.Delivered,
            Timestamp = timestamp
        };

        if (MediaTypes.Contains(messageType))
            await AttachMediaAsync(inbound, account.AccessToken, message, typeString, cancellationToken);

        await db.Messages.AddAsync(inbound, cancellationToken);

        conversation.LastMessageAt = timestamp;
        // Auto-reopen: a new inbound on a Resolved conversation moves it back to Reopened
        // (one of only three automatic transitions; the rest are manual.
        var reopened = conversation.Status == ConversationStatus.Resolved;
        if (reopened)
            conversation.Status = ConversationStatus.Reopened;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Stored inbound WhatsApp message {WaMessageId} for tenant {TenantId}", waMessageId, account.TenantId);

        await notifier.MessageReceivedAsync(account.TenantId, new InboxMessagePayload(
            inbound.Id, conversation.Id, inbound.SenderType.ToString(), inbound.Content,
            inbound.MessageType.ToString(), inbound.Status.ToString(), inbound.Timestamp,
            inbound.SenderUserId, inbound.MediaUrl, inbound.MediaMimeType, inbound.MediaFileName), cancellationToken);

        if (reopened)
            await notifier.ConversationChangedAsync(account.TenantId, new ConversationChangedPayload(
                conversation.Id, conversation.Status.ToString(), conversation.AssignedUserId, conversation.LastMessageAt), cancellationToken);
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

        var tenantId = await db.Conversations.Where(c => c.Id == message.ConversationId)
            .Select(c => c.TenantId).FirstOrDefaultAsync(cancellationToken);
        await notifier.MessageStatusChangedAsync(tenantId, message.ConversationId, message.Id, message.Status.ToString(), cancellationToken);
    }

    /// <summary>
    /// Processes a <c>message_template_status_update</c> webhook: upserts the local TemplateReview
    /// row with Meta's review decision (status + rejection reason). The tenant is resolved via the
    /// entry's WhatsApp Business Account id (these events carry no phone_number_id, unlike inbound
    /// messages). Best-effort: logs and never throws, mirroring the inbound handler.
    /// </summary>
    private async Task HandleTemplateStatusUpdateAsync(JsonElement entry, JsonElement value, CancellationToken cancellationToken)
    {
        // entry.id = the WhatsApp Business Account id that owns the template.
        var wabaId = entry.TryGetProperty("id", out var entryIdEl) ? entryIdEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(wabaId))
            return;

        var account = await db.WhatsAppAccounts.FirstOrDefaultAsync(x => x.whatsAppBusinessAccountId == wabaId, cancellationToken);
        if (account is null)
        {
            logger.LogWarning("Template status update: no WhatsApp account matches WABA id {WabaId}", wabaId);
            return;
        }

        if (!value.TryGetProperty("message_template_status_update", out var update))
            return;

        var metaTemplateId = update.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(metaTemplateId))
            return;

        var templateName = update.TryGetProperty("message_template_name", out var nameEl) ? nameEl.GetString() : null;
        var language = update.TryGetProperty("message_template_language", out var langEl) ? langEl.GetString() : null;
        var newStatus = update.TryGetProperty("message_template_status", out var stEl) ? stEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(newStatus))
            return;

        // Meta only sends a "reason" on REJECTED; absent otherwise. Never invent a reason.
        string? reason = update.TryGetProperty("reason", out var reasonEl) && reasonEl.ValueKind == JsonValueKind.String
            ? reasonEl.GetString()
            : null;

        var review = await db.TemplateReviews
            .FirstOrDefaultAsync(r => r.TenantId == account.TenantId && r.MetaTemplateId == metaTemplateId, cancellationToken);

        var now = DateTime.UtcNow;
        if (review is null)
        {
            // Webhook arrived before we persisted the create-audit row (or template was created
            // outside the app). Create a minimal row with the data Meta gave us.
            review = new TemplateReview
            {
                TenantId = account.TenantId,
                MetaTemplateId = metaTemplateId,
                MessageTemplateName = templateName ?? string.Empty,
                Language = language,
                Status = newStatus,
                ReasonText = reason,
                SubmittedCategory = string.Empty,
                AllowCategoryChange = false,
                ReviewedAt = newStatus is "APPROVED" or "REJECTED" ? now : null
            };
            await db.TemplateReviews.AddAsync(review, cancellationToken);
        }
        else
        {
            review.Status = newStatus;
            // Update reason only when Meta provides one (null = keep prior, or clear on non-rejected).
            review.ReasonText = newStatus == "REJECTED" ? reason : null;
            review.ReasonCode = newStatus == "REJECTED" ? review.ReasonCode : null;
            review.ReviewedAt = newStatus is "APPROVED" or "REJECTED" ? now : review.ReviewedAt;
            review.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Template status update: template {TemplateId} → {Status} (reason={Reason})",
            metaTemplateId, newStatus, reason ?? "none");
    }

    /// <summary>
    /// Downloads the inbound media from Meta (short-lived, auth-protected URL) and re-uploads it
    /// to permanent storage. Best-effort: if this fails, the message is still stored (with its
    /// caption and message type) but without a playable/viewable MediaUrl.
    /// </summary>
    private async Task AttachMediaAsync(Message message, string accessToken, JsonElement raw, string? type, CancellationToken cancellationToken)
    {
        var mediaId = ExtractMediaId(raw, type);
        if (string.IsNullOrWhiteSpace(mediaId))
            return;

        var downloadResult = await graphApi.DownloadMediaAsync(mediaId, accessToken, cancellationToken);
        if (downloadResult.IsFailure)
        {
            logger.LogWarning("Failed to download inbound media {MediaId}: {Error}", mediaId, downloadResult.Error.Description);
            return;
        }

        var fileName = ExtractFileName(raw, type) ?? mediaId;
        var uploadResult = await mediaStorage.UploadAsync(downloadResult.Value.Content, fileName, downloadResult.Value.ContentType, cancellationToken);
        if (uploadResult.IsFailure)
        {
            logger.LogWarning("Failed to store inbound media {MediaId} in permanent storage: {Error}", mediaId, uploadResult.Error.Description);
            return;
        }

        message.MediaUrl = uploadResult.Value.Url;
        message.MediaMimeType = uploadResult.Value.MimeType;
        message.MediaFileName = fileName;
    }

    private static MessageType MapMessageType(string? type) => type switch
    {
        "image" => MessageType.Image,
        "document" => MessageType.Document,
        "audio" => MessageType.Audio,
        "video" => MessageType.Video,
        "sticker" => MessageType.Sticker,
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

    /// <summary>Text body, or a media message's caption (empty if it has none — never falls back to the media id).</summary>
    private static string ExtractCaption(JsonElement message, string? type)
    {
        if (type == "text" && message.TryGetProperty("text", out var text) && text.TryGetProperty("body", out var body))
            return body.GetString() ?? string.Empty;

        if (type is not null && message.TryGetProperty(type, out var media) && media.TryGetProperty("caption", out var caption))
            return caption.GetString() ?? string.Empty;

        return string.Empty;
    }

    /// <summary>The Meta media id for image/document/audio/video/sticker messages, used to download the bytes.</summary>
    private static string? ExtractMediaId(JsonElement message, string? type)
    {
        if (type is not null && message.TryGetProperty(type, out var media) && media.TryGetProperty("id", out var idEl))
            return idEl.GetString();
        return null;
    }

    /// <summary>The original filename for document messages, if Meta supplied one.</summary>
    private static string? ExtractFileName(JsonElement message, string? type)
    {
        if (type is not null && message.TryGetProperty(type, out var media) && media.TryGetProperty("filename", out var nameEl))
            return nameEl.GetString();
        return null;
    }
}
