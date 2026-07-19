using WaslX.Domain.Common;

namespace WaslX.Domain.Entities;

public class MessageClassification : BaseEntity
{
    public int TenantId { get; set; }
    public int ConversationId { get; set; }
    public int MessageId { get; set; }

    public string Topic { get; set; } = "general";
    public string Language { get; set; } = "unknown";
    public string Sentiment { get; set; } = "neutral";
    public string Priority { get; set; } = "normal";
    
    public bool Escalate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ClassifierVersion { get; set; } = string.Empty;

    public Tenant Tenant { get; set; } = null!;
    public Conversation Conversation { get; set; } = null!;
    public Message Message { get; set; } = null!;
}
