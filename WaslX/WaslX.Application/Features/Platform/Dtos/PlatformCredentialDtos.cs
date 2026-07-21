namespace WaslX.Application.Features.Platform.Dtos;

/// <summary>
/// A global platform secret / integration credential (US-6.6), returned in MASKED form only. The raw
/// value and the encrypted-at-rest value are NEVER serialized — <see cref="Masked"/> is a display-safe
/// preview (e.g. the last 4 characters). Global — no tenant scope.
/// </summary>
public record PlatformCredentialResponse(
    int Id,
    string Key,
    string DisplayName,
    string Category,
    string Masked,
    bool IsActive,
    DateTime? RotatedAt,
    DateTime CreatedAt);

/// <summary>Create a platform credential. <paramref name="Value"/> is encrypted at rest and never returned.</summary>
public record CreatePlatformCredentialInput(
    string Key,
    string DisplayName,
    string Category,
    string Value,
    bool IsActive);

/// <summary>
/// Update a platform credential's metadata. <paramref name="Value"/> is optional — provide it only to
/// re-encrypt a new secret; leave it null/blank to keep the stored secret unchanged.
/// </summary>
public record UpdatePlatformCredentialInput(
    string DisplayName,
    string Category,
    string? Value,
    bool IsActive);

/// <summary>Rotate a credential's secret in place (re-encrypts + refreshes the mask + stamps RotatedAt).</summary>
public record RotatePlatformCredentialInput(string Value);
