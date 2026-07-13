namespace WaslX.Domain.SharedEnums;

/// <summary>
/// Classifies the type of the currently active (or most recent) WhatsApp conversation window.
///
/// ARCHITECTURE NOTE:
///   This value is a cached classification only. Meta is the ultimate source of truth.
///   Never make business decisions based solely on this enum — always consider WindowExpiresAt
///   in conjunction with the current UTC time.
///
///   CustomerService24h — started by any inbound customer message without a referral object.
///   FreeEntryPoint72h  — started by an inbound message carrying a Meta referral object
///                        (Click-to-WhatsApp Ad, Facebook/Instagram Page CTA).
/// </summary>
public enum ConversationWindowType
{
    None,
    CustomerService24h,
    FreeEntryPoint72h
}
