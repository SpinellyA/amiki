namespace Amiki.Domain.Wishlist;

/// <summary>Where a wishlist item stands. Fixed set, like the Ideas board.</summary>
public enum WishStatus
{
    /// <summary>Something you'd like, someday.</summary>
    Want,
    /// <summary>You've decided to get it and are putting money aside.</summary>
    Saving,
    /// <summary>Done: you got it (usually with a logged expense).</summary>
    Bought,
    /// <summary>You changed your mind. Kept so you can see what you passed on.</summary>
    Skipped,
}
