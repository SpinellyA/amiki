using Amiki.Modules.Tasks;

namespace Amiki.Modules.Assistant;

/// <summary>
/// What Amiki thinks an Inbox note is, ready to accept with one tap. Only the fields that fit
/// the kind are filled: a task has a due date and project, an expense has an amount, category
/// and account, and so on.
/// </summary>
/// <param name="Title">The cleaned-up text: the task title, idea text, or transaction note.</param>
/// <param name="Reason">A few words on why, shown on hover (e.g. "mentions an amount spent").</param>
public sealed record TriageSuggestion(
    Guid NoteId,
    TriageKind Kind,
    string Title,
    DateTime? Due = null,
    string? Project = null,
    Priority Priority = Priority.None,
    decimal? Amount = null,
    string? Category = null,
    string? Account = null,
    string? Reason = null);
