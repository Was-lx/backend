using System.Text;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Features.Conversations.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Conversation summarization. Reuses the same tenant + RBAC gating as the shared inbox,
/// caches one summary row per conversation, and refreshes it when the thread has newer messages.
/// </summary>
internal sealed class ConversationSummaryService(
    ApplicationDbContext db,
    ILLMProvider llm) : IConversationSummaryService
{
    // Cap the transcript we send to the model so long threads stay within a sane token budget.
    private const int MaxMessages = 60;
    private const int MaxContentChars = 500;

    public Task<Result<ConversationSummaryResponse>> GetOrCreateShortAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, CancellationToken cancellationToken = default) =>
        SummarizeAsync(tenantId, currentUserId, isPrivileged, conversationId, full: false, cancellationToken);

    public Task<Result<ConversationSummaryResponse>> GenerateFullAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, CancellationToken cancellationToken = default) =>
        SummarizeAsync(tenantId, currentUserId, isPrivileged, conversationId, full: true, cancellationToken);

    private async Task<Result<ConversationSummaryResponse>> SummarizeAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, bool full, CancellationToken cancellationToken)
    {
        var access = await ResolveConversationAsync(tenantId, currentUserId, isPrivileged, conversationId, cancellationToken);
        if (access.IsFailure)
            return Result.Failure<ConversationSummaryResponse>(access.Error);

        var existing = await db.ConversationSummaries
            .FirstOrDefaultAsync(s => s.ConversationId == conversationId, cancellationToken);

        var latestMessageId = await db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.Id)
            .Select(m => (int?)m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestMessageId is null)
            return Result.Failure<ConversationSummaryResponse>(AppErrors.ConversationHasNoMessages);

        // Short summary: serve the cache when it already reflects the newest message and (for a plain
        // "open the conversation" read) we're not being asked for the full structured view.
        var isFresh = existing is not null && existing.UpToMessageId >= latestMessageId.Value;
        if (!full && isFresh)
            return Result.Success(ToResponse(existing!, latestMessageId.Value));

        var messages = await db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.Id)
            .Take(MaxMessages)
            .Select(m => new { m.SenderType, m.Content, m.MessageType })
            .ToListAsync(cancellationToken);
        messages.Reverse(); // back to chronological order

        var transcript = BuildTranscript(messages.Select(m => (m.SenderType.ToString(), m.Content, m.MessageType.ToString())));
        var messageCount = await db.Messages.CountAsync(m => m.ConversationId == conversationId, cancellationToken);

        var (system, user) = full ? FullPrompt(transcript) : ShortPrompt(transcript);
        var completion = await llm.GenerateAsync(new LlmRequest(system, [new LlmMessage("user", user)]), cancellationToken);
        if (completion.IsFailure)
            return Result.Failure<ConversationSummaryResponse>(completion.Error);

        var text = completion.Value.Text;
        var now = DateTime.UtcNow;

        var summary = existing ?? new ConversationSummary
        {
            TenantId = tenantId!.Value,
            ConversationId = conversationId,
            CreatedAt = now
        };

        if (full)
            summary.FullSummary = text;
        else
            summary.ShortSummary = text;

        // A full-summary run also refreshes the short line if we never had one.
        if (full && string.IsNullOrWhiteSpace(summary.ShortSummary))
            summary.ShortSummary = FirstLine(text);

        summary.UpToMessageId = latestMessageId.Value;
        summary.MessageCount = messageCount;
        summary.GeneratedByUserId = currentUserId == 0 ? null : currentUserId;
        summary.GeneratedAt = now;
        summary.UpdatedAt = now;

        if (existing is null)
            db.ConversationSummaries.Add(summary);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToResponse(summary, latestMessageId.Value));
    }

    private static ConversationSummaryResponse ToResponse(ConversationSummary s, int latestMessageId) =>
        new(s.ConversationId, s.ShortSummary, s.FullSummary, s.MessageCount, s.UpToMessageId < latestMessageId, s.GeneratedAt);

    private static string BuildTranscript(IEnumerable<(string Sender, string Content, string Kind)> messages)
    {
        var sb = new StringBuilder();
        foreach (var (sender, content, kind) in messages)
        {
            var line = string.IsNullOrWhiteSpace(content)
                ? $"[{kind}]"
                : content.Length > MaxContentChars ? content[..MaxContentChars] + "…" : content;
            sb.Append(sender).Append(": ").AppendLine(line);
        }
        return sb.ToString();
    }

    private static string FirstLine(string text)
    {
        var idx = text.IndexOf('\n');
        var line = idx >= 0 ? text[..idx] : text;
        return line.Trim().TrimStart('-', '•', '*', ' ');
    }

    private static (string System, string User) ShortPrompt(string transcript) =>
    (
        "You are a support-inbox assistant. Summarize the conversation between a customer and support " +
        "agents in ONE concise sentence so an agent can grasp the situation instantly. Reply in the same " +
        "language the customer uses (Arabic — including Egyptian dialect — or English). Output only the sentence.",
        $"Conversation:\n{transcript}"
    );

    private static (string System, string User) FullPrompt(string transcript) =>
    (
        "You are a support-inbox assistant. Produce a structured summary of the conversation with exactly " +
        "these three sections, each as a short bullet list: \"Key points\", \"Decisions\", \"What's needed\". " +
        "Reply in the same language the customer uses (Arabic — including Egyptian dialect — or English). " +
        "Be concise and factual; do not invent details.",
        $"Conversation:\n{transcript}"
    );

    /// <summary>Tenant-scopes and RBAC-checks a conversation: managers/admins may access any; agents only their own.</summary>
    private async Task<Result> ResolveConversationAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, CancellationToken cancellationToken)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var conv = await db.Conversations.AsNoTracking()
            .Where(c => c.Id == conversationId && c.TenantId == tid && !c.IsDeleted)
            .Select(c => new { c.AssignedUserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (conv is null)
            return Result.Failure(AppErrors.ConversationNotFound);

        if (!isPrivileged && conv.AssignedUserId != currentUserId)
            return Result.Failure(AppErrors.ConversationAccessDenied);

        return Result.Success();
    }
}
