using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Features.Classification.Models;

namespace WaslX.Infrastructure.AI.Classification;

public class RuleBasedMessageClassifier(ILogger<RuleBasedMessageClassifier> logger) : IMessageClassifier
{
    private const string Version = "rule-based:1";

    private static readonly string[] ArabicAnger =
    [
        "\u0632\u0639\u0644\u0627\u0646", "\u063a\u0627\u0636\u0628", "\u0648\u062d\u0634", "\u0633\u064a\u0621",
        "\u0633\u064a\u0626\u0629", "\u0645\u0634 \u0631\u0627\u0636\u064a", "\u0623\u0633\u0648\u0623", "\u0645\u0634 \u0647\u0643\u0645\u0644"
    ];

    private static readonly string[] ArabicNegative =
    [
        "\u0645\u0634\u0643\u0644\u0629", "\u0645\u0634 \u0634\u063a\u0627\u0644", "\u0639\u0637\u0644", "\u0628\u064a\u0647\u0646\u062c"
    ];

    private static readonly string[] ArabicPositive =
    [
        "\u0634\u0643\u0631\u0627", "\u0645\u0645\u062a\u0627\u0632", "\u062c\u064a\u062f", "\u064a\u0639\u0637\u064a\u0643 \u0627\u0644\u0639\u0627\u0641\u064a\u0629",
        "\u0645\u0645\u062a\u0627\u0632\u0629"
    ];

    private static readonly string[] ArabicUrgency =
    [
        "\u0636\u0631\u0648\u0631\u064a", "\u062d\u0627\u0644\u0627\u064b", "\u062d\u0627\u0644\u0627", "\u0645\u0633\u062a\u0639\u062c\u0644", "\u0641\u0648\u0631\u0627"
    ];

    private static readonly string[] EnglishAnger = [ "angry", "bad service", "terrible", "not happy", "complaint", "worst" ];
    private static readonly string[] EnglishNegative = [ "not working", "broken", "issue", "error", "bug", "cannot" ];
    private static readonly string[] EnglishPositive = [ "thanks", "thank you", "great", "good", "excellent", "awesome" ];
    private static readonly string[] EnglishUrgency = [ "urgent", "asap", "immediately", "now" ];

    private static readonly string[] TechKeywords =
    [
        "error", "bug", "login", "crash", "access", "account", "system",
        "\u0645\u0634 \u0634\u063a\u0627\u0644", "\u0628\u064a\u0647\u0646\u062c", "\u0633\u064a\u0633\u062a\u0645"
    ];

    private static readonly string[] PricingKeywords =
    [
        "price", "cost", "subscription", "\u0633\u0639\u0631", "\u062a\u0643\u0644\u0641\u0629", "\u0627\u0634\u062a\u0631\u0627\u0643"
    ];

    private static readonly string[] DeliveryKeywords =
    [
        "delivery", "shipping", "\u0648\u0635\u0644", "\u062a\u0648\u0635\u064a\u0644", "\u0634\u062d\u0646"
    ];

    private static readonly string[] SalesKeywords = [ "buy", "order", "purchase", "\u0627\u0634\u062a\u0631\u064a", "\u0637\u0644\u0628" ];

    public Task<MessageClassificationResult> ClassifyAsync(
        MessageClassificationInput input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var text = input.MessageText.ToLowerInvariant();

            var hasArabic = text.Any(c => c >= 0x0600 && c <= 0x06FF);
            var hasEnglish = text.Any(c => c >= 'a' && c <= 'z');
            var language = (hasArabic, hasEnglish) switch
            {
                (true, true) => "mixed",
                (true, false) => "arabic",
                (false, true) => "english",
                _ => "unknown"
            };

            var topic = "general";
            if (TechKeywords.Any(k => text.Contains(k))) topic = "technical";
            else if (PricingKeywords.Any(k => text.Contains(k))) topic = "pricing";
            else if (DeliveryKeywords.Any(k => text.Contains(k))) topic = "delivery";
            else if (SalesKeywords.Any(k => text.Contains(k))) topic = "sales";
            else if (text.Contains("\u0645\u0634\u0643\u0644\u0629") || text.Contains("complaint")) topic = "complaint";

            var sentiment = "neutral";
            var priority = "normal";

            if (ArabicAnger.Any(k => text.Contains(k)) || EnglishAnger.Any(k => text.Contains(k)))
            {
                sentiment = "angry";
                if (topic == "general") topic = "complaint";
            }
            else if (ArabicNegative.Any(k => text.Contains(k)) || EnglishNegative.Any(k => text.Contains(k)))
            {
                sentiment = "negative";
            }
            else if (ArabicPositive.Any(k => text.Contains(k)) || EnglishPositive.Any(k => text.Contains(k)))
            {
                sentiment = "positive";
                priority = "low";
            }

            if (ArabicUrgency.Any(k => text.Contains(k)) || EnglishUrgency.Any(k => text.Contains(k)))
            {
                priority = "urgent";
            }
            else if (topic == "technical" && sentiment == "negative")
            {
                priority = "high";
            }

            var escalate = sentiment == "angry" || priority == "urgent" || (topic == "complaint" && sentiment == "angry");

            return Task.FromResult(new MessageClassificationResult
            {
                Topic = topic,
                Language = language,
                Sentiment = sentiment,
                Priority = priority,
                Escalate = escalate,
                Reason = escalate ? "Rule-based escalation criteria met" : string.Empty,
                ClassifierVersion = Version
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rule-based classification failed for message {MessageId}", input.MessageId);
            return Task.FromResult(new MessageClassificationResult
            {
                Reason = "Classification failed; fallback result used",
                ClassifierVersion = Version
            });
        }
    }
}