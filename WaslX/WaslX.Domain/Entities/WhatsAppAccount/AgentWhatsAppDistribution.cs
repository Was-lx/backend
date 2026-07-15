using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// Join: an agent is in the auto-distribution list of a specific WhatsApp number.
    /// An agent may have channel access to a number without being in its distribution
    /// (they can still handle chats manually, but round-robin won't route new ones to them).
    /// </summary>
    public class AgentWhatsAppDistribution : BaseEntity
    {
        public int UserId { get; set; }
        public int WhatsAppAccountId { get; set; }

        public User User { get; set; } = null!;
        public WhatsAppAccount WhatsAppAccount { get; set; } = null!;
    }
}
