using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Behaviors;


public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var description = string.Join(" | ", failures.Select(f => f.ErrorMessage).Distinct());
        var error = new Error("Validation.Failed", description, StatusCodes.BadRequest);

        return CreateValidationResult(error);
    }

    private static TResponse CreateValidationResult(Error error)
    {
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        var failureMethod = typeof(Result)
            .GetMethods()
            .First(m => m is { Name: nameof(Result.Failure), IsGenericMethod: true });

        var typedFailure = failureMethod
            .MakeGenericMethod(typeof(TResponse).GenericTypeArguments[0])
            .Invoke(null, [error])!;

        return (TResponse)typedFailure;
    }
}

internal static class StatusCodes
{
    public const int BadRequest = 400;
}
