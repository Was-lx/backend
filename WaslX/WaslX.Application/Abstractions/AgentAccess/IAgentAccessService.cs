using System.Threading;
using System.Threading.Tasks;
using WaslX.Application.Features.AgentAccess.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.AgentAccess;

/// <summary>
/// Manages an agent's access &amp; distribution assignment (tenant-scoped): which channels they can access,
/// which WhatsApp numbers' distribution lists they're in, which groups they belong to and which shifts they work.
/// The agent is identified by their ApplicationUser (Identity) id and resolved to the domain user by tenant + email.
/// </summary>
public interface IAgentAccessService
{
    Task<Result<AgentAccessResponse>> GetAccessAsync(int? tenantId, string appUserId, CancellationToken cancellationToken = default);
    Task<Result<AgentAccessResponse>> SetAccessAsync(int? tenantId, string appUserId, SetAgentAccessRequest request, CancellationToken cancellationToken = default);
}
