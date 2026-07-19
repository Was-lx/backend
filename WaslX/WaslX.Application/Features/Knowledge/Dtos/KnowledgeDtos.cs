namespace WaslX.Application.Features.Knowledge.Dtos;

/// <summary>A tenant FAQ, with its ingestion status (via the linked knowledge document, if any).</summary>
public record FaqResponse(int Id, string Question, string Answer, string Language, bool IsActive, int? DocumentId, string? IndexStatus);

public record UpsertFaqRequest(string Question, string Answer, string Language, bool IsActive = true);

/// <summary>Returned after a mutation that also touches a knowledge document — the caller enqueues indexing.</summary>
public record KnowledgeMutationResult(int EntityId, int DocumentId);

/// <summary>One ingest unit (FAQ / document / website page) and its indexing state.</summary>
public record KnowledgeDocumentResponse(
    int Id,
    string SourceType,
    string Title,
    string Language,
    string Status,
    string? ErrorMessage,
    int ChunkCount,
    int Version,
    DateTime? UpdatedAt,
    string? FileUrl,
    string? FileName,
    string? SourceUrl);

public record KnowledgePagedResult<T>(IReadOnlyList<T> Items, bool HasMore);
