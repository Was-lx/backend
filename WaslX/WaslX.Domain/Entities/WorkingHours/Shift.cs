using System.Collections.Generic;
using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// A named working shift (tenant-scoped). Holds per-day time windows (which must sit inside the
    /// company's working hours). Agents are attached to a shift; the "Round Robin by working hours"
    /// distribution routes to agents whose shift is currently active.
    /// </summary>
    public class Shift : BaseEntity
    {
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;

        public Tenant Tenant { get; set; } = null!;
        public ICollection<ShiftDay> ShiftDays { get; set; } = new HashSet<ShiftDay>();
        public ICollection<AgentShift> AgentShifts { get; set; } = new HashSet<AgentShift>();
    }
}
