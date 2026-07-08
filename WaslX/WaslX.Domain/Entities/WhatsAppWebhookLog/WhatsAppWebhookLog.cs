using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// Raw audit/replay record of every WhatsApp webhook payload Meta delivers to us.
    /// The actual conversation/message persistence happens asynchronously (Hangfire) from this row.
    /// </summary>
    public class WhatsAppWebhookLog : BaseEntity
    {
        /// <summary>The unmodified JSON body Meta POSTed.</summary>
        public string RawPayload { get; set; } = string.Empty;

        /// <summary>Coarse classification, e.g. "message", "status", "unknown".</summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>Business phone number id the event targets (used to resolve the tenant), when present.</summary>
        public string? PhoneNumberId { get; set; }

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public bool Processed { get; set; }

        /// <summary>Populated when async processing failed, so the row can be inspected/replayed.</summary>
        public string? ProcessingError { get; set; }
    }
}
