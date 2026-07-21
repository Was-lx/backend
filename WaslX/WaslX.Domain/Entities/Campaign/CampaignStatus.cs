namespace WaslX.Domain.Entities
{
    /// <summary>
    /// Campaign lifecycle statuses (stored as strings on <see cref="Campaign.Status"/>).
    /// Draft → Scheduled → Running → (Paused ⇄ Running) → Completed, or Cancelled from any active state.
    /// </summary>
    public static class CampaignStatus
    {
        public const string Draft = "Draft";
        public const string Scheduled = "Scheduled";
        public const string Running = "Running";
        public const string Paused = "Paused";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
    }
}
