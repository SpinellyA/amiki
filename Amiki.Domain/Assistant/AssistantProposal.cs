using Amiki.Domain.Finance;
using Amiki.Domain.Tasks;

namespace Amiki.Domain.Assistant;

/// <summary>
/// A change Amiki suggests in chat, already checked against your real accounts, categories and
/// tasks. It's only a suggestion: the app makes the change when you tap "Do it", the same way
/// your own taps do (so it's audited and can be undone).
/// </summary>
/// <param name="Title">Task title, transaction note, idea text, inbox note, or the task to finish.</param>
/// <param name="TaskId">For <see cref="ProposalKind.CompleteTask"/>: which task.</param>
public sealed record AssistantProposal(
    Guid Id,
    ProposalKind Kind,
    string Title,
    DateTime? Due = null,
    string? Project = null,
    Priority Priority = Priority.None,
    TxKind? MoneyKind = null,
    decimal? Amount = null,
    string? Category = null,
    string? Account = null,
    DateTime? Date = null,
    IReadOnlyList<string>? Tags = null,
    Guid? TaskId = null,
    ProposalStatus Status = ProposalStatus.Waiting)
{
    /// <summary>One line for the card and for telling the model what it suggested.</summary>
    public string Describe() => Kind switch
    {
        ProposalKind.AddTask => Join($"Add task “{Title}”",
            Due is { } due ? $"due {due:ddd, MMM d}" : null,
            Project is { } p ? $"#{p}" : null,
            Priority != Priority.None ? $"{Priority} priority" : null),
        ProposalKind.LogMoney => Join(
            $"{(MoneyKind == TxKind.Income ? "Received" : "Spent")} {Money.Format(Amount ?? 0)}",
            Category, Account, Date is { } d ? $"{d:MMM d}" : null, $"“{Title}”"),
        ProposalKind.AddIdea => Join($"Save idea “{Title}”", Tags is { Count: > 0 } tags ? string.Join(" ", tags.Select(t => "#" + t)) : null),
        ProposalKind.CompleteTask => $"Mark “{Title}” as done",
        ProposalKind.Remember => $"Remember: “{Title}”",
        _ => $"Add to Inbox: “{Title}”",
    };

    private static string Join(params string?[] parts) => string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
}
