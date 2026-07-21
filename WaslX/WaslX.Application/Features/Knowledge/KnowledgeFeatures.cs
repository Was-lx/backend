using FluentValidation;
using WaslX.Application.Abstractions.Knowledge;
using WaslX.Application.Abstractions.Media;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Rag;
using WaslX.Application.Features.Knowledge.Dtos;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;

namespace WaslX.Application.Features.Knowledge;

// ── FAQ queries/commands ──

public record GetFaqsQuery(int? TenantId, int Page, int PageSize) : IQuery<KnowledgePagedResult<FaqResponse>>;
public class GetFaqsQueryHandler(IKnowledgeService svc) : IQueryHandler<GetFaqsQuery, KnowledgePagedResult<FaqResponse>>
{
    public Task<Result<KnowledgePagedResult<FaqResponse>>> Handle(GetFaqsQuery request, CancellationToken cancellationToken) =>
        svc.GetFaqsAsync(request.TenantId, request.Page, request.PageSize, cancellationToken);
}

public record CreateFaqCommand(int? TenantId, UpsertFaqRequest Request) : ICommand<KnowledgeMutationResult>;
public class CreateFaqCommandHandler(IKnowledgeService svc) : ICommandHandler<CreateFaqCommand, KnowledgeMutationResult>
{
    public Task<Result<KnowledgeMutationResult>> Handle(CreateFaqCommand request, CancellationToken cancellationToken) =>
        svc.CreateFaqAsync(request.TenantId, request.Request, cancellationToken);
}

public record UpdateFaqCommand(int? TenantId, int Id, UpsertFaqRequest Request) : ICommand<KnowledgeMutationResult>;
public class UpdateFaqCommandHandler(IKnowledgeService svc) : ICommandHandler<UpdateFaqCommand, KnowledgeMutationResult>
{
    public Task<Result<KnowledgeMutationResult>> Handle(UpdateFaqCommand request, CancellationToken cancellationToken) =>
        svc.UpdateFaqAsync(request.TenantId, request.Id, request.Request, cancellationToken);
}

public record DeleteFaqCommand(int? TenantId, int Id) : ICommand;
public class DeleteFaqCommandHandler(IKnowledgeService svc) : ICommandHandler<DeleteFaqCommand>
{
    public Task<Result> Handle(DeleteFaqCommand request, CancellationToken cancellationToken) =>
        svc.DeleteFaqAsync(request.TenantId, request.Id, cancellationToken);
}

// ── Document (generic: FAQ/Document/Website) queries/commands ──

public record GetKnowledgeDocumentsQuery(int? TenantId, KnowledgeSourceType? SourceType, int Page, int PageSize)
    : IQuery<KnowledgePagedResult<KnowledgeDocumentResponse>>;
public class GetKnowledgeDocumentsQueryHandler(IKnowledgeService svc)
    : IQueryHandler<GetKnowledgeDocumentsQuery, KnowledgePagedResult<KnowledgeDocumentResponse>>
{
    public Task<Result<KnowledgePagedResult<KnowledgeDocumentResponse>>> Handle(GetKnowledgeDocumentsQuery request, CancellationToken cancellationToken) =>
        svc.GetDocumentsAsync(request.TenantId, request.SourceType, request.Page, request.PageSize, cancellationToken);
}

public record DeleteKnowledgeDocumentCommand(int? TenantId, int DocumentId) : ICommand;
public class DeleteKnowledgeDocumentCommandHandler(IKnowledgeService svc) : ICommandHandler<DeleteKnowledgeDocumentCommand>
{
    public Task<Result> Handle(DeleteKnowledgeDocumentCommand request, CancellationToken cancellationToken) =>
        svc.DeleteDocumentAsync(request.TenantId, request.DocumentId, cancellationToken);
}

/// <summary>Resets a document to Pending; the controller enqueues the ingestion job with the returned id.</summary>
public record PrepareReindexCommand(int? TenantId, int DocumentId) : ICommand<int>;
public class PrepareReindexCommandHandler(IKnowledgeService svc) : ICommandHandler<PrepareReindexCommand, int>
{
    public Task<Result<int>> Handle(PrepareReindexCommand request, CancellationToken cancellationToken) =>
        svc.PrepareReindexAsync(request.TenantId, request.DocumentId, cancellationToken);
}

