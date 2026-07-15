using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WaslX.Application.Features.Channels.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Channels;

/// <summary>Channel management (tenant-scoped): grouping WhatsApp numbers so agents can be granted access.</summary>
public interface IChannelService
{
    Task<Result<IReadOnlyList<ChannelResponse>>> GetAllAsync(int? tenantId, CancellationToken cancellationToken = default);
    Task<Result<ChannelResponse>> GetByIdAsync(int? tenantId, int id, CancellationToken cancellationToken = default);
    Task<Result<ChannelResponse>> CreateAsync(int? tenantId, UpsertChannelRequest request, CancellationToken cancellationToken = default);
    Task<Result<ChannelResponse>> UpdateAsync(int? tenantId, int id, UpsertChannelRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int? tenantId, int id, CancellationToken cancellationToken = default);
}
