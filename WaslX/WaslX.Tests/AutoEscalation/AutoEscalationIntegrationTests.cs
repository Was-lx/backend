using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Features.Escalation.Services;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;

namespace WaslX.Tests.AutoEscalation;

public class AutoEscalationIntegrationTests
{
    private const int TenantId = 1;
    private const int OtherTenantId = 2;
    private const int ConversationId = 100;
    private const int MessageId = 500;
    private const int ClassificationId = 600;
    private const int EscalationId = 700;

    [Fact]
    public async Task UrgentAngry_TriggersEscalation_NotifiesManagersAdmins()
    {
        var mocks = BuildServices();
        var conversation = new Conversation { Id = ConversationId, TenantId = TenantId, IsEscalated = false };
        mocks.ConversationRepo.Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var input = new Application.Features.Escalation.Models.EscalationInput
        {
            TenantId = TenantId,
            ConversationId = ConversationId,
            MessageId = MessageId,
            ClassificationId = ClassificationId,
            Topic = "complaint",
            Sentiment = "angry",
            Priority = "high",
            Reason = "urgent angry complaint"
        };

        var result = await mocks.Service.EscalateAsync(input);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Created);
        Assert.False(result.Value.AlreadyEscalated);
        Assert.Equal("open", result.Value.Status);
        Assert.Equal(EscalationId, result.Value.EscalationId);

        Assert.True(conversation.IsEscalated);
        Assert.NotNull(conversation.EscalatedAtUtc);
        Assert.Equal("urgent angry complaint", conversation.EscalationReason);

        mocks.EscalationRepo.Verify(r => r.AddAsync(
            It.Is<Domain.Entities.Escalation>(e =>
                e.TenantId == TenantId &&
                e.ConversationId == ConversationId &&
                e.Status == EscalationStatus.Open &&
                e.Priority == "high" &&
                e.Sentiment == "angry" &&
                e.CreatedBySystem == true),
            It.IsAny<CancellationToken>()), Times.Once);

        mocks.Notifier.Verify(n => n.ConversationEscalatedAsync(
            TenantId,
            It.IsAny<ConversationEscalatedPayload>(),
            It.IsAny<CancellationToken>()), Times.Once);

