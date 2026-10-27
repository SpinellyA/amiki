using Amiki.Application.Common;

namespace Amiki.Api.Endpoints;

/// <summary>Turns a use case's <see cref="Outcome"/> into the HTTP answer.</summary>
public static class OutcomeResults
{
    public static IResult ToHttp(this Outcome outcome) =>
        outcome.Succeeded ? Results.NoContent() : Problem(outcome);

    public static IResult ToHttp<T>(this Outcome<T> outcome) =>
        outcome.Succeeded ? Results.Ok(outcome.Value) : Problem(outcome.Status);

    private static IResult Problem(Outcome outcome) => outcome.Kind switch
    {
        OutcomeKind.NotFound => outcome.Problem is null ? Results.NotFound() : Results.Problem(outcome.Problem, statusCode: StatusCodes.Status404NotFound),
        _ => Results.Problem(outcome.Problem, statusCode: outcome.Kind switch
        {
            OutcomeKind.Conflict => StatusCodes.Status409Conflict,
            OutcomeKind.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        }),
    };
}
