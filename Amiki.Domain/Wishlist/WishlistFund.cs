namespace Amiki.Domain.Wishlist;

/// <summary>
/// The part of your balance you've set aside for wishlist things. Only a label on money you
/// already have: your accounts and total balance don't change. Wishlist items are weighed
/// against this, not your whole balance, and buying one spends from it.
/// </summary>
public sealed class WishlistFund : IEntity
{
    /// <summary>There's only ever one per person; this is its id.</summary>
    public static readonly Guid SingleId = Guid.Parse("a15f0000-0000-4000-8000-000000000001");

    public Guid Id { get; init; } = SingleId;
    public decimal Amount { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public WishlistFund Clone() => (WishlistFund)MemberwiseClone();

    /// <summary>A copy set to <paramref name="amount"/>.</summary>
    public WishlistFund SetTo(decimal amount, DateTime now)
    {
        var set = Clone();
        set.Amount = Math.Round(amount, 2);
        set.UpdatedAt = now;
        return set;
    }

    /// <summary>A copy with <paramref name="price"/> spent from it (never below zero).</summary>
    public WishlistFund Spend(decimal price, DateTime now) => SetTo(Math.Max(0, Amount - price), now);

    public string? Validate() =>
        Id != SingleId ? "There's only one wishlist fund."
        : Amount < 0 ? "Wishlist money can't be negative."
        : Amount > WishItem.MaxPrice ? "That amount looks wrong."
        : null;
}
