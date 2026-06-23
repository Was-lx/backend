
using MediatR;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Mediator;

public interface ICommand : IRequest<Result>;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
