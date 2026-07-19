using System.Threading;
using System.Threading.Tasks;

namespace WaslX.Application.Abstractions.Performance
{
    public interface IAgentPerformanceUpdateService
    {
        Task RecordAgentReplyAsync(int agentUserId, int conversationId, double responseTimeSeconds, CancellationToken ct = default);
        Task RecordConversationClosedAsync(int agentUserId, bool resolved, CancellationToken ct = default);
        Task RecordConversationAssignedAsync(int agentUserId, CancellationToken ct = default);
        Task RecordConversationReopenedAsync(int agentUserId, CancellationToken ct = default);
    }
}
