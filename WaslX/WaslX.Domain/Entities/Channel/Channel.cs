using System.Collections.Generic;
using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// A tenant-defined grouping of WhatsApp numbers. Agents are granted access to a channel
    /// (they can see/handle its numbers' chats); within a channel, an agent is additionally
    /// placed into the distribution list of specific numbers.
    /// </summary>
    public class Channel : BaseEntity
    {
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public Tenant Tenant { get; set; } = null!;
        public ICollection<ChannelWhatsAppAccount> ChannelWhatsAppAccounts { get; set; } = new HashSet<ChannelWhatsAppAccount>();
        public ICollection<AgentChannelAccess> AgentChannelAccesses { get; set; } = new HashSet<AgentChannelAccess>();
    }
}
