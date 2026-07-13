using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{

    public class TemplateReview : BaseEntity
    {
        public int TenantId { get; set; }
        public string MetaTemplateId { get; set; } = string.Empty;
        public string MessageTemplateName { get; set; } = string.Empty;
        public string? Language { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? ReasonCode { get; set; }

        public string? ReasonText { get; set; }

        public string? MetaNotes { get; set; }

        public string SubmittedCategory { get; set; } = string.Empty;

        public bool AllowCategoryChange { get; set; }

        public DateTime? ReviewedAt { get; set; }
        

        public string? FinalCategory { get; set; }

        public string? PauseInfo { get; set; }

        public DateTime? DisableTimestamp { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? MetaStatusRaw { get; set; }

        public Tenant Tenant { get; set; } = null!;
    }
}
