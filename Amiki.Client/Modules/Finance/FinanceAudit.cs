namespace Amiki.Modules.Finance;

public enum FindingLevel { Warning, Info }

public sealed record Finding(FindingLevel Level, string Message, string? Account = null);

/// <summary>
/// The self-audit: looks through the books for things that can't be right, and says what to look
/// at. Only real problems, never reminders: nothing about when you last checked a balance, and no
/// "duplicate?" guesses (buying the same thing twice is normal). Runs on every change, so problems
/// show up while they're still easy to remember.
/// </summary>
public static class FinanceAudit
{
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
        }

        foreach (var transfer in store.Transfers.Where(t => t.Fee > 0 && t.Fee >= t.Amount))
            findings.Add(new(FindingLevel.Warning,
                $"The fee on moving {Money.Format(transfer.Amount)} from {transfer.From} to {transfer.To} ({Money.Format(transfer.Fee)}) is as big as the amount. Typo?",
                transfer.From));

        // A balance check whose correction entry was later deleted no longer holds.
        var ids = store.All.Select(t => t.Id).ToHashSet();
        foreach (var check in store.Checks.Where(c => c.AdjustmentId is { } id && !ids.Contains(id)))
            findings.Add(new(FindingLevel.Warning,
                $"The correction from {check.Account}'s check on {check.CheckedAt:MMM d} was deleted, so {check.Account} no longer matches what you checked.",
                check.Account));

        return findings.OrderBy(f => f.Level).ToList();
    }
}
