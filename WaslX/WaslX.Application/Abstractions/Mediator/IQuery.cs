using MediatR;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Mediator;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
