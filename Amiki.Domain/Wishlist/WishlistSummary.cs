namespace Amiki.Domain.Wishlist;

/// <summary>
/// The wishlist against the money set aside for it (<see cref="Fund"/>), with your full
/// <see cref="Balance"/> alongside for reference.
/// </summary>
public sealed class WishlistSummary(IEnumerable<WishItem> items, decimal fund, decimal balance)
{
    private readonly IReadOnlyList<WishItem> _items = [.. items];

    /// <summary>Wishlist money: what the items are weighed against.</summary>
    public decimal Fund { get; } = fund;

    /// <summary>Your whole balance, shown but not spent from.</summary>
    public decimal Balance { get; } = balance;

    /// <summary>When more is set aside than you actually have (e.g. after spending elsewhere).</summary>
    public bool FundExceedsBalance => Fund > Balance;

    public decimal SavingFor => Total(WishStatus.Saving);
    public decimal Wanted => Total(WishStatus.Want);

    /// <summary>Wishlist money after buying everything you're saving for.</summary>
    public Affordability AfterSaving => new(SavingFor, Fund);

    /// <summary>Wishlist money after buying everything still open (saving for and wanted).</summary>
    public Affordability AfterEverything => new(SavingFor + Wanted, Fund);

    public int OpenCount => _items.Count(i => i.IsOpen);

    private decimal Total(WishStatus status) => _items.Where(i => i.Status == status).Sum(i => i.Price);
}
