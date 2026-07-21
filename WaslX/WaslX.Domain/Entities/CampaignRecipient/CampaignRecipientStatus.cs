namespace WaslX.Domain.Entities
{
    /// <summary>Per-recipient delivery statuses (stored as strings on <see cref="CampaignRecipient.Status"/>).</summary>
    public static class CampaignRecipientStatus
    {
        public const string Queued = "queued";
        public const string Sent = "sent";
        public const string Delivered = "delivered";
        public const string Read = "read";
        public const string Failed = "failed";
    }
}
