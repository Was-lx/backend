using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using WaslX.Application.Abstractions.Permissions;

namespace WaslX.Api.Authorization;

/// <summary>
/// Gate an endpoint on a fine-grained permission code from the per-tenant RBAC matrix,
/// e.g. <c>[HasPermission(Permissions.CampaignCreateSend)]</c>. Enforcement is server-side
/// via <see cref="IPermissionService"/>, so tenant Admin toggles are authoritative — not
/// just cosmetic in the UI. Composes with role checks; use for Configurable-tier actions.
/// </summary>
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permissionCode)
        => Policy = $"{PermissionPolicyProvider.Prefix}{permissionCode}";
}

/// <summary>The permission code an endpoint requires.</summary>
public sealed class PermissionRequirement(string code) : IAuthorizationRequirement
{
    public string Code { get; } = code;
}

/// <summary>
/// Builds a policy on demand for any <c>perm:{code}</c> policy name, and delegates every
/// other policy (and the default/fallback) to the framework's default provider.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    public const string Prefix = "perm:";

    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            var code = policyName[Prefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(code))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}

/// <summary>
/// Resolves the caller's role + tenant from the JWT and asks <see cref="IPermissionService"/>
/// whether that role may perform the permission in that tenant (defaults + tenant toggles +
/// hard locks). SuperAdmin is unrestricted at the platform level.
/// </summary>
public sealed class PermissionAuthorizationHandler(IPermissionService permissions)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var user = context.User;
        if (user.Identity is not { IsAuthenticated: true })
            return;

        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(role))
            return;

        if (string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return;
        }

        int? tenantId = int.TryParse(user.FindFirst("tenantId")?.Value, out var parsed) ? parsed : null;

        if (await permissions.HasAsync(tenantId, role, requirement.Code))
            context.Succeed(requirement);
    }
}
