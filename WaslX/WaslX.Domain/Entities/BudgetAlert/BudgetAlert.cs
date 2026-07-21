using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// A spending threshold (Sprint 6 — Platform Owner AI-cost monitoring). When accrued AI cost for the
    /// given <see cref="Scope"/> over the given <see cref="Period"/> crosses <see cref="ThresholdUsd"/>,
    /// the alert fires. <see cref="TenantId"/> is a plain nullable column: null = a global (platform-wide)
    /// alert, otherwise a per-tenant alert. Deliberately no FK / navigation.
    /// </summary>
    public class BudgetAlert : BaseEntity
    {
        public int? TenantId { get; set; }
        public string Scope { get; set; } = string.Empty;
        public decimal ThresholdUsd { get; set; }
        public string Period { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
        public string? NotifyEmail { get; set; }
    }
}
