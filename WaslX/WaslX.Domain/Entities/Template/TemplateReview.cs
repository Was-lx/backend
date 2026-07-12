using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// Locally-mirrored review metadata for a Meta message template. Meta is still the source of
    /// truth for a template's content (header/body/buttons) and approval status on the live API,
    /// but it never echoes back the category WE submitted at create time nor whether we opted into
    /// <c>allow_category_change</c>. This row captures those create-time values plus the rejection
    /// reason / reviewed timestamp delivered later through the
    /// <c>message_template_status_update</c> webhook, so the UI can show "requested vs final"
    /// category comparisons and rejection reasons.
    /// </summary>
    public class TemplateReview : BaseEntity
    {
        public int TenantId { get; set; }
        public string MetaTemplateId { get; set; } = string.Empty;
        public string MessageTemplateName { get; set; } = string.Empty;
        public string? Language { get; set; }

        /// <summary>Latest approval status mirrored from Meta (APPROVED / PENDING / REJECTED).</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Rejection reason code, when Meta provides one. NULL when none is supplied.</summary>
        public string? ReasonCode { get; set; }

        /// <summary>Human-readable rejection reason, when Meta provides one. NULL when none is supplied.</summary>
        public string? ReasonText { get; set; }

        /// <summary>Free-form notes Meta may attach to a review decision. NULL when absent.</summary>
        public string? MetaNotes { get; set; }

        /// <summary>The category WE submitted at create time (never echoed back by Meta).</summary>
        public string SubmittedCategory { get; set; } = string.Empty;

        /// <summary>Whether we sent allow_category_change=true at create time.</summary>
        public bool AllowCategoryChange { get; set; }

        /// <summary>When Meta reached a review decision (approved/rejected). NULL while still pending.</summary>
        public DateTime? ReviewedAt { get; set; }

        public Tenant Tenant { get; set; } = null!;
    }
}
