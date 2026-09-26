namespace Amiki.Modules.Finance;

public enum FindingLevel { Warning, Info }

public sealed record Finding(FindingLevel Level, string Message, string? Account = null);

/// <summary>
/// The self-audit: looks through the books for the ways they usually go wrong, and says what to
/// look at. Runs on every change, so problems show up while they're still easy to remember.
/// </summary>
public static class FinanceAudit
{
    public static readonly TimeSpan StaleAfter = TimeSpan.FromDays(14);
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(5);

    public static List<Finding> Run(FinanceStore store)
    {
        var findings = new List<Finding>();

        foreach (var account in store.Accounts.Select(a => a.Name))
        {
            var balance = store.Balance(account);
            if (balance < 0)
                findings.Add(new(FindingLevel.Warning,
                    $"{account} is at −{Money.Format(balance)}. An account can't go below zero, so an entry is probably missing or on the wrong account.",
                    account));

            var used = store.All.Any(t => t.Account == account) || store.Transfers.Any(t => t.From == account || t.To == account);
            if (store.LastCheck(account) is not { } last)
            {
                if (used || balance != 0)
                    findings.Add(new(FindingLevel.Info, $"{account} hasn't been checked against its real balance yet.", account));
            }
            else if (DateTime.Now - last.CheckedAt > StaleAfter)
            {
                findings.Add(new(FindingLevel.Info,
                    $"{account} was last checked {(DateTime.Now - last.CheckedAt).Days} days ago.", account));
            }
        }

        // The same entry logged twice (a double tap, or once on the phone and once at home).
        var duplicates = store.All
            .GroupBy(t => (t.Account, t.Kind, t.Amount, t.Category, t.Date.Date))
            .SelectMany(g => g.OrderBy(t => t.CreatedAt).Zip(g.OrderBy(t => t.CreatedAt).Skip(1)))
            .Where(pair => pair.Second.CreatedAt - pair.First.CreatedAt <= DuplicateWindow);
        foreach (var (first, _) in duplicates)
            findings.Add(new(FindingLevel.Warning,
                $"Two {(first.Kind == TxKind.Income ? "+" : "−")}{Money.Format(first.Amount)} {first.Category} entries on {first.Date:MMM d} in {first.Account}, logged minutes apart. Duplicate?",
                first.Account));

        var transferDuplicates = store.Transfers
            .GroupBy(t => (t.From, t.To, t.Amount, t.Date.Date))
            .SelectMany(g => g.OrderBy(t => t.CreatedAt).Zip(g.OrderBy(t => t.CreatedAt).Skip(1)))
            .Where(pair => pair.Second.CreatedAt - pair.First.CreatedAt <= DuplicateWindow);
        foreach (var (first, _) in transferDuplicates)
            findings.Add(new(FindingLevel.Warning,
                $"Two {Money.Format(first.Amount)} moves from {first.From} to {first.To} on {first.Date:MMM d}, minutes apart. Duplicate?",
                first.From));

        foreach (var transfer in store.Transfers.Where(t => t.Fee > 0 && t.Fee >= t.Amount))
            findings.Add(new(FindingLevel.Warning,
                $"The fee on moving {Money.Format(transfer.Amount)} from {transfer.From} to {transfer.To} ({Money.Format(transfer.Fee)}) is as big as the amount. Typo?",
                transfer.From));

        // A balance check whose correction entry was later deleted no longer holds.
        var ids = store.All.Select(t => t.Id).ToHashSet();
        foreach (var check in store.Checks.Where(c => c.AdjustmentId is { } id && !ids.Contains(id)))
            findings.Add(new(FindingLevel.Warning,
                $"The correction from {check.Account}'s check on {check.CheckedAt:MMM d} was deleted, so that check no longer holds. Check {check.Account} again.",
                check.Account));

        return findings.OrderBy(f => f.Level).ToList();
    }
}
