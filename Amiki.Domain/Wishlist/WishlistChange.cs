namespace Amiki.Domain.Wishlist;

/// <summary>
/// The wishlist money and the item after one operation. Money only ever moves between the two,
/// so both are saved together and the total set aside never drifts.
/// </summary>
public sealed record WishlistChange(WishlistFund Fund, WishItem Item);
