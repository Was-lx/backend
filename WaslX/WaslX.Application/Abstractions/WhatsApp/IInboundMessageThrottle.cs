namespace WaslX.Application.Abstractions.WhatsApp;

/// <summary>
/// Caps how many inbound WhatsApp messages from a single customer phone number can trigger
/// AI-costing work (classification + AI-reply generation) within a short window. This is the
/// direct countermeasure to a spam flood from one number: no auth is required to send a WhatsApp
/// message to a connected business number, so this is the cheapest lever an attacker has to trigger
/// unbounded LLM calls, and the per-tenant monthly quota alone would still let a single burst spend
/// the whole month's budget in seconds. Message storage/display is never throttled — only the
/// downstream AI work is skipped when a number is over its limit.
/// </summary>
public interface IInboundMessageThrottle
{
    /// <summary>True if this phone number is still within its allowance and the caller may proceed.</summary>
    bool TryAcquire(string phoneNumber);
}
