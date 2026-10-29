namespace Amiki.Domain.Wishlist;

/// <summary>Something you'd like to buy one day, with its price, so you can see what it does to your money.</summary>
public sealed class WishItem : IEntity
{
    public const int MaxName = 200;
    public const int MaxLink = 1000;
    public const int MaxNote = 1000;
    public const int MaxPriceHistory = 50;
    public const decimal MaxPrice = 100_000_000;

    /// <summary>How new a Want item is when buying it gets a gentle "sure?".</summary>
    public static readonly TimeSpan JustAdded = TimeSpan.FromDays(2);

    /// <summary>How long something sits in Want before Amiki asks if you still want it.</summary>
    public static readonly TimeSpan CoolingOff = TimeSpan.FromDays(14);

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    /// <summary>Where to buy it (a shop page), if you saved one.</summary>
    public string? Link { get; set; }
    public string Note { get; set; } = "";
    public WishStatus Status { get; set; } = WishStatus.Want;
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    /// <summary>When it was marked bought; empty otherwise.</summary>
    public DateTime? BoughtAt { get; set; }
    /// <summary>The expense logged when it was bought, if one was.</summary>
    public Guid? TransactionId { get; set; }

    /// <summary>Wishlist money put toward this item (part of your wishlist money, just earmarked).</summary>
    public decimal Saved { get; set; }
    /// <summary>When you'd like to have it, if you picked a date.</summary>
    public DateTime? TargetDate { get; set; }
    /// <summary>Your order among the things you're saving for: lower comes first.</summary>
    public int Rank { get; set; }
    /// <summary>Earlier prices, newest last.</summary>
    public List<PricePoint> PriceHistory { get; set; } = [];

    /// <summary>Still something you might buy (not bought, not skipped).</summary>
    public bool IsOpen => Status is WishStatus.Want or WishStatus.Saving;

    /// <summary>What's still to save.</summary>
    public decimal Remaining => Math.Max(0, Price - Saved);

    /// <summary>How much of the price is saved, 0 to 1.</summary>
    public double Progress => Price <= 0 ? 1 : (double)Math.Clamp(Saved / Price, 0, 1);

    /// <summary>The price before the latest change, if it ever changed.</summary>
    public decimal? PreviousPrice => PriceHistory.Count > 0 ? PriceHistory[^1].Price : null;

    public WishItem Clone()
    {
        var copy = (WishItem)MemberwiseClone();
        copy.PriceHistory = [.. PriceHistory];
        return copy;
    }

    /// <summary>A Want item added moments ago: buying it right away may be an impulse.</summary>
    public bool IsFreshWant(DateTime now) => Status == WishStatus.Want && now - CreatedAt < JustAdded;

    /// <summary>Wanted long enough that it's worth asking whether you still do.</summary>
    public bool HasCooledOff(DateTime now) => Status == WishStatus.Want && now - CreatedAt >= CoolingOff;

    /// <summary>What it takes to make the target date; null without one or when it's all saved.</summary>
    public SavingPace? PaceOn(DateTime today) =>
        IsOpen && TargetDate is { } target && Remaining > 0 ? new SavingPace(Remaining, today, target) : null;

    /// <summary>
    /// A copy moved to <paramref name="status"/>. Becoming Bought records when; moving away from
    /// Bought forgets it (and the link to its expense, which stays in Money as it is). Money put
    /// toward it is handled by <see cref="WishlistFund"/>, which moves it back when needed.
    /// </summary>
    public WishItem WithStatus(WishStatus status, DateTime now)
    {
        var moved = Clone();
        moved.Status = status;
        if (status == WishStatus.Bought) moved.BoughtAt ??= now;
        else (moved.BoughtAt, moved.TransactionId) = (null, null);
        return moved;
    }

    /// <summary>A copy with a new price; the old one goes into the history (if it really changed).</summary>
    public WishItem Repriced(decimal price, DateTime now)
    {
        var repriced = Clone();
        if (price == Price) return repriced;
        repriced.PriceHistory = [.. PriceHistory.TakeLast(MaxPriceHistory - 1), new PricePoint(Price, now)];
        repriced.Price = price;
        return repriced;
    }

    /// <summary>
    /// What buying it would do to your wishlist money: what's put toward it plus the
    /// <paramref name="pool"/> of wishlist money not put toward anything yet.
    /// </summary>
    public Affordability Against(decimal pool) => new(Price, Saved + pool);

    public string? Validate() =>
        string.IsNullOrWhiteSpace(Name) ? "Give it a name."
        : Name.Length > MaxName ? $"Name is too long ({MaxName} characters max)."
        : Price < 0 ? "A price can't be negative."
        : Price > MaxPrice ? "That price looks wrong."
        : Saved < 0 ? "Saved money can't be negative."
        : Saved > Price && IsOpen ? "More is put toward it than it costs."
        : Link?.Length > MaxLink ? "That link is too long."
        : Link is { Length: > 0 } link && !IsWebLink(link) ? "A link has to start with http:// or https://."
        : Note.Length > MaxNote ? $"Note is too long ({MaxNote} characters max)."
        : PriceHistory.Count > MaxPriceHistory ? "Too much price history."
        : TransactionId is not null && Status != WishStatus.Bought ? "Only a bought item can point at its expense."
        : null;

    private static bool IsWebLink(string link) =>
        Uri.TryCreate(link, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
