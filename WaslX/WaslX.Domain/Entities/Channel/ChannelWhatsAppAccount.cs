using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>Join: which WhatsApp numbers belong to a channel.</summary>
    public class ChannelWhatsAppAccount : BaseEntity
    {
        public int ChannelId { get; set; }
        public int WhatsAppAccountId { get; set; }

        public Channel Channel { get; set; } = null!;
        public WhatsAppAccount WhatsAppAccount { get; set; } = null!;
    }
}
