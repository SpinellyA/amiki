namespace Amiki.Application.Common;

/// <summary>The result of a use case that returns a value: the value, or why there isn't one.</summary>
public sealed record Outcome<T>(T? Value, Outcome Status)
{
    public bool Succeeded => Status.Succeeded;

    public static implicit operator Outcome<T>(T value) => new(value, Outcome.Ok);
    public static implicit operator Outcome<T>(Outcome failure) => new(default, failure);
}
