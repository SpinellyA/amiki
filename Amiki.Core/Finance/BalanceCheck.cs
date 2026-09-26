using Amiki.Core;

namespace Amiki.Modules.Finance;

/// <summary>
/// You compared Amiki's balance for an account with the real one (bank app, wallet). Kept as
/// part of the audit trail; if they differed, <see cref="AdjustmentId"/> points at the labeled
/// correction that brought Amiki in line. Recorded checks can't be edited afterwards.
/// </summary>
public sealed class BalanceCheck : IEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Account { get; set; } = "";
    public DateTime CheckedAt { get; init; } = DateTime.Now;
    /// <summary>What Amiki said at the time.</summary>
    public decimal Expected { get; set; }
    /// <summary>What the real account showed.</summary>
    public decimal Actual { get; set; }
    public Guid? AdjustmentId { get; set; }

    public decimal Difference => Actual - Expected;
    public bool Matched => Difference == 0;

    public string? Validate() =>
        string.IsNullOrWhiteSpace(Account) ? "Pick an account."
        : Math.Abs(Actual) > 1_000_000_000 ? "That balance looks wrong."
        : Difference != 0 && AdjustmentId is null ? "A mismatch needs its correction entry."
        : null;

    public string Describe() =>
        $"Checked {Account}: Amiki said {Signed(Expected)}, real balance {Signed(Actual)}"
        + (Matched ? " · matched" : $" · corrected by {Money.Signed(Difference)}");

    private static string Signed(decimal v) => (v < 0 ? "−" : "") + Money.Format(v);
}
