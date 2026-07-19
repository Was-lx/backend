using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using WaslX.Application.Abstractions.Performance;
using WaslX.Application.Features.Escalation.Services;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Contracts.Specification;
using WaslX.Domain.Entities;
using Xunit;

namespace WaslX.Tests.Performance;

public class AgentPerformanceUpdateServiceTests
{
    private const int AgentUserId = 42;

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<AgentPerformance, int>> _repoMock;
    private readonly Mock<ILogger<AgentPerformanceUpdateService>> _loggerMock;
    private readonly IAgentPerformanceUpdateService _service;

    public AgentPerformanceUpdateServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _repoMock = new Mock<IGenericRepository<AgentPerformance, int>>();
        _loggerMock = new Mock<ILogger<AgentPerformanceUpdateService>>();
        _service = new AgentPerformanceUpdateService(
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    private void SetupRepository(
        Func<ISpecification<AgentPerformance, int>, AgentPerformance?> lookup,
        int saveCalls = 1)
    {
        // GetWithSpecAsync returns the provided lookup result
        _repoMock
            .Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<AgentPerformance, int>>()))
            .ReturnsAsync(lookup);

        _unitOfWorkMock
            .Setup(u => u.GetRepository<AgentPerformance, int>())
            .Returns(_repoMock.Object);

