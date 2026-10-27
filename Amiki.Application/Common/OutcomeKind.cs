namespace Amiki.Application.Common;

/// <summary>How a use case went, in words the Api turns into HTTP status codes.</summary>
public enum OutcomeKind
{
    Ok,
    /// <summary>The request itself is wrong (bad input). 400.</summary>
    Invalid,
    NotFound,
    /// <summary>Fine on its own, but clashes with what's stored. 409.</summary>
    Conflict,
    /// <summary>Not allowed for this person. 403.</summary>
    Forbidden,
}
