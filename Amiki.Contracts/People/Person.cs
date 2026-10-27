using Amiki.Domain.People;

namespace Amiki.Contracts.People;

/// <summary>Someone who can sign in, as the owner sees them on the People page. Their API key itself is never sent back.</summary>
/// <param name="KeySetBy">Who saved their Gemini key; null when they don't have one.</param>
public sealed record Person(
    Guid Id,
    string Email,
    string Name,
    bool IsOwner,
    bool Enabled,
    KeySource? KeySetBy,
    DateTime AddedAt,
    DateTime? LastSignInAt)
{
    public bool HasKey => KeySetBy is not null;

    /// <summary>Their name once they've signed in, their address until then.</summary>
    public string Label => string.IsNullOrWhiteSpace(Name) ? Email : Name;
}
