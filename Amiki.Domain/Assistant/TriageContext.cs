namespace Amiki.Domain.Assistant;

/// <summary>
/// The little the model needs to know about you to sort a note: today's date, your accounts,
/// categories and project tags, and a few recent transactions as examples. Never diary entries.
/// </summary>
public sealed record TriageContext(
    DateTime Today,
    IReadOnlyList<string> Accounts,
    IReadOnlyList<string> ExpenseCategories,
    IReadOnlyList<string> IncomeCategories,
    IReadOnlyList<string> Projects,
    IReadOnlyList<TriageExample> Examples);
