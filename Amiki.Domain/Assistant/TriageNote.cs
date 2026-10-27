namespace Amiki.Domain.Assistant;

/// <summary>An Inbox note sent for a suggestion: its id and text, nothing else.</summary>
public sealed record TriageNote(Guid Id, string Text);
