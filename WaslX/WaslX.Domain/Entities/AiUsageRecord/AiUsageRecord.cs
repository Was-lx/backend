using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// One metered AI call (Sprint 6 — Platform Owner usage / AI-cost monitoring). Records the tokens
    /// and USD cost of a single request to an AI component (RAG, classification, agent reply, summary…)
    /// for a tenant, optionally tied to the conversation it served.
    /// </summary>
    public class AiUsageRecord : BaseEntity
    {
        public int TenantId { get; set; }
        public int? ConversationId { get; set; }
        public string Component { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public decimal CostUsd { get; set; }

        public Tenant Tenant { get; set; } = null!;
    }
}
