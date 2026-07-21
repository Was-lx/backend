using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Global platform credential / secret store (US-6.6). Secrets are ENCRYPTED AT REST with ASP.NET Core
/// Data Protection (a keyed protector) and are NEVER returned to any caller — reads expose only a masked
/// preview. These secrets are only ever reachable through the SuperAdmin console
/// (<c>[Authorize(Roles = "SuperAdmin")]</c>), never from a tenant API. Every mutation is audited.
/// </summary>
internal sealed class PlatformCredentialService : IPlatformCredentialService
{
    private readonly ApplicationDbContext _db;
    private readonly IPlatformAuditService _audit;
    private readonly IDataProtector _protector;

    public PlatformCredentialService(ApplicationDbContext db, IPlatformAuditService audit, IDataProtectionProvider provider)
    {
        _db = db;
        _audit = audit;
        _protector = provider.CreateProtector("platform-credentials");
    }

    public async Task<Result<IReadOnlyList<PlatformCredentialResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Projection deliberately omits EncryptedValue — it must never leave the persistence layer.
        var creds = await _db.PlatformCredentials.AsNoTracking()
            .OrderBy(c => c.Category).ThenBy(c => c.Key)
            .Select(c => new PlatformCredentialResponse(
                c.Id, c.Key, c.DisplayName, c.Category, c.Masked, c.IsActive, c.RotatedAt, c.CreatedAt))
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PlatformCredentialResponse>>(creds);
    }

    public async Task<Result<PlatformCredentialResponse>> CreateAsync(CreatePlatformCredentialInput input, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var key = (input.Key ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(key)) return Result.Failure<PlatformCredentialResponse>(AppErrors.CredentialKeyRequired);
        if (string.IsNullOrWhiteSpace(input.Value)) return Result.Failure<PlatformCredentialResponse>(AppErrors.CredentialValueRequired);

        if (await _db.PlatformCredentials.AnyAsync(c => c.Key == key, cancellationToken))
            return Result.Failure<PlatformCredentialResponse>(AppErrors.DuplicateCredentialKey);

        var cred = new PlatformCredential
        {
            Key = key,
            DisplayName = (input.DisplayName ?? string.Empty).Trim(),
            Category = (input.Category ?? string.Empty).Trim(),
            EncryptedValue = _protector.Protect(input.Value),
            Masked = Mask(input.Value),
            IsActive = input.IsActive
        };
        _db.PlatformCredentials.Add(cred);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(actor.ActorId, actor.ActorEmail, "PlatformCredentialCreated", "PlatformCredential", cred.Id.ToString(),
            null, $"key='{cred.Key}'; category='{cred.Category}'", actor.Ip, cancellationToken);

        return Result.Success(Map(cred));
    }

    public async Task<Result<PlatformCredentialResponse>> UpdateAsync(int id, UpdatePlatformCredentialInput input, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var cred = await _db.PlatformCredentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (cred is null) return Result.Failure<PlatformCredentialResponse>(AppErrors.CredentialNotFound);

        cred.DisplayName = (input.DisplayName ?? string.Empty).Trim();
        cred.Category = (input.Category ?? string.Empty).Trim();
        cred.IsActive = input.IsActive;

        // Only re-encrypt when a new secret is supplied; otherwise keep the stored one untouched.
        var secretChanged = !string.IsNullOrWhiteSpace(input.Value);
        if (secretChanged)
        {
            cred.EncryptedValue = _protector.Protect(input.Value!);
            cred.Masked = Mask(input.Value!);
            cred.RotatedAt = DateTime.UtcNow;
        }
        cred.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(actor.ActorId, actor.ActorEmail, "PlatformCredentialUpdated", "PlatformCredential", cred.Id.ToString(),
            null, $"key='{cred.Key}'; secretRotated={secretChanged}", actor.Ip, cancellationToken);

        return Result.Success(Map(cred));
    }

    public async Task<Result<PlatformCredentialResponse>> RotateAsync(int id, RotatePlatformCredentialInput input, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var cred = await _db.PlatformCredentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (cred is null) return Result.Failure<PlatformCredentialResponse>(AppErrors.CredentialNotFound);
        if (string.IsNullOrWhiteSpace(input.Value)) return Result.Failure<PlatformCredentialResponse>(AppErrors.CredentialValueRequired);

        cred.EncryptedValue = _protector.Protect(input.Value);
        cred.Masked = Mask(input.Value);
        cred.RotatedAt = DateTime.UtcNow;
        cred.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(actor.ActorId, actor.ActorEmail, "PlatformCredentialRotated", "PlatformCredential", cred.Id.ToString(),
            null, $"key='{cred.Key}'", actor.Ip, cancellationToken);

        return Result.Success(Map(cred));
    }

    public async Task<Result> DeleteAsync(int id, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var cred = await _db.PlatformCredentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (cred is null) return Result.Failure(AppErrors.CredentialNotFound);

        _db.PlatformCredentials.Remove(cred);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(actor.ActorId, actor.ActorEmail, "PlatformCredentialDeleted", "PlatformCredential", id.ToString(),
            null, $"key='{cred.Key}'", actor.Ip, cancellationToken);

        return Result.Success();
    }

    private static PlatformCredentialResponse Map(PlatformCredential c) =>
        new(c.Id, c.Key, c.DisplayName, c.Category, c.Masked, c.IsActive, c.RotatedAt, c.CreatedAt);

    // Display-safe preview: last 4 characters, the rest bulleted. Never reveals the full secret.
    private static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var last4 = value.Length <= 4 ? value : value[^4..];
        return $"••••{last4}";
    }
}
