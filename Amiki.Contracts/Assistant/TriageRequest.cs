namespace Amiki.Contracts.Assistant;

/// <param name="Today">Your date (the phone's or PC's), so "tomorrow" means your tomorrow, not the server's.</param>
public sealed record TriageRequest(DateTime Today, IReadOnlyList<TriageNote> Notes)
{
    public const int MaxNotes = 20;
    public const int MaxNoteLength = 2000;

    public string? Validate() =>
        Notes.Count == 0 ? "Nothing to sort."
        : Notes.Count > MaxNotes ? $"Up to {MaxNotes} notes at a time."
        : Notes.Any(n => string.IsNullOrWhiteSpace(n.Text) || n.Text.Length > MaxNoteLength) ? "Each note needs text (2000 characters max)."
        : null;
}
