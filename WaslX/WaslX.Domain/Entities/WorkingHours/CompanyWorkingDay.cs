using System;
using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// The company's operating hours for one day of the week (tenant-wide, applies across the platform).
    /// A day marked not-working (e.g. Friday off) has null times. Shifts must fall within these hours.
    /// </summary>
    public class CompanyWorkingDay : BaseEntity
    {
        public int TenantId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public bool IsWorkingDay { get; set; } = true;
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }

        public Tenant Tenant { get; set; } = null!;
    }
}
