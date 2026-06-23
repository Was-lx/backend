using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WaslX.Domain.Results;

namespace WaslX.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result) =>
        result.IsSuccess
            ? new OkResult()
            : result.ToProblem();

    public static IActionResult ToActionResult<TValue>(this Result<TValue> result) =>
        result.IsSuccess
            ? new OkObjectResult(result.Value)
            : result.ToProblem();

    public static ObjectResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert a successful result to a problem");

        var problem = Results.Problem(statusCode: result.Error.StatusCode);
        var problemDetails = problem.GetType().GetProperty(nameof(ProblemDetails))!.GetValue(problem) as ProblemDetails;
        problemDetails!.Extensions = new Dictionary<string, object?>
            {
                {
                    "errors",new[]
                    {
                        result.Error.Code,
                        result.Error.Description
                    }
                }
            };
        return new ObjectResult(problemDetails);
    }
}

