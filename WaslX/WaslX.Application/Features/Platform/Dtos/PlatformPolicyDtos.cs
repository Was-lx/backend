namespace WaslX.Application.Features.Platform.Dtos;

/// <summary>
/// The global platform policy (US-6.7), materialized from <c>PlatformSetting</c> key/value rows. The three
/// well-known keys are surfaced as typed fields; <see cref="Settings"/> carries the full raw list for any
/// additional keys.
/// </summary>
public record PlatformPolicyResponse(
    int RetentionDays,
    int RateLimitPerMinute,
    string RoutingDefaultMode,
    IReadOnlyList<PlatformSettingItem> Settings);

/// <summary>A single raw platform setting key/value pair.</summary>
public record PlatformSettingItem(string Key, string Value, string ValueType, string? Description);

/// <summary>
/// Set one or more platform policy keys. Every field is optional — only the provided ones are written
/// (keys: <c>retention.days</c>, <c>ratelimit.perMinute</c>, <c>routing.defaultMode</c>).
/// </summary>
public record SetPlatformPolicyInput(
    int? RetentionDays,
    int? RateLimitPerMinute,
    string? RoutingDefaultMode);
