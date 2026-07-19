using Xunit;
using WaslX.Infrastructure.AI.Classification;
using WaslX.Application.Features.Classification.Models;

namespace WaslX.Tests.Classification;

public class RuleBasedMessageClassifierTests
{
    private readonly RuleBasedMessageClassifier _classifier;

    public RuleBasedMessageClassifierTests()
    {
        _classifier = new RuleBasedMessageClassifier(new Moq.Mock<Microsoft.Extensions.Logging.ILogger<RuleBasedMessageClassifier>>().Object);
    }

    [Fact]
    public async Task ClassifyAsync_ArabicAngryUrgentComplaint_ReturnsExpected()
    {
        var input = new MessageClassificationInput { MessageText = "الخدمة سيئة جدا ومحتاج حد يرد حالا" };
        var result = await _classifier.ClassifyAsync(input, CancellationToken.None);

        Assert.Equal("arabic", result.Language);
        Assert.Equal("angry", result.Sentiment);
        Assert.Equal("urgent", result.Priority);
        Assert.Equal("complaint", result.Topic);
        Assert.True(result.Escalate);
    }

    [Fact]
    public async Task ClassifyAsync_ArabicPricingQuestion_ReturnsExpected()
    {
        var input = new MessageClassificationInput { MessageText = "كم سعر الاشتراك الشهري؟" };
        var result = await _classifier.ClassifyAsync(input, CancellationToken.None);

        Assert.Equal("arabic", result.Language);
        Assert.Equal("pricing", result.Topic);
        Assert.Equal("neutral", result.Sentiment);
        Assert.Equal("normal", result.Priority);
        Assert.False(result.Escalate);
    }

    [Fact]
    public async Task ClassifyAsync_EnglishTechnicalIssue_ReturnsExpected()
    {
        var input = new MessageClassificationInput { MessageText = "login is not working" };
        var result = await _classifier.ClassifyAsync(input, CancellationToken.None);

        Assert.Equal("english", result.Language);
        Assert.Equal("technical", result.Topic);
        Assert.Equal("negative", result.Sentiment);
        Assert.Equal("high", result.Priority);
        Assert.False(result.Escalate);
    }

    [Fact]
    public async Task ClassifyAsync_EnglishUrgentTechnical_ReturnsExpected()
    {
        var input = new MessageClassificationInput { MessageText = "urgent, I cannot access my account" };
        var result = await _classifier.ClassifyAsync(input, CancellationToken.None);

        Assert.Equal("english", result.Language);
        Assert.Equal("technical", result.Topic);
        Assert.Equal("urgent", result.Priority);
        Assert.True(result.Escalate);
    }

    [Fact]
    public async Task ClassifyAsync_MixedArabicEnglish_ReturnsExpected()
    {
        var input = new MessageClassificationInput { MessageText = "محتاج help في login" };
        var result = await _classifier.ClassifyAsync(input, CancellationToken.None);

        Assert.Equal("mixed", result.Language);
        Assert.Equal("technical", result.Topic);
        Assert.Equal("normal", result.Priority);
        Assert.False(result.Escalate);
    }

    [Fact]
    public async Task ClassifyAsync_PositiveMessage_ReturnsExpected()
    {
        var input = new MessageClassificationInput { MessageText = "شكرا، الخدمة ممتازة" };
        var result = await _classifier.ClassifyAsync(input, CancellationToken.None);

        Assert.Equal("arabic", result.Language);
        Assert.Equal("positive", result.Sentiment);
        Assert.Equal("low", result.Priority);
        Assert.False(result.Escalate);
    }

    [Fact]
    public async Task ClassifyAsync_EgyptianDialectNegative_ReturnsExpected()
    {
        var input = new MessageClassificationInput { MessageText = "السيستم مش شغال وبيهنج" };
        var result = await _classifier.ClassifyAsync(input, CancellationToken.None);

        Assert.Equal("arabic", result.Language);
        Assert.Equal("technical", result.Topic);
        Assert.Equal("negative", result.Sentiment);
        Assert.False(result.Escalate);
    }

    [Fact]
    public async Task ClassifyAsync_ComplaintStrongNegative_Escalates()
    {
        var input = new MessageClassificationInput { MessageText = "دي أسوأ خدمة ومش راضي خالص" };
        var result = await _classifier.ClassifyAsync(input, CancellationToken.None);

        Assert.Equal("complaint", result.Topic);
        Assert.Equal("angry", result.Sentiment);
        Assert.True(result.Escalate);
    }

    [Fact]
    public async Task ClassifyAsync_EmptyMessage_ReturnsNeutral()
    {
        var input = new MessageClassificationInput { MessageText = "" };
        var result = await _classifier.ClassifyAsync(input, CancellationToken.None);

        Assert.Equal("unknown", result.Language);
        Assert.Equal("general", result.Topic);
        Assert.Equal("neutral", result.Sentiment);
        Assert.Equal("normal", result.Priority);
        Assert.False(result.Escalate);
    }
}
