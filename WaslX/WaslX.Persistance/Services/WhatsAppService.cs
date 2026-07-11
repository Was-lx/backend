using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class WhatsAppService(
    ApplicationDbContext db,
    IMetaGraphApiService graphApi,
    ILogger<WhatsAppService> logger) : IWhatsAppService
{
    public async Task<Result<WhatsAppAccountDto>> ConnectAsync(int? tenantId, string authorizationCode, string? wabaId, string? redirectUri = null, CancellationToken cancellationToken = default)
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
        account.whatsAppBusinessAccountId = info.WhatsAppBusinessAccountId;
        account.PhoneNumber = info.DisplayPhoneNumber;
        account.Status = WhatsAppAccountStatus.Connected;
        account.ConnectedAt = DateTime.UtcNow;

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

    public Task<Result<SendMessageResult>> SendTemplateAsync(int? tenantId, string toPhone, string templateName, string languageCode, CancellationToken cancellationToken = default) =>
        SendAsync(tenantId, toPhone, MessageType.Template, templateName,
            (account) => graphApi.SendTemplateMessageAsync(account.PhoneNumberId, account.AccessToken, toPhone, templateName, languageCode, cancellationToken),
            null,
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

        var sendResult = await send(account);
        if (sendResult.IsFailure)
            return Result.Failure<SendMessageResult>(sendResult.Error);

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

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new SendMessageResult(message.Id, conversation.Id, message.WhatsAppMessageId, message.Status.ToString()));
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
        var conversation = await db.Conversations
            .Where(c => c.TenantId == tenantId && c.CustomerId == customer.Id && c.Status != ConversationStatus.Resolved && !c.IsDeleted)
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
        a.whatsAppBusinessAccountId,
        a.Status.ToString(),
        a.ConnectedAt,
        a.TokenExpiresAt);
}
