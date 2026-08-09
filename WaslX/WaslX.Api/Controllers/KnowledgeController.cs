using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WaslX.Api.Contracts;
using WaslX.Api.Extensions;
using WaslX.Application.Abstractions.Knowledge;
using WaslX.Application.Features.Knowledge;
using WaslX.Application.Features.Knowledge.Dtos;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;

namespace WaslX.Api.Controllers;

/// <summary>
/// Tenant knowledge-base management: FAQs plus the generic document lifecycle shared by every
/// ingest source (FAQ/Document/Website). Every mutation that needs (re-)indexing enqueues the
/// ingestion pipeline as a Hangfire job — this is the one place in the app allowed to do so for
/// knowledge, since Application has no dependency on Hangfire.
/// </summary>
[ApiController]
[Route("api/knowledge")]
[Authorize(Roles = "Admin,Manager")]
public class KnowledgeController(ISender sender, IBackgroundJobClient backgroundJobs) : ControllerBase
{
    // ── FAQs ──

    [HttpGet("faqs")]
    public async Task<IActionResult> GetFaqs([FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetFaqsQuery(User.GetTenantId(), page, pageSize), cancellationToken)).ToActionResult();

    [HttpPost("faqs")]
    public async Task<IActionResult> CreateFaq([FromBody] UpsertFaqRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateFaqCommand(User.GetTenantId(), request), cancellationToken);
        if (result.IsSuccess)
            Enqueue(result.Value.DocumentId);
        return result.ToActionResult();
    }

    [HttpPut("faqs/{id:int}")]
    public async Task<IActionResult> UpdateFaq(int id, [FromBody] UpsertFaqRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateFaqCommand(User.GetTenantId(), id, request), cancellationToken);
        if (result.IsSuccess)
            Enqueue(result.Value.DocumentId);
        return result.ToActionResult();
    }

    [HttpDelete("faqs/{id:int}")]
    public async Task<IActionResult> DeleteFaq(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteFaqCommand(User.GetTenantId(), id), cancellationToken)).ToActionResult();

    [HttpPost("documents/upload")]
    [RequestSizeLimit(25_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument(
        [FromForm] IFormFile? file, [FromForm] string? title, [FromForm] string language = "English", CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return Result.Failure<KnowledgeMutationResult>(AppErrors.MediaFileRequired).ToActionResult();

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        var command = new UploadKnowledgeDocumentCommand(User.GetTenantId(), buffer.ToArray(), file.FileName, contentType, title, language);
        var result = await sender.Send(command, cancellationToken);
        if (result.IsSuccess)
            Enqueue(result.Value.DocumentId);
        return result.ToActionResult();
    }

    [HttpPost("websites")]
    public async Task<IActionResult> AddWebsite([FromBody] AddWebsiteRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddWebsiteKnowledgeCommand(User.GetTenantId(), request.Url, request.Title, request.Language ?? "English"), cancellationToken);
        if (result.IsSuccess)
            Enqueue(result.Value.DocumentId);
        return result.ToActionResult();
    }

    // ── Documents (generic) ──

    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] string? sourceType, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken cancellationToken = default)
    {
        KnowledgeSourceType? parsed = Enum.TryParse<KnowledgeSourceType>(sourceType, true, out var st) ? st : null;
        return (await sender.Send(new GetKnowledgeDocumentsQuery(User.GetTenantId(), parsed, page, pageSize), cancellationToken)).ToActionResult();
    }

    [HttpDelete("documents/{id:int}")]
    public async Task<IActionResult> DeleteDocument(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteKnowledgeDocumentCommand(User.GetTenantId(), id), cancellationToken)).ToActionResult();

    [HttpPost("documents/{id:int}/reindex")]
    public async Task<IActionResult> ReindexDocument(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PrepareReindexCommand(User.GetTenantId(), id), cancellationToken);
        if (result.IsSuccess)
            Enqueue(result.Value);
        return result.ToActionResult();
    }

    // ── Search (M6) — also usable by Agents while helping a customer, not just Admin/Manager ──

    // Rate-limited: unlike the webhook-triggered AI path, this on-demand endpoint has no per-tenant
    // AI-usage quota gate of its own, so it's the one place a user could otherwise call the embedding/
    // LLM providers as fast as they like.
    [HttpPost("search")]
    [Authorize(Roles = "Admin,Manager,Agent")]
    [EnableRateLimiting("ai-costly")]
    public async Task<IActionResult> Search([FromBody] SearchKnowledgeRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new SearchKnowledgeQuery(User.GetTenantId(), request.Query, request.TopK, request.SourceType), cancellationToken)).ToActionResult();

    // ── Ask (M7) — grounded, cited answer generation. Also Agent-usable. ──

    [HttpPost("ask")]
    [Authorize(Roles = "Admin,Manager,Agent")]
    [EnableRateLimiting("ai-costly")]
    public async Task<IActionResult> Ask([FromBody] AskKnowledgeRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new AskKnowledgeQuery(User.GetTenantId(), request.Question), cancellationToken)).ToActionResult();

    private void Enqueue(int documentId) =>
        backgroundJobs.Enqueue<IKnowledgeIngestionPipeline>(p => p.RunAsync(documentId, CancellationToken.None));
}
