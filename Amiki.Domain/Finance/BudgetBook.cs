namespace Amiki.Domain.Finance;

/// <summary>Which budget applies to a period: its own, or the latest repeating one before it.</summary>
public sealed class BudgetBook(IEnumerable<Budget> budgets)
{
    private readonly IReadOnlyList<Budget> _budgets = [.. budgets];

    /// <summary>The budget in force for <paramref name="period"/>, or null when there's none (or it was turned off).</summary>
    public Budget? For(MoneyPeriod period)
    {
        var own = _budgets.FirstOrDefault(b => b.Length == period.Length && b.Start == period.Start);
        var inForce = own ?? _budgets
            .Where(b => b.Length == period.Length && b.Repeats && b.Start < period.Start)
            .MaxBy(b => b.Start);
        return inForce is { Amount: > 0 } ? inForce : null;
    }

    /// <summary>Whether <paramref name="period"/> has its own budget, rather than one carried over from before.</summary>
    public bool HasOwn(MoneyPeriod period) => _budgets.Any(b => b.Length == period.Length && b.Start == period.Start);
}
