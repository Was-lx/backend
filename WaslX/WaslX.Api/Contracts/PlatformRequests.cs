using WaslX.Application.Features.Permissions.Dtos;

namespace WaslX.Api.Contracts;

public record SetPlanActiveRequest(bool IsActive);
public record SetTenantStatusRequest(string Status);
public record OnboardingUpdateRequest(int Step, bool Completed);
public record UpdatePermissionsRequest(List<PermissionUpdateItem> Changes);
