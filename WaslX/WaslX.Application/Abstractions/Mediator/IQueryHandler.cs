using MediatR;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Mediator;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
where TQuery : IQuery<TResponse>;
