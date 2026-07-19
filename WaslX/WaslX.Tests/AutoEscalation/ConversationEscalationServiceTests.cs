using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.AutoEscalation;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Features.Escalation.Models;
using WaslX.Application.Features.Escalation.Services;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;

namespace WaslX.Tests.AutoEscalation;

public class ConversationEscalationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IInboxRealtimeNotifier> _notifierMock;
    private readonly Mock<ILogger<ConversationEscalationService>> _loggerMock;
    private readonly Mock<IGenericRepository<Domain.Entities.Escalation, int>> _escalationRepoMock;
    private readonly Mock<IGenericRepository<Conversation, int>> _conversationRepoMock;
    private readonly Mock<IGenericRepository<AuditLog, int>> _auditRepoMock;
    private readonly ConversationEscalationService _service;

    private const int TenantId = 1;
    private const int ConversationId = 100;
    private const int MessageId = 500;
    private const int ClassificationId = 600;
    private const int EscalationId = 700;

    public ConversationEscalationServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _notifierMock = new Mock<IInboxRealtimeNotifier>();
        _loggerMock = new Mock<ILogger<ConversationEscalationService>>();
        _escalationRepoMock = new Mock<IGenericRepository<Domain.Entities.Escalation, int>>();
        _conversationRepoMock = new Mock<IGenericRepository<Conversation, int>>();
        _auditRepoMock = new Mock<IGenericRepository<AuditLog, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Domain.Entities.Escalation, int>())
            .Returns(_escalationRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Conversation, int>())
            .Returns(_conversationRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<AuditLog, int>())
            .Returns(_auditRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.CompleteAsync())
            .ReturnsAsync(1);

        _escalationRepoMock.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Escalation>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Escalation, CancellationToken>((e, _) => e.Id = EscalationId)
            .Returns(Task.CompletedTask);

        _service = new ConversationEscalationService(
            _unitOfWorkMock.Object,
            _notifierMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// PH-1.2: When escalate=true, a new escalation record is created with correct fields,
    /// conversation.IsEscalated is set, SignalR event is emitted, and audit is written.
    /// </summary>
    [Fact]
    public async Task EscalateTrue_CreatesEscalation()
    {
        var conversation = new Conversation { Id = ConversationId, TenantId = TenantId, IsEscalated = false };
        _conversationRepoMock.Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var input = BuildInput();
        var result = await _service.EscalateAsync(input);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Created);
        Assert.False(result.Value.AlreadyEscalated);
        Assert.Equal("open", result.Value.Status);
        Assert.Equal(EscalationId, result.Value.EscalationId);
        Assert.Equal(ConversationId, result.Value.ConversationId);

        _escalationRepoMock.Verify(r => r.AddAsync(
            It.Is<Domain.Entities.Escalation>(e =>
                e.TenantId == TenantId &&
                e.ConversationId == ConversationId &&
                e.MessageId == MessageId &&
                e.MessageClassificationId == ClassificationId &&
                e.Status == EscalationStatus.Open &&
                e.Priority == "high" &&
                e.Sentiment == "angry" &&
                e.CreatedBySystem == true),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.True(conversation.IsEscalated);
        Assert.NotNull(conversation.EscalatedAtUtc);
        Assert.Equal("urgent angry complaint", conversation.EscalationReason);

        _notifierMock.Verify(n => n.ConversationEscalatedAsync(
            TenantId,
            It.IsAny<ConversationEscalatedPayload>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _auditRepoMock.Verify(r => r.AddAsync(
            It.Is<AuditLog>(a =>
                a.TenantId == TenantId &&
                a.Action == AuditAction.EscalationTriggered &&
                a.EntityType == "Escalation" &&
                a.ActorUserId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// PH-1.3: If conversation.IsEscalated is already true, return AlreadyEscalated=true
    /// and no duplicate escalation or notification is created.
    /// </summary>
    [Fact]
    public async Task AlreadyEscalated_ReturnsAlreadyEscalated()
    {
        var conversation = new Conversation { Id = ConversationId, TenantId = TenantId, IsEscalated = true };
        _conversationRepoMock.Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var input = BuildInput();
        var result = await _service.EscalateAsync(input);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Created);
        Assert.True(result.Value.AlreadyEscalated);
        Assert.Equal("open", result.Value.Status);

        _escalationRepoMock.Verify(r => r.AddAsync(
            It.IsAny<Domain.Entities.Escalation>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _notifierMock.Verify(n => n.ConversationEscalatedAsync(
            It.IsAny<int>(),
            It.IsAny<ConversationEscalatedPayload>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// PH-1.4: Even if no Manager/Admin is online in the tenant (SignalR group is empty),
    /// the escalation is still created and the service succeeds.
    /// </summary>
    [Fact]
    public async Task NoManagersAdmins_EscalationStillCreated()
    {
        var conversation = new Conversation { Id = ConversationId, TenantId = TenantId, IsEscalated = false };
        _conversationRepoMock.Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _notifierMock.Setup(n => n.ConversationEscalatedAsync(
                It.IsAny<int>(),
                It.IsAny<ConversationEscalatedPayload>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var input = BuildInput();
        var result = await _service.EscalateAsync(input);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Created);

        _escalationRepoMock.Verify(r => r.AddAsync(
            It.IsAny<Domain.Entities.Escalation>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _notifierMock.Verify(n => n.ConversationEscalatedAsync(
            TenantId,
            It.IsAny<ConversationEscalatedPayload>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// PH-1.5: If the notifier throws, the escalation is still persisted.
    /// The notifier error is caught internally and logged as a warning.
    /// </summary>
    [Fact]
    public async Task NotifierThrows_EscalationPersisted()
    {
        var conversation = new Conversation { Id = ConversationId, TenantId = TenantId, IsEscalated = false };
        _conversationRepoMock.Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _notifierMock.Setup(n => n.ConversationEscalatedAsync(
                It.IsAny<int>(),
                It.IsAny<ConversationEscalatedPayload>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SignalR unavailable"));

        var input = BuildInput();
        var result = await _service.EscalateAsync(input);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Created);

        _escalationRepoMock.Verify(r => r.AddAsync(
            It.IsAny<Domain.Entities.Escalation>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _notifierMock.Verify(n => n.ConversationEscalatedAsync(
            TenantId,
            It.IsAny<ConversationEscalatedPayload>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// PH-1.6: The conversation is flagged with IsEscalated=true and the reason is persisted.
    /// </summary>
    [Fact]
    public async Task AiAgentHandoffFlagSet_ConversationFlagged()
    {
        var conversation = new Conversation { Id = ConversationId, TenantId = TenantId, IsEscalated = false };
        _conversationRepoMock.Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var input = BuildInput(reason: "AI agent handoff required");
        var result = await _service.EscalateAsync(input);

        Assert.True(result.IsSuccess);
        Assert.True(conversation.IsEscalated);
        Assert.NotNull(conversation.EscalatedAtUtc);
        Assert.Equal("AI agent handoff required", conversation.EscalationReason);
    }

    /// <summary>
    /// PH-1.7: Tenant isolation - conversation from a different tenant returns not found.
    /// </summary>
    [Fact]
    public async Task TenantIsolation_WrongTenant_ReturnsNotFound()
    {
        var conversation = new Conversation { Id = ConversationId, TenantId = 999, IsEscalated = false };
        _conversationRepoMock.Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var input = BuildInput();
        var result = await _service.EscalateAsync(input);

        Assert.True(result.IsFailure);
        Assert.Equal("Escalation.ConversationNotFound", result.Error.Code);

        _escalationRepoMock.Verify(r => r.AddAsync(
            It.IsAny<Domain.Entities.Escalation>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// PH-1.7: Tenant isolation - null conversation also returns not found.
    /// </summary>
    [Fact]
    public async Task TenantIsolation_ConversationNotFound_ReturnsNotFound()
    {
        _conversationRepoMock.Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var input = BuildInput();
        var result = await _service.EscalateAsync(input);

        Assert.True(result.IsFailure);
        Assert.Equal("Escalation.ConversationNotFound", result.Error.Code);

        _escalationRepoMock.Verify(r => r.AddAsync(
            It.IsAny<Domain.Entities.Escalation>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// PH-1.8: An audit entry is written with the correct action and JSON details.
    /// </summary>
    [Fact]
    public async Task AuditEntryWritten_CorrectActionAndDetails()
    {
        var conversation = new Conversation { Id = ConversationId, TenantId = TenantId, IsEscalated = false };
        _conversationRepoMock.Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var input = BuildInput();
        var result = await _service.EscalateAsync(input);

        Assert.True(result.IsSuccess);

        _auditRepoMock.Verify(r => r.AddAsync(
            It.Is<AuditLog>(a =>
                a.TenantId == TenantId &&
                a.Action == AuditAction.EscalationTriggered &&
                a.EntityType == "Escalation" &&
                a.EntityId == EscalationId &&
                a.ActorUserId == null &&
                a.Details.Contains("EscalationId") &&
                a.Details.Contains("ClassificationId") &&
                a.Details.Contains("Reason") &&
                a.Details.Contains("Priority") &&
                a.Details.Contains("Sentiment")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static EscalationInput BuildInput(
        string reason = "urgent angry complaint",
        string priority = "high",
        string sentiment = "angry")
    {
        return new EscalationInput
        {
            TenantId = TenantId,
            ConversationId = ConversationId,
            MessageId = MessageId,
            ClassificationId = ClassificationId,
            Topic = "complaint",
            Sentiment = sentiment,
            Priority = priority,
            Reason = reason
        };
    }
}
