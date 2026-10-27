using Amiki.Application.People;
using Amiki.Domain.People;

namespace Amiki.Api.Users;

/// <summary>
/// Who this request is for, filled in by <see cref="UserContextMiddleware"/> at the start of each
/// request. The rest of the app only sees it as <see cref="ICurrentUser"/>.
/// </summary>
public sealed class UserContext : ICurrentUser
{
    /// <summary>Until a request sets it (e.g. at startup), the owner.</summary>
    public Guid SignedInId { get; private set; } = AppUser.OwnerId;

    public bool IsOwner => SignedInId == AppUser.OwnerId;

    /// <summary>Whose Amiki the owner is looking at, if not their own.</summary>
    public Guid? ViewingId { get; private set; }

    public Guid DataUserId => ViewingId ?? SignedInId;

    public bool IsViewingSomeoneElse => ViewingId is not null;

    public string? GeminiKey { get; private set; }

    public void SignIn(AppUser user, string? geminiKey)
    {
        SignedInId = user.Id;
        GeminiKey = geminiKey;
        ViewingId = null;
    }

    /// <summary>The owner looking at someone's Amiki (read-only).</summary>
    public void View(Guid userId) => ViewingId = userId == SignedInId ? null : userId;
}
