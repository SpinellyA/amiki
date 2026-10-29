namespace Amiki.Domain.Wishlist;

/// <summary>The wishlist against your money: what you're saving for, what you want, and what's left if you buy it all.</summary>
public sealed class WishlistSummary(IEnumerable<WishItem> items, decimal balance)
{
    private readonly IReadOnlyList<WishItem> _items = [.. items];

    public decimal Balance { get; } = balance;

    public decimal SavingFor => Total(WishStatus.Saving);
    public decimal Wanted => Total(WishStatus.Want);

    /// <summary>Your balance after buying everything you're saving for.</summary>
    public Affordability AfterSaving => new(SavingFor, Balance);

    /// <summary>Your balance after buying everything still open (saving for and wanted).</summary>
    public Affordability AfterEverything => new(SavingFor + Wanted, Balance);

    public int OpenCount => _items.Count(i => i.IsOpen);

    private decimal Total(WishStatus status) => _items.Where(i => i.Status == status).Sum(i => i.Price);
}
