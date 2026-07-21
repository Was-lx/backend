using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.AI;

public interface IAiAgentReplyService
{
    Task ReplyAsync(int tenantId, int conversationId, int messageId, CancellationToken cancellationToken = default);
}
