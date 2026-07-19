using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using WaslX.Application.Features.Classification;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Abstractions.AutoEscalation;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Domain.Entities;
using WaslX.Application.Features.Classification.Models;
using System.Threading;
using System.Threading.Tasks;
using WaslX.Domain.Contracts.Specification;
using System.Collections.Generic;

namespace WaslX.Tests.Classification;

public class ClassificationOrchestratorTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMessageClassifier> _classifierMock;
    private readonly Mock<IInboxRealtimeNotifier> _notifierMock;
    private readonly Mock<IConversationEscalationService> _escalationServiceMock;
    private readonly ClassificationOrchestrator _orchestrator;
    private readonly Mock<IGenericRepository<Message, int>> _msgRepoMock;
    private readonly Mock<IGenericRepository<MessageClassification, int>> _classRepoMock;

    public ClassificationOrchestratorTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _classifierMock = new Mock<IMessageClassifier>();
        _notifierMock = new Mock<IInboxRealtimeNotifier>();
        _escalationServiceMock = new Mock<IConversationEscalationService>();
        
        _msgRepoMock = new Mock<IGenericRepository<Message, int>>();
        _classRepoMock = new Mock<IGenericRepository<MessageClassification, int>>();
        
        _unitOfWorkMock.Setup(u => u.GetRepository<Message, int>()).Returns(_msgRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<MessageClassification, int>()).Returns(_classRepoMock.Object);

        _orchestrator = new ClassificationOrchestrator(
            _unitOfWorkMock.Object,
            _classifierMock.Object,
            _notifierMock.Object,
            _escalationServiceMock.Object,
            Mock.Of<ILogger<ClassificationOrchestrator>>()
        );
    }

    [Fact]
    public async Task ProcessClassificationAsync_ValidMessage_SavesClassificationAndEmitsSignalR()
    {
        int tenantId = 1;
        int conversationId = 100;
        int messageId = 500;

        _msgRepoMock.Setup(r => r.GetByIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = messageId, ConversationId = conversationId, Content = "Test content" });

        _msgRepoMock.Setup(r => r.GetAllWithSpecAsync(It.IsAny<ISpecification<Message, int>>(), false))
            .ReturnsAsync(new List<Message>());

        _classifierMock.Setup(c => c.ClassifyAsync(It.IsAny<MessageClassificationInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MessageClassificationResult { Topic = "technical", Language = "english" });

        await _orchestrator.ProcessClassificationAsync(tenantId, conversationId, messageId, CancellationToken.None);

        _classRepoMock.Verify(r => r.AddAsync(It.Is<MessageClassification>(c => 
            c.TenantId == tenantId && 
            c.ConversationId == conversationId && 
            c.MessageId == messageId && 
            c.Topic == "technical"), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);

        _notifierMock.Verify(n => n.MessageClassificationUpdatedAsync(tenantId, It.Is<MessageClassificationPayload>(p => 
            p.ConversationId == conversationId && 
            p.MessageId == messageId && 
            p.Classification.Topic == "technical"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
