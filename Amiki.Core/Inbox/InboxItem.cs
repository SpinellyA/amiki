using Amiki.Core;

namespace Amiki.Modules.Inbox;

/// <summary>
/// Something jotted down without deciding what it is yet. Deliberately untyped: capture at
/// school has to be faster than thinking, and sorting happens later at home.
/// </summary>
public sealed class InboxItem : IEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Text { get; init; } = "";
    public DateTime CapturedAt { get; init; } = DateTime.Now;

    public string? Validate() =>
        string.IsNullOrWhiteSpace(Text) ? "Can't save an empty note."
        : Text.Length > 2000 ? "Note is too long (2000 characters max)."
        : null;
}