        mocks.AuditRepo.Verify(r => r.AddAsync(
            It.Is<AuditLog>(a =>
                a.TenantId == TenantId &&
                a.Action == AuditAction.EscalationTriggered &&
                a.EntityType == "Escalation"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NormalMessage_DoesNotEscalate()
    {
        var mocks = BuildServices();
        var conversation = new Conversation { Id = ConversationId, TenantId = TenantId, IsEscalated = false };
        mocks.ConversationRepo.Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var input = new Application.Features.Escalation.Models.EscalationInput
        {
            TenantId = TenantId,
            ConversationId = ConversationId,
            MessageId = MessageId,
            ClassificationId = ClassificationId,
            Topic = "general",
            Sentiment = "neutral",
            Priority = "normal",
            Reason = "normal message"
        };

        var result = await mocks.Service.EscalateAsync(input);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Created);

        mocks.EscalationRepo.Verify(r => r.AddAsync(
            It.IsAny<Domain.Entities.Escalation>(),
            It.IsAny<CancellationToken>()), Times.Once);

        mocks.Notifier.Verify(n => n.ConversationEscalatedAsync(
            It.IsAny<int>(),
            It.IsAny<ConversationEscalatedPayload>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TenantIsolation_EscalationScopedToTenant()
    {
        var mocks = BuildServices();
        var conversation = new Conversation { Id = ConversationId, TenantId = OtherTenantId, IsEscalated = false };
        mocks.ConversationRepo.Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var input = new Application.Features.Escalation.Models.EscalationInput
        {
            TenantId = TenantId,
            ConversationId = ConversationId,
            MessageId = MessageId,
            ClassificationId = ClassificationId,
            Topic = "complaint",
            Sentiment = "angry",
            Priority = "high",
            Reason = "urgent angry complaint"
        };

        var result = await mocks.Service.EscalateAsync(input);

        Assert.True(result.IsFailure);
        Assert.Equal("Escalation.ConversationNotFound", result.Error.Code);

        mocks.EscalationRepo.Verify(r => r.AddAsync(
            It.IsAny<Domain.Entities.Escalation>(),
            It.IsAny<CancellationToken>()), Times.Never);

        mocks.Notifier.Verify(n => n.ConversationEscalatedAsync(
            It.IsAny<int>(),
            It.IsAny<ConversationEscalatedPayload>(),
            It.IsAny<CancellationToken>()), Times.Never);

        mocks.AuditRepo.Verify(r => r.AddAsync(
            It.IsAny<AuditLog>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DuplicateProcessing_Safe()
    {
        var mocks = BuildServices();
        var conversation = new Conversation { Id = ConversationId, TenantId = TenantId, IsEscalated = true };
        mocks.ConversationRepo.Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var input = new Application.Features.Escalation.Models.EscalationInput
        {
            TenantId = TenantId,
            ConversationId = ConversationId,
            MessageId = MessageId,
            ClassificationId = ClassificationId,
            Topic = "complaint",
            Sentiment = "angry",
            Priority = "high",
            Reason = "urgent angry complaint"
        };

        var result1 = await mocks.Service.EscalateAsync(input);
        var result2 = await mocks.Service.EscalateAsync(input);

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.True(result1.Value.AlreadyEscalated);
        Assert.True(result2.Value.AlreadyEscalated);

        mocks.EscalationRepo.Verify(r => r.AddAsync(
            It.IsAny<Domain.Entities.Escalation>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NormalAssignmentUnchanged_OwnershipPreserved()
    {
        var mocks = BuildServices();
        var assignedUserId = 42;
        var conversation = new Conversation
        {
            Id = ConversationId,
            TenantId = TenantId,
            AssignedUserId = assignedUserId,
            IsEscalated = false
        };
        mocks.ConversationRepo.Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var input = new Application.Features.Escalation.Models.EscalationInput
        {
            TenantId = TenantId,
            ConversationId = ConversationId,
            MessageId = MessageId,
            ClassificationId = ClassificationId,
            Topic = "complaint",
            Sentiment = "angry",
            Priority = "high",
            Reason = "urgent angry complaint"
        };

        var result = await mocks.Service.EscalateAsync(input);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Created);
        Assert.Equal(assignedUserId, conversation.AssignedUserId);
    }

    [Fact]
    public async Task EndToEndAuditTrail_AuditEntryExists()
    {
        var mocks = BuildServices();
        var conversation = new Conversation { Id = ConversationId, TenantId = TenantId, IsEscalated = false };
        mocks.ConversationRepo.Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var input = new Application.Features.Escalation.Models.EscalationInput
        {
            TenantId = TenantId,
            ConversationId = ConversationId,
            MessageId = MessageId,
            ClassificationId = ClassificationId,
            Topic = "complaint",
            Sentiment = "angry",
            Priority = "high",
            Reason = "urgent angry complaint"
        };

        var result = await mocks.Service.EscalateAsync(input);

        Assert.True(result.IsSuccess);

        mocks.AuditRepo.Verify(r => r.AddAsync(
            It.Is<AuditLog>(a =>
                a.TenantId == TenantId &&
                a.ActorUserId == null &&
                a.Action == AuditAction.EscalationTriggered &&
                a.EntityType == "Escalation" &&
                a.EntityId == EscalationId &&
                a.Details.Contains("ConversationId") &&
                a.Details.Contains("EscalationId") &&
                a.Details.Contains("ClassificationId") &&
                a.Details.Contains("urgent angry complaint") &&
                a.Details.Contains("high") &&
                a.Details.Contains("angry")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ServiceMocks BuildServices()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var notifierMock = new Mock<IInboxRealtimeNotifier>();
        var loggerMock = new Mock<ILogger<ConversationEscalationService>>();
        var escalationRepoMock = new Mock<IGenericRepository<Domain.Entities.Escalation, int>>();
        var conversationRepoMock = new Mock<IGenericRepository<Conversation, int>>();
        var auditRepoMock = new Mock<IGenericRepository<AuditLog, int>>();

        unitOfWorkMock.Setup(u => u.GetRepository<Domain.Entities.Escalation, int>())
            .Returns(escalationRepoMock.Object);
        unitOfWorkMock.Setup(u => u.GetRepository<Conversation, int>())
            .Returns(conversationRepoMock.Object);
        unitOfWorkMock.Setup(u => u.GetRepository<AuditLog, int>())
            .Returns(auditRepoMock.Object);
        unitOfWorkMock.Setup(u => u.CompleteAsync())
            .ReturnsAsync(1);

        escalationRepoMock.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Escalation>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Escalation, CancellationToken>((e, _) => e.Id = EscalationId)
            .Returns(Task.CompletedTask);

        var service = new ConversationEscalationService(
            unitOfWorkMock.Object,
            notifierMock.Object,
            loggerMock.Object);

        return new ServiceMocks
        {
            Service = service,
            Notifier = notifierMock,
            EscalationRepo = escalationRepoMock,
            ConversationRepo = conversationRepoMock,
            AuditRepo = auditRepoMock,
            UnitOfWork = unitOfWorkMock
        };
    }

    private sealed class ServiceMocks
    {
        public ConversationEscalationService Service { get; init; } = null!;
        public Mock<IInboxRealtimeNotifier> Notifier { get; init; } = null!;
        public Mock<IGenericRepository<Domain.Entities.Escalation, int>> EscalationRepo { get; init; } = null!;
        public Mock<IGenericRepository<Conversation, int>> ConversationRepo { get; init; } = null!;
        public Mock<IGenericRepository<AuditLog, int>> AuditRepo { get; init; } = null!;
        public Mock<IUnitOfWork> UnitOfWork { get; init; } = null!;
    }
}
