using System;
using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>One day's time window for a shift. Absent for days the shift doesn't run (or company days off).</summary>
    public class ShiftDay : BaseEntity
    {
        public int ShiftId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public Shift Shift { get; set; } = null!;
    }
}
