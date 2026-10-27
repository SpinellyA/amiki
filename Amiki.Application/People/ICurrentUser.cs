namespace Amiki.Application.People;

/// <summary>
/// Who this request is for. Usually that's the signed-in person; the owner can also view someone
/// else's Amiki, and then it's that person's data, read-only.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The signed-in person.</summary>
    Guid SignedInId { get; }

    bool IsOwner { get; }

    /// <summary>The person whose data this request reads and writes.</summary>
    Guid DataUserId { get; }

    bool IsViewingSomeoneElse { get; }

    /// <summary>The signed-in person's own Gemini key (decrypted), if they saved one.</summary>
    string? GeminiKey { get; }
}
