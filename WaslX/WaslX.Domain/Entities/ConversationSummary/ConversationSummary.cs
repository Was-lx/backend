using System;
using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// A cached AI summary for a conversation. One row per conversation, upserted as the
    /// thread grows. <see cref="UpToMessageId"/> is the freshness cursor: the summary is stale once
    /// the conversation has a message newer than this id.
    /// </summary>
    public class ConversationSummary : BaseEntity
    {
        public int TenantId { get; set; }
        public int ConversationId { get; set; }

        /// <summary>Concise one-line summary shown at the top of any conversation.</summary>
        public string ShortSummary { get; set; } = string.Empty;

        /// <summary>Longer structured summary (key points · decisions · what's needed). Generated on demand.</summary>
        public string? FullSummary { get; set; }

        /// <summary>Id of the newest message reflected by this summary — the staleness cursor.</summary>
        public int UpToMessageId { get; set; }

        /// <summary>Number of messages the summary was generated from (informational).</summary>
        public int MessageCount { get; set; }

        /// <summary>Domain user id that last triggered generation (null for automatic generation).</summary>
        public int? GeneratedByUserId { get; set; }

        public DateTime GeneratedAt { get; set; }

        public Tenant Tenant { get; set; } = null!;
        public Conversation Conversation { get; set; } = null!;
    }
}
