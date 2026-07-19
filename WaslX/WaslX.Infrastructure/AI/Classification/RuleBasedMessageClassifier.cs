using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Features.Classification.Models;

namespace WaslX.Infrastructure.AI.Classification;

public class RuleBasedMessageClassifier(ILogger<RuleBasedMessageClassifier> logger) : IMessageClassifier
{
    private static readonly string[] ArabicAnger = [ "زعلان", "غاضب", "وحش", "سيء", "سيئة", "مش راضي", "أسوأ", "مش هكمل" ];
    private static readonly string[] ArabicNegative = [ "مشكلة", "مش شغال", "عطل", "بيهنج" ];
    private static readonly string[] ArabicPositive = [ "شكرا", "ممتاز", "جيد", "يعطيك العافية", "ممتازة" ];
    private static readonly string[] ArabicUrgency = [ "ضروري", "حالاً", "حالا", "مستعجل", "فورا" ];
    
    private static readonly string[] EnglishAnger = [ "angry", "bad service", "terrible", "not happy", "complaint", "worst" ];
    private static readonly string[] EnglishNegative = [ "not working", "broken", "issue", "error", "bug", "cannot" ];
    private static readonly string[] EnglishPositive = [ "thanks", "thank you", "great", "good", "excellent", "awesome" ];
    private static readonly string[] EnglishUrgency = [ "urgent", "asap", "immediately", "now" ];
    
    private static readonly string[] TechKeywords = [ "error", "bug", "login", "crash", "مش شغال", "بيهنج", "سيستم", "access", "account" ];
    private static readonly string[] PricingKeywords = [ "price", "cost", "subscription", "سعر", "تكلفة", "اشتراك" ];
    private static readonly string[] DeliveryKeywords = [ "delivery", "shipping", "وصل", "توصيل", "شحن" ];
    private static readonly string[] SalesKeywords = [ "buy", "order", "purchase", "اشتري", "طلب" ];

    public Task<MessageClassificationResult> ClassifyAsync(
        MessageClassificationInput input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var text = input.MessageText.ToLowerInvariant();
            
            // Language heuristics
            bool hasArabic = text.Any(c => c >= 0x0600 && c <= 0x06FF);
            bool hasEnglish = text.Any(c => c >= 'a' && c <= 'z');
            string language = (hasArabic, hasEnglish) switch
            {
                (true, true) => "mixed",
                (true, false) => "arabic",
                (false, true) => "english",
                _ => "unknown"
            };

            // Topic heuristics
            string topic = "general";
            if (TechKeywords.Any(k => text.Contains(k))) topic = "technical";
            else if (PricingKeywords.Any(k => text.Contains(k))) topic = "pricing";
            else if (DeliveryKeywords.Any(k => text.Contains(k))) topic = "delivery";
            else if (SalesKeywords.Any(k => text.Contains(k))) topic = "sales";
            else if (text.Contains("مشكلة") || text.Contains("complaint")) topic = "complaint";
            
            // Sentiment & Priority heuristics
            string sentiment = "neutral";
            string priority = "normal";
            
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

            // Escalate flag logic
            bool escalate = false;
            if (sentiment == "angry" || priority == "urgent" || (topic == "complaint" && sentiment == "angry"))
            {
                escalate = true;
            }

            var result = new MessageClassificationResult
            {
                Topic = topic,
                Language = language,
                Sentiment = sentiment,
                Priority = priority,
                Escalate = escalate,
                Reason = escalate ? "Rule-based escalation criteria met" : string.Empty
            };
            
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rule-based classification failed for message {MessageId}", input.MessageId);
            return Task.FromResult(new MessageClassificationResult
            {
                Reason = "Classification failed; fallback result used"
            });
        }
    }
}
