using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Abstractions.Distribution;
using WaslX.Application.Abstractions.Media;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.Classification;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;


internal sealed class WhatsAppWebhookProcessor(
    ApplicationDbContext db,
    IMetaGraphApiService graphApi,
    IMediaStorageService mediaStorage,
    IInboxRealtimeNotifier notifier,
    IConversationWindowService windowService,
    IDistributionService distribution,
    IInboundMessageThrottle throttle,
    Hangfire.IBackgroundJobClient backgroundJobs,
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

        // Meta sends a sibling "contacts" array (wa_id + profile.name) alongside inbound messages.
        // Parse it into a wa_id -> profile name map so we can label the customer with their WhatsApp
        // display name instead of the raw number. Strictly additive; absent/malformed = empty map.
        var profileNames = ExtractProfileNames(value);

        if (value.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array)
        {
            foreach (var message in messages.EnumerateArray())
            {
                var from = message.TryGetProperty("from", out var fromEl) ? fromEl.GetString() : null;
                var profileName = from is not null && profileNames.TryGetValue(from, out var pn) ? pn : null;
                await HandleInboundMessageAsync(account, message, profileName, cancellationToken);
            }
        }

        if (value.TryGetProperty("statuses", out var statuses) && statuses.ValueKind == JsonValueKind.Array)
        {
            foreach (var status in statuses.EnumerateArray())
                await HandleStatusAsync(status, cancellationToken);
        }
    }

    private async Task HandleInboundMessageAsync(WhatsAppAccount account, JsonElement message, string? profileName, CancellationToken cancellationToken)
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

        // Label the customer with their WhatsApp profile name when Meta provides one, but only if the
        // customer is still unnamed or carries the raw phone as its name — never overwrite a name an
        // agent set manually. Additive: persisted by the SaveChanges already made below.
        if (!string.IsNullOrWhiteSpace(profileName) &&
            (string.IsNullOrWhiteSpace(customer.Name) || customer.Name == customer.PhoneNumber))
        {
            customer.Name = profileName!;
        }

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
        var hasReferral = message.TryGetProperty("referral", out _);
        windowService.UpdateFromInboundMessage(conversation, timestamp, hasReferral);
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

        // US-4.8: Queue classification + AI agent reply as Hangfire jobs (bounded by the server's
        // configured WorkerCount) instead of raw unthrottled Task.Run — a burst of inbound messages
        // can no longer spin up unbounded concurrent LLM/embedding calls against our own process or
        // the AI providers' rate limits. Gated first by the per-phone-number throttle: message storage
        // above is never skipped, only this downstream AI-costing work.
        if (inbound.MessageType == MessageType.Text)
        {
            var tenantId = account.TenantId;
            var convId = conversation.Id;
            var msgId = inbound.Id;

            if (!throttle.TryAcquire(from!))
            {
                logger.LogWarning(
                    "Inbound message throttle: {From} exceeded the per-minute limit — skipping classification/AI reply for message {MsgId}",
                    from, msgId);
            }
            else
            {
                backgroundJobs.Enqueue<IClassificationOrchestrator>(
                    c => c.ProcessClassificationAsync(tenantId, convId, msgId, CancellationToken.None));

                // AiAgentReplyService re-derives and checks tenant/number Enabled + AutoReplyEnabled
                // itself; AiMode is the one gate not already inside it, so it's checked here.
                if (conversation.AiMode == AiConversationMode.Active)
                {
                    backgroundJobs.Enqueue<IAiAgentReplyService>(
                        a => a.ReplyAsync(tenantId, convId, msgId, CancellationToken.None));
                }
            }
        }

        // Auto-distribution (Sprint 3, Phase B): route a brand-new, still-unassigned conversation to an
        // eligible agent per the number's DistributionMode. Strictly additive and fully guarded — a
        // distribution failure must never prevent the inbound message from being stored or the webhook
        // from succeeding, so it is wrapped in try/catch and swallowed (logged and continue).
        if (conversation.AssignedUserId is null && conversation.Status == ConversationStatus.New)
        {
            try
            {
                await distribution.AutoAssignAsync(conversation.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Auto-assign failed for conversation {ConversationId} (tenant {TenantId}); message already stored",
                    conversation.Id, account.TenantId);
                // Drop any half-applied assignment from the shared change-tracker so it can't be
                // re-flushed by a later message in this webhook batch (the inbound message is already committed).
                db.ChangeTracker.Clear();
            }
        }
    }

    private async Task HandleStatusAsync(JsonElement status, CancellationToken cancellationToken)
    {
        var waMessageId = status.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        var statusString = status.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(waMessageId) || string.IsNullOrWhiteSpace(statusString))
            return;

        var message = await db.Messages.FirstOrDefaultAsync(m => m.WhatsAppMessageId == waMessageId, cancellationToken);
        Conversation? conversation = null;

        if (message is not null)
        {
            message.Status = MapStatus(statusString, message.Status);
            message.UpdatedAt = DateTime.UtcNow;

            conversation = await db.Conversations.FirstOrDefaultAsync(c => c.Id == message.ConversationId, cancellationToken);

            if (conversation is not null)
                windowService.UpdateFromMetaStatus(conversation, ExtractConversationExpiry(status));
        }

        // Mirror the delivery status onto a matching campaign recipient (if this send was a campaign
        // broadcast). Campaign analytics derive from CampaignRecipient.Status, so advancing it here is
        // what surfaces Delivered/Read in the campaign dashboard.
        await ApplyCampaignRecipientStatusAsync(waMessageId, statusString, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        if (message is not null && conversation is not null)
            await notifier.MessageStatusChangedAsync(conversation.TenantId, message.ConversationId, message.Id, message.Status.ToString(), cancellationToken);
    }

    /// <summary>
    /// Advances the campaign recipient that owns this Meta message id (queued/sent → delivered → read,
    /// or → failed) from a delivery-status webhook. Forward-only and idempotent, so Meta's webhook
    /// retries and out-of-order statuses never double-count or regress. No-op when the message was not
    /// a campaign send (no recipient carries this id).
    /// </summary>
    private async Task ApplyCampaignRecipientStatusAsync(string waMessageId, string statusString, CancellationToken cancellationToken)
    {
        var recipient = await db.CampaignRecipients
            .FirstOrDefaultAsync(r => r.WhatsAppMessageId == waMessageId, cancellationToken);
        if (recipient is null)
            return;

        var now = DateTime.UtcNow;
        switch (statusString)
        {
            case "delivered":
                if (recipient.Status is "queued" or "sent")
                {
                    recipient.Status = "delivered";
                    recipient.DeliveredAt ??= now;
                    recipient.UpdatedAt = now;
                }
                break;

            case "read":
                if (recipient.Status is "queued" or "sent" or "delivered")
                {
                    recipient.DeliveredAt ??= now; // read implies delivered (a delivered webhook may be skipped)
                    recipient.Status = "read";
                    recipient.ReadAt ??= now;
                    recipient.UpdatedAt = now;
                }
                break;

            case "failed":
                if (recipient.Status != "failed")
                {
                    recipient.Status = "failed";
                    recipient.UpdatedAt = now;
                }
                break;

            // "sent" is already recorded at send time — ignore to avoid regressing a later status.
        }
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

        var account = await db.WhatsAppAccounts.FirstOrDefaultAsync(x => x.WhatsAppBusinessAccountId == wabaId, cancellationToken);
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

        // Capture raw payload for audit — never lose Meta data.
        var rawJson = value.GetRawText();

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
                ReviewedAt = newStatus is "APPROVED" or "REJECTED" ? now : null,
                MetaStatusRaw = rawJson
            };
            await db.TemplateReviews.AddAsync(review, cancellationToken);
            await db.SaveChangesAsync(cancellationToken); // Need ID for history
        }
        else
        {
            review.Status = newStatus;
            review.MetaStatusRaw = rawJson;
            // Update reason only when Meta provides one (null = keep prior, or clear on non-rejected).
            review.ReasonText = newStatus == "REJECTED" ? reason : null;
            review.ReasonCode = newStatus == "REJECTED" ? review.ReasonCode : null;
            review.ReviewedAt = newStatus is "APPROVED" or "REJECTED" ? now : review.ReviewedAt;
            review.UpdatedAt = now;

            // FinalCategory: set when Meta reports a different category than we submitted.
            if (!string.IsNullOrEmpty(newStatus))
            {
                var metaCategory = update.TryGetProperty("message_template_category", out var catEl)
                    ? catEl.GetString() : null;
                if (metaCategory is not null && !string.Equals(metaCategory, review.SubmittedCategory,
                        StringComparison.OrdinalIgnoreCase))
                {
                    review.FinalCategory = metaCategory;
                    // ChangedByMeta is derived at read time (FinalCategory != SubmittedCategory);
                    // no separate bool is persisted to avoid drift.
                }
            }

            // Lifecycle fields
            if (newStatus == "PAUSED" && update.TryGetProperty("pause_info", out var piEl))
                review.PauseInfo = piEl.GetRawText();

            if (newStatus == "DISABLED")
                review.DisableTimestamp ??= DateTime.UtcNow;

            if (newStatus is "DELETED")
                review.DeletedAt ??= DateTime.UtcNow;
        }

        // Append an immutable history event — one row per status change, never edited.
        var historyEvent = new TemplateReviewHistory
        {
            TemplateReviewId = review.Id,
            TenantId         = account.TenantId,
            Status           = newStatus,
            ChangedAt        = now,
            ReasonCode       = review.ReasonCode,
            ReasonText       = reason,
            FinalCategory    = review.FinalCategory,
            PauseInfo        = review.PauseInfo,
            MetaStatusRaw    = rawJson
        };
        await db.TemplateReviewHistories.AddAsync(historyEvent, cancellationToken);

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

    /// <summary>
    /// Meta's authoritative window-close time from a status webhook (conversation.expiration_timestamp,
    /// Unix seconds). Present only on the "sent" status, and on Graph API v24.0+ only for free-entry-point
    /// windows (72h). Null when absent — the caller then keeps the inbound-anchored window.
    /// </summary>
    private static DateTime? ExtractConversationExpiry(JsonElement status)
    {
        if (status.TryGetProperty("conversation", out var conv) &&
            conv.TryGetProperty("expiration_timestamp", out var expEl) &&
            long.TryParse(expEl.GetString(), out var unix))
            return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
        return null;
    }

    /// <summary>
    /// Builds a wa_id -> WhatsApp profile name map from the webhook value's "contacts" array
    /// (each entry: { "wa_id": "...", "profile": { "name": "..." } }). Best-effort and additive:
    /// returns an empty map when contacts are absent or malformed, and skips entries with a blank name.
    /// </summary>
    private static Dictionary<string, string> ExtractProfileNames(JsonElement value)
    {
        var map = new Dictionary<string, string>();
        if (!value.TryGetProperty("contacts", out var contacts) || contacts.ValueKind != JsonValueKind.Array)
            return map;

        foreach (var contact in contacts.EnumerateArray())
        {
            var waId = contact.TryGetProperty("wa_id", out var waIdEl) ? waIdEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(waId))
                continue;

            var name = contact.TryGetProperty("profile", out var profile) && profile.TryGetProperty("name", out var nameEl)
                ? nameEl.GetString()
                : null;
            if (!string.IsNullOrWhiteSpace(name))
                map[waId] = name!;
        }

        return map;
    }

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
