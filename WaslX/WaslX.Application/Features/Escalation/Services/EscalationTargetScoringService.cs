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

                // 1. Load the conversation to find the currently assigned agent
                var conversationRepo = unitOfWork.GetRepository<Conversation, int>();
                var conversation = await conversationRepo.GetByIdAsync(input.ConversationId, cancellationToken);
                int? assignedAgentId = conversation?.AssignedUserId;

                // 2. Load eligible Agents
                var spec = new EligibleAgentsSpecification(input.TenantId);
                var candidates = (await userRepo.GetAllWithSpecAsync(spec, false)).ToList();

                // 3. Exclude the currently assigned agent for THIS conversation (business rule)
                if (assignedAgentId.HasValue)
                {
                    candidates = candidates.Where(c => c.Id != assignedAgentId.Value).ToList();
                }

                if (candidates.Count == 0)
                {
                    logger.LogWarning("No alternative eligible agents for escalation {EscalationId} (assigned agent {AssignedAgentId} excluded).", input.EscalationId, assignedAgentId);
                    return await RecordNoTargetAsync(input, escalationRepo, cancellationToken);
                }

                var candidateIds = candidates.Select(c => c.Id).ToList();

                // 2. Load performance metrics (includes ActiveChats from AgentPerformance table)
                var performances = await performanceProvider.GetManyAsync(candidateIds);

                // 3. Build workload map from AgentPerformance.ActiveChats (single source of truth)
                var candidateWorkloads = candidateIds.ToDictionary(id => id, id =>
                {
                    var p = performances.TryGetValue(id, out var perf) ? perf : null;
                    return p?.ActiveChats ?? 0;
                });

                // 4. Apply relative workload filtering: eligible = ActiveChats <= (lowest + WorkloadLimit)
                var lowestActiveChats = candidateWorkloads.Values.Min();
                var maxAllowedWorkload = lowestActiveChats + _options.WorkloadLimit;

                // 5. Build candidate scores (workload score computed only for eligible agents)
                var eligibleCandidateScores = new List<EscalationCandidateScore>();

                foreach (var candidate in candidates)
                {
                    var p = performances.TryGetValue(candidate.Id, out var perf) ? perf : new AgentPerformanceSnapshot { UserId = candidate.Id };
                    var activeChats = candidateWorkloads[candidate.Id];
                    bool isEligible = activeChats <= maxAllowedWorkload;

                    decimal workloadScore = isEligible ? 1.0m / (1.0m + activeChats) : 0m;
                    decimal totalScore = isEligible
                        ? (p.PerformanceScore * _options.PerformanceWeight) +
                          (p.ResponseSpeedScore * _options.ResponseSpeedWeight) +
                          (workloadScore * _options.WorkloadWeight)
                        : 0m;

                    eligibleCandidateScores.Add(new EscalationCandidateScore
                    {
                        UserId = candidate.Id,
                        UserName = candidate.Name,
                        PerformanceScore = p.PerformanceScore,
                        ResponseSpeedScore = p.ResponseSpeedScore,
                        WorkloadScore = workloadScore,
                        TotalScore = totalScore,
                        ActiveChats = activeChats,
                        Status = isEligible ? "Eligible" : "Overloaded"
                    });
                }

                // 6. Select the fairest Agent (only from eligible candidates): the one with the FEWEST
                // active chats wins, so escalations rotate across the team instead of always piling onto
                // whoever currently scores highest — a consistently top-performing agent would otherwise
                // never stop receiving new work while teammates sit idle. TotalScore only breaks ties
                // between agents carrying the same current load.
                var availableCandidates = eligibleCandidateScores.Where(c => c.Status == "Eligible").ToList();
                if (!availableCandidates.Any())
                {
                    logger.LogWarning("No eligible agents after workload filtering for escalation {EscalationId}.", input.EscalationId);
                    return await RecordNoTargetAsync(input, escalationRepo, cancellationToken);
                }
                var bestCandidate = availableCandidates
                    .OrderBy(c => c.ActiveChats)
                    .ThenByDescending(c => c.TotalScore)
                    .First();

                var reason = $"Selected {bestCandidate.UserName} to balance workload ({bestCandidate.ActiveChats} active chats), score {bestCandidate.TotalScore:0.00}.";

                // 7. Store the recommendation
                var result = new EscalationScoringResult
                {
                    EscalationId = input.EscalationId,
                    SuggestedAssigneeId = bestCandidate.UserId,
                    SuggestedAssigneeName = bestCandidate.UserName,
                    Score = bestCandidate.TotalScore,
                    Reason = reason,
                    Candidates = eligibleCandidateScores
                };

                await SaveRecommendationAsync(escalationRepo, input, bestCandidate.UserId, reason, bestCandidate.TotalScore, eligibleCandidateScores, cancellationToken);
                await unitOfWork.CompleteAsync();

                // 8. Connect to US-4.5 mode handling (recommend vs autoAssign)
                var screeningResult = await assignmentService.HandleScoringResultAsync(input.TenantId, input.EscalationId, cancellationToken);

                // 9. Emit SignalR event (send the full recommendation, incl. ConversationId, for the
                // inbox banner — EscalationScoringResult carries no ConversationId and can't be used here).
                if (screeningResult.IsSuccess && screeningResult.Value.Mode == "Recommend")
                {
                    await realtimeNotifier.EscalationRecommendationUpdatedAsync(input.TenantId, screeningResult.Value, cancellationToken);
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
            CancellationToken cancellationToken)
        {
            var reason = "No eligible agents available";
            var result = new EscalationScoringResult
            {
                EscalationId = input.EscalationId,
                SuggestedAssigneeId = null,
                SuggestedAssigneeName = string.Empty,
                Score = 0,
                Reason = reason,
                Candidates = Array.Empty<EscalationCandidateScore>()
            };

            await SaveRecommendationAsync(escalationRepo, input, null, reason, 0, new List<EscalationCandidateScore>(), cancellationToken);
            await unitOfWork.CompleteAsync();

            // No candidate scores here, so route through the assignment service to get the full
            // EscalationRecommendation (incl. ConversationId) the inbox banner needs.
            var recommendation = await assignmentService.GetRecommendationAsync(input.TenantId, input.ConversationId, cancellationToken);
            if (recommendation.IsSuccess)
            {
                await realtimeNotifier.EscalationRecommendationUpdatedAsync(input.TenantId, recommendation.Value, cancellationToken);
            }

            return result;
        }

        private async Task SaveRecommendationAsync(
            IGenericRepository<Domain.Entities.Escalation, int> escalationRepo,
            EscalationScoringInput input,
            int? assigneeId,
            string reason,
            decimal score,
            List<EscalationCandidateScore> candidates,
            CancellationToken cancellationToken)
        {
            var escalation = await escalationRepo.GetByIdAsync(input.EscalationId, cancellationToken);
            if (escalation != null)
            {
                escalation.SuggestedAssigneeId = assigneeId;
                escalation.SuggestedReason = reason;
                escalation.SuggestedScore = score;
                escalation.Topic = input.Topic;
                escalation.RecommendationGeneratedAtUtc = DateTime.UtcNow;
                escalationRepo.Update(escalation);

                var snapshotRepo = unitOfWork.GetRepository<EscalationCandidateSnapshot, int>();
                // Same ordering as the selection above: fewest active chats first, score as tiebreaker —
                // so RankingOrder in the saved snapshot actually matches who got picked and why.
                var ranked = candidates
                    .OrderBy(c => c.Status == "Overloaded" ? 1 : 0)
                    .ThenBy(c => c.ActiveChats)
                    .ThenByDescending(c => c.TotalScore)
                    .ToList();

                for (int i = 0; i < ranked.Count; i++)
                {
                    var c = ranked[i];
                    await snapshotRepo.AddAsync(new EscalationCandidateSnapshot
                    {
                        EscalationId = input.EscalationId,
                        AgentId = c.UserId,
                        AgentName = c.UserName,
                        OverallScore = c.TotalScore,
                        PerformanceScore = c.PerformanceScore,
                        ResponseSpeedScore = c.ResponseSpeedScore,
                        WorkloadScore = c.WorkloadScore,
                        ActiveChats = c.ActiveChats,
                        RankingOrder = i + 1,
                        Status = c.Status,
                        Reason = i == 0 ? reason : null,
                        CreatedAtUtc = DateTime.UtcNow
                    }, cancellationToken);
                }
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
    }
}
