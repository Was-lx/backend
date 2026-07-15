using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Tags;
using WaslX.Application.Features.Tags.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Tags;

// ── Queries ──
public record GetTagsQuery(int? TenantId) : IQuery<IReadOnlyList<TagResponse>>;
public class GetTagsQueryHandler(ITagService svc) : IQueryHandler<GetTagsQuery, IReadOnlyList<TagResponse>>
{
    public Task<Result<IReadOnlyList<TagResponse>>> Handle(GetTagsQuery request, CancellationToken cancellationToken) =>
        svc.GetAllAsync(request.TenantId, cancellationToken);
}

// ── Commands ──
public record CreateTagCommand(int? TenantId, UpsertTagRequest Request) : ICommand<TagResponse>;
public class CreateTagCommandHandler(ITagService svc) : ICommandHandler<CreateTagCommand, TagResponse>
{
    public Task<Result<TagResponse>> Handle(CreateTagCommand request, CancellationToken cancellationToken) =>
        svc.CreateAsync(request.TenantId, request.Request, cancellationToken);
}

public record UpdateTagCommand(int? TenantId, int Id, UpsertTagRequest Request) : ICommand<TagResponse>;
public class UpdateTagCommandHandler(ITagService svc) : ICommandHandler<UpdateTagCommand, TagResponse>
{
    public Task<Result<TagResponse>> Handle(UpdateTagCommand request, CancellationToken cancellationToken) =>
        svc.UpdateAsync(request.TenantId, request.Id, request.Request, cancellationToken);
}

public record DeleteTagCommand(int? TenantId, int Id) : ICommand;
public class DeleteTagCommandHandler(ITagService svc) : ICommandHandler<DeleteTagCommand>
{
    public Task<Result> Handle(DeleteTagCommand request, CancellationToken cancellationToken) =>
        svc.DeleteAsync(request.TenantId, request.Id, cancellationToken);
}

public record ApplyTagCommand(int? TenantId, int ConversationId, int TagId) : ICommand;
public class ApplyTagCommandHandler(ITagService svc) : ICommandHandler<ApplyTagCommand>
{
    public Task<Result> Handle(ApplyTagCommand request, CancellationToken cancellationToken) =>
        svc.ApplyToConversationAsync(request.TenantId, request.ConversationId, request.TagId, cancellationToken);
}

public record RemoveTagCommand(int? TenantId, int ConversationId, int TagId) : ICommand;
public class RemoveTagCommandHandler(ITagService svc) : ICommandHandler<RemoveTagCommand>
{
    public Task<Result> Handle(RemoveTagCommand request, CancellationToken cancellationToken) =>
        svc.RemoveFromConversationAsync(request.TenantId, request.ConversationId, request.TagId, cancellationToken);
}
