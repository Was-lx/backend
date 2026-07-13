using WaslX.Domain.Common;

namespace WaslX.Domain.Entities;

/// <summary>
/// Immutable audit log of every Meta template lifecycle event for a given template.
/// One row is appended on every status change (PENDING → APPROVED → PAUSED → DISABLED → DELETED …).
/// Rows are NEVER modified or deleted — this is a permanent history record.
/// </summary>
public class TemplateReviewHistory : BaseEntity
{
    public int TemplateReviewId { get; set; }
    public int TenantId { get; set; }

    /// <summary>The new status that arrived in this event (APPROVED, REJECTED, PAUSED, …).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>UTC time this event was recorded locally.</summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public string? ReasonCode { get; set; }
    public string? ReasonText { get; set; }

    /// <summary>
    /// The category Meta reported in this event's payload (may differ from SubmittedCategory).
    /// </summary>
    public string? FinalCategory { get; set; }

    /// <summary>Raw JSON of Meta's pause_info object when Status = PAUSED.</summary>
    public string? PauseInfo { get; set; }

    /// <summary>Complete raw webhook JSON that generated this event. Never truncated.</summary>
    public string? MetaStatusRaw { get; set; }

    // Navigation
    public TemplateReview TemplateReview { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
