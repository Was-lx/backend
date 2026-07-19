using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Abstractions.Screening;
using WaslX.Application.Features.Escalation.Models;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using WaslX.Domain.Specifications;

namespace WaslX.Application.Features.Escalation.Services
{
    public class EscalationTargetScoringService(
        IUnitOfWork unitOfWork,
        IAgentPerformanceProvider performanceProvider,
        IOptions<EscalationScoringOptions> options,
        IInboxRealtimeNotifier realtimeNotifier,
        IEscalationAssignmentService assignmentService,
        ILogger<EscalationTargetScoringService> logger) : IEscalationTargetScoringService
    {
        private readonly EscalationScoringOptions _options = options.Value;

        public async Task<EscalationScoringResult> ScoreAsync(EscalationScoringInput input, CancellationToken cancellationToken = default)
        {
            try
            {
                var userRepo = unitOfWork.GetRepository<User, int>();
                var escalationRepo = unitOfWork.GetRepository<Domain.Entities.Escalation, int>();

                // 1. Load eligible Agents
                var spec = new EligibleAgentsSpecification(input.TenantId);
                var candidates = await userRepo.GetAllWithSpecAsync(spec, false);

                if (!candidates.Any())
                {
                    logger.LogWarning("No eligible agents found for escalation {EscalationId} in tenant {TenantId}.", input.EscalationId, input.TenantId);
                    return await RecordNoTargetAsync(input, escalationRepo, "No active agents available", cancellationToken);
                }

                var candidateIds = candidates.Select(c => c.Id).ToList();

                // 2. Load performance metrics
                var performances = await performanceProvider.GetManyAsync(candidateIds);

                // 3. Calculate workload dynamically
                // We calculate open escalations currently assigned to these agents
                var openEscalationsSpec = new OpenEscalationsSpecification(input.TenantId);
                var openEscalations = await escalationRepo.GetAllWithSpecAsync(openEscalationsSpec, false);
                
                var workloadMap = openEscalations
                    .Where(e => e.AssignedUserId.HasValue)
                    .GroupBy(e => e.AssignedUserId!.Value)
                    .ToDictionary(g => g.Key, g => g.Count());

                var candidateWorkloads = candidateIds.ToDictionary(id => id, id => new WorkloadSnapshot
                {
                    UserId = id,
                    OpenEscalations = workloadMap.TryGetValue(id, out var count) ? count : 0,
                    ActiveConversations = 0 // Future: query active conversations
                });

                // 4. Apply workload tolerance
                var lowestWorkload = candidateWorkloads.Values.Min(w => w.OpenEscalations);
                var maxAllowedWorkload = lowestWorkload + _options.WorkloadTolerance;

                var eligibleCandidateScores = new List<EscalationCandidateScore>();

                foreach (var candidate in candidates)
                {
                    var p = performances.TryGetValue(candidate.Id, out var perf) ? perf : new AgentPerformanceSnapshot { UserId = candidate.Id };
                    var w = candidateWorkloads[candidate.Id];

                    bool isOverloaded = w.OpenEscalations > maxAllowedWorkload;

                    // Workload score (lower is better, so we invert it relative to others if we normalize. 
                    // To keep things simple 0-1, we can use 1 / (1 + openEscalations))
                    decimal workloadScore = 1.0m / (1.0m + w.OpenEscalations);

                    decimal totalScore = (p.PerformanceScore * _options.PerformanceWeight) +
                                         (p.ResponseSpeedScore * _options.ResponseSpeedWeight) +
                                         (workloadScore * _options.WorkloadWeight);

                    eligibleCandidateScores.Add(new EscalationCandidateScore
                    {
                        UserId = candidate.Id,
                        UserName = candidate.Name,
                        PerformanceScore = p.PerformanceScore,
                        ResponseSpeedScore = p.ResponseSpeedScore,
                        WorkloadScore = workloadScore,
                        TotalScore = totalScore,
                        Status = isOverloaded ? "Overloaded" : "Eligible"
                    });
                }

                // If all overloaded, fallback to least loaded
                var availableCandidates = eligibleCandidateScores.Where(c => c.Status == "Eligible").ToList();
                if (!availableCandidates.Any())
                {
                    logger.LogInformation("All candidates overloaded for escalation {EscalationId}. Falling back to least loaded.", input.EscalationId);
                    // Find the one with highest workloadScore (which means lowest actual workload)
                    var leastLoaded = eligibleCandidateScores.OrderByDescending(c => c.WorkloadScore).First();
                    leastLoaded.Status = "Eligible (Fallback)";
                    availableCandidates.Add(leastLoaded);
                }

                // 5. Select the best Agent
                var bestCandidate = availableCandidates.OrderByDescending(c => c.TotalScore).First();

                var reason = $"Selected {bestCandidate.UserName} based on performance and workload.";

                // 6. Store the recommendation
                var result = new EscalationScoringResult
                {
                    EscalationId = input.EscalationId,
                    SuggestedAssigneeId = bestCandidate.UserId,
                    SuggestedAssigneeName = bestCandidate.UserName,
                    Score = bestCandidate.TotalScore,
                    Reason = reason,
                    Candidates = eligibleCandidateScores
                };

                await SaveRecommendationAsync(escalationRepo, input.EscalationId, bestCandidate.UserId, reason, cancellationToken);
                await unitOfWork.CompleteAsync();

                // 7. Connect to US-4.5 mode handling (recommend vs autoAssign)
                var screeningResult = await assignmentService.HandleScoringResultAsync(input.TenantId, input.EscalationId, cancellationToken);

                // 8. Emit SignalR event (always send scoring result for recommendation panels)
                // For recommend mode: `HandleScoringResultAsync` already emits events.
                // For autoAssign mode: ownership transfer events are emitted by the assignment service.
                // Only manually emit if in recommend mode (assignment service handles autoAssign events).
                if (screeningResult.IsSuccess && screeningResult.Value.Mode == "Recommend")
                {
                    await realtimeNotifier.EscalationRecommendationUpdatedAsync(input.TenantId, result, cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to score escalation targets for escalation {EscalationId}", input.EscalationId);
                throw;
            }
        }

        private async Task<EscalationScoringResult> RecordNoTargetAsync(
            EscalationScoringInput input, 
            IGenericRepository<Domain.Entities.Escalation, int> escalationRepo, 
            string reason, 
            CancellationToken cancellationToken)
        {
            var result = new EscalationScoringResult
            {
                EscalationId = input.EscalationId,
                SuggestedAssigneeId = null,
                SuggestedAssigneeName = string.Empty,
                Score = 0,
                Reason = reason,
                Candidates = Array.Empty<EscalationCandidateScore>()
            };

            await SaveRecommendationAsync(escalationRepo, input.EscalationId, null, reason, cancellationToken);
            await unitOfWork.CompleteAsync();

            await realtimeNotifier.EscalationRecommendationUpdatedAsync(input.TenantId, result, cancellationToken);

            return result;
        }

        private async Task SaveRecommendationAsync(
            IGenericRepository<Domain.Entities.Escalation, int> escalationRepo, 
            int escalationId, 
            int? assigneeId, 
            string reason, 
            CancellationToken cancellationToken)
        {
            var escalation = await escalationRepo.GetByIdAsync(escalationId, cancellationToken);
            if (escalation != null)
            {
                escalation.SuggestedAssigneeId = assigneeId;
                escalation.SuggestedReason = reason;
                escalationRepo.Update(escalation);
            }
        }

        private class EligibleAgentsSpecification : Specification<User, int>
        {
            public EligibleAgentsSpecification(int tenantId) 
                : base(u => u.TenantId == tenantId && u.Role.Name == "Agent" && u.Status == "Active" && u.IsOnline && !u.IsOnBreak)
            {
                AddInclude(u => u.Role);
            }
        }

        private class OpenEscalationsSpecification : Specification<Domain.Entities.Escalation, int>
        {
            public OpenEscalationsSpecification(int tenantId)
                : base(e => e.TenantId == tenantId && e.Status == EscalationStatus.Open)
            {
            }
        }
    }
}