        _unitOfWorkMock
            .Setup(u => u.CompleteAsync())
            .ReturnsAsync(1);
    }

    private void SetupNewAgent()
    {
        // Simulate no existing row: lookup returns null
        SetupRepository(_ => null);

        // Capture the added entity so tests can read its state
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<AgentPerformance>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private AgentPerformance SetupExistingAgent()
    {
        var perf = new AgentPerformance
        {
            Id = 1,
            UserId = AgentUserId,
            ChatsHandled = 10,
            AvgResponseTime = 30m,
            ResolutionRate = 0.8m,
            ResolvedChats = 8,
            ActiveChats = 3,
            LastUpdated = DateTime.UtcNow.AddHours(-1)
        };

        SetupRepository(_ => perf);
        return perf;
    }

    // ───────────────────────────────
    // AvgResponseTime — running average
    // ───────────────────────────────

    [Fact]
    public async Task RecordAgentReplyAsync_UpdatesRunningAverage()
    {
        var perf = SetupExistingAgent();
        // Before: ChatsHandled=10, AvgResponseTime=30 => total = 300
        // New reply: 45 seconds
        await _service.RecordAgentReplyAsync(AgentUserId, 100, 45.0);

        // Expected: total = 300 + 45 = 345, ChatsHandled = 11, avg = 345/11 = 31.36
        Assert.Equal(11, perf.ChatsHandled);
        Assert.Equal(31.36m, perf.AvgResponseTime);
    }

    [Fact]
    public async Task RecordAgentReplyAsync_FirstReply_SetsAverage()
    {
        SetupNewAgent();

        await _service.RecordAgentReplyAsync(AgentUserId, 100, 60.0);

        // Capture the entity that was added
        var added = _repoMock.Invocations
            .FirstOrDefault(i => i.Method.Name == nameof(IGenericRepository<AgentPerformance, int>.AddAsync))
            ?.Arguments[0] as AgentPerformance;

        Assert.NotNull(added);
        Assert.Equal(1, added.ChatsHandled);
        Assert.Equal(60.00m, added.AvgResponseTime);
    }

    [Fact]
    public async Task RecordAgentReplyAsync_SecondReply_RecalculatesCorrectly()
    {
        var perf = SetupExistingAgent();
        perf.ChatsHandled = 1;
        perf.AvgResponseTime = 60m; // first reply took 60s

        // Second reply: 30s → expected avg = (60 + 30) / 2 = 45
        await _service.RecordAgentReplyAsync(AgentUserId, 101, 30.0);

        Assert.Equal(2, perf.ChatsHandled);
        Assert.Equal(45.00m, perf.AvgResponseTime);
    }

    [Fact]
    public async Task RecordAgentReplyAsync_ZeroResponseTime_StillUpdates()
    {
        SetupNewAgent();

        await _service.RecordAgentReplyAsync(AgentUserId, 100, 0.0);

        var added = _repoMock.Invocations
            .FirstOrDefault(i => i.Method.Name == nameof(IGenericRepository<AgentPerformance, int>.AddAsync))
            ?.Arguments[0] as AgentPerformance;

        Assert.NotNull(added);
        Assert.Equal(1, added.ChatsHandled);
        Assert.Equal(0m, added.AvgResponseTime);
    }

    // ───────────────────────────────
    // ResolutionRate — direct from ResolvedChats
    // ───────────────────────────────

    [Fact]
    public async Task RecordConversationClosed_Resolved_IncrementsResolvedChats()
    {
        var perf = SetupExistingAgent();
        // Before: ChatsHandled=10, ResolvedChats=8, ResolutionRate=0.8

        await _service.RecordConversationClosedAsync(AgentUserId, resolved: true);

        Assert.Equal(9, perf.ResolvedChats);
        Assert.Equal(0.9m, perf.ResolutionRate); // 9/10
        Assert.Equal(2, perf.ActiveChats);        // decremented from 3
    }

    [Fact]
    public async Task RecordConversationClosed_NotResolved_DoesNotChangeResolutionRate()
    {
        var perf = SetupExistingAgent();
        var originalRate = perf.ResolutionRate;
        var originalResolved = perf.ResolvedChats;

        await _service.RecordConversationClosedAsync(AgentUserId, resolved: false);

        Assert.Equal(originalResolved, perf.ResolvedChats);
        Assert.Equal(originalRate, perf.ResolutionRate);
        Assert.Equal(2, perf.ActiveChats); // still decremented
    }

    [Fact]
    public async Task RecordConversationClosed_ActiveChatsNeverNegative()
    {
        var perf = SetupExistingAgent();
        perf.ActiveChats = 0;

        await _service.RecordConversationClosedAsync(AgentUserId, resolved: false);

        Assert.Equal(0, perf.ActiveChats); // stays at floor
    }

    [Fact]
    public async Task RecordConversationClosed_Resolved_WithZeroChatsHandled()
    {
        SetupNewAgent(); // ChatsHandled=0, ResolvedChats=0

        await _service.RecordConversationClosedAsync(AgentUserId, resolved: true);

        var added = _repoMock.Invocations
            .FirstOrDefault(i => i.Method.Name == nameof(IGenericRepository<AgentPerformance, int>.AddAsync))
            ?.Arguments[0] as AgentPerformance;

        Assert.NotNull(added);
        Assert.Equal(1, added.ResolvedChats);
        Assert.Equal(1m, added.ResolutionRate); // safety fallback when ChatsHandled=0
    }

    // ───────────────────────────────
    // ActiveChats — invariant tracking
    // ───────────────────────────────

    [Fact]
    public async Task RecordConversationAssignedAsync_IncrementsActiveChats()
    {
        var perf = SetupExistingAgent();
        Assert.Equal(3, perf.ActiveChats);

        await _service.RecordConversationAssignedAsync(AgentUserId);

        Assert.Equal(4, perf.ActiveChats);
    }

    [Fact]
    public async Task RecordConversationReopenedAsync_IncrementsActiveChatsAndDecrementsResolved()
    {
        var perf = SetupExistingAgent();
        // Before: ResolvedChats=8, ActiveChats=3

        await _service.RecordConversationReopenedAsync(AgentUserId);

        Assert.Equal(4, perf.ActiveChats);
        Assert.Equal(7, perf.ResolvedChats);
        Assert.Equal(0.7m, perf.ResolutionRate);
    }

    [Fact]
    public async Task RecordConversationReopenedAsync_WithZeroResolved_StaysAtZero()
    {
        var perf = SetupExistingAgent();
        perf.ResolvedChats = 0;
        perf.ResolutionRate = 0m;

        await _service.RecordConversationReopenedAsync(AgentUserId);

        Assert.Equal(0, perf.ResolvedChats);
        Assert.Equal(0m, perf.ResolutionRate);
    }

    // ───────────────────────────────
    // GetOrCreateAsync — new agent creation
    // ───────────────────────────────

    [Fact]
    public async Task NewAgent_CreatesRowWithDefaults()
    {
        SetupNewAgent();

        await _service.RecordConversationAssignedAsync(AgentUserId);

        var added = _repoMock.Invocations
            .FirstOrDefault(i => i.Method.Name == nameof(IGenericRepository<AgentPerformance, int>.AddAsync))
            ?.Arguments[0] as AgentPerformance;

        Assert.NotNull(added);
        Assert.Equal(AgentUserId, added.UserId);
        Assert.Equal(0, added.ChatsHandled);
        Assert.Equal(0, added.AvgResponseTime);
        Assert.Equal(1m, added.ResolutionRate);
        Assert.Equal(0, added.ResolvedChats);
        Assert.Equal(1, added.ActiveChats);
        Assert.NotEqual(default, added.LastUpdated);
    }

    // ───────────────────────────────
    // PerformanceScore — calculated value (consumer)
    // ───────────────────────────────

    [Fact]
    public async Task FullLifecycle_AllMetricsCorrect()
    {
        var perf = SetupExistingAgent();
        // Start: ChatsHandled=10, AvgResponseTime=30, ResolvedChats=8, ActiveChats=3

        // Agent replies to conversation 100 (45s)
        await _service.RecordAgentReplyAsync(AgentUserId, 100, 45.0);
        // ChatsHandled=11, AvgResponseTime=31.36

        // Agent replies to conversation 101 (120s)
        await _service.RecordAgentReplyAsync(AgentUserId, 101, 120.0);
        // total = 345+120 = 465, ChatsHandled=12, AvgResponseTime=38.75

        // Conversation closed as resolved
        await _service.RecordConversationClosedAsync(AgentUserId, resolved: true);
        // ResolvedChats=9, ResolutionRate=9/12=0.75, ActiveChats=2

        // New conversation assigned
        await _service.RecordConversationAssignedAsync(AgentUserId);
        // ActiveChats=3

        // A resolved conversation reopened
        await _service.RecordConversationReopenedAsync(AgentUserId);
        // ResolvedChats=8, ResolutionRate=8/12≈0.6667, ActiveChats=4

        Assert.Equal(12, perf.ChatsHandled);
        Assert.Equal(38.75m, perf.AvgResponseTime);
        Assert.Equal(8, perf.ResolvedChats);
        Assert.Equal(0.6667m, perf.ResolutionRate);
        Assert.Equal(4, perf.ActiveChats);
    }

    // ───────────────────────────────
    // Concurrency — RowVersion prevents lost updates
    // ───────────────────────────────

    [Fact]
    public async Task Concurrency_CallerIsolation_Preserved()
    {
        // Tests that the service does not swallow exceptions — if a concurrency
        // conflict occurs, it propagates. The RowVersion column on AgentPerformance
        // (configured via IsRowVersion()) causes EF to check the original version
        // on every UPDATE and throw DbUpdateConcurrencyException on mismatch.
        var perf = SetupExistingAgent();
        var originalRowVersion = perf.RowVersion;

        _unitOfWorkMock
            .Setup(u => u.CompleteAsync())
            .ThrowsAsync(new InvalidOperationException("Simulated DB failure"));

        // The exception propagates; the caller handles retry.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RecordConversationAssignedAsync(AgentUserId));
    }
}
