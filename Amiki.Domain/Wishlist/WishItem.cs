namespace Amiki.Domain.Wishlist;

/// <summary>Something you'd like to buy one day, with its price, so you can see what it does to your money.</summary>
public sealed class WishItem : IEntity
{
    public const int MaxName = 200;
    public const int MaxLink = 1000;
    public const int MaxNote = 1000;
    public const decimal MaxPrice = 100_000_000;

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

    /// <summary>Still something you might buy (not bought, not skipped).</summary>
    public bool IsOpen => Status is WishStatus.Want or WishStatus.Saving;

    public WishItem Clone() => (WishItem)MemberwiseClone();

    /// <summary>
    /// A copy moved to <paramref name="status"/>. Becoming Bought records when; moving away from
    /// Bought forgets it (and the link to its expense, which stays in Money as it is).
    /// </summary>
    public WishItem WithStatus(WishStatus status, DateTime now)
    {
        var moved = Clone();
        moved.Status = status;
        if (status == WishStatus.Bought) moved.BoughtAt ??= now;
        else (moved.BoughtAt, moved.TransactionId) = (null, null);
        return moved;
    }

    /// <summary>A copy marked bought, linked to the expense that paid for it.</summary>
    public WishItem BoughtWith(Guid transactionId, DateTime now)
    {
        var bought = WithStatus(WishStatus.Bought, now);
        bought.TransactionId = transactionId;
        return bought;
    }

    /// <summary>What buying it would do to <paramref name="money"/> (usually your wishlist money).</summary>
    public Affordability Against(decimal money) => new(Price, money);

    public string? Validate() =>
        string.IsNullOrWhiteSpace(Name) ? "Give it a name."
        : Name.Length > MaxName ? $"Name is too long ({MaxName} characters max)."
        : Price < 0 ? "A price can't be negative."
        : Price > MaxPrice ? "That price looks wrong."
        : Link?.Length > MaxLink ? "That link is too long."
        : Link is { Length: > 0 } link && !IsWebLink(link) ? "A link has to start with http:// or https://."
        : Note.Length > MaxNote ? $"Note is too long ({MaxNote} characters max)."
        : TransactionId is not null && Status != WishStatus.Bought ? "Only a bought item can point at its expense."
        : null;

    private static bool IsWebLink(string link) =>
        Uri.TryCreate(link, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
