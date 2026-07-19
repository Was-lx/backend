using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Features.Escalation.Models;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;
using WaslX.Domain.Specifications;

namespace WaslX.Application.Features.Escalation.Providers
{
    public class DefaultAgentPerformanceProvider : IAgentPerformanceProvider
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly EscalationScoringOptions _options;

        public DefaultAgentPerformanceProvider(IUnitOfWork unitOfWork, IOptions<EscalationScoringOptions> options)
        {
            _unitOfWork = unitOfWork;
            _options = options.Value;
        }

        public async Task<IReadOnlyDictionary<int, AgentPerformanceSnapshot>> GetManyAsync(IEnumerable<int> userIds)
        {
            var idList = userIds.ToList();
            if (idList.Count == 0)
                return new Dictionary<int, AgentPerformanceSnapshot>();

            var perfRepo = _unitOfWork.GetRepository<AgentPerformance, int>();
            var spec = new UserIdsSpecification(idList);
            var performances = await perfRepo.GetAllWithSpecAsync(spec, false);

            var perfByUser = performances.ToDictionary(p => p.UserId);

            var result = new Dictionary<int, AgentPerformanceSnapshot>(idList.Count);
            foreach (var userId in idList)
            {
                if (perfByUser.TryGetValue(userId, out var perf))
                {
                    result[userId] = new AgentPerformanceSnapshot
                    {
                        UserId = userId,
                        PerformanceScore = CalculatePerformanceScore(perf),
                        ResponseSpeedScore = CalculateResponseSpeedScore(perf),
                        ResolutionScore = perf.ResolutionRate
                    };
                }
                else
                {
                    result[userId] = new AgentPerformanceSnapshot
                    {
                        UserId = userId,
                        PerformanceScore = 0.3m,
                        ResponseSpeedScore = 0.5m,
                        ResolutionScore = 0m
                    };
                }
            }

            return result;
        }

        private sealed class UserIdsSpecification : Specification<AgentPerformance, int>
        {
            public UserIdsSpecification(List<int> ids)
                : base(p => ids.Contains(p.UserId))
            {
            }
        }

        private decimal CalculatePerformanceScore(AgentPerformance perf)
        {
            var resolutionRate = Math.Clamp(perf.ResolutionRate, 0m, 1m);
            var normalizedChats = Math.Min(perf.ChatsHandled / 100m, 1m);
            return Math.Clamp(resolutionRate * 0.7m + normalizedChats * 0.3m, 0m, 1m);
        }

        private decimal CalculateResponseSpeedScore(AgentPerformance perf)
        {
            var target = _options.AvgResponseTimeTargetSeconds;
            if (target <= 0 || perf.AvgResponseTime <= 0)
                return 0.5m;
            return Math.Clamp(1m - (perf.AvgResponseTime / (decimal)target), 0m, 1m);
        }
    }
}
