using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.Performance;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;
using WaslX.Domain.Specifications;

namespace WaslX.Application.Features.Escalation.Services
{
    public class AgentPerformanceUpdateService : IAgentPerformanceUpdateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AgentPerformanceUpdateService> _logger;
        private const int MaxRetries = 3;

        public AgentPerformanceUpdateService(IUnitOfWork unitOfWork, ILogger<AgentPerformanceUpdateService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private sealed class ByUserIdSpec : Specification<AgentPerformance, int>
        {
            public ByUserIdSpec(int userId) : base(p => p.UserId == userId) { }
        }

        public async Task RecordAgentReplyAsync(int agentUserId, int conversationId, double responseTimeSeconds, CancellationToken ct = default)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                var perf = await GetOrCreateAsync(agentUserId, ct);
                var totalResponseSeconds = perf.AvgResponseTime * perf.ChatsHandled;
                perf.ChatsHandled++;
                totalResponseSeconds += (decimal)responseTimeSeconds;
                perf.AvgResponseTime = perf.ChatsHandled > 0
                    ? decimal.Round(totalResponseSeconds / perf.ChatsHandled, 2)
                    : 0;
                perf.LastUpdated = DateTime.UtcNow;
                await SaveAsync(perf, ct);
            }, ct);
        }

        public async Task RecordConversationClosedAsync(int agentUserId, bool resolved, CancellationToken ct = default)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                var perf = await GetOrCreateAsync(agentUserId, ct);
                if (perf.ActiveChats > 0)
                    perf.ActiveChats--;

                if (resolved)
                {
                    perf.ResolvedChats++;
                    perf.ResolutionRate = perf.ChatsHandled > 0
                        ? decimal.Round((decimal)perf.ResolvedChats / perf.ChatsHandled, 4)
                        : 1m;
                }
                perf.LastUpdated = DateTime.UtcNow;
                await SaveAsync(perf, ct);
            }, ct);
        }

        public async Task RecordConversationAssignedAsync(int agentUserId, CancellationToken ct = default)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                var perf = await GetOrCreateAsync(agentUserId, ct);
                perf.ActiveChats++;
                perf.LastUpdated = DateTime.UtcNow;
                await SaveAsync(perf, ct);
            }, ct);
        }

        public async Task RecordConversationReopenedAsync(int agentUserId, CancellationToken ct = default)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                var perf = await GetOrCreateAsync(agentUserId, ct);
                perf.ActiveChats++;
                if (perf.ResolvedChats > 0)
                    perf.ResolvedChats--;
                perf.ResolutionRate = perf.ChatsHandled > 0
                    ? decimal.Round((decimal)perf.ResolvedChats / perf.ChatsHandled, 4)
                    : 0m;
                perf.LastUpdated = DateTime.UtcNow;
                await SaveAsync(perf, ct);
            }, ct);
        }

        private async Task ExecuteWithRetryAsync(Func<Task> operation, CancellationToken ct, int attempt = 0)
        {
            while (true)
            {
                try
                {
                    await operation();
                    return;
                }
                catch (Exception ex) when (attempt < MaxRetries && IsConcurrencyException(ex))
                {
                    attempt++;
                    _logger.LogWarning(ex, "Concurrency conflict updating agent performance (attempt {Attempt}/{MaxRetries}). Retrying.", attempt, MaxRetries);
                    // Ensure a fresh DbContext on retry
                    _unitOfWork.ResetContext();
                }
            }
        }

        private static bool IsConcurrencyException(Exception ex)
        {
            var typeName = ex.GetType().FullName ?? string.Empty;
            return typeName.Contains("DbUpdateConcurrencyException") ||
                   typeName.Contains("OptimisticConcurrencyException");
        }

        private async Task<AgentPerformance> GetOrCreateAsync(int userId, CancellationToken ct)
        {
            var repo = _unitOfWork.GetRepository<AgentPerformance, int>();
            var spec = new ByUserIdSpec(userId);
            var perf = await repo.GetWithSpecAsync(spec);
            if (perf != null)
                return perf;

            perf = new AgentPerformance
            {
                UserId = userId,
                ChatsHandled = 0,
                AvgResponseTime = 0,
                ResolutionRate = 1m,
                ActiveChats = 0,
                ResolvedChats = 0,
                LastUpdated = DateTime.UtcNow
            };
            await repo.AddAsync(perf, ct);
            return perf;
        }

        private async Task SaveAsync(AgentPerformance perf, CancellationToken ct)
        {
            var repo = _unitOfWork.GetRepository<AgentPerformance, int>();
            repo.Update(perf);
            await _unitOfWork.CompleteAsync();
        }
    }
}
