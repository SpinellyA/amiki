namespace Amiki.Api.Users;

/// <summary>
/// Who this request is for, filled in by <see cref="UserContextMiddleware"/>. The database only
/// ever shows and saves rows for <see cref="DataUserId"/>, so one person can't see another's data.
///
/// Usually that's the signed-in person. The owner can also "view" someone else's Amiki: then
/// it's that person's data, read-only, and never their diary.
/// </summary>
public sealed class UserContext
{
    /// <summary>The signed-in person. Until a request sets it (e.g. at startup), the owner.</summary>
    public Guid SignedInId { get; private set; } = AppUser.OwnerId;

    public string Email { get; private set; } = "";

    public bool IsOwner => SignedInId == AppUser.OwnerId;

    /// <summary>Whose Amiki the owner is looking at, if not their own.</summary>
    public Guid? ViewingId { get; private set; }

    /// <summary>The person whose data this request reads and writes.</summary>
    public Guid DataUserId => ViewingId ?? SignedInId;

    public bool IsViewingSomeoneElse => ViewingId is not null;

    /// <summary>The signed-in person's own Gemini key (decrypted), if they have one saved.</summary>
    public string? GeminiKey { get; private set; }

    public void SignIn(AppUser user, string? geminiKey)
    {
        SignedInId = user.Id;
        Email = user.Email;
        GeminiKey = geminiKey;
        ViewingId = null;
    }

    /// <summary>The owner looking at someone's Amiki (read-only).</summary>
    public void View(Guid userId) => ViewingId = userId == SignedInId ? null : userId;
}
