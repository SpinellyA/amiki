using Amiki.Domain.Finance;
using Amiki.Domain.Tasks;

namespace Amiki.Domain.Assistant;

/// <summary>
/// Never trust the model blindly: its suggestion is checked against your real accounts and
/// categories before you see it. A category it made up becomes the matching "Other", an
/// unknown account becomes Cash, money without an amount isn't money, and so on.
/// </summary>
public static class TriageRules
{
    public const int MaxTitleLength = 200;

    public static TriageSuggestion Clean(TriageSuggestion raw, TriageNote note, TriageContext context)
    {
        var title = string.IsNullOrWhiteSpace(raw.Title) ? note.Text.Trim() : raw.Title.Trim();
        if (title.Length > MaxTitleLength) title = title[..MaxTitleLength].TrimEnd() + "…";
        var cleaned = raw with { NoteId = note.Id, Title = title, Reason = raw.Reason?.Trim() };

        return raw.Kind switch
        {
            TriageKind.Expense or TriageKind.Income => Money(cleaned, context),
            TriageKind.Task => cleaned with
            {
                Due = raw.Due?.Date,
                Project = Project(raw.Project),
                Amount = null, Category = null, Account = null,
            },
            TriageKind.Idea => Bare(cleaned) with { Project = Project(raw.Project) },
            _ => Bare(cleaned) with { Kind = TriageKind.Unsure },
        };
    }

    private static TriageSuggestion Money(TriageSuggestion s, TriageContext context)
    {
        // No amount, no transaction: let the person decide what it is.
        if (s.Amount is not > 0) return Bare(s) with { Kind = TriageKind.Unsure };

        var categories = s.Kind == TriageKind.Income ? context.IncomeCategories : context.ExpenseCategories;
        var fallback = Category.FallbackFor(s.Kind == TriageKind.Income ? TxKind.Income : TxKind.Expense);
        return s with
        {
            Amount = Math.Round(s.Amount.Value, 2),
            Category = Match(s.Category, categories) ?? fallback,
            Account = Match(s.Account, context.Accounts) ?? Match("Cash", context.Accounts) ?? context.Accounts.FirstOrDefault(),
            Due = null, Project = null, Priority = Priority.None,
        };
    }

    private static TriageSuggestion Bare(TriageSuggestion s) =>
        s with { Due = null, Project = null, Priority = Priority.None, Amount = null, Category = null, Account = null };

    // The real name, matched without caring about case ("gcash" → "GCash"), or null if there's no such thing.
    private static string? Match(string? value, IEnumerable<string> known) =>
        value is null ? null : known.FirstOrDefault(k => string.Equals(k, value.Trim(), StringComparison.OrdinalIgnoreCase));

    // Projects are lowercase tags without the '#', like the task quick-add uses.
    private static string? Project(string? value)
    {
        var tag = value?.Trim().TrimStart('#').ToLowerInvariant();
        return string.IsNullOrEmpty(tag) || tag.Contains(' ') ? null : tag;
    }
}
