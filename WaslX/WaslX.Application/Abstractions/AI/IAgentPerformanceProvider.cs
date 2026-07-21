using System.Collections.Generic;
using System.Threading.Tasks;
using WaslX.Application.Features.Escalation.Models;

namespace WaslX.Application.Abstractions.AI
{
    public interface IAgentPerformanceProvider
    {
        Task<IReadOnlyDictionary<int, AgentPerformanceSnapshot>> GetManyAsync(IEnumerable<int> userIds);
    }
}
