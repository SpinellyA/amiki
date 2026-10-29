namespace Amiki.Domain.Wishlist;

/// <summary>A price an item used to have, and when it stopped being that ("was ₱4,999").</summary>
public sealed record PricePoint(decimal Price, DateTime ChangedAt);