// ── Document upload (M4) ──

public record UploadKnowledgeDocumentCommand(
    int? TenantId, byte[] FileContent, string FileName, string ContentType, string? Title, string Language)
    : ICommand<KnowledgeMutationResult>;

public class UploadKnowledgeDocumentCommandHandler(IKnowledgeService svc, IMediaStorageService mediaStorage)
    : ICommandHandler<UploadKnowledgeDocumentCommand, KnowledgeMutationResult>
{
    public async Task<Result<KnowledgeMutationResult>> Handle(UploadKnowledgeDocumentCommand request, CancellationToken cancellationToken)
    {
        var upload = await mediaStorage.UploadAsync(request.FileContent, request.FileName, request.ContentType, cancellationToken);
        if (upload.IsFailure)
            return Result.Failure<KnowledgeMutationResult>(upload.Error);

        var language = Enum.TryParse<Language>(request.Language, true, out var lang) ? lang : Language.English;
        var title = string.IsNullOrWhiteSpace(request.Title) ? request.FileName : request.Title;

        var document = await svc.CreateDocumentAsync(
            request.TenantId, KnowledgeSourceType.Document, title, language,
            fileUrl: upload.Value.Url, fileName: request.FileName, mimeType: request.ContentType,
            cancellationToken: cancellationToken);
        if (document.IsFailure)
            return Result.Failure<KnowledgeMutationResult>(document.Error);

        return Result.Success(new KnowledgeMutationResult(document.Value, document.Value));
    }
}

// ── Website (M5) ──

public record AddWebsiteKnowledgeCommand(int? TenantId, string Url, string? Title, string Language) : ICommand<KnowledgeMutationResult>;

public class AddWebsiteKnowledgeCommandValidator : AbstractValidator<AddWebsiteKnowledgeCommand>
{
    public AddWebsiteKnowledgeCommandValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Enter a valid http(s) URL.");
    }
}

public class AddWebsiteKnowledgeCommandHandler(IKnowledgeService svc) : ICommandHandler<AddWebsiteKnowledgeCommand, KnowledgeMutationResult>
{
    public async Task<Result<KnowledgeMutationResult>> Handle(AddWebsiteKnowledgeCommand request, CancellationToken cancellationToken)
    {
        var language = Enum.TryParse<Language>(request.Language, true, out var lang) ? lang : Language.English;
        var title = string.IsNullOrWhiteSpace(request.Title) ? request.Url : request.Title;

        var document = await svc.CreateDocumentAsync(
            request.TenantId, KnowledgeSourceType.Website, title, language,
            sourceUrl: request.Url, cancellationToken: cancellationToken);
        if (document.IsFailure)
            return Result.Failure<KnowledgeMutationResult>(document.Error);

        return Result.Success(new KnowledgeMutationResult(document.Value, document.Value));
    }
}

// ── Search (M6) ──

public record SearchKnowledgeQuery(int? TenantId, string Query, int? TopK, string? SourceType) : IQuery<RetrievalResult>;

public class SearchKnowledgeQueryHandler(IKnowledgeRetriever retriever) : IQueryHandler<SearchKnowledgeQuery, RetrievalResult>
{
    public Task<Result<RetrievalResult>> Handle(SearchKnowledgeQuery request, CancellationToken cancellationToken) =>
        request.TenantId is not { } tid
            ? Task.FromResult(Result.Failure<RetrievalResult>(AppErrors.NoTenantContext))
            : retriever.RetrieveAsync(tid, request.Query, request.TopK, request.SourceType, cancellationToken);
}

// ── Ask (M7) ──

public record AskKnowledgeQuery(int? TenantId, string Question) : IQuery<RagAnswer>;

public class AskKnowledgeQueryHandler(IRagOrchestrator orchestrator) : IQueryHandler<AskKnowledgeQuery, RagAnswer>
{
    public Task<Result<RagAnswer>> Handle(AskKnowledgeQuery request, CancellationToken cancellationToken) =>
        request.TenantId is not { } tid
            ? Task.FromResult(Result.Failure<RagAnswer>(AppErrors.NoTenantContext))
            : orchestrator.AskAsync(tid, request.Question, cancellationToken: cancellationToken);
}
