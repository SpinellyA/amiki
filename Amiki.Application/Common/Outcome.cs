namespace Amiki.Application.Common;

/// <summary>The result of a use case that returns nothing: ok, or why not.</summary>
public sealed record Outcome(OutcomeKind Kind, string? Problem = null)
{
    public static readonly Outcome Ok = new(OutcomeKind.Ok);

    public bool Succeeded => Kind == OutcomeKind.Ok;

    public static Outcome Invalid(string problem) => new(OutcomeKind.Invalid, problem);
    public static Outcome NotFound(string? problem = null) => new(OutcomeKind.NotFound, problem);
    public static Outcome Conflict(string problem) => new(OutcomeKind.Conflict, problem);
    public static Outcome Forbidden(string problem) => new(OutcomeKind.Forbidden, problem);

    /// <summary>Ok when there's no problem, otherwise <paramref name="kind"/> with it.</summary>
    public static Outcome From(string? problem, OutcomeKind kind = OutcomeKind.Invalid) =>
        problem is null ? Ok : new(kind, problem);
}
