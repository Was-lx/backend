namespace WaslX.Domain.SharedEnums
{
    /// <summary>
    /// Who a tenant mainly serves — captured at sign-up to help seed AI routing
    /// and tailor the workspace. "Unknown" is the safe default when unanswered.
    /// </summary>
    public enum CustomerType
    {
        Unknown,
        B2B,
        B2C,
        Both
    }
}
