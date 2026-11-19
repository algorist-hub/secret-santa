using Algorist.Contracts.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Algorist.Api.ErrorHandling;

/// <summary>Builds problem details responses that carry an API error code.</summary>
public static class ProblemResults
{
    public static ProblemDetails Create(int statusCode, string errorCode)
    {
        var problem = new ProblemDetails { Status = statusCode, Title = errorCode };
        problem.Extensions[ApiError.CodeProperty] = errorCode;
        return problem;
    }

    public static IResult Problem(int statusCode, string errorCode) =>
        TypedResults.Problem(Create(statusCode, errorCode));
}
