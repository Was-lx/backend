namespace WaslX.Application.Features.Permissions.Dtos;

/// <summary>A single cell of the Roles × Permissions matrix for the settings screen.</summary>
public record PermissionCellDto(bool Granted, string? Scope, bool Locked);

/// <summary>One permission row with its per-role cells.</summary>
public record PermissionRowDto(
    string Code,
    string Description,
    string Tier,
    bool IsScope,
    string? ScopeOptions,
    int Sort,
    IReadOnlyDictionary<string, PermissionCellDto> Cells);

public record PermissionCategoryDto(string Name, IReadOnlyList<PermissionRowDto> Permissions);

/// <summary>The full editable matrix a tenant Admin sees on the Roles & Permissions screen.</summary>
public record PermissionMatrixResponse(
    IReadOnlyList<string> Roles,
    IReadOnlyList<PermissionCategoryDto> Categories);

/// <summary>A user's own resolved permissions — used by the frontend to show/hide UI.</summary>
public record EffectivePermissionsResponse(
    IReadOnlyList<string> Granted,
    IReadOnlyDictionary<string, string> Scopes);

/// <summary>One requested change from the settings screen.</summary>
public record PermissionUpdateItem(string Role, string Code, bool Granted, string? Scope);
