using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>Join: an agent has access to a channel (can see/handle all its numbers' chats).</summary>
    public class AgentChannelAccess : BaseEntity
    {
        public int UserId { get; set; }
        public int ChannelId { get; set; }

        public User User { get; set; } = null!;
        public Channel Channel { get; set; } = null!;
    }
}
