using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>Join: an agent works a given shift.</summary>
    public class AgentShift : BaseEntity
    {
        public int UserId { get; set; }
        public int ShiftId { get; set; }

        public User User { get; set; } = null!;
        public Shift Shift { get; set; } = null!;
    }
}
