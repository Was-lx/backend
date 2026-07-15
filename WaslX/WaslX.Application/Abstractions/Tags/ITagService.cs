using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WaslX.Application.Features.Tags.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Tags;

/// <summary>Tag management (tenant-scoped) and applying/removing tags on conversations.</summary>
public interface ITagService
{
    Task<Result<IReadOnlyList<TagResponse>>> GetAllAsync(int? tenantId, CancellationToken cancellationToken = default);
    Task<Result<TagResponse>> CreateAsync(int? tenantId, UpsertTagRequest request, CancellationToken cancellationToken = default);
    Task<Result<TagResponse>> UpdateAsync(int? tenantId, int id, UpsertTagRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int? tenantId, int id, CancellationToken cancellationToken = default);
    Task<Result> ApplyToConversationAsync(int? tenantId, int conversationId, int tagId, CancellationToken cancellationToken = default);
    Task<Result> RemoveFromConversationAsync(int? tenantId, int conversationId, int tagId, CancellationToken cancellationToken = default);
}
