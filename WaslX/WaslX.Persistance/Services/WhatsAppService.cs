using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Application.Features.WhatsApp.Templates.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class WhatsAppService(
    ApplicationDbContext db,
    IMetaGraphApiService graphApi,
    IInboxRealtimeNotifier notifier,
    IConversationWindowService windowService,
    ILogger<WhatsAppService> logger) : IWhatsAppService
{
    public async Task<Result<WhatsAppAccountDto>> ConnectAsync(int? tenantId, string authorizationCode, string? wabaId, string? redirectUri = null, WhatsAppConnectSettings? settings = null, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<WhatsAppAccountDto>(AppErrors.NoTenantContext);

        var tokenResult = await graphApi.ExchangeCodeForTokenAsync(authorizationCode, redirectUri, cancellationToken);
        if (tokenResult.IsFailure)
            return Result.Failure<WhatsAppAccountDto>(tokenResult.Error);

        var infoResult = await graphApi.GetBusinessInfoAsync(tokenResult.Value.AccessToken, wabaId, cancellationToken);
        if (infoResult.IsFailure)
            return Result.Failure<WhatsAppAccountDto>(infoResult.Error);

        var info = infoResult.Value;

        // Without this, Meta never delivers inbound message/status webhooks for this WABA to our
        // callback URL, even when the App-level Callback URL/Verify Token are correctly configured.
        var subscribeResult = await graphApi.SubscribeToWebhooksAsync(info.WhatsAppBusinessAccountId, tokenResult.Value.AccessToken, cancellationToken);
        if (subscribeResult.IsFailure)
            return Result.Failure<WhatsAppAccountDto>(subscribeResult.Error);

        // Upsert: one WhatsApp account per tenant.
        var account = await db.WhatsAppAccounts.FirstOrDefaultAsync(x => x.TenantId == tid, cancellationToken);
        var isNew = account is null;
        account ??= new WhatsAppAccount { TenantId = tid };

        account.AccessToken = tokenResult.Value.AccessToken;
        account.TokenExpiresAt = tokenResult.Value.ExpiresAt;
        account.PhoneNumberId = info.PhoneNumberId;
        account.WhatsAppBusinessAccountId = info.WhatsAppBusinessAccountId;
        account.PhoneNumber = info.DisplayPhoneNumber;
        account.Status = WhatsAppAccountStatus.Connected;
        account.ConnectedAt = DateTime.UtcNow;

        // ── Step-1 wizard settings (friendly name + distribution config) ──────────────────
        // Purely additive: only applied when the caller sends them, so an OAuth-only re-connect
        // never clobbers previously saved values, and a brand-new account keeps its entity
        // defaults (PlatformName="", RoundRobin, DistributeToOffline=true, ReassignOnOffline=false).
        if (settings is not null)
        {
            if (settings.PlatformName is not null)
                account.PlatformName = settings.PlatformName;

            // Only apply the mode when the caller actually sent it, so a bare OAuth re-connect never
            // resets a number that was configured as ByAdmin / working-hours back to RoundRobin.
            if (!string.IsNullOrWhiteSpace(settings.DistributionMode))
                account.DistributionMode = Enum.TryParse<DistributionMode>(settings.DistributionMode, ignoreCase: true, out var mode)
                    ? mode
                    : DistributionMode.RoundRobin;

            if (settings.DistributeToOffline is { } distributeToOffline)
                account.DistributeToOffline = distributeToOffline;

            if (settings.ReassignOnOffline is { } reassignOnOffline)
                account.ReassignOnOffline = reassignOnOffline;

            // Only bind a starting group that actually belongs to this tenant; ignore otherwise.
            if (settings.StartingGroupId is { } startingGroupId)
            {
                var groupExists = await db.Groups.AnyAsync(g => g.Id == startingGroupId && g.TenantId == tid, cancellationToken);
                if (groupExists)
                    account.StartingGroupId = startingGroupId;
            }
        }

        if (isNew)
            await db.WhatsAppAccounts.AddAsync(account, cancellationToken);
        else
            account.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("WhatsApp account {AccountId} connected for tenant {TenantId}", account.Id, tid);
        return Result.Success(Map(account));
    }

    public async Task<Result<WhatsAppAccountDto>> GetAccountAsync(int? tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<WhatsAppAccountDto>(AppErrors.NoTenantContext);

        var account = await db.WhatsAppAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tid, cancellationToken);
        return account is null
            ? Result.Failure<WhatsAppAccountDto>(AppErrors.WhatsAppAccountNotFound)
            : Result.Success(Map(account));
    }

    public async Task<Result<IReadOnlyList<WhatsAppAccountListItemDto>>> GetAccountsAsync(int? tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<IReadOnlyList<WhatsAppAccountListItemDto>>(AppErrors.NoTenantContext);

        var accounts = await db.WhatsAppAccounts.AsNoTracking()
            .Where(x => x.TenantId == tid)
            .OrderBy(x => x.Id)
            .Select(x => new WhatsAppAccountListItemDto(
                x.Id,
                x.PhoneNumber,
                x.PlatformName,
                x.Status.ToString(),
                x.DistributionMode.ToString(),
                x.ConnectedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<WhatsAppAccountListItemDto>>(accounts);
    }

    public async Task<Result> DisconnectAsync(int? tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var account = await db.WhatsAppAccounts.FirstOrDefaultAsync(x => x.TenantId == tid, cancellationToken);
        if (account is null)
            return Result.Failure(AppErrors.WhatsAppAccountNotFound);

        account.Status = WhatsAppAccountStatus.Disconnected;
        account.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public Task<Result<SendMessageResult>> SendTextAsync(int? tenantId, string toPhone, string text, int? senderUserId = null, CancellationToken cancellationToken = default) =>
        SendAsync(tenantId, toPhone, MessageType.Text, text,
            (account) => graphApi.SendTextMessageAsync(account.PhoneNumberId, account.AccessToken, toPhone, text, cancellationToken),
            senderUserId,
            cancellationToken);

    public Task<Result<SendMessageResult>> SendTemplateAsync(int? tenantId, string toPhone, string templateName, string languageCode, TemplateSendParameters? parameters = null, int? senderUserId = null, CancellationToken cancellationToken = default) =>
        SendAsync(tenantId, toPhone, MessageType.Template, templateName,
            (account) => graphApi.SendTemplateMessageAsync(account.PhoneNumberId, account.AccessToken, toPhone, templateName, languageCode, parameters, cancellationToken),
            senderUserId,
            cancellationToken);

    public Task<Result<SendMessageResult>> SendMediaAsync(
        int? tenantId, string toPhone, string mediaType, string mediaUrl, string? caption, string? fileName,
        string mimeType, int? senderUserId = null, CancellationToken cancellationToken = default) =>
        SendAsync(tenantId, toPhone, MapMediaMessageType(mediaType), caption ?? string.Empty,
            (account) => graphApi.SendMediaMessageAsync(account.PhoneNumberId, account.AccessToken, toPhone, mediaType, mediaUrl, caption, fileName, cancellationToken),
            senderUserId,
            cancellationToken,
            mediaUrl,
            mimeType,
            fileName);

    private static MessageType MapMediaMessageType(string mediaType) => mediaType switch
    {
        "image" => MessageType.Image,
        "video" => MessageType.Video,
        "sticker" => MessageType.Sticker,
        "document" => MessageType.Document,
        _ => MessageType.Document
    };

    private async Task<Result<SendMessageResult>> SendAsync(
        int? tenantId,
        string toPhone,
        MessageType messageType,
        string content,
        Func<WhatsAppAccount, Task<Result<string>>> send,
        int? senderUserId,
        CancellationToken cancellationToken,
        string? mediaUrl = null,
        string? mediaMimeType = null,
        string? mediaFileName = null)
    {
        if (tenantId is not { } tid)
            return Result.Failure<SendMessageResult>(AppErrors.NoTenantContext);

        var account = await db.WhatsAppAccounts.FirstOrDefaultAsync(x => x.TenantId == tid, cancellationToken);
        if (account is null)
            return Result.Failure<SendMessageResult>(AppErrors.WhatsAppAccountNotFound);
        if (account.Status != WhatsAppAccountStatus.Connected)
            return Result.Failure<SendMessageResult>(AppErrors.WhatsAppNotConnected);

        // The 24-hour window is enforced by Meta, not by our DB: we always attempt the send and let
        // Meta be the authority. If Meta rejects free-form with the window-closed error (131047), we
        // reconcile our local mirror so the UI locks to templates.
        var sendResult = await send(account);
        if (sendResult.IsFailure)
        {
            if (sendResult.Error == AppErrors.WhatsAppWindowClosed)
            {
                // Reconcile only an EXISTING conversation — never materialise a phantom customer/
                // conversation for a failed send to a phone that never messaged us (a closed window
                // normally implies a prior inbound anyway). Look up read-only; skip silently if absent.
                var closedConversation = await FindExistingConversationAsync(db, tid, toPhone, cancellationToken);
                windowService.SynchronizeAfterFailedSend(closedConversation);
                // ── caller (SendAsync) owns SaveChanges ────────────────────────────────
                if (closedConversation is not null)
                    await db.SaveChangesAsync(cancellationToken);
            }
            return Result.Failure<SendMessageResult>(sendResult.Error);
        }

        var customer = await FindOrCreateCustomerAsync(db, tid, toPhone, cancellationToken);
        var conversation = await FindOrCreateConversationAsync(db, tid, account.Id, customer, cancellationToken);

        var now = DateTime.UtcNow;
        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderUserId = senderUserId,
            SenderType = SenderType.Agent,
            MessageType = messageType,
            Content = content,
            MediaUrl = mediaUrl,
            MediaMimeType = mediaMimeType,
            MediaFileName = mediaFileName,
            WhatsAppMessageId = sendResult.Value,
            Status = MessageStatus.Sent,
            Timestamp = now
        };
        await db.Messages.AddAsync(message, cancellationToken);
        conversation.LastMessageAt = now;

        // A successful free-form (non-template) send is proof Meta still considers the 24h window open,
        // so self-heal a stale/early local mirror instead of letting the UI lock the composer while Meta
        // is happily accepting free text (e.g. after a transient 131047 or a missed inbound webhook). The
        // window is anchored to the customer's last inbound — agent replies never extend it — so heal to
        // LastCustomerMessageAt + 24h; if we never recorded an inbound, fall back to now + 24h since Meta
        // just confirmed it is open. Only ever extend forward: never pull the expiry earlier (this keeps
        // a 72h free-entry-point window intact and never shrinks Meta's authoritative value).
        // ARCHITECTURE NOTE: only free-form sends prove the window is open.
        // Template sends are never used to heal the window — templates bypass the window entirely.
        if (messageType != MessageType.Template)
            windowService.SynchronizeAfterSuccessfulSend(conversation);
        // ── caller (SendAsync) owns SaveChanges below ──────────────────────────────

        // First agent reply advances the lifecycle to In Progress (one of the three automatic
        // transitions — also covers the "reply while Pending → In Progress" edge case. Resolved is
        // included so an agent-initiated reply to a reused resolved thread doesn't stay Resolved.
        var advanced = conversation.Status is ConversationStatus.New or ConversationStatus.Assigned
            or ConversationStatus.Reopened or ConversationStatus.Pending or ConversationStatus.Resolved;
        if (advanced)
            conversation.Status = ConversationStatus.InProgress;

        await db.SaveChangesAsync(cancellationToken);

        await notifier.MessageReceivedAsync(tid, new InboxMessagePayload(
            message.Id, conversation.Id, message.SenderType.ToString(), message.Content,
            message.MessageType.ToString(), message.Status.ToString(), message.Timestamp,
            message.SenderUserId, message.MediaUrl, message.MediaMimeType, message.MediaFileName), cancellationToken);

        if (advanced)
            await notifier.ConversationChangedAsync(tid, new ConversationChangedPayload(
                conversation.Id, conversation.Status.ToString(), conversation.AssignedUserId, conversation.LastMessageAt), cancellationToken);

        return Result.Success(new SendMessageResult(message.Id, conversation.Id, message.WhatsAppMessageId, message.Status.ToString()));
    }

    /// <summary>
    /// Read-only lookup of an existing, non-deleted conversation for a phone (latest first). Used by the
    /// window-closed reconcile so a failed out-of-window send never creates a phantom customer/conversation.
    /// </summary>
    private static async Task<Conversation?> FindExistingConversationAsync(ApplicationDbContext db, int tenantId, string phone, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.PhoneNumber == phone, cancellationToken);
        if (customer is null)
            return null;

        return await db.Conversations
            .Where(c => c.TenantId == tenantId && c.CustomerId == customer.Id && !c.IsDeleted)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Shared find-or-create helpers (also used, conceptually, by inbound webhook processing).
    internal static async Task<Customer> FindOrCreateCustomerAsync(ApplicationDbContext db, int tenantId, string phone, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.PhoneNumber == phone, cancellationToken);
        if (customer is not null)
            return customer;

        customer = new Customer
        {
            TenantId = tenantId,
            PhoneNumber = phone,
            Name = phone,
            Tier = CustomerTier.Standard
        };
        await db.Customers.AddAsync(customer, cancellationToken);
        await db.SaveChangesAsync(cancellationToken); // materialise Id for FK use
        return customer;
    }

    internal static async Task<Conversation> FindOrCreateConversationAsync(ApplicationDbContext db, int tenantId, int whatsAppAccountId, Customer customer, CancellationToken cancellationToken)
    {
        // A soft-deleted conversation is never reused — a new message from this customer starts fresh.
        // A Resolved conversation IS reused: the inbound handler auto-reopens it (Resolved -> Reopened)
        // so a returning customer keeps one continuous thread instead of spawning a duplicate.
        var conversation = await db.Conversations
            .Where(c => c.TenantId == tenantId && c.CustomerId == customer.Id && !c.IsDeleted)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is not null)
            return conversation;

        conversation = new Conversation
        {
            TenantId = tenantId,
            WhatsAppAccountId = whatsAppAccountId,
            CustomerId = customer.Id,
            Status = ConversationStatus.New,
            Priority = ConversationPriority.Normal,
            LastMessageAt = DateTime.UtcNow
        };
        await db.Conversations.AddAsync(conversation, cancellationToken);
        await db.SaveChangesAsync(cancellationToken); // materialise Id for FK use
        return conversation;
    }

    private static WhatsAppAccountDto Map(WhatsAppAccount a) => new(
        a.Id,
        a.PhoneNumber,
        a.PhoneNumberId,
        a.WhatsAppBusinessAccountId,
        a.Status.ToString(),
        a.ConnectedAt,
        a.TokenExpiresAt);
}
