using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    public class AiAgentNumberSettings : BaseEntity
    {
        public int WhatsAppAccountId { get; set; }
        public bool Enabled { get; set; }
        public bool AutoReplyEnabled { get; set; }
        public bool AllowOutsideWindow { get; set; }
        public int MaxConversationMessages { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
        public int? UpdatedByUserId { get; set; }

        public WhatsAppAccount WhatsAppAccount { get; set; } = null!;
        public User? UpdatedByUser { get; set; }
    }
}
